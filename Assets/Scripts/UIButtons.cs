using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButtons : MonoBehaviour
{
    private GameManager _gameManager;

    private LoadLevelFromJson _jsonLoader;

    GameObject MenuHolderObjects;
    GameObject LevelPacksHolder;
    GameObject LevelsHolder;
    GameObject InGame;
    GameObject StoreHolder;
    GameObject OptionsHolder;

    private SceneState _sceneState;

    public static UIButtons Create(GameManager gameManager, LoadLevelFromJson jsonLoader)
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "UiButtons";
        UIButtons uiButtons = gameObject.AddComponent<UIButtons>();
        uiButtons._gameManager = gameManager;
        uiButtons._jsonLoader = jsonLoader;
        uiButtons.MenuHolderObjects = gameManager.MenuHolderObjects;
        uiButtons.LevelPacksHolder = gameManager.LevelPacksHolder;
        uiButtons.LevelsHolder = gameManager.LevelsHolder;
        uiButtons.InGame = gameManager.InGame;
        uiButtons.StoreHolder = gameManager.StoreHolder;
        uiButtons.OptionsHolder = gameManager.OptionsHolder;
        uiButtons._sceneState = new SceneState();
        return uiButtons;
    }

    // Use this for initialization
    void Start()
    {
        
    }

    public void GoToPackHolder()
    {
        _sceneState.gameState = SceneState.GameState.PackHolder;
        NavigationBetweenScenes();
    }

    public void SelectPack()
    {
        _sceneState.gameState = SceneState.GameState.LevelPack;
        NavigationBetweenScenes();
    }

    public void PackHolderGoBack()
    {
        _sceneState.gameState = SceneState.GameState.Menu;
        NavigationBetweenScenes();
    }

    public void LevelsHolderGoBack()
    {
        _sceneState.gameState = SceneState.GameState.PackHolder;
        NavigationBetweenScenes();
    }

    public void PlayUnlockedLevel()
    {
        _sceneState.gameState = SceneState.GameState.InGame;
        NavigationBetweenScenes();
        _jsonLoader.LoadFromJson("1");
    }

    public void NavigationBetweenScenes()
    {
        switch (_sceneState.gameState)
        {
            case SceneState.GameState.Menu:
                {
                    MenuHolderObjects.SetActive(true);
                    LevelPacksHolder.SetActive(false);
                    LevelsHolder.SetActive(false);
                    InGame.SetActive(false);
                    StoreHolder.SetActive(false);
                    OptionsHolder.SetActive(false);
                    break;
                }
            case SceneState.GameState.PackHolder:
                {
                    MenuHolderObjects.SetActive(false);
                    LevelPacksHolder.SetActive(true);
                    LevelsHolder.SetActive(false);
                    InGame.SetActive(false);
                    StoreHolder.SetActive(false);
                    OptionsHolder.SetActive(false);
                    break;
                }
            case SceneState.GameState.LevelPack:
                {
                    MenuHolderObjects.SetActive(false);
                    LevelPacksHolder.SetActive(false);
                    LevelsHolder.SetActive(true);
                    InGame.SetActive(false);
                    StoreHolder.SetActive(false);
                    OptionsHolder.SetActive(false);
                    break;
                }
            case SceneState.GameState.Store:
                {
                    MenuHolderObjects.SetActive(false);
                    LevelPacksHolder.SetActive(false);
                    LevelsHolder.SetActive(false);
                    InGame.SetActive(false);
                    StoreHolder.SetActive(true);
                    OptionsHolder.SetActive(false);
                    break;
                }
            case SceneState.GameState.Options:
                {
                    MenuHolderObjects.SetActive(false);
                    LevelPacksHolder.SetActive(false);
                    LevelsHolder.SetActive(false);
                    InGame.SetActive(false);
                    StoreHolder.SetActive(false);
                    OptionsHolder.SetActive(true);
                    break;
                }
            case SceneState.GameState.InGame:
                {
                    MenuHolderObjects.SetActive(false);
                    LevelPacksHolder.SetActive(false);
                    LevelsHolder.SetActive(false);
                    InGame.SetActive(true);
                    StoreHolder.SetActive(false);
                    OptionsHolder.SetActive(true);
                    break;
                }
            default:
                {
                    break;
                }
        }
    }
}
