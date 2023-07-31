using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PiecesInfo
{
    public int number;
    public Vector2 position;
    public PieceType pieceType;
}

public enum PieceType
{
    Playable,
    Block,
    Finish
}