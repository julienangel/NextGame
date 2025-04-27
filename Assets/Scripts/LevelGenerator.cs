using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator
{
    private bool _findEnd = false;
    private readonly Vector2Int[] _directions = {
        Vector2Int.right, // 0
        Vector2Int.left,  // 1
        Vector2Int.down,  // 2
        Vector2Int.up     // 3
    };

    public Level GenerateLevel(int maxPieces, int numMax, int maxSum, int size)
    {
        var level = new Level();
        var board = new int[size, size];

        // Initialize board
        for (var x = 0; x < size; x++)
            for (var y = 0; y < size; y++)
                board[x, y] = 0;

        // Initial position
        var initial = new Vector2Int(Random.Range(0, size), Random.Range(0, size));
        level.mousePos = initial;
        IncCoord(initial, board);

        var actual = initial;
        level.solucao ??= new List<Vector2Int>();

        // Generate a random path
        for (var k = 0; k < maxSum; k++)
            GenerateDirection();

        _findEnd = true;
        while (_findEnd)
            GenerateDirection();

        return level;

        void GenerateDirection()
        {
            var availables = VerifyAvailables();
            if (availables.Count == 0)
            {
                // Restart level generation if stuck
                GenerateLevel(maxPieces, numMax, maxSum, size);
                return;
            }

            var dirIdx = availables[Random.Range(0, availables.Count)];
            var dir = _directions[dirIdx];
            var next = actual + dir;

            var isFinish = _findEnd && IsCellFinishable(next, board);
            actual = next;
            level.solucao.Add(dir);

            if (isFinish)
            {
                AddFinish(actual, board);
            }
            else
            {
                IncCoord(actual, board);
            }
        }

        List<int> VerifyAvailables()
        {
            var disp = new List<int>();
            for (var i = 0; i < _directions.Length; ++i)
            {
                var next = actual + _directions[i];
                if (IsWithinBounds(next, size))
                {
                    if (board[next.x, next.y] < numMax)
                        disp.Add(i);
                    else if (_findEnd)
                    {
                        numMax++;
                        disp.Add(i);
                    }
                }
            }
            return disp;
        }

        bool IsWithinBounds(Vector2Int pos, int limit)
            => pos.x >= 0 && pos.x < limit && pos.y >= 0 && pos.y < limit;

        bool IsCellFinishable(Vector2Int pos, int[,] arr)
            => arr[pos.x, pos.y] <= 0;

        void AddFinish(Vector2Int pos, int[,] arr)
        {
            arr[pos.x, pos.y] = -1;
            _findEnd = false;
        }

        void IncCoord(Vector2Int pos, int[,] arr)
        {
            arr[pos.x, pos.y] += 1;
        }
    }
}