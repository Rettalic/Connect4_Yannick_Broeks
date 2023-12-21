using System.Collections.Generic;
using UnityEngine;

public class Field
{
    private int numberRows;
    private int numberColumns;
    private int numberPiecesToWin;
    private bool allowDiagonalConnection = true;

    private PieceType[,] field;

    private bool isPlayersTurn;
    public bool IsPlayersTurn => isPlayersTurn; 

    private int piecesNumber = 0;
    public int PiecesNumber => piecesNumber;

    // Memorize last move
    private int dropColumn;
    private int dropRow;

    public Field(int _numRows, int _numColumns, int _numPiecesToWin, bool _allowDiagonally)
    {
        numberRows = _numRows;
        numberColumns = _numColumns;
        numberPiecesToWin = _numPiecesToWin;
        allowDiagonalConnection = _allowDiagonally;

        isPlayersTurn = Random.value > 0.5f;

        field = new PieceType[_numColumns, _numRows];
        for (int x = 0; x < _numColumns; x++)
        {
            for (int y = 0; y < _numRows; y++)
            {
                field[x, y] = PieceType.Empty;
            }
        }

        dropColumn = 0;
        dropRow = 0;
    }

    public Field(int _numRows, int _numColumns, int _numPiecesToWin, bool _allowDiagonally, bool _isPlayersTurn, int _piecesNumber, PieceType[,] _field)
    {
        numberRows = _numRows;
        numberColumns = _numColumns;
        numberPiecesToWin = _numPiecesToWin;
        allowDiagonalConnection = _allowDiagonally;
        isPlayersTurn = _isPlayersTurn;
        piecesNumber = _piecesNumber;

        field = new PieceType[_numColumns, _numRows];
        for (int x = 0; x < _numColumns; x++)
        {
            for (int y = 0; y < _numRows; y++)
            {
                field[x, y] = _field[x, y];
            }
        }
    }

    // Returns the list of cells where the player can add a piece
    public Dictionary<int, int> GetPossibleCells()
    {
        Dictionary<int, int> possibleCells = new Dictionary<int, int>();
        for (int x = 0; x < numberColumns; x++)
        {
            for (int y = numberRows - 1; y >= 0; y--)
            {
                if (field[x, y] == PieceType.Empty)
                {
                    possibleCells.Add(x, y);
                    break;
                }
            }
        }
        return possibleCells;
    }

    // Returns the list of columns where the player can drop a piece
    public List<int> GetPossibleDrops()
    {
        List<int> possibleDrops = new List<int>();
        for (int x = 0; x < numberColumns; x++)
        {
            for (int y = numberRows - 1; y >= 0; y--)
            {
                if (field[x, y] == PieceType.Empty)
                {
                    possibleDrops.Add(x);
                    break;
                }
            }
        }
        return possibleDrops;
    }

    // Returns a random move from all possible moves
    public int GetRandomMove()
    {
        List<int> moves = GetPossibleDrops();

        if (moves.Count > 0)
        {
            System.Random r = new System.Random();
            return moves[r.Next(0, moves.Count)];
        }
        return -1;
    }

    // Drops a piece into column i, returns the row where it falls
    public int DropInColumn(int col)
    {
        if (IsColumnFull(col))
            return -1;

        for (int i = numberRows - 1; i >= 0; i--)
        {
            if (field[col, i] == PieceType.Empty)
            {
                field[col, i] = isPlayersTurn ? PieceType.Yellow : PieceType.Red;
                piecesNumber += 1;
                dropColumn = col;
                dropRow = i;
                return i;
            }
        }
        return -1;
    }
    private bool IsColumnFull(int col)
    {
        for (int i = 0; i < numberRows; i++)
        {
            if (field[col, i] == PieceType.Empty)
            {
                return false; 
            }
        }

        return true; 
    }

    public void SwitchPlayer()
    {
        isPlayersTurn = !isPlayersTurn;
    }

