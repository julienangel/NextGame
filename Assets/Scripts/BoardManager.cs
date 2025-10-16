using Gameplay;
using UnityEngine;
using Cursor = Gameplay.Cursor;

public class BoardManager
{
    public Level Level { get; private set; }

    private Piece[,] _pieceBoard;
    private Piece _finishPiece;
    private Cursor _cursor;
    private bool _finishedLevel = false;

    private readonly Piece _piecePrefab;
    private readonly Cursor _cursorPrefab;

    public BoardManager(Piece piecePrefab, Cursor cursorPrefab)
    {
        _piecePrefab = piecePrefab;
        _cursorPrefab = cursorPrefab;

        using var level = LevelGenerator.GenerateLevelWithBenchmark(
            minPieces: 0, // Ignorado
            maxPieces: 0, // Ignorado
            numMax: 5,
            maxMoves: 100,
            size: 8,
            mode: GenerationMode.Nightmare
        );
        LevelPrinter.PrintLevelWithSolution(level);

        // using var level = LevelGenerator.GenerateLevel(
        //     minPieces: 10,
        //     maxPieces: 15,
        //     numMax: 6,
        //     maxMoves: 50,
        //     size: 6,
        //     Difficulty.Easy,
        //     GenerationMode.Challenge,
        //     true,
        //     true
        // );
        // LevelPrinter.PrintLevelWithSolution(level);
    }

    public void GenerateLevel(int size)
    {
        _pieceBoard = new Piece[size, size];
        foreach (var piece in Level.PiecesInfo)
        {
            // var x = piece.Item2.x;
            // var y = piece.Item2.y;
            // var pieceGo = Object.Instantiate(_piecePrefab, new Vector3(x, y, 0),
            //     Quaternion.identity);
            // pieceGo.Type = (PieceType)piece.Item1;
            // pieceGo.CurrentNumber = piece.Item1;
            // _pieceBoard[x, y] = pieceGo;
        }

        _finishPiece = Object.Instantiate(_piecePrefab, new Vector3(Level.FinishPos.x, Level.FinishPos.y, 0),
            Quaternion.identity);
        _cursor = Object.Instantiate(_cursorPrefab, new Vector3(Level.MousePos.x, Level.MousePos.y, 0),
            Quaternion.identity);
    }

    public bool AvailableToMove(Vector2 actualPosition, Vector2 dir)
    {
        var x = (int)actualPosition.x;
        var y = (int)actualPosition.y;

        var xDir = (int)(dir.x + x);
        var yDir = (int)(dir.y + y);

        if (xDir >= _pieceBoard.GetLength(0) || xDir < 0 || yDir >= _pieceBoard.GetLength(1) || yDir < 0 ||
            _pieceBoard[xDir, yDir] == null)
            return false;

        var actualPiece = _pieceBoard[x, y];
        var actualType = actualPiece.Type;
        var dirType = _pieceBoard[xDir, yDir].Type;

        switch (actualType)
        {
            case PieceType.Movable when dirType == PieceType.Movable:
                actualPiece.UpdateValue();
                _finishedLevel = false;
                return true;
            case PieceType.Movable when dirType == PieceType.Goal && CanFinish():
                actualPiece.UpdateValue();
                _finishedLevel = true;
                return true;
            default:
                return false;
        }
    }

    private bool CanFinish()
    {
        // TODO: implement here
        return true;
    }

    public bool FinishedLevel()
    {
        return _finishedLevel;
    }
}