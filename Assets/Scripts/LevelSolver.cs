using Unity.Collections;
using Unity.Mathematics;
using System.Diagnostics;

public static class LevelSolver
{
    private static void InitializeBoard(NativeArray<BurstSolver.BoardCell> board, Level level, int size)
    {
        for (int i = 0; i < board.Length; i++)
            board[i] = new BurstSolver.BoardCell { Value = 0, AllowedIn = Direction.All, AllowedOut = Direction.All };

        int finishIdx = level.FinishPos.y * size + level.FinishPos.x;
        board[finishIdx] = new BurstSolver.BoardCell
            { Value = -1, AllowedIn = Direction.All, AllowedOut = Direction.None };

        foreach (var piece in level.PiecesInfo)
        {
            int idx = piece.Position.y * size + piece.Position.x;
            board[idx] = new BurstSolver.BoardCell
                { Value = (sbyte)piece.Value, AllowedIn = Direction.All, AllowedOut = Direction.All };
        }

        foreach (var piece in level.DirectionalPiecesInfo)
        {
            int idx = piece.Position.y * size + piece.Position.x;
            board[idx] = new BurstSolver.BoardCell
                { Value = (sbyte)piece.Value, AllowedIn = piece.AllowedIn, AllowedOut = piece.AllowedOut };
        }
    }

    public static bool TrySolveLite(Level level, int2 startPos, float msCap, int stateCap, int depthCap)
    {
        int size = level.BoardSize.x;
        using var board = new NativeArray<BurstSolver.BoardCell>(size * size, Allocator.Temp);
        InitializeBoard(board, level, size);

        int startIdx = startPos.y * size + startPos.x;
        if (board[startIdx].Value <= 0) return false;

        long startTimestamp = Stopwatch.GetTimestamp();
        bool result = BurstSolver.DFSIterative(board, startPos, level.FinishPos, size, msCap, stateCap, depthCap,
            startTimestamp, default);
        return result;
    }

    public static NativeList<MoveDirection> SolveComplete(Level level, int2 startPos, float msCap, int stateCap,
        int depthCap)
    {
        int size = level.BoardSize.x;
        using var board = new NativeArray<BurstSolver.BoardCell>(size * size, Allocator.Temp);
        InitializeBoard(board, level, size);

        int startIdx = startPos.y * size + startPos.x;
        if (board[startIdx].Value <= 0) return default;

        var path = new NativeList<MoveDirection>(depthCap, Allocator.Persistent);
        long startTimestamp = Stopwatch.GetTimestamp();
        bool found = BurstSolver.DFSIterative(board, startPos, level.FinishPos, size, msCap, stateCap, depthCap,
            startTimestamp, path);

        if (!found)
        {
            path.Dispose();
            return default;
        }

        return path;
    }
}