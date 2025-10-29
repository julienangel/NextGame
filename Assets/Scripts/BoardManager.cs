using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [HideInInspector]
    public GameObject[,] board;
    private int numberOfPieces = -1;
    private bool finishedLevel = false;
    private Vector2Int _cursorPosition;

    public static BoardManager Create()
    {
        GameObject gameObject = new GameObject
        {
            name = "BoardManager"
        };
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

    public void SetCursorPosition(Vector2Int pos)
    {
        _cursorPosition = pos;
    }

    public void AddOnBoard(GameObject number, Vector2 pos)
    {
        board[(int)pos.x, (int)pos.y] = number;
        numberOfPieces++;
    }

    public bool HasPieceAt(Vector2 pos)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;

        if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1))
            return false;

        return board[x, y] != null;
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
            _cursorPosition = new Vector2Int(xDir, yDir);
            return true;
        }

        else if (tagAtual == "Number" && tagDir == "Finish" && CanFinish())
        {
            atualPiece.UpdateValue();
            finishedLevel = true;
            _cursorPosition = new Vector2Int(xDir, yDir);
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
    
    public Level GetCurrentLevelState()
    {
        int size = board.GetLength(0);
        Level level = new Level(size, size * size);
        
        List<PieceInfo> pieces = new List<PieceInfo>();
        Position finishPos = new Position(0, 0);
        
        // Percorrer todo o board
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject obj = board[x, y];
                
                if (obj == null)
                    continue;
                
                // Verificar se é o Finish
                if (obj.tag == "Finish")
                {
                    finishPos = new Position(x, y);
                    continue;
                }
                
                // Verificar se é uma peça numerada
                if (obj.tag == "Number")
                {
                    Piece piece = obj.GetComponent<Piece>();
                    if (piece != null)
                    {
                        int value = piece.number; // Assumindo que tens este método
                        pieces.Add(new PieceInfo(new Position(x, y), value));
                    }
                }
            }
        }
        
        // Preencher level
        level.MousePos = new Position(_cursorPosition.x, _cursorPosition.y);
        level.FinishPos = finishPos;
        level.BoardSize = size;
        level.PiecesInfo = pieces.ToArray();
        level.DirectionalPiecesInfo = Array.Empty<DirectionalPieceInfo>(); // Vazio por enquanto
        
        return level;
    }
}
