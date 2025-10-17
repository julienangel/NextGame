using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Diagnostics;

[BurstCompile]
public static class BurstSolver
{
    private static readonly int2[] Directions = { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) };

    public struct BoardCell
    {
        public sbyte Value;
        public Direction AllowedIn;
        public Direction AllowedOut;
    }

    private struct DFSState
    {
        public int2 Position;
        public byte NextNeighborIdx;
        public BoardCell OldValue;
    }

    public static bool DFSIterative(
        NativeArray<BoardCell> board, int2 startPos, int2 finishPos, int size,
        float msCap, int stateCap, int depthCap, long startTimestamp, NativeList<MoveDirection> path)
    {
        var stack = new NativeList<DFSState>(depthCap, Allocator.Temp);
        stack.Add(new DFSState { Position = startPos, NextNeighborIdx = 0, OldValue = new BoardCell { Value = -1 } });
        int statesExplored = 0;

        while (stack.Length > 0 && statesExplored < stateCap)
        {
            if ((statesExplored & 1023) == 0 &&
                (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency > msCap / 1000.0) break;
            if (stack.Length > depthCap) break;

            ref var state = ref stack.ElementAt(stack.Length - 1);
            int2 pos = state.Position;
            int idx = pos.y * size + pos.x;
            statesExplored++;

            if (CanFinish(board, pos, finishPos, size))
            {
                if (path.IsCreated) path.Add(MoveDirection.Finish);
                stack.Dispose();
                return true;
            }

            var neighbors = new NativeList<(int2, MoveDirection)>(4, Allocator.Temp);
            GetValidNeighbors(board, pos, size, neighbors);

            if (state.NextNeighborIdx >= neighbors.Length)
            {
                neighbors.Dispose();
                if (state.OldValue.Value >= 0) board[idx] = state.OldValue;
                stack.RemoveAt(stack.Length - 1);
                if (path.IsCreated && path.Length > 0) path.RemoveAt(path.Length - 1);
                continue;
            }

            var (nextPos, moveDir) = neighbors[(int)state.NextNeighborIdx];
            neighbors.Dispose();
            state.NextNeighborIdx++;

            var currentCell = board[idx];
            var newCell = currentCell;
            newCell.Value--;
            board[idx] = newCell;

            if (!IsConnected(board, nextPos, size))
            {
                board[idx] = currentCell;
                continue;
            }

            if (path.IsCreated) path.Add(moveDir);
            stack.Add(new DFSState { Position = nextPos, NextNeighborIdx = 0, OldValue = currentCell });
        }

        stack.Dispose();
        return false;
    }

    private static int CountPositiveCells(in NativeArray<BoardCell> board)
    {
        int count = 0;
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i].Value > 0) count++;
        }

        return count;
    }

    private static bool CanFinish(in NativeArray<BoardCell> board, int2 pos, int2 finishPos, int size)
    {
        if (board[pos.y * size + pos.x].Value != 1) return false;
        if (CountPositiveCells(board) != 1) return false;
        return math.abs(pos.x - finishPos.x) + math.abs(pos.y - finishPos.y) == 1;
    }

    private static void GetValidNeighbors(in NativeArray<BoardCell> board, int2 pos, int size,
        NativeList<(int2, MoveDirection)> neighbors)
    {
        var currentCell = board[pos.y * size + pos.x];

        for (int i = 0; i < Directions.Length; i++)
        {
            int2 neighborPos = pos + Directions[i];
            if (neighborPos.x < 0 || neighborPos.x >= size || neighborPos.y < 0 || neighborPos.y >= size) continue;

            var neighborCell = board[neighborPos.y * size + neighborPos.x];
            if (neighborCell.Value <= 0) continue;

            var moveDir = (Direction)(1 << i);
            var oppositeDir = (Direction)(1 << (i % 2 == 0 ? i + 1 : i - 1));

            if ((currentCell.AllowedOut & moveDir) == 0) continue;
            if ((neighborCell.AllowedIn & oppositeDir) == 0) continue;

            neighbors.Add((neighborPos, (MoveDirection)(i + 1)));
        }
    }

    private static bool IsConnected(in NativeArray<BoardCell> board, int2 startPos, int size)
    {
        int totalPositive = CountPositiveCells(board);
        if (totalPositive <= 1) return true;

        var visited = new NativeArray<bool>(size * size, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var queue = new NativeList<int2>(64, Allocator.Temp);
        queue.Add(startPos);
        visited[startPos.y * size + startPos.x] = true;
        int visitedCount = 1;

        while (queue.Length > 0)
        {
            int2 current = queue[0];
            queue.RemoveAtSwapBack(0);
            foreach (var dir in Directions)
            {
                int2 neighbor = current + dir;
                if (neighbor.x < 0 || neighbor.x >= size || neighbor.y < 0 || neighbor.y >= size) continue;
                int idx = neighbor.y * size + neighbor.x;
                if (visited[idx] || board[idx].Value <= 0) continue;
                visited[idx] = true;
                visitedCount++;
                queue.Add(neighbor);
            }
        }

        visited.Dispose();
        queue.Dispose();
        return visitedCount == totalPositive;
    }
}