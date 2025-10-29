using System;
using System.Runtime.CompilerServices;

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

public enum Direction : byte
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}

public struct Position : IEquatable<Position>
{
    public readonly int X;
    public readonly int Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Position other) => X == other.X && Y == other.Y;
    
    public override bool Equals(object obj) => obj is Position pos && Equals(pos);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => (X << 16) | (Y & 0xFFFF);
}

public struct PieceInfo
{
    public Position Position;
    public int Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PieceInfo(Position position, int value)
    {
        Position = position;
        Value = value;
    }
}

public struct DirectionalPieceInfo
{
    public Position Position;
    public int Value;
    public Direction AllowedIn;
    public Direction AllowedOut;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DirectionalPieceInfo(Position position, int value, Direction allowedIn, Direction allowedOut)
    {
        Position = position;
        Value = value;
        AllowedIn = allowedIn;
        AllowedOut = allowedOut;
    }
}

public struct Level
{
    public Position MousePos;
    public int BoardSize;
    public Position FinishPos;
    public PieceInfo[] PiecesInfo;
    public DirectionalPieceInfo[] DirectionalPiecesInfo;

    public Level(int boardSize, int maxPieces = 50)
    {
        MousePos = default;
        BoardSize = boardSize;
        FinishPos = default;
        PiecesInfo = new PieceInfo[maxPieces];
        DirectionalPiecesInfo = Array.Empty<DirectionalPieceInfo>(); // Por enquanto vazio
    }
}

#endregion