using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class UIButtons : MonoBehaviour
{
    private GameManager _gameManager;

    private LevelDisplayManager _levelDisplayManager;

    GameObject MenuHolderObjects;
    GameObject LevelPacksHolder;
    GameObject LevelsHolder;
    GameObject InGame;
    GameObject StoreHolder;
    GameObject OptionsHolder;

    private SceneState _sceneState;

    private Dictionary<string, DifficultyLimits> _difficultyLimitsLookup;

    private class DifficultyLimits
    {
        public int MinPieces;
        public int MaxPieces;
        public int NumMax;
        public int MaxMoves;
    }

    public static UIButtons Create(GameManager gameManager, LevelDisplayManager levelDisplayManager)
    {
        GameObject gameObject = new GameObject
        {
            name = "UiButtons"
        };
        UIButtons uiButtons = gameObject.AddComponent<UIButtons>();
        uiButtons._gameManager = gameManager;
        uiButtons._levelDisplayManager = levelDisplayManager;
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
        LoadDifficultyLimits();
    }

    private void LoadDifficultyLimits()
    {
        _difficultyLimitsLookup = new Dictionary<string, DifficultyLimits>();

        string filePath = Path.Combine(Application.dataPath, "difficulty_limits.txt");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Difficulty limits file not found at: {filePath}");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(',');
            if (parts.Length != 6)
                continue;

            string difficulty = parts[0].Trim();
            string size = parts[1].Trim();

            // Create key like "Easy5x5"
            string key = difficulty + size;

            DifficultyLimits limits = new DifficultyLimits
            {
                MinPieces = int.Parse(parts[2].Trim()),
                MaxPieces = int.Parse(parts[3].Trim()),
                NumMax = int.Parse(parts[4].Trim()),
                MaxMoves = int.Parse(parts[5].Trim())
            };

            _difficultyLimitsLookup[key] = limits;
        }

        Debug.Log($"Loaded {_difficultyLimitsLookup.Count} difficulty limit entries");
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

    public void PlayUnlockedLevel(int numberLevel)
    {
        numberLevel = 25;
        _sceneState.gameState = SceneState.GameState.InGame;
        //_gameManager.fadeScenes.PlayFade();
        NavigationBetweenScenes();
        // Generate level based on level number
        // Adjust parameters based on progression
        int size = 5 + (numberLevel / 2); // Increase board size faster
        size = Mathf.Clamp(size, 5, 20); // Keep between 5 and 20

        int minPieces = 5 + (numberLevel / 2);
        int maxPieces = 8 + numberLevel;
        maxPieces = Mathf.Clamp(maxPieces, minPieces, size * size - 1);

        int numMax = 5 + (numberLevel / 4);
        numMax = Mathf.Clamp(numMax, 5, 30);

        int maxMoves = minPieces + 3;

        // Determine difficulty based on level number within 8-level cycles
        int cyclePosition = (numberLevel - 1) % 8;
        Difficulty difficulty = Difficulty.Easy;

        if (cyclePosition == 7) difficulty = Difficulty.Impossible;
        else if (cyclePosition == 6) difficulty = Difficulty.SuperHard;
        else if (cyclePosition >= 4) difficulty = Difficulty.Hard;
        else if (cyclePosition >= 2) difficulty = Difficulty.Medium;

        // Validate parameters against difficulty limits (see difficulty_limits.txt)
        ValidateLevelParameters(ref size, ref minPieces, ref maxPieces, ref numMax, ref maxMoves, difficulty);

        Level level = LevelGenerator.GenerateLevel(
            minPieces,
            maxPieces,
            numMax,
            maxMoves,
            size,
            useDirectionalPieces: false,
            avoidBacktracking: true
        );

        ShowLevelAsDebugLog(level);

        if (level.PiecesInfo.Length > 0)
        {
            _levelDisplayManager.DisplayLevel(level);
        }
        else
        {
            Debug.LogError($"Failed to generate level {numberLevel}");
        }
    }

    private void ValidateLevelParameters(ref int size, ref int minPieces, ref int maxPieces, ref int numMax,
        ref int maxMoves, Difficulty difficulty)
    {
        // Use difficulty_limits.txt for upper bounds
        if (_difficultyLimitsLookup == null || _difficultyLimitsLookup.Count == 0)
        {
            Debug.LogWarning("Difficulty limits not loaded, skipping validation");
            return;
        }

        // Create lookup key: "DifficultyNxN" (e.g., "Easy5x5")
        string key = difficulty.ToString() + size + "x" + size;

        if (_difficultyLimitsLookup.TryGetValue(key, out DifficultyLimits limits))
        {
            // Clamp parameters to the upper bounds from the file
            minPieces = limits.MinPieces;
            maxPieces = limits.MaxPieces;
            numMax = limits.NumMax;
            maxMoves = limits.MaxMoves;

            Debug.Log(
                $"Applied limits for {key}: minPieces={minPieces}, maxPieces={maxPieces}, numMax={numMax}, maxMoves={maxMoves}");
        }
        else
        {
            Debug.LogWarning($"No difficulty limits found for key: {key}");
        }

        // Ensure maxPieces is always greater than or equal to minPieces
        if (maxPieces < minPieces)
        {
            maxPieces = minPieces;
        }
    }

    private void ShowLevelAsDebugLog(in Level level)
    {
        int size = level.BoardSize;
        string[][] board = new string[size][];
        for (int index = 0; index < size; index++)
        {
            board[index] = new string[size];
        }

        // Initialize empty board
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            board[x][y] = ".";

        // Place regular pieces
        foreach (var piece in level.PiecesInfo)
        {
            board[piece.Position.X][piece.Position.Y] = piece.Value.ToString();
        }

        // Place directional pieces
        foreach (var piece in level.DirectionalPiecesInfo)
        {
            board[piece.Position.X][piece.Position.Y] = piece.Value + "d";
        }

        // Place finish
        board[level.FinishPos.X][level.FinishPos.Y] = "F";

        // Build string representation
        string boardStr = "Board Layout:\n";
        for (int y = size - 1; y >= 0; y--)
        {
            for (int x = 0; x < size; x++)
            {
                boardStr += board[x][y].PadLeft(3);
            }

            boardStr += "\n";
        }

        Debug.Log(boardStr);
    }

    public void ReplayCurrentLevel()
    {
        _sceneState.gameState = SceneState.GameState.InGame;
        NavigationBetweenScenes();
        _levelDisplayManager.RedisplayCurrentLevel();
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
                //Desativar peças
                //_gameManager.backgroundManager.DesativatePieces();
                _gameManager.piecesManager.DesativatePieces();
                _gameManager.cursorObject.SetActive(false);
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