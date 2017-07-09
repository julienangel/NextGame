using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelSaver {

    public void SaveInText(string levelName, Level level)
    {
        File.WriteAllText(Application.dataPath + "/Resources" + "/levelsJson/" + levelName + ".json", JsonUtility.ToJson(level));
#if Unity_Editor
        UnityEditor.AssetDatabase.Refresh ();
#endif
    }
}
