using Unity.Collections;
using Unity.Mathematics;

#region Enums

public enum MoveDirection : byte
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 3,
    Right = 4,
    Finish = 5
}

[System.Flags]
public enum Direction : byte
{
    None = 0,
    Up = 1 << 0,    // 1
    Down = 1 << 1,  // 2
    Left = 1 << 2,  // 4
    Right = 1 << 3, // 8
    All = Up | Down | Left | Right // 15
}

public enum Difficulty : byte
{
    Easy = 0,
    Medium = 1,
    Hard = 2,
    SuperHard = 3,
    Impossible = 4
}

public enum GenerationMode : byte
{
    UltraFast = 0,
    FastSafe = 1,
    Premium = 2,
    Challenge = 3,
    Nightmare = 4
}

#endregion

#region Structs de Dados

public struct PieceInfo
{
    public int2 Position;
    public sbyte Value;
}

public struct DirectionalPieceInfo
{
    public int2 Position;
    public sbyte Value;
    public Direction AllowedIn;
    public Direction AllowedOut;
}

public struct Level : System.IDisposable
{
    public int2 MousePos;
    public int2 BoardSize;
    public int2 FinishPos;
    public NativeList<PieceInfo> PiecesInfo;
    public NativeList<DirectionalPieceInfo> DirectionalPiecesInfo;

    public void Dispose()
    {
        if (PiecesInfo.IsCreated) PiecesInfo.Dispose();
        if (DirectionalPiecesInfo.IsCreated) DirectionalPiecesInfo.Dispose();
    }
}

#endregion