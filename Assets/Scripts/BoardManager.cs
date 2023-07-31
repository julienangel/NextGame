using Unity.Collections;
using UnityEngine;
using Utils;
using Zenject;

public class BoardManager
{
    [Inject] [ReadOnly] private ObjectPooler _objectPooler;
    
    private Piece[,] _board;

    private int _numberOfPieces = 0;

    public void LoadLevel()
    {
        _board = new Piece[8, 8];

        var newLevel = LevelGenerator.GenerateLevel(8);
        foreach (var piecesInfo in newLevel.piecesInfoList)
        {
            _board[(int)piecesInfo.position.x, (int)piecesInfo.position.y] =
                _objectPooler.SpawnFromPool(piecesInfo.pieceType.ToString(), piecesInfo.position, Quaternion.identity).GetComponent<Piece>();
            _numberOfPieces += piecesInfo.number;
        }
    }

    public bool AvailableToMove(Vector2 atualPosition, Vector2 dir)
    {
        int x = (int)atualPosition.x;
        int y = (int)atualPosition.y;

        int xDir = (int)(dir.x + x);
        int yDir = (int)(dir.y + y);

        if (xDir >= _board.GetLength(0) || xDir < 0 || yDir >= _board.GetLength(1) || yDir < 0 || _board[xDir, yDir] == null)
            return false;

        string tagAtual = _board[x, y].tag;
        string tagDir = _board[xDir, yDir].tag;

        Piece actualPiece = _board[x, y];

        switch (tagAtual)
        {
            case "Number" when tagDir == "Number":
                actualPiece.OnMove();
                return true;
            case "Number" when tagDir == "Finish" && _numberOfPieces <= 1:
                actualPiece.OnMove();
                return true;
            default:
                return false;
        }
    }
}
