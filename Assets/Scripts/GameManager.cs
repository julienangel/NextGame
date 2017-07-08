using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour {

    //Objects on scene
    public GameObject MenuHolderObjects;
    public GameObject LevelPacksHolder;
    public GameObject LevelsHolder;
    public GameObject InGame;
    public GameObject StoreHolder;
    public GameObject OptionsHolder;

    //Controllers
    private SceneState _sceneState;
    private UIButtons uiButtons;
    private LevelGenerator _levelGenerator;
    private Level _level;
    private LevelSaver _levelSaver;
    private LevelLoader _levelLoader;
    private BackGroundManager _backgroundManager;
    private CameraCalculation _cameraCalculation;

    //aux's


    void Awake() {
        _level = new Level();
        _levelSaver = new LevelSaver(this._level);
        _levelLoader = new LevelLoader(this._level);
        _sceneState = new SceneState();
        _cameraCalculation = new CameraCalculation();
        uiButtons = UIButtons.Create(this);
        _levelGenerator = LevelGenerator.Create();
        _backgroundManager = BackGroundManager.Create();
    }
    
	void Start () {
        _levelGenerator.GenerateLevel(5, 5, 10, 5);
    }

    #region GetReferences
    // Gets reference
    public SceneState GetSceneState()
    {
        return _sceneState;
    }
    #endregion

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
        _backgroundManager.DisplayBackground(5);
        _cameraCalculation.CameraOrtAndPosition(5);
    }
    #endregion
}
