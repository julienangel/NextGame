using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;
using Cursor = Gameplay.Cursor;

public class GameManager : Singleton<GameManager>
{
    public BoardManager BoardManager { get; private set; }
    
    public GameObject MenuHolderObjects;
    public GameObject InGame;
    public GameObject OptionsHolder;

    [HideInInspector]
    public FadeScenes fadeScenes;

    [HideInInspector]
    public int levelNumber = 1;
    
    [SerializeField] private Piece _piecePrefab;
    [SerializeField] private Cursor _cursorPrefab;
    
    

    private void Awake()
    {
        Application.targetFrameRate = 60;
        BoardManager = new BoardManager(_piecePrefab, _cursorPrefab);
    }

    #region Buttons
    //Buttons functions
    #endregion
}
