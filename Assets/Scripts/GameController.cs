using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("Change Board")]
    [Range(4, 8)]
    public int numberRows = 4;
    [Range(4, 8)]
    public int numberColumns = 4;

    [SerializeField] private int numPiecesToWin = 4;
    [SerializeField] private bool allowDiagonalConnection = true;

    [Header("Helper Scripts")]
    [SerializeField] private UpdateUI UI; 

    [Header("AI Interactions")]
    [Range(1, 8)] public int parallelProcesses = 2;
    [Range(7, 5000)] public int MCTS_Iterations = 100; //20 is easy, 40 is medium, 80 is hard, above 100 is challenging (I haven't won from 1000).
    [Tooltip("Shows column number next to its probability.")]
    [SerializeField] private bool log_column = false;

    [Header("Visuals")]
    [Range(1, 8)]
    [SerializeField] private float dropTime = 4f;

    [Header("GameObjects")]
    [SerializeField] private GameObject pieceRed;
    [SerializeField] private GameObject pieceField;
    [SerializeField] private GameObject pieceBlue;

    //Game reference
    private GameObject gameObjectField; //this is the field object that gets created.
    private GameObject gameObjectTurn;  //represent player/AI 

    private Field field;

    private bool isDropping = false;
    private bool gameOver = false;
    private bool isCheckingForWinner = false;

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (isCheckingForWinner) 
            return;

        if (gameOver)
        {
            UI.winningText.SetActive(true);
            UI.buttonPlayAgain.SetActive(true);
            return;
        }

        if (field.IsPlayersTurn)
        {
            if (gameObjectTurn == null)
            {
                gameObjectTurn = SpawnPiece();
            }
            else
            {
                UpdatePiecePosition();

                if (Input.GetKeyUp(KeyCode.Mouse0) && !isDropping)
                    StartCoroutine(DropPiece(gameObjectTurn));
            }
        }
        else
        {
            if (gameObjectTurn == null)
                gameObjectTurn = SpawnPiece();
            else if (!isDropping) 
                StartCoroutine(DropPiece(gameObjectTurn));
        }
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

        InstantiateCells();

        gameOver = false;
    }

    private void InstantiateCells()
    {
        for (int x = 0; x < numberColumns; x++)
        for (int y = 0; y < numberRows; y++)
        {
            GameObject cell = Instantiate(pieceField, new Vector3(x, y * -1, -1), Quaternion.identity);
            cell.transform.parent = gameObjectField.transform;
        }
    }

    private GameObject SpawnPiece()
    {
        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!field.IsPlayersTurn) 
            spawnPos.x = CalculateRow();

        GameObject piece = Instantiate(
            field.IsPlayersTurn ? pieceBlue : pieceRed,
            new Vector3(Mathf.Clamp(spawnPos.x, 0, numberColumns - 1), gameObjectField.transform.position.y + 1, 0),
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
            string log = "";

            for (int i = 0; i < parallelProcesses; i++)
            {
                log += "( ";
                var sortedChildren = trees[i].rootNode.children.ToList();
                sortedChildren.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

                for (int c = 0; c < sortedChildren.Count; c++)
                {
                    System.Collections.Generic.KeyValuePair<Node, int> child = sortedChildren[c];
                    if (log_column)
                        log += child.Value + ": ";
                    log += (int)(((double)child.Key.wins / (double)child.Key.plays) * 100) + "% | ";

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

                log = log.Remove(log.Length - 3, 3);
                log += " )\n";
            }

#if UNITY_EDITOR
            string log2 = "( ";
            foreach (var child in rootNode.children)
            {
                if (log_column) log2 += child.Value + ": ";
                log2 += (int)(((double)child.Key.wins / (double)child.Key.plays) * 100) + "% | ";
            }
            log2 = log2.Remove(log2.Length - 3, 3);
            log2 += " )\n";
            log2 += "*********************************************\n";
            Debug.Log(log);
            Debug.Log(log2);
#endif
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
        System.Random random = new(System.Guid.NewGuid().GetHashCode());

        for (int i = 0; i < tree.iterationCount; i++)
        {
            tree.simulatedStateField = tree.currentStateField.Clone();

            selectedNode = tree.rootNode.SelectNodeToExpand(tree.rootNode.plays, tree.simulatedStateField);
            expandedNode = selectedNode.Expand(tree.simulatedStateField, random);
            expandedNode.BackPropagate(expandedNode.Simulate(tree.simulatedStateField));
        }
        tree.doneEvent.Set();
    }

    private void UpdatePiecePosition()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gameObjectTurn.transform.position = new Vector3(
            Mathf.Clamp(pos.x, 0, numberColumns - 1),
            gameObjectField.transform.position.y + 1, 0
        );
    }

    private IEnumerator DropPiece(GameObject gObject)
    {
        isDropping = true;

        Vector3 startPosition = gObject.transform.position;

        int x = Mathf.RoundToInt(startPosition.x);
        startPosition = new Vector3(x, startPosition.y, startPosition.z);

        int y = field.DropInColumn(x);

        if (y != -1)
        {
            Vector3 endPosition = new (x, y * -1, startPosition.z);

            GameObject piece = Instantiate(gObject) as GameObject;
            gameObjectTurn.GetComponent<Renderer>().enabled = false;

            float distance = Vector3.Distance(startPosition, endPosition);

            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime * dropTime * ((numberRows - distance) + 1);
                piece.transform.position = Vector3.Lerp(startPosition, endPosition, time);
                yield return null;
            }

            piece.transform.parent = gameObjectField.transform;
            Destroy(gameObjectTurn);

            Won();

            while (isCheckingForWinner)
                yield return null;

            field.SwitchPlayer();
        }
        isDropping = false;
        yield return 0;
    }

    private void Won()
    {
        isCheckingForWinner = true;

        gameOver = field.CheckForWinner();

        if (gameOver)
        {
            UI.winningText.GetComponent<TextMeshProUGUI>().text = field.IsPlayersTurn ? UI.playerWonText : UI.playerLoseText;
        }
        else
        {
            if (!field.ContainsEmptyCell())
            {
                gameOver = true;
                UI.winningText.GetComponent<TextMeshProUGUI>().text = UI.drawText;
            }
        }

        isCheckingForWinner = false;
    }
}
