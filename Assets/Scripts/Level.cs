using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Level
{
    public Vector2 mousePos;
    public Vector2 size;
    public List<PiecesInfo> piecesInfoList;
    public FinishInfo finishInfo;
    public List<Vector2> solucao;
}
