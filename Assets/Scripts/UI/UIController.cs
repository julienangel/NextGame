using System;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("References - Level Viewers")]
    [SerializeField] private TextMeshProUGUI _mainMenuLevelText;
    [SerializeField] private TextMeshProUGUI _inGameLevelText;

    private void UpdateTexts(int levelNumber)
    {
        _mainMenuLevelText.text = "Level " + levelNumber;
        _inGameLevelText.text = $"{levelNumber}";
    }

    private void OnEnable()
    {
        GameManager.LevelUpdated += UpdateTexts;
    }
    
    private void OnDisable()
    {
        GameManager.LevelUpdated -= UpdateTexts;
    }
}
