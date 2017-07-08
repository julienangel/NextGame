using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelSaver {

    private Level _level;

    public LevelSaver(Level level)
    {
        this._level = level;
    }

    public void SaveLevel(Vector2 mousePosInitial, Vector2 size, string levelString, string levelName, string solucao)
    {
        _level.size = size;
        _level.mousePos = mousePosInitial;
        _level.levelString = levelString;
        _level.solucao = solucao;
    }

    public void SaveInText(string levelName)
    {
        File.WriteAllText(Application.dataPath + "/Resources" + "/" + levelName + ".json", JsonUtility.ToJson(_level));
#if Unity_Editor
        UnityEditor.AssetDatabase.Refresh ();
#endif
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
