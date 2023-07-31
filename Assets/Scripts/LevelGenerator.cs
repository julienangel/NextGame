using System.Collections.Generic;
using UnityEngine;

public static class LevelGenerator
{
    public static Level GenerateLevel(int size)
    {
        var newLevel = new Level();

        var board = new int[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                board[j, i] = 0;
            }
        }

        var initial = new Vector2(Random.Range(0, size), Random.Range(0, size));
        var current = initial;

        board[(int)current.x, (int)current.y]++;

        Vector2[] directions = {
            new Vector2(0, 1), // up
            new Vector2(0, -1), // down
            new Vector2(-1, 0), // left
            new Vector2(1, 0) // right
        };

        for (var i = 0; i < 15; i++)
        {
            var possibleDirections = new List<Vector2>();

            foreach (var direction in directions)
            {
                var next = current + direction;
                if (next.x >= 0 && next.x < size && next.y >= 0 && next.y < size)
                {
                    possibleDirections.Add(direction);
                }
            }

            var chosenDirection = possibleDirections[Random.Range(0, possibleDirections.Count)];
            current += chosenDirection;
            board[(int)current.x, (int)current.y]++;
        }

        // Now find an empty space adjacent to the finish
        var finish = new Vector2();
        var finishFound = false;

        foreach (var direction in directions)
        {
            var possibleFinish = current + direction;
            if (possibleFinish.x >= 0 && possibleFinish.x < size && possibleFinish.y >= 0 && possibleFinish.y < size &&
                board[(int)possibleFinish.x, (int)possibleFinish.y] == 0)
            {
                finish = possibleFinish;
                finishFound = true;
                break;
            }
        }

        // If no adjacent free space, keep moving
        while (!finishFound)
        {
            var chosenDirection = directions[Random.Range(0, directions.Length)];
            current += chosenDirection;
            if (current.x >= 0 && current.x < size && current.y >= 0 && current.y < size)
            {
                board[(int)current.x, (int)current.y]++;

                foreach (var direction in directions)
                {
                    var possibleFinish = current + direction;
                    if (possibleFinish.x >= 0 && possibleFinish.x < size && possibleFinish.y >= 0 &&
                        possibleFinish.y < size && board[(int)possibleFinish.x, (int)possibleFinish.y] == 0)
                    {
                        finish = possibleFinish;
                        finishFound = true;
                        break;
                    }
                }
            }
        }

        newLevel.size = size;
        newLevel.mousePos = initial;
        newLevel.piecesInfoList.Add(new PiecesInfo { number = 0, pieceType = PieceType.Finish, position = finish });
        
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                if (board[i, j] > 0)
                {
                    var newPiece = new PiecesInfo
                    {
                        number = board[i,j],
                        position = new Vector2(i,j)
                    };
                    newLevel.piecesInfoList.Add(newPiece);
                }
            }
        }

        return newLevel;
    }
}