using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class LevelDisplayManager : MonoBehaviour
{
    private PiecesManager _piecesManager;
    private CameraCalculation _cameraCalculation;
    private Cursor _cursorPrefab;
    private BoardManager _board;
    private Level _cachedLevel;
    private bool _hasLevelCached = false;

    public static LevelDisplayManager Create()
    {
        GameObject gameObject = new GameObject
        {
            name = "LevelDisplayManager"
        };
        LevelDisplayManager displayManager = gameObject.AddComponent<LevelDisplayManager>();
        displayManager._cameraCalculation = new CameraCalculation();
        displayManager._cursorPrefab = Resources.Load<Cursor>("Prefabs/Cursor");
        displayManager._cursorPrefab = Instantiate(displayManager._cursorPrefab, Vector2.zero, Quaternion.identity);
        GameManager.GetInstance().cursorObject = displayManager._cursorPrefab.gameObject;
        displayManager._cursorPrefab.gameObject.SetActive(false);
        return displayManager;
    }

    void Start()
    {
        _board = GameManager.GetInstance().board;
        _piecesManager = GameManager.GetInstance().piecesManager;
    }

    void OnDestroy()
    {
        _hasLevelCached = false;
    }

    public void DisplayLevel(Level level)
    {
        // Cache the level for replay
        _cachedLevel = level;
        _hasLevelCached = true;

        DisplayLevelInternal(level);
    }

    public void RedisplayCurrentLevel()
    {
        if (!_hasLevelCached)
        {
            Debug.LogWarning("No level cached to redisplay!");
            return;
        }

        // Redisplay the cached level without regenerating
        DisplayLevelInternal(_cachedLevel);
    }

    private void DisplayLevelInternal(Level level)
    {
        int size = level.BoardSize;
        _piecesManager.DesativatePieces();
        _board.ResetNumberCount();
        _board.NewBoard(size);
        _cameraCalculation.CameraOrtAndPosition(size);

        // Display regular pieces
        List<GameObject> pieces = new List<GameObject>();
        for (int i = 0; i < level.PiecesInfo.Length; i++)
        {
            PieceInfo pieceInfo = level.PiecesInfo[i];
            Vector2 piecePos = new Vector2(pieceInfo.Position.X, pieceInfo.Position.Y);
            int number = pieceInfo.Value;

            GameObject piece = _piecesManager.DisplayPiece(i, piecePos, number);
            piece.tag = "Number";

            _board.AddOnBoard(piece, piecePos);
            pieces.Add(piece);
        }

        // Display directional pieces
        int directionalStartIndex = level.DirectionalPiecesInfo.Length;
        for (int i = 0; i < level.DirectionalPiecesInfo.Length; i++)
        {
            DirectionalPieceInfo dirPieceInfo = level.DirectionalPiecesInfo[i];
            Vector2 piecePos = new Vector2(dirPieceInfo.Position.X, dirPieceInfo.Position.Y);
            int number = dirPieceInfo.Value;

            GameObject piece = _piecesManager.DisplayPiece(directionalStartIndex + i, piecePos, number);
            piece.tag = "Number";

            // Add a directional component if needed
            DirectionalPiece dirComponent = piece.GetComponent<DirectionalPiece>();
            if (dirComponent == null)
            {
                dirComponent = piece.AddComponent<DirectionalPiece>();
            }
            dirComponent.AllowedIn = dirPieceInfo.AllowedIn;
            dirComponent.AllowedOut = dirPieceInfo.AllowedOut;

            _board.AddOnBoard(piece, piecePos);
            pieces.Add(piece);
        }

        // Display finish piece
        Vector2 finishPos = new Vector2(level.FinishPos.X, level.FinishPos.Y);
        GameObject finish = _piecesManager.DisplayFinish(finishPos);
        finish.tag = "Finish";
        _board.AddOnBoard(finish, finishPos);

        // Set borders for regular pieces
        for (int i = 0; i < level.DirectionalPiecesInfo.Length; i++)
        {
            DirectionalPieceInfo pieceInfo = level.DirectionalPiecesInfo[i];
            Vector2 piecePos = new Vector2(pieceInfo.Position.X, pieceInfo.Position.Y);
            GameObject piece = pieces[i];

            SetBorders(piece, piecePos);
        }

        // Set borders for directional pieces
        for (int i = 0; i < level.DirectionalPiecesInfo.Length; i++)
        {
            DirectionalPieceInfo dirPieceInfo = level.DirectionalPiecesInfo[i];
            Vector2 piecePos = new Vector2(dirPieceInfo.Position.X, dirPieceInfo.Position.Y);
            GameObject piece = pieces[level.DirectionalPiecesInfo.Length + i];

            SetBorders(piece, piecePos);
        }

        // Set borders for finish piece
        SetBorders(finish, finishPos);

        // Initialize cursor
        Vector2 mousePos = new Vector2(level.MousePos.X, level.MousePos.Y);
        _board.SetCursorPosition(new Vector2Int(level.MousePos.X, level.MousePos.Y));
        _cursorPrefab.InitialStart(mousePos);
    }

    private void SetBorders(GameObject piece, Vector2 pos)
    {
        bool hasLeft = _board.HasPieceAt(new Vector2(pos.x - 1, pos.y));
        bool hasRight = _board.HasPieceAt(new Vector2(pos.x + 1, pos.y));
        bool hasTop = _board.HasPieceAt(new Vector2(pos.x, pos.y + 1));
        bool hasBottom = _board.HasPieceAt(new Vector2(pos.x, pos.y - 1));

        BorderInfo borderInfo = piece.GetComponent<BorderInfo>();
        if (borderInfo == null)
        {
            borderInfo = piece.AddComponent<BorderInfo>();
        }

        borderInfo.SetBorders(!hasLeft, !hasRight, !hasTop, !hasBottom);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (_hasLevelCached)
            {
                var solution = LevelSolver.SolveWithException(_board.GetCurrentLevelState());
                if (solution.Count > 0)
                {
                    string solutionLog = $"Solution found with {solution.Count} moves:\n";
                    for (int i = 0; i < solution.Count; i++)
                    {
                        solutionLog += $"Move {i + 1}: {solution[i]}\n";
                    }
                    Debug.Log(solutionLog);
                    //solution.Dispose();
                }
                else
                {
                    Debug.LogWarning("No solution found!");
                }
            }
        }
    }
}
