using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneState {

    public GameState gameState;

    public enum GameState
    {
        Menu,
        PackHolder,
        Store,
        Options,
        LevelPack,
        InGame
    }
}
