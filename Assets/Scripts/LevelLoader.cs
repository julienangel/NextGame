using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelLoader {

    private Level _level;

    public LevelLoader(Level level)
    {
        this._level = level;
    }

    public Level LoadFromJson(string mapName)
    {
        string levelTemp = Resources.Load<TextAsset>("LevelsJson/" + mapName).ToString();
        return _level = JsonUtility.FromJson<Level>(levelTemp);
    }
}
