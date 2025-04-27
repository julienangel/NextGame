using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level
{
    public Vector2Int mousePos;
    public Vector2Int size;
    public List<(byte, Vector2Int)> piecesInfo;
    public Vector2Int FinishPos;
    public List<Vector2Int> solucao;
}