    public bool CheckForWinner()
    {
        for (int x = 0; x < numberColumns; x++)
        {
            for (int y = 0; y < numberRows; y++)
            {
                int layermask = isPlayersTurn ? (1 << 8) : (1 << 9);

                // If it's the Players turn ignore red as Starting piece and vise versa.
                if (field[x, y] != (isPlayersTurn ? PieceType.Yellow : PieceType.Red))
                {
                    continue;
                }

                RaycastHit[] hitsHorz = Physics.RaycastAll(
                                          new Vector3(x, y * -1, 0),
                                          Vector3.right,
                                          numberPiecesToWin - 1,
                                          layermask);

                // return true (won) if enough hits
                if (hitsHorz.Length == numberPiecesToWin - 1)
                {
                    return true;
                }

                // shoot a ray up to test vertically
                RaycastHit[] hitsVert = Physics.RaycastAll(
                                          new Vector3(x, y * -1, 0),
                                          Vector3.up,
                                          numberPiecesToWin - 1,
                                          layermask);

                if (hitsVert.Length == numberPiecesToWin - 1)
                {
                    return true;
                }

                // test diagonally
                if (allowDiagonalConnection)
                {
                    // calculate the length of the ray to shoot diagonally
                    float length = Vector2.Distance(new Vector2(0, 0), new Vector2(numberPiecesToWin - 1, numberPiecesToWin - 1));

                    RaycastHit[] hitsDiaLeft = Physics.RaycastAll(
                                                 new Vector3(x, y * -1, 0),
                                                 new Vector3(-1, 1),
                                                 length,
                                                 layermask);

                    if (hitsDiaLeft.Length == numberPiecesToWin - 1)
                    {
                        return true;
                    }

                    RaycastHit[] hitsDiaRight = Physics.RaycastAll(
                                                  new Vector3(x, y * -1, 0),
                                                  new Vector3(1, 1),
                                                  length,
                                                  layermask);

                    if (hitsDiaRight.Length == numberPiecesToWin - 1)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool CheckForVictory()
    {
        PieceType colour = field[dropColumn, dropRow];
        if (colour == PieceType.Empty)
            return false;

        bool bottomDirection = true;
        int currentAlignment = 1; //count current Piece

        //check vertical alignment
        for (int i = 1; i <= numberPiecesToWin; i++)
        {
            if (bottomDirection && dropRow + i < numberRows)
            {
                if (field[dropColumn, dropRow + i] == colour)
                    currentAlignment++;
                else
                    bottomDirection = false;
            }

            if (currentAlignment >= numberPiecesToWin)
                return true;
        }

        bool rightDirection = true;
        bool leftDirection = true;
        currentAlignment = 1;

        //check horizontal alignment
        for (int i = 1; i <= numberPiecesToWin; i++)
        {
            if (rightDirection && dropColumn + i < numberColumns)
            {
                if (field[dropColumn + i, dropRow] == colour)
                    currentAlignment++;
                else
                    rightDirection = false;
            }

            if (leftDirection && dropColumn - i >= 0)
            {
                if (field[dropColumn - i, dropRow] == colour)
                    currentAlignment++;
                else
                    leftDirection = false;
            }

            if (currentAlignment >= numberPiecesToWin)
                return true;
        }

        //check diagonal alignment
        if (allowDiagonalConnection)
        {
            bool upRightDirection = true;
            bool bottomLeftDirection = true;
            currentAlignment = 1;

            for (int i = 1; i <= numberPiecesToWin; i++)
            {
                if (upRightDirection && dropColumn + i < numberColumns && dropRow + i < numberRows)
                {
                    if (field[dropColumn + i, dropRow + i] == colour)
                        currentAlignment++;
                    else
                        upRightDirection = false;
                }

                if (bottomLeftDirection && dropColumn - i >= 0 && dropRow - i >= 0)
                {
                    if (field[dropColumn - i, dropRow - i] == colour)
                        currentAlignment++;
                    else
                        bottomLeftDirection = false;
                }

                if (currentAlignment >= numberPiecesToWin)
                    return true;
            }

            bool upLeftDirection = true;
            bool bottomRightDirection = true;
            currentAlignment = 1;

            for (int i = 1; i <= numberPiecesToWin; i++)
            {
                if (upLeftDirection && dropColumn + i < numberColumns && dropRow - i >= 0)
                {
                    if (field[dropColumn + i, dropRow - i] == colour)
                        currentAlignment++;
                    else
                        upLeftDirection = false;
                }

                if (bottomRightDirection && dropColumn - i >= 0 && dropRow + i < numberRows)
                {
                    if (field[dropColumn - i, dropRow + i] == colour)
                        currentAlignment++;
                    else
                        bottomRightDirection = false;
                }

                if (currentAlignment >= numberPiecesToWin)
                    return true;
            }
        }

        return false;
    }

    public bool ContainsEmptyCell()
    {
        return (piecesNumber < numberRows * numberColumns);
    }

    // Executes a deep copy of the game state
    public Field Clone()
    {
        return new Field(numberRows, numberColumns, numberPiecesToWin, allowDiagonalConnection, isPlayersTurn, piecesNumber, field);
    }
}
