// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class LoadLevelFromJson: MonoBehaviour {
//     
//     private Level _level;
//     private PiecesManager _piecesManager;
//     private CameraCalculation _cameraCalculation;
//     private LevelLoader _levelLoader;
//     private BackGroundManager _backgroundManager;
//     private Cursor cursorPrefab;
//     private BoardManager board;
//
//     public static LoadLevelFromJson Create()
//     {
//         GameObject gameObject = new GameObject();
//         gameObject.name = "JsonLoader";
//         LoadLevelFromJson jsonLoader = gameObject.AddComponent<LoadLevelFromJson>();
//         jsonLoader._levelLoader = new LevelLoader(jsonLoader._level);
//         //GameManager.GetInstance().backgroundManager = jsonLoader._backgroundManager = BackGroundManager.Create();
//         GameManager.GetInstance().piecesManager = jsonLoader._piecesManager = PiecesManager.Create();
//         jsonLoader._levelLoader = new LevelLoader(jsonLoader._level);
//         jsonLoader.cursorPrefab = Resources.Load<Cursor>("Prefabs/Cursor");
//         jsonLoader._cameraCalculation = new CameraCalculation();
//         jsonLoader.cursorPrefab = Instantiate(jsonLoader.cursorPrefab, Vector2.zero, Quaternion.identity);
//         GameManager.GetInstance().cursorObject = jsonLoader.cursorPrefab.gameObject;
//         jsonLoader.cursorPrefab.gameObject.SetActive(false);
//         return jsonLoader;
//     }
//
//     void Start()
//     {
//         board = GameManager.GetInstance().board;
//     }
//
//     public void LoadFromJson(string levelName)
//     {
//         _level = new Level();
//         _level = _levelLoader.LoadFromJson(levelName);
//         int size = (int)_level.size.x;
//         _piecesManager.DesativatePieces();
//
//         board.ResetNumberCount();
//
//         board.NewBoard(size);
//
//         //_backgroundManager.DisplayBackground(size);
//         _cameraCalculation.CameraOrtAndPosition(size);
//
//         int piecesCount = _level.piecesInfoList.Count;
//
//         // Display numbers
//         List<GameObject> pieces = new List<GameObject>();
//         for (int i = 0; i < piecesCount; i++)
//         {
//             Vector2 piecePos = _level.piecesInfoList[i].position;
//             int number = _level.piecesInfoList[i].number;
//
//             GameObject piece = _piecesManager.DisplayPiece(i, piecePos, number);
//             piece.tag = "Number";
//
//             board.AddOnBoard(piece, piecePos);
//             pieces.Add(piece);
//         }
//
//         // Display finish and add to board before border calculation
//         GameObject finish = _piecesManager.DisplayFinish(_level.finishInfo.pos);
//         finish.tag = "Finish";
//         board.AddOnBoard(finish, _level.finishInfo.pos);
//
//         // Set borders after all pieces (including finish) are placed
//         for (int i = 0; i < piecesCount; i++)
//         {
//             Vector2 piecePos = _level.piecesInfoList[i].position;
//             GameObject piece = pieces[i];
//
//             // Check for borders (now includes finish piece as neighbor)
//             bool hasLeft = board.HasPieceAt(new Vector2(piecePos.x - 1, piecePos.y));
//             bool hasRight = board.HasPieceAt(new Vector2(piecePos.x + 1, piecePos.y));
//             bool hasTop = board.HasPieceAt(new Vector2(piecePos.x, piecePos.y + 1));
//             bool hasBottom = board.HasPieceAt(new Vector2(piecePos.x, piecePos.y - 1));
//
//             // Add or get BorderInfo component
//             BorderInfo borderInfo = piece.GetComponent<BorderInfo>();
//             if (borderInfo == null)
//             {
//                 borderInfo = piece.AddComponent<BorderInfo>();
//             }
//
//             borderInfo.SetBorders(!hasLeft, !hasRight, !hasTop, !hasBottom);
//         }
//
//         // Set borders for finish piece
//         Vector2 finishPos = _level.finishInfo.pos;
//         bool finishHasLeft = board.HasPieceAt(new Vector2(finishPos.x - 1, finishPos.y));
//         bool finishHasRight = board.HasPieceAt(new Vector2(finishPos.x + 1, finishPos.y));
//         bool finishHasTop = board.HasPieceAt(new Vector2(finishPos.x, finishPos.y + 1));
//         bool finishHasBottom = board.HasPieceAt(new Vector2(finishPos.x, finishPos.y - 1));
//
//         BorderInfo finishBorderInfo = finish.GetComponent<BorderInfo>();
//         if (finishBorderInfo == null)
//         {
//             finishBorderInfo = finish.AddComponent<BorderInfo>();
//         }
//
//         finishBorderInfo.SetBorders(!finishHasLeft, !finishHasRight, !finishHasTop, !finishHasBottom);
//         // Cursor
//         cursorPrefab.InitialStart(_level.mousePos);
//     }
// }
