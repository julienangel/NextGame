using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData {

    private int _currentLevel;
    private int _unlockedLevel;
    private int _maxLevel;
    private bool _removedAds;
    private int _hintsCount;

	public PlayerData()
    {

    }

    public bool IsLastLevel()
    {
        return _currentLevel == (_maxLevel - 1);
    }

    public bool IsMaximunUnlockedLevel()
    {
        return _currentLevel == _unlockedLevel;
    }
}
