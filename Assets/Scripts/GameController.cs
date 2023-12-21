using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Change Board")]
    [Range(4, 8)]
    public int numberRows = 4;
    [Range(4, 8)]
    public int numberColumns = 4;

    [SerializeField] private int numPiecesToWin = 4;
    [SerializeField] private bool allowDiagonalConnection = true;

    [Header("AI Interactions")]
    [Range(1, 8)]
    [SerializeField] private int parallelProcesses = 2;
    [Range(7, 5000)]
    [Tooltip("20 is easy, 40 is medium, 80 is hard, above 100 is challenging (I haven't won from 500).")]
    [SerializeField] private int MCTS_Iterations = 100; 
    [Tooltip("Shows column number next to its probability.")]
    [SerializeField] private bool logColumn = false;

    [Header("Visuals")]
    [Range(1, 8)]
    [SerializeField] private float dropTime = 4f;

    [Header("Game Pieces")]
    [SerializeField] private Piece pieceRed;
    [SerializeField] private Piece pieceField;
    [SerializeField] private Piece pieceYellow;

    //Game reference
    private GameObject gameObjectField; //this is the field object that gets created.
    private Piece pieceTurn;  //represent player/AI 

    private Field field;

    private bool isDropping = false;
    private bool gameOver = false;

    public static event Action<GameOverState> OnGameOver;

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (gameOver) 
            return;

        if (pieceTurn == null)
        {
            pieceTurn = SpawnPiece();
            return;
        }

        if (field.IsPlayersTurn)
        {
            UpdatePlayerPiecePosition();

            if (Input.GetKeyUp(KeyCode.Mouse0) && !isDropping)
                StartCoroutine(DropPiece(pieceTurn));

            return;
        }

        if (!isDropping) 
            StartCoroutine(DropPiece(pieceTurn));
    }

    private void InitializeGame()
    {
        int max = Mathf.Max(numberRows, numberColumns);
        if (numPiecesToWin > max) numPiecesToWin = max;

        CreateField();
    }

    private void CreateField()
    {
        gameObjectField = new GameObject("Field");
        field = new Field(numberRows, numberColumns, numPiecesToWin, allowDiagonalConnection);

        //Instantiate empty pieces on field.
        for (int x = 0; x < numberColumns; x++)
        for (int y = 0; y < numberRows; y++)
        {
            Piece piece = Instantiate(pieceField, new Vector3(x, y * -1, -1), Quaternion.identity, gameObjectField.transform);
        }

        gameOver = false;
    }

    private Piece SpawnPiece()
    {
        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!field.IsPlayersTurn) 
            spawnPos.x = CalculateRow();

        Piece piece = Instantiate(
            field.IsPlayersTurn ? pieceYellow : pieceRed,
            new Vector3(
                Mathf.Clamp(spawnPos.x, 0, numberColumns - 1), 
                gameObjectField.transform.position.y + 1,
                0),
            Quaternion.identity);

        return piece;
    }

    private float CalculateRow()
    {
        int column;

        if (field.PiecesNumber != 0)
        {
            ManualResetEvent[] doneEvents = new ManualResetEvent[parallelProcesses];
            MonteCarloSearchTree[] trees  = new MonteCarloSearchTree[parallelProcesses];

            for (int i = 0; i < parallelProcesses; i++)
            {
                doneEvents[i] = new ManualResetEvent(false);
                trees[i] = new MonteCarloSearchTree(field, doneEvents[i], MCTS_Iterations);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ExpandTree), trees[i]);
            }

            WaitHandle.WaitAll(doneEvents);

            Node rootNode = new Node();

            for (int i = 0; i < parallelProcesses; i++)
            {
                List<KeyValuePair<Node, int>> sortedChildren = trees[i].rootNode.children.ToList();
                sortedChildren.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

                for (int c = 0; c < sortedChildren.Count; c++)
                {
                    KeyValuePair<Node, int> child = sortedChildren[c];

                    if (!rootNode.children.ContainsValue(child.Value))
                    {
                        Node rootChild = new();
                        rootChild.wins = child.Key.wins;
                        rootChild.plays = child.Key.plays;
                        rootNode.children.Add(rootChild, child.Value);
                    }
                    else
                    {
                        Node rootChild = rootNode.children.First(p => p.Value == child.Value).Key;
                        rootChild.wins += child.Key.wins;
                        rootChild.plays += child.Key.plays;
                    }
                }
            }

            column = rootNode.MostSelectedMove();
        }
        else
        {
            column = field.GetRandomMove();
        }

        return column;
    }

    private static void ExpandTree(object _tree)
    {
        MonteCarloSearchTree tree = (MonteCarloSearchTree)_tree;
        tree.simulatedStateField = tree.currentStateField.Clone();
        tree.rootNode = new Node(tree.simulatedStateField.IsPlayersTurn);

        Node selectedNode;
        Node expandedNode;

        System.Random random = new(Guid.NewGuid().GetHashCode());

        for (int i = 0; i < tree.iterationCount; i++)
        {
            tree.simulatedStateField = tree.currentStateField.Clone();

            selectedNode = tree.rootNode.SelectNodeToExpand(tree.rootNode.plays, tree.simulatedStateField);
            expandedNode = selectedNode.Expand(tree.simulatedStateField, random);
            expandedNode.BackPropagate(expandedNode.Simulate(tree.simulatedStateField));
        }

        tree.doneEvent.Set();
    }

    private void UpdatePlayerPiecePosition()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pieceTurn.transform.position = new Vector3(
            Mathf.Clamp(pos.x, 0, numberColumns - 1),
            gameObjectField.transform.position.y + 1,
            0
        );
    }

    private IEnumerator DropPiece(Piece _piece)
    {
        isDropping = true;

        Vector3 startPosition = _piece.transform.position;

        int x = Mathf.RoundToInt(startPosition.x);
        startPosition = new Vector3(x, startPosition.y, startPosition.z);

        int y = field.DropInColumn(x);

        if (y != -1)
        {
            Vector3 endPosition = new (x, y * -1, startPosition.z);

            Piece piece = Instantiate(_piece);
            pieceTurn.SetRendererActive(false);

            float distance = Vector3.Distance(startPosition, endPosition);

            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime * dropTime * ((numberRows - distance) + 1);
                piece.transform.position = Vector3.Lerp(startPosition, endPosition, time);

                yield return null;
            }

            piece.transform.parent = gameObjectField.transform;
            Destroy(pieceTurn);

            CheckGameState();

            field.SwitchPlayer();
        }

        isDropping = false;
    }

    private void CheckGameState()
    {
        gameOver = field.CheckForWinner();

        if (gameOver)
        {
            OnGameOver?.Invoke(field.IsPlayersTurn ? GameOverState.win : GameOverState.lose);

            return;
        }
        
        if (!field.ContainsEmptyCell())
        {
            gameOver = true;
            OnGameOver?.Invoke(GameOverState.draw);
        }
    }
}
