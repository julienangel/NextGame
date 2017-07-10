using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [HideInInspector]
    public GameObject[,] board;
    
    private int numberOfPieces = -1;

    private Cursor cursor;

    private bool finishedLevel = false;

    public static BoardManager Create()
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "BoardManager";
        BoardManager board = gameObject.AddComponent<BoardManager>();
        return board;
    }

    public void NewBoard(int size)
    {
        board = new GameObject[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                board[i, j] = null;
            }
        }
    }

    public void AddOnBoard(GameObject number, Vector2 pos)
    {
        board[(int)pos.x, (int)pos.y] = number;
        numberOfPieces++;
    }

    public bool AvailableToMove(Vector2 atualPosition, Vector2 dir)
    {
        int x = (int)atualPosition.x;
        int y = (int)atualPosition.y;

        int xDir = (int)(dir.x + x);
        int yDir = (int)(dir.y + y);

        if (xDir >= board.GetLength(0) || xDir < 0 || yDir >= board.GetLength(1) || yDir < 0 || board[xDir, yDir] == null)
            return false;

        string tagAtual = board[x, y].tag;
        string tagDir = board[xDir, yDir].tag;

        Piece atualPiece = board[x, y].GetComponent<Piece>();

        if (tagAtual == "Number" && tagDir == "Number")
        {
            atualPiece.UpdateValue();
            finishedLevel = false;
            return true;
        }

        else if (tagAtual == "Number" && tagDir == "Finish" && CanFinish())
        {
            atualPiece.UpdateValue();
            finishedLevel = true;
            return true;
        }
            
        return false;
    }

    public bool CanFinish()
    {
        if (numberOfPieces > 1)
            return false;
        return true;
    }

    public bool FinishedLevel()
    {
        return finishedLevel;
    }

    public void ResetNumberCount()
    {
        numberOfPieces = -1;
    }

    public void DecrementNumberCount()
    {
        numberOfPieces--;
    }
}
