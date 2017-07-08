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
        string levelTemp = Resources.Load<TextAsset>(mapName).ToString();
        return _level = JsonUtility.FromJson<Level>(levelTemp);
    }

    public string DisplayLevel()
    {
        return _level.levelString;
    }

    public Vector2 MousePos()
    {
        return _level.mousePos;
    }

    public string ReturnSolution()
    {
        return _level.solucao;
    }
}
