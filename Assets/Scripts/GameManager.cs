using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    //Objects on scene
    public GameObject MenuHolderObjects;
    public GameObject LevelPacksHolder;
    public GameObject LevelsHolder;
    public GameObject InGame;
    public GameObject StoreHolder;
    public GameObject OptionsHolder;

    //Controllers
    private UIButtons uiButtons;
    private LevelGenerator _levelGenerator;
    private LoadLevelFromJson jsonLoader;
    [HideInInspector]
    public BoardManager board;

    //aux's

    public static GameManager GetInstance()
    {
        return FindObjectOfType<GameManager>();
    }

    void Awake()
    {
        jsonLoader = LoadLevelFromJson.Create();
        uiButtons = UIButtons.Create(this, jsonLoader);
        _levelGenerator = new LevelGenerator();
        board = new BoardManager();
    }

    void Start()
    {
        //jsonLoader.LoadFromJson("1");
    }

    #region Buttons
    //Buttons functions
    public void GoToPackHolder()
    {
        uiButtons.GoToPackHolder();
    }

    public void PackHolderGoBack()
    {
        uiButtons.PackHolderGoBack();
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
        uiButtons.PlayUnlockedLevel();
    }
    #endregion
}
