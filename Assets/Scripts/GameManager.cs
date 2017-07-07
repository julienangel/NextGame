using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Awake() {
        _sceneState = new SceneState();
        uiButtons = UIButtons.Create(this);
    }
    
	void Start () {
        
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
    #endregion
}
