using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadLevelFromJson: MonoBehaviour {
    
    private Level _level;
    private PiecesManager _piecesManager;
    private CameraCalculation _cameraCalculation;
    private LevelLoader _levelLoader;
    private BackGroundManager _backgroundManager;
    private Cursor cursorPrefab;
    private BoardManager board;

    public static LoadLevelFromJson Create()
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "JsonLoader";
        LoadLevelFromJson jsonLoader = gameObject.AddComponent<LoadLevelFromJson>();
        jsonLoader._levelLoader = new LevelLoader(jsonLoader._level);
        jsonLoader._backgroundManager = BackGroundManager.Create();
        jsonLoader._piecesManager = PiecesManager.Create();
        jsonLoader._levelLoader = new LevelLoader(jsonLoader._level);
        jsonLoader.cursorPrefab = Resources.Load<Cursor>("Prefabs/Cursor");
        jsonLoader._cameraCalculation = new CameraCalculation();
        jsonLoader.cursorPrefab = Instantiate(jsonLoader.cursorPrefab, Vector2.zero, Quaternion.identity);
        jsonLoader.cursorPrefab.gameObject.SetActive(false);
        return jsonLoader;
    }

    void Start()
    {
        board = GameManager.GetInstance().board;
    }

    public void LoadFromJson(string levelName)
    {
        _level = new Level();
        _level = _levelLoader.LoadFromJson(levelName);
        int size = (int)_level.size.x;

        board.NewBoard(size);

        _backgroundManager.DisplayBackground(size);

        _cameraCalculation.CameraOrtAndPosition(size);

        int piecesCount = _level.piecesInfoList.Count;
        for (int i = 0; i < piecesCount; i++)
        {
            Vector2 piecePos = _level.piecesInfoList[i].position;
            int number = _level.piecesInfoList[i].number;

            _piecesManager.DisplayPiece(i, piecePos, number);

            board.AddOnBoard(number, piecePos);
        }

        cursorPrefab.InitialStart(_level.mousePos);
    }
	
}
