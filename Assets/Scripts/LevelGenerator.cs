using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

public static class LevelGenerator
{
    private static readonly int2[] Directions = { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) };

    #region Public API & Fallback System

    public static Level GenerateLevel(
        int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty = Difficulty.SuperHard, GenerationMode mode = GenerationMode.Challenge,
        bool requireUniqueSolution = false)
    {
        return GenerateWithCascadingFallback(minPieces, maxPieces, numMax, maxMoves, size, difficulty, mode,
            requireUniqueSolution);
    }

    public static async System.Threading.Tasks.Task<Level> GenerateLevelAsync(
        int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty = Difficulty.SuperHard, GenerationMode mode = GenerationMode.Challenge,
        bool requireUniqueSolution = false)
    {
        return await System.Threading.Tasks.Task.Run(() =>
            GenerateWithCascadingFallback(minPieces, maxPieces, numMax, maxMoves, size, difficulty, mode,
                requireUniqueSolution));
    }

    private static Level GenerateWithCascadingFallback(
        int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty, GenerationMode mode, bool requireUniqueSolution)
    {
        if (mode == GenerationMode.Nightmare)
        {
            var level = AttemptGenerationForMode(0, 0, numMax, maxMoves, size, difficulty, mode, false, false, 400);
            if (level.PiecesInfo.IsCreated || level.DirectionalPiecesInfo.IsCreated) return level;

            Debug.LogWarning("[LevelGenerator] Nightmare failed. Trying again with 25% more moves...");
            level = AttemptGenerationForMode(0, 0, numMax, (int)(maxMoves * 1.25f), size, difficulty, mode, false,
                false, 400);
            if (level.PiecesInfo.IsCreated || level.DirectionalPiecesInfo.IsCreated) return level;

            Debug.LogError(
                $"[LevelGenerator] Nightmare mode failed to generate a level even with relaxed constraints.");
            return default;
        }

        var standardLevel = GenerateLevelInternal(minPieces, maxPieces, numMax, maxMoves, size, difficulty, mode,
            requireUniqueSolution);
        if (standardLevel.PiecesInfo.IsCreated || standardLevel.DirectionalPiecesInfo.IsCreated) return standardLevel;

        Debug.LogWarning(
            $"[LevelGenerator] Initial generation failed. Trying Fallback Tier 1 (relaxing constraints)...");
        standardLevel = GenerateLevelInternal((int)(minPieces * 0.9f), maxPieces, numMax, (int)(maxMoves * 1.1f), size,
            difficulty, mode, requireUniqueSolution);
        if (standardLevel.PiecesInfo.IsCreated || standardLevel.DirectionalPiecesInfo.IsCreated) return standardLevel;

        Debug.LogWarning(
            $"[LevelGenerator] Fallback Tier 1 failed. Trying Fallback Tier 2 (relaxing constraints further)...");
        standardLevel = GenerateLevelInternal((int)(minPieces * 0.8f), maxPieces, numMax, (int)(maxMoves * 1.25f), size,
            difficulty, mode, requireUniqueSolution);
        if (standardLevel.PiecesInfo.IsCreated || standardLevel.DirectionalPiecesInfo.IsCreated) return standardLevel;

        Debug.LogError($"[LevelGenerator] All fallback tiers failed for size {size}x{size}.");
        return default;
    }

    private static Level GenerateLevelInternal(
        int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty, GenerationMode mode, bool requireUniqueSolution)
    {
        bool rejectWeakLevels = difficulty >= Difficulty.Hard;
        int maxAttempts = mode switch
        {
            GenerationMode.Challenge => 200, GenerationMode.Premium => 150, GenerationMode.FastSafe => 100,
            GenerationMode.UltraFast => 50, _ => 200
        };
        var modesToTry = new[] { mode, GenerationMode.Premium, GenerationMode.FastSafe, GenerationMode.UltraFast }
            .Distinct();

        foreach (var tryMode in modesToTry)
        {
            var level = AttemptGenerationForMode(minPieces, maxPieces, numMax, maxMoves, size, difficulty, tryMode,
                requireUniqueSolution, rejectWeakLevels, maxAttempts);
            if (level.PiecesInfo.IsCreated || level.DirectionalPiecesInfo.IsCreated) return level;
        }

        return default;
    }

    #endregion

    private static Level AttemptGenerationForMode(
        int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty, GenerationMode mode, bool requireUniqueSolution,
        bool rejectWeakLevels, int maxAttempts)
    {
        if (mode == GenerationMode.Nightmare)
        {
            rejectWeakLevels = false;
            int maxCells = size * size - 1;
            float fillRatio = size switch { <= 5 => 0.60f, <= 7 => 0.65f, <= 9 => 0.70f, <= 11 => 0.75f, _ => 0.80f };
            minPieces = (int)(maxCells * (fillRatio - 0.15f));
            maxPieces = (int)(maxCells * fillRatio);
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Level level;
            if (mode == GenerationMode.Nightmare)
            {
                level = TryGenerateNightmareLevel(size, numMax, maxMoves);
            }
            else if (mode >= GenerationMode.Premium && size >= 7)
            {
                level = TryGenerateGuaranteedConnectedLevel(minPieces, maxPieces, numMax, maxMoves, size, difficulty,
                    mode);
            }
            else
            {
                level = TryGenerateLevel(minPieces, maxPieces, numMax, maxMoves, size, difficulty, mode);
            }

            if (!level.PiecesInfo.IsCreated && !level.DirectionalPiecesInfo.IsCreated) continue;

            int totalPieces = level.PiecesInfo.Length + level.DirectionalPiecesInfo.Length;
            if (totalPieces < minPieces)
            {
                level.Dispose();
                continue;
            }

            if (!LevelSolver.TrySolveLite(level, level.MousePos, 20f, 500000, 8192))
            {
                level.Dispose();
                continue;
            }

            if (mode >= GenerationMode.Premium)
            {
                using var solution = LevelSolver.SolveComplete(level, level.MousePos, 30f, 800000, 8192);
                if (!solution.IsCreated || solution.Length == 0)
                {
                    level.Dispose();
                    continue;
                }

                if (rejectWeakLevels)
                {
                    var quality = EvaluateLevelQuality(level, solution, difficulty);
                    if (quality.Rating < GetMinimumRatingForDifficulty(difficulty, mode == GenerationMode.Challenge))
                    {
                        level.Dispose();
                        continue;
                    }
                }

                if (requireUniqueSolution && CountSolutions(level) != 1)
                {
                    level.Dispose();
                    continue;
                }
            }

            return level;
        }

        return default;
    }

    private static Level TryGenerateLevel(int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty, GenerationMode mode)
    {
        int targetDirectionalPieces = difficulty switch
        {
            Difficulty.Easy => UnityEngine.Random.Range(0, 2),
            Difficulty.Medium => UnityEngine.Random.Range(1, 3),
            Difficulty.Hard => UnityEngine.Random.Range(2, 4),
            Difficulty.SuperHard => UnityEngine.Random.Range(3, 5),
            Difficulty.Impossible => UnityEngine.Random.Range(4, 6),
            _ => 0
        };
        if (mode == GenerationMode.UltraFast) targetDirectionalPieces = 0;

        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        for (int i = 0; i < board.Length; i++) board[i] = 0;
        int2 finishPos = new int2(UnityEngine.Random.Range(0, size), UnityEngine.Random.Range(0, size));
        board[finishPos.y * size + finishPos.x] = -1;
        int directionalPiecesPlaced = 0;
        var tempPieces = new NativeList<PieceInfo>(maxPieces, Allocator.Temp);
        var tempDirectionalPieces = new NativeList<DirectionalPieceInfo>(targetDirectionalPieces, Allocator.Temp);
        var validStarts = new NativeList<int2>(4, Allocator.Temp);
        foreach (var dir in Directions)
        {
            if (IsInBounds(finishPos + dir, size)) validStarts.Add(finishPos + dir);
        }

        if (validStarts.Length == 0)
        {
            board.Dispose();
            validStarts.Dispose();
            tempPieces.Dispose();
            tempDirectionalPieces.Dispose();
            return default;
        }

        int2 start = validStarts[UnityEngine.Random.Range(0, validStarts.Length)];
        validStarts.Dispose();
        board[start.y * size + start.x] = 1;
        tempPieces.Add(new PieceInfo { Position = start, Value = 1 });
        int2 current = start, previous = start;
        int uniqueCells = 1, moves = 0;
        float preferNewProb = GetPreferNewProbability(difficulty);
        while (moves < maxMoves && uniqueCells < maxPieces)
        {
            var newCells = new NativeList<int2>(4, Allocator.Temp);
            var revisitCells = new NativeList<int2>(4, Allocator.Temp);
            foreach (var dir in Directions)
            {
                int2 n = current + dir;
                if (!IsInBounds(n, size)) continue;
                sbyte v = board[n.y * size + n.x];
                if (v == -1) continue;
                if (v == 0 && uniqueCells < maxPieces) newCells.Add(n);
                else if (v > 0 && v < numMax) revisitCells.Add(n);
            }

            if (newCells.Length == 0 && revisitCells.Length == 0)
            {
                newCells.Dispose();
                revisitCells.Dispose();
                break;
            }

            bool shouldPlaceDirectional = directionalPiecesPlaced < targetDirectionalPieces && uniqueCells > 1 &&
                                          revisitCells.Length > 0 && UnityEngine.Random.value < 0.2f;
            int2 chosen;
            bool pickNew = UnityEngine.Random.value < preferNewProb;
            if (uniqueCells < minPieces && newCells.Length > 0)
            {
                chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
            }
            else if (pickNew && newCells.Length > 0)
            {
                chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
            }
            else if (revisitCells.Length > 0)
            {
                chosen = revisitCells[UnityEngine.Random.Range(0, revisitCells.Length)];
            }
            else
            {
                chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
            }

            if (shouldPlaceDirectional && (math.abs(current.x - finishPos.x) + math.abs(current.y - finishPos.y)) == 1)
            {
                shouldPlaceDirectional = false;
            }

            if (shouldPlaceDirectional)
            {
                directionalPiecesPlaced++;
                Direction entryDir = ToDirectionFlag(GetMoveDirection(previous, current)) | GetRandomDirection();
                Direction exitDir = ToDirectionFlag(GetMoveDirection(current, chosen)) | GetRandomDirection();
                tempDirectionalPieces.Add(new DirectionalPieceInfo
                {
                    Position = current, Value = board[current.y * size + current.x], AllowedIn = entryDir,
                    AllowedOut = exitDir
                });
                for (int i = 0; i < tempPieces.Length; i++)
                {
                    if (tempPieces[i].Position.Equals(current))
                    {
                        tempPieces.RemoveAtSwapBack(i);
                        break;
                    }
                }
            }

            int chosenIdx = chosen.y * size + chosen.x;
            if (board[chosenIdx] == 0)
            {
                board[chosenIdx] = 1;
                uniqueCells++;
                tempPieces.Add(new PieceInfo { Position = chosen, Value = 1 });
            }
            else
            {
                board[chosenIdx]++;
                bool f = false;
                for (int i = 0; i < tempPieces.Length; i++)
                {
                    if (tempPieces[i].Position.Equals(chosen))
                    {
                        var p = tempPieces[i];
                        p.Value = board[chosenIdx];
                        tempPieces[i] = p;
                        f = true;
                        break;
                    }
                }

                if (!f)
                {
                    for (int i = 0; i < tempDirectionalPieces.Length; i++)
                    {
                        if (tempDirectionalPieces[i].Position.Equals(chosen))
                        {
                            var p = tempDirectionalPieces[i];
                            p.Value = board[chosenIdx];
                            tempDirectionalPieces[i] = p;
                            break;
                        }
                    }
                }
            }

            newCells.Dispose();
            revisitCells.Dispose();
            previous = current;
            current = chosen;
            moves++;
        }

        if (uniqueCells < minPieces)
        {
            board.Dispose();
            tempPieces.Dispose();
            tempDirectionalPieces.Dispose();
            return default;
        }

        var finalPieces = new NativeList<PieceInfo>(tempPieces.Length, Allocator.Persistent);
        finalPieces.CopyFrom(tempPieces);
        var finalDirectionalPieces =
            new NativeList<DirectionalPieceInfo>(tempDirectionalPieces.Length, Allocator.Persistent);
        finalDirectionalPieces.CopyFrom(tempDirectionalPieces);
        tempPieces.Dispose();
        tempDirectionalPieces.Dispose();
        board.Dispose();
        return new Level
        {
            MousePos = current, BoardSize = new int2(size, size), FinishPos = finishPos, PiecesInfo = finalPieces,
            DirectionalPiecesInfo = finalDirectionalPieces
        };
    }

    private static Level TryGenerateGuaranteedConnectedLevel(int minPieces, int maxPieces, int numMax, int maxMoves,
        int size, Difficulty difficulty, GenerationMode mode)
    {
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        for (int i = 0; i < board.Length; i++) board[i] = 0;
        int2 finishPos = new int2(UnityEngine.Random.Range(0, size), UnityEngine.Random.Range(0, size));
        board[finishPos.y * size + finishPos.x] = -1;
        var validStarts = new NativeList<int2>(4, Allocator.Temp);
        foreach (var dir in Directions)
        {
            if (IsInBounds(finishPos + dir, size)) validStarts.Add(finishPos + dir);
        }

        if (validStarts.Length == 0)
        {
            board.Dispose();
            validStarts.Dispose();
            return default;
        }

        int2 start = validStarts[UnityEngine.Random.Range(0, validStarts.Length)];
        validStarts.Dispose();
        var piecesToPlace = new NativeList<int2>(minPieces, Allocator.Temp);
        var stack = new NativeList<int2>(minPieces, Allocator.Temp);
        stack.Add(start);
        piecesToPlace.Add(start);
        board[start.y * size + start.x] = 1;
        while (piecesToPlace.Length < minPieces && stack.Length > 0)
        {
            int2 current = stack[stack.Length - 1];
            var neighbors = new NativeList<int2>(4, Allocator.Temp);
            foreach (var dir in Directions)
            {
                int2 n = current + dir;
                if (IsInBounds(n, size) && board[n.y * size + n.x] == 0) neighbors.Add(n);
            }

            if (neighbors.Length == 0)
            {
                stack.RemoveAt(stack.Length - 1);
            }
            else
            {
                int2 next = neighbors[UnityEngine.Random.Range(0, neighbors.Length)];
                board[next.y * size + next.x] = 1;
                piecesToPlace.Add(next);
                stack.Add(next);
            }

            neighbors.Dispose();
        }

        stack.Dispose();
        if (piecesToPlace.Length < minPieces)
        {
            board.Dispose();
            piecesToPlace.Dispose();
            return default;
        }

        var tempPieces = new NativeList<PieceInfo>(maxPieces, Allocator.Temp);
        foreach (var p in piecesToPlace) tempPieces.Add(new PieceInfo { Position = p, Value = 1 });
        piecesToPlace.Dispose();

        // CORREÇÃO: O switch que estava em falta foi preenchido.
        int targetDirectionalPieces = difficulty switch
        {
            Difficulty.Easy => UnityEngine.Random.Range(0, 2),
            Difficulty.Medium => UnityEngine.Random.Range(1, 3),
            Difficulty.Hard => UnityEngine.Random.Range(2, 4),
            Difficulty.SuperHard => UnityEngine.Random.Range(3, 5),
            Difficulty.Impossible => UnityEngine.Random.Range(4, 6),
            _ => 0
        };
        if (mode == GenerationMode.UltraFast) targetDirectionalPieces = 0;

        int directionalPiecesPlaced = 0;
        var tempDirectionalPieces = new NativeList<DirectionalPieceInfo>(targetDirectionalPieces, Allocator.Temp);
        int2 currentWalkPos = start, previous = start;
        int uniqueCells = tempPieces.Length;
        int moves = 0;
        float preferNewProb = GetPreferNewProbability(difficulty);
        while (moves < maxMoves && uniqueCells < maxPieces)
        {
            var newCells = new NativeList<int2>(4, Allocator.Temp);
            var revisitCells = new NativeList<int2>(4, Allocator.Temp);
            foreach (var dir in Directions)
            {
                int2 n = currentWalkPos + dir;
                if (!IsInBounds(n, size)) continue;
                sbyte v = board[n.y * size + n.x];
                if (v == -1) continue;
                if (v == 0 && uniqueCells < maxPieces) newCells.Add(n);
                else if (v > 0 && v < numMax) revisitCells.Add(n);
            }

            if (newCells.Length == 0 && revisitCells.Length == 0)
            {
                newCells.Dispose();
                revisitCells.Dispose();
                break;
            }

            bool shouldPlaceDirectional = directionalPiecesPlaced < targetDirectionalPieces && uniqueCells > 1 &&
                                          revisitCells.Length > 0 && UnityEngine.Random.value < 0.2f;
            int2 chosen;
            bool pickNew = UnityEngine.Random.value < preferNewProb;
            if (pickNew && newCells.Length > 0)
            {
                chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
            }
            else if (revisitCells.Length > 0)
            {
                chosen = revisitCells[UnityEngine.Random.Range(0, revisitCells.Length)];
            }
            else
            {
                chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
            }

            if (shouldPlaceDirectional &&
                (math.abs(currentWalkPos.x - finishPos.x) + math.abs(currentWalkPos.y - finishPos.y)) == 1)
            {
                shouldPlaceDirectional = false;
            }

            if (shouldPlaceDirectional)
            {
                directionalPiecesPlaced++;
                Direction entryDir = ToDirectionFlag(GetMoveDirection(previous, currentWalkPos)) | GetRandomDirection();
                Direction exitDir = ToDirectionFlag(GetMoveDirection(currentWalkPos, chosen)) | GetRandomDirection();
                tempDirectionalPieces.Add(new DirectionalPieceInfo
                {
                    Position = currentWalkPos, Value = board[currentWalkPos.y * size + currentWalkPos.x],
                    AllowedIn = entryDir, AllowedOut = exitDir
                });
                for (int i = 0; i < tempPieces.Length; i++)
                {
                    if (tempPieces[i].Position.Equals(currentWalkPos))
                    {
                        tempPieces.RemoveAtSwapBack(i);
                        break;
                    }
                }
            }

            int chosenIdx = chosen.y * size + chosen.x;
            if (board[chosenIdx] == 0)
            {
                board[chosenIdx] = 1;
                uniqueCells++;
                tempPieces.Add(new PieceInfo { Position = chosen, Value = 1 });
            }
            else
            {
                board[chosenIdx]++;
                bool f = false;
                for (int i = 0; i < tempPieces.Length; i++)
                {
                    if (tempPieces[i].Position.Equals(chosen))
                    {
                        var p = tempPieces[i];
                        p.Value = board[chosenIdx];
                        tempPieces[i] = p;
                        f = true;
                        break;
                    }
                }

                if (!f)
                {
                    for (int i = 0; i < tempDirectionalPieces.Length; i++)
                    {
                        if (tempDirectionalPieces[i].Position.Equals(chosen))
                        {
                            var p = tempDirectionalPieces[i];
                            p.Value = board[chosenIdx];
                            tempDirectionalPieces[i] = p;
                            break;
                        }
                    }
                }
            }

            newCells.Dispose();
            revisitCells.Dispose();
            previous = currentWalkPos;
            currentWalkPos = chosen;
            moves++;
        }

        var finalPieces = new NativeList<PieceInfo>(tempPieces.Length, Allocator.Persistent);
        finalPieces.CopyFrom(tempPieces);
        var finalDirectionalPieces =
            new NativeList<DirectionalPieceInfo>(tempDirectionalPieces.Length, Allocator.Persistent);
        finalDirectionalPieces.CopyFrom(tempDirectionalPieces);
        tempPieces.Dispose();
        tempDirectionalPieces.Dispose();
        board.Dispose();
        return new Level
        {
            MousePos = currentWalkPos, BoardSize = new int2(size, size), FinishPos = finishPos,
            PiecesInfo = finalPieces, DirectionalPiecesInfo = finalDirectionalPieces
        };
    }

    private static bool IsInBounds(int2 pos, int size) => pos.x >= 0 && pos.x < size && pos.y >= 0 && pos.y < size;

    private static Level TryGenerateNightmareLevel(int size, int numMax, int maxMoves)
    {
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        for (int i = 0; i < board.Length; i++) board[i] = 0;
        int2 finishPos = new int2(UnityEngine.Random.Range(0, size), UnityEngine.Random.Range(0, size));
        board[finishPos.y * size + finishPos.x] = -1;
        var validStarts = new NativeList<int2>(4, Allocator.Temp);
        foreach (var dir in Directions)
        {
            if (IsInBounds(finishPos + dir, size)) validStarts.Add(finishPos + dir);
        }

        if (validStarts.Length == 0)
        {
            board.Dispose();
            validStarts.Dispose();
            return default;
        }

        int2 start = validStarts[UnityEngine.Random.Range(0, validStarts.Length)];
        validStarts.Dispose();
        var queue = new NativeList<int2>(size * size, Allocator.Temp);
        var visited = new NativeArray<bool>(size * size, Allocator.Temp, NativeArrayOptions.ClearMemory);
        queue.Add(start);
        visited[start.y * size + start.x] = true;
        board[start.y * size + start.x] = 1;
        while (queue.Length > 0)
        {
            int2 current = queue[0];
            queue.RemoveAtSwapBack(0);
            var neighbors = new NativeList<int2>(4, Allocator.Temp);
            foreach (var dir in Directions)
            {
                int2 n = current + dir;
                if (IsInBounds(n, size))
                {
                    int idx = n.y * size + n.x;
                    if (!visited[idx] && board[idx] != -1) neighbors.Add(n);
                }
            }

            for (int i = 0; i < neighbors.Length; i++)
            {
                int r = UnityEngine.Random.Range(i, neighbors.Length);
                (neighbors[i], neighbors[r]) = (neighbors[r], neighbors[i]);
            }

            foreach (var n in neighbors)
            {
                int idx = n.y * size + n.x;
                if (!visited[idx])
                {
                    visited[idx] = true;
                    board[idx] = 1;
                    queue.Add(n);
                }
            }

            neighbors.Dispose();
        }

        queue.Dispose();
        visited.Dispose();
        int2 current2 = start;
        int complexityMoves = 0;
        int maxComplexityMoves = (size * size) * 2;
        while (complexityMoves < maxComplexityMoves)
        {
            var candidates = new NativeList<int2>(4, Allocator.Temp);
            sbyte minValue = (sbyte)numMax;
            foreach (var dir in Directions)
            {
                int2 n = current2 + dir;
                if (!IsInBounds(n, size)) continue;
                sbyte val = board[n.y * size + n.x];
                if (val > 0 && val < numMax)
                {
                    if (val < minValue)
                    {
                        candidates.Clear();
                        candidates.Add(n);
                        minValue = val;
                    }
                    else if (val == minValue)
                    {
                        candidates.Add(n);
                    }
                }
            }

            if (candidates.Length == 0) break;
            int2 chosen = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            board[chosen.y * size + chosen.x]++;
            current2 = chosen;
            candidates.Dispose();
            complexityMoves++;
        }

        var piecesInfo = new NativeList<PieceInfo>(size * size, Allocator.Persistent);
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] > 0)
                piecesInfo.Add(new PieceInfo { Position = new int2(i % size, i / size), Value = board[i] });
        }

        board.Dispose();
        return new Level
        {
            MousePos = current2, BoardSize = new int2(size, size), FinishPos = finishPos, PiecesInfo = piecesInfo,
            DirectionalPiecesInfo = new NativeList<DirectionalPieceInfo>(0, Allocator.Persistent)
        };
    }

    private static float GetPreferNewProbability(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 0.75f, Difficulty.Medium => 0.5f, Difficulty.Hard => 0.3f, Difficulty.SuperHard => 0.2f,
        _ => 0.15f
    };

    #region Sistema de Qualidade (Placeholders)

    public struct QualityMetrics
    {
        public int Rating;
        public bool PassesMinimumStandards;
    }

    private static QualityMetrics EvaluateLevelQuality(Level level, NativeList<MoveDirection> solution,
        Difficulty difficulty)
    {
        return new QualityMetrics { Rating = 100, PassesMinimumStandards = true };
    }

    private static int GetMinimumRatingForDifficulty(Difficulty difficulty, bool isChallenge) => isChallenge ? 50 : 30;
    private static int CountSolutions(Level level) => 1;

    #endregion

    #region Helpers Direcionais

    private static Direction ToDirectionFlag(MoveDirection moveDir) => moveDir switch
    {
        MoveDirection.Up => Direction.Up, MoveDirection.Down => Direction.Down,
        MoveDirection.Left => Direction.Left, MoveDirection.Right => Direction.Right, _ => Direction.None
    };

    private static MoveDirection GetMoveDirection(int2 from, int2 to)
    {
        int2 d = to - from;
        if (d.y == 1) return MoveDirection.Up;
        if (d.y == -1) return MoveDirection.Down;
        if (d.x == 1) return MoveDirection.Right;
        if (d.x == -1) return MoveDirection.Left;
        return MoveDirection.None;
    }

    private static Direction GetRandomDirection(Direction exclude = Direction.None)
    {
        var d = new NativeList<Direction>(4, Allocator.Temp);
        if ((exclude & Direction.Up) == 0) d.Add(Direction.Up);
        if ((exclude & Direction.Down) == 0) d.Add(Direction.Down);
        if ((exclude & Direction.Left) == 0) d.Add(Direction.Left);
        if ((exclude & Direction.Right) == 0) d.Add(Direction.Right);
        if (d.Length == 0) return Direction.None;
        var r = d[UnityEngine.Random.Range(0, d.Length)];
        d.Dispose();
        return r;
    }

    #endregion
}