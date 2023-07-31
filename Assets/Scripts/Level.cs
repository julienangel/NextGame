using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Level
{
    public Vector2 mousePos;
    public int size;
    public List<PiecesInfo> piecesInfoList = new List<PiecesInfo>();
}
