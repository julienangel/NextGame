using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //Objects on scene
    public GameObject MenuHolderObjects;
    public GameObject LevelPacksHolder;
    public GameObject LevelsHolder;
    public GameObject InGame;
    public GameObject StoreHolder;
    public GameObject OptionsHolder;
    [HideInInspector] public GameObject cursorObject;

    [SerializeField] private RectTransform _transitionCircle;

    //Controllers
    private UIButtons uiButtons;
    private LevelDisplayManager levelDisplayManager;
    [HideInInspector] public BoardManager board;
    [HideInInspector] public BackGroundManager backgroundManager;
    [HideInInspector] public PiecesManager piecesManager;
    public FadeScenes fadeScenes;

    //aux's
    public int levelNumber = 1;
    public static GameManager Instance;

    private static GameManager instance;
    public static event Action<Action> PlayFinishPieceTransition;
    public static event Action<int> LevelUpdated;
    public static GameManager GetInstance()
    {
        if (instance == null)
            instance = FindAnyObjectByType<GameManager>();
        return instance;
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        
        //Screen.SetResolution(720, 1280, false);
        piecesManager = PiecesManager.Create();
        levelDisplayManager = LevelDisplayManager.Create();
        board = BoardManager.Create();
        uiButtons = UIButtons.Create(this, levelDisplayManager);
    }

    void Start()
    {
        //jsonLoader.LoadFromJson("1");
        LevelUpdated?.Invoke(levelNumber);
    }

    #region Buttons

    //Buttons functions
    public void GoToPackHolder()
    {
        uiButtons.GoToPackHolder();
    }

    public void PackHolderGoBack()
    {
        LevelUpdated?.Invoke(levelNumber);
        PlayFinishPieceTransition?.Invoke(()=>uiButtons.PackHolderGoBack());
    }

    public void LevelsHolderGoBack()
    {
        uiButtons.LevelsHolderGoBack();
    }

    public void SelectPack()
    {
        uiButtons.SelectPack();
    }

    public void PlayUnlockedLevel()
    {
        if(_transitionCircle.gameObject.activeInHierarchy) 
            PlayNextUnlockedLevel();
        else 
            PlayFinishPieceTransition?.Invoke(()=>
            {
                uiButtons.PlayUnlockedLevel(levelNumber);
                LevelUpdated?.Invoke(levelNumber);
            });
    }

    public void RestartCurrentLevel()
    {
        PlayFinishPieceTransition?.Invoke(()=>
        {
            uiButtons.ReplayCurrentLevel();
            LevelUpdated?.Invoke(levelNumber);
        });
    }

    public async void PlayNextUnlockedLevel()
    {
        var sq = PlayTransitionIn();
        
        await sq.AsyncWaitForCompletion(); 
        
        LevelUpdated?.Invoke(levelNumber);
        uiButtons.PlayUnlockedLevel(levelNumber);
    }

    #endregion

    private Sequence PlayTransitionIn()
    {
        var image = _transitionCircle.GetComponent<Image>();
        var cachedColor = image.color;
        
        Sequence sequence = DOTween.Sequence();
        sequence.Append(_transitionCircle.DOScale(Vector3.one * 30f, 0.5f));
        //sequence.Append(image.DOFade(0f, 0.25f));
        sequence.OnComplete(() => {
            _transitionCircle.localScale = Vector3.one;
            image.color = cachedColor;
        });

        return sequence;
    }
}