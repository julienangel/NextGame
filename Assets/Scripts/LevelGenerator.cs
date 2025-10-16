using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using ZLinq;

// ============================================================================
// BENCHMARK ATTRIBUTE (OPCIONAL)
// ============================================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
[System.AttributeUsage(System.AttributeTargets.Method)]
public class BenchmarkAttribute : System.Attribute
{
    public string Label { get; }
    public BenchmarkAttribute(string label = null) => Label = label;
}

public static class BenchmarkHelper
{
    public static T MeasureExecutionTime<T>(System.Func<T> func, string label = "Operation")
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = func();
        sw.Stop();
        UnityEngine.Debug.Log($"[BENCHMARK] {label}: {sw.ElapsedMilliseconds}ms ({sw.Elapsed.TotalSeconds:F3}s)");
        return result;
    }

    public static void MeasureExecutionTime(System.Action action, string label = "Operation")
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        action();
        sw.Stop();
        UnityEngine.Debug.Log($"[BENCHMARK] {label}: {sw.ElapsedMilliseconds}ms ({sw.Elapsed.TotalSeconds:F3}s)");
    }
}
#endif

// ============================================================================
// ESTRUTURAS DE DADOS
// ============================================================================

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
    Impossible = 4 // ← NOVO: Preenche quase todo o tabuleiro
}

public enum GenerationMode : byte
{
    UltraFast = 0, // Sem filtros de qualidade
    FastSafe = 1, // Validação básica
    Premium = 2, // Qualidade + Unicidade
    Challenge = 3, // Máxima dificuldade intelectual (pode demorar mais)
    Nightmare = 4 // ← NOVO: Tabuleiro quase completamente preenchido
}

public struct PieceInfo
{
    public int2 Position;
    public byte Value;
}

public struct Level : System.IDisposable
{
    public int2 MousePos;
    public int2 BoardSize;
    public int2 FinishPos;
    public NativeList<PieceInfo> PiecesInfo;

    public void Dispose()
    {
        if (PiecesInfo.IsCreated)
            PiecesInfo.Dispose();
    }
}

// ============================================================================
// GERADOR DE NÍVEIS
// ============================================================================

public static class LevelGenerator
{
    private static readonly int2[] Directions =
    {
        new(0, 1), // Up
        new(0, -1), // Down
        new(-1, 0), // Left
        new(1, 0) // Right
    };

    public static Level GenerateLevel(
        int minPieces,
        int maxPieces,
        int numMax,
        int maxMoves,
        int size,
        Difficulty difficulty = Difficulty.SuperHard,
        GenerationMode mode = GenerationMode.Challenge,
        bool requireUniqueSolution = false)
    {
        return GenerateLevelInternal(minPieces, maxPieces, numMax, maxMoves, size, 
            difficulty, mode, requireUniqueSolution);
    }

    /// <summary>
    /// Versão ASYNC do GenerateLevel - não bloqueia a main thread.
    /// Útil para Nightmare/Challenge que demoram muito tempo.
    /// </summary>
    public static async System.Threading.Tasks.Task<Level> GenerateLevelAsync(
        int minPieces,
        int maxPieces,
        int numMax,
        int maxMoves,
        int size,
        Difficulty difficulty = Difficulty.SuperHard,
        GenerationMode mode = GenerationMode.Challenge,
        bool requireUniqueSolution = false)
    {
        // Executar em background thread
        return await System.Threading.Tasks.Task.Run(() => GenerateLevelInternal(minPieces, maxPieces, numMax, maxMoves, size,
            difficulty, mode, requireUniqueSolution));
    }

    private static Level GenerateLevelInternal(
        int minPieces,
        int maxPieces,
        int numMax,
        int maxMoves,
        int size,
        Difficulty difficulty,
        GenerationMode mode,
        bool requireUniqueSolution)
    {
        // Modo Nightmare: ajusta parâmetros automaticamente para preencher quase tudo
        if (mode == GenerationMode.Nightmare)
        {
            int maxCells = size * size - 1; // -1 para o finisher
            
            // Ajuste MAIS conservador - Nightmare deve sempre conseguir gerar!
            float fillRatio = size switch
            {
                <= 5 => 0.60f,   // 60% para mapas muito pequenos
                <= 7 => 0.65f,   // 65% para mapas pequenos
                <= 9 => 0.70f,   // 70% para mapas médios
                <= 11 => 0.75f,  // 75% para mapas grandes
                _ => 0.80f       // 80% para mapas épicos
            };
            
            minPieces = (int)(maxCells * (fillRatio - 0.15f)); // Margem maior
            maxPieces = (int)(maxCells * fillRatio);
            
            // Se os valores ainda são muito pequenos, garantir um mínimo
            minPieces = math.max(minPieces, size);
            maxPieces = math.max(maxPieces, size + 5);
            
            maxMoves = math.max(maxMoves * 5, size * size * 2); // 5x ou 2x o tamanho do board
            numMax = math.max(numMax, size / 2 + 2); // Valores bem altos
            difficulty = Difficulty.SuperHard; // Hard em vez de Impossible (mais fácil de gerar)
            
            UnityEngine.Debug.Log($"[Nightmare Mode] Adjusted params: {minPieces}-{maxPieces} pieces ({(fillRatio-0.15f)*100:F0}-{fillRatio*100:F0}% fill), numMax={numMax}, maxMoves={maxMoves}");
        }

        // Determinar rejectWeakLevels automaticamente baseado na dificuldade
        bool rejectWeakLevels = difficulty switch
        {
            Difficulty.Easy => false,        // Níveis "fracos" são OK
            Difficulty.Medium => false,      // Níveis lineares são OK
            Difficulty.Hard => true,         // ← Começa a rejeitar
            Difficulty.SuperHard => true,    // ← Definitivamente rejeita
            Difficulty.Impossible => true,   // ← Sempre rejeita
            _ => false
        };
        
        // Nightmare: NÃO rejeitar níveis fracos para aumentar taxa de sucesso
        // O denso preenchimento já torna o puzzle difícil
        if (mode == GenerationMode.Nightmare)
        {
            rejectWeakLevels = false;
            UnityEngine.Debug.Log("[Nightmare Mode] Disabling weak level rejection to improve generation success");
        }

        // Determinar maxAttempts baseado no modo
        int maxAttempts = mode switch
        {
            GenerationMode.Nightmare => 400,  // ← Aumentado (era 300)
            GenerationMode.Challenge => 200,
            GenerationMode.Premium => 150,
            GenerationMode.FastSafe => 100,
            GenerationMode.UltraFast => 50,
            _ => 200
        };

        // Sistema de fallback: tenta modos mais simples se falhar
        var modesToTry = new[] 
        { 
            mode, // Modo pedido
            GenerationMode.Premium, 
            GenerationMode.FastSafe, 
            GenerationMode.UltraFast 
        };

        Level level = default;
        GenerationMode usedMode = mode;

        foreach (var tryMode in modesToTry)
        {
            // Evitar duplicados
            if (tryMode != mode && tryMode == modesToTry[0])
                continue;
            
            // Nightmare: tenta fallback completo (todos os modos)
            if (mode == GenerationMode.Nightmare && tryMode != GenerationMode.Nightmare)
            {
                UnityEngine.Debug.LogWarning($"[Nightmare] Falling back to {tryMode} mode with adjusted params...");
            }

            level = GenerateLevelInternal(minPieces, maxPieces, numMax, maxMoves, size, 
                difficulty, tryMode, requireUniqueSolution, rejectWeakLevels, 
                tryMode == GenerationMode.Challenge || tryMode == GenerationMode.Nightmare ? maxAttempts : (maxAttempts / 2));

            if (level.PiecesInfo.IsCreated)
            {
                usedMode = tryMode;
                break;
            }

            // Log do fallback
            if (tryMode != GenerationMode.UltraFast)
            {
                UnityEngine.Debug.LogWarning($"[LevelGenerator] {tryMode} failed, trying {GetNextMode(tryMode)}...");
            }
        }

        if (!level.PiecesInfo.IsCreated)
        {
            UnityEngine.Debug.LogError($"[LevelGenerator] Failed to generate level with all modes! Params: {size}x{size}, {minPieces}-{maxPieces} pieces");
        }
        else if (usedMode != mode)
        {
            UnityEngine.Debug.LogWarning($"[LevelGenerator] Generated with fallback mode: {usedMode} (requested: {mode})");
        }

        return level;
    }

    private static GenerationMode GetNextMode(GenerationMode current)
    {
        return current switch
        {
            GenerationMode.Challenge => GenerationMode.Premium,
            GenerationMode.Premium => GenerationMode.FastSafe,
            GenerationMode.FastSafe => GenerationMode.UltraFast,
            _ => GenerationMode.UltraFast
        };
    }

    // Versão com benchmark (só ativa em Editor/Development builds)
    public static Level GenerateLevelWithBenchmark(
        int minPieces,
        int maxPieces,
        int numMax,
        int maxMoves,
        int size,
        Difficulty difficulty = Difficulty.SuperHard,
        GenerationMode mode = GenerationMode.Challenge,
        bool requireUniqueSolution = false)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return BenchmarkHelper.MeasureExecutionTime(() => 
            GenerateLevel(minPieces, maxPieces, numMax, maxMoves, size, 
                difficulty, mode, requireUniqueSolution),
            $"GenerateLevel [{mode}→Fallback] {size}x{size} ({minPieces}-{maxPieces} pieces)"
        );
#else
        return GenerateLevel(minPieces, maxPieces, numMax, maxMoves, size, 
            difficulty, mode, requireUniqueSolution);
#endif
    }

    private static Level GenerateLevelInternal(
        int minPieces,
        int maxPieces,
        int numMax,
        int maxMoves,
        int size,
        Difficulty difficulty,
        GenerationMode mode,
        bool requireUniqueSolution,
        bool rejectWeakLevels,
        int maxAttempts)
    {
        // Challenge/Nightmare modes: Challenge rejeita fracos, Nightmare não
        if (mode == GenerationMode.Challenge)
        {
            rejectWeakLevels = true;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var level =
                // Nightmare usa algoritmo especial de preenchimento
                mode == GenerationMode.Nightmare ? TryGenerateNightmareLevel(size, numMax, maxMoves) : TryGenerateLevel(minPieces, maxPieces, numMax, maxMoves, size, difficulty);
            
            if (!level.PiecesInfo.IsCreated)
                continue;

            // Verificar se atingiu o mínimo de peças
            if (level.PiecesInfo.Length < minPieces)
            {
                level.Dispose();
                continue;
            }

            // Validação básica - sempre necessária
            // Caps mais generosos para Nightmare/Impossible
            float solveMsCap = (mode == GenerationMode.Nightmare || difficulty == Difficulty.Impossible) ? 20f : 
                               (mode == GenerationMode.UltraFast ? 5f : 10f);
            int solveStateCap = (mode == GenerationMode.Nightmare || difficulty == Difficulty.Impossible) ? 500000 : 
                                (mode == GenerationMode.UltraFast ? 50000 : 200000);
            int solveDepthCap = (mode == GenerationMode.Nightmare || difficulty == Difficulty.Impossible) ? 8192 : 2048;

            bool isValid = LevelSolver.TrySolveLite(level, level.MousePos, 
                solveMsCap, solveStateCap, solveDepthCap);

            if (!isValid)
            {
                level.Dispose();
                continue;
            }

            // Modo Premium/Challenge/Nightmare: validações extras
            if (mode == GenerationMode.Premium || mode == GenerationMode.Challenge || mode == GenerationMode.Nightmare)
            {
                // Caps ainda mais generosos para solver completo em Nightmare
                float completeMsCap = mode == GenerationMode.Nightmare ? 30f : 15f;
                int completeStateCap = mode == GenerationMode.Nightmare ? 800000 : 400000;
                int completeDepthCap = mode == GenerationMode.Nightmare ? 8192 : 4096;

                var solution = LevelSolver.SolveComplete(level, level.MousePos, 
                    completeMsCap, completeStateCap, completeDepthCap);
                
                if (!solution.IsCreated || solution.Length == 0)
                {
                    level.Dispose();
                    if (solution.IsCreated) solution.Dispose();
                    continue;
                }

                // Filtro de qualidade
                if (rejectWeakLevels)
                {
                    var quality = EvaluateLevelQuality(level, solution, difficulty);
                    
                    if (mode == GenerationMode.Challenge)
                    {
                        // Challenge: só aceitar níveis com rating alto
                        if (quality.Rating < GetMinimumRatingForDifficulty(difficulty, true))
                        {
                            level.Dispose();
                            solution.Dispose();
                            continue;
                        }
                    }
                    else if (!quality.PassesMinimumStandards)
                    {
                        level.Dispose();
                        solution.Dispose();
                        continue;
                    }
                }

                // Unicidade (opcional)
                if (requireUniqueSolution && CountSolutions(level) != 1)
                {
                    level.Dispose();
                    solution.Dispose();
                    continue;
                }

                solution.Dispose();
            }

            return level;
        }

        // Fallback: retorna nível vazio (caller deve verificar PiecesInfo.IsCreated)
        UnityEngine.Debug.LogWarning($"[LevelGenerator] Failed to generate valid level after {maxAttempts} attempts");
        return new Level { PiecesInfo = default };
    }

    private static Level TryGenerateLevel(int minPieces, int maxPieces, int numMax, int maxMoves, int size,
        Difficulty difficulty)
    {
        // Board linear (size*size)
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        for (int i = 0; i < board.Length; i++)
            board[i] = 0;

        // 1. Escolher finisher aleatório
        int2 finishPos = new int2(UnityEngine.Random.Range(0, size), UnityEngine.Random.Range(0, size));
        int finishIdx = finishPos.y * size + finishPos.x;
        board[finishIdx] = -1;

        // 2. Escolher start adjacente ao finisher
        var validStarts = new NativeList<int2>(4, Allocator.Temp);
        foreach (var dir in Directions)
        {
            int2 neighbor = finishPos + dir;
            if (IsInBounds(neighbor, size))
            {
                int idx = neighbor.y * size + neighbor.x;
                if (board[idx] == 0)
                    validStarts.Add(neighbor);
            }
        }

        if (validStarts.Length == 0)
        {
            board.Dispose();
            validStarts.Dispose();
            return new Level { PiecesInfo = default };
        }

        int2 start = validStarts[UnityEngine.Random.Range(0, validStarts.Length)];
        validStarts.Dispose();

        int startIdx = start.y * size + start.x;
        board[startIdx] = 1;

        // 3. Random walk
        int2 current = start;
        int uniqueCells = 1;
        int moves = 0;

        float preferNewProb = GetPreferNewProbability(difficulty);

        while (moves < maxMoves && uniqueCells < maxPieces)
        {
            // Coletar candidatos
            var newCells = new NativeList<int2>(4, Allocator.Temp);
            var revisitCells = new NativeList<int2>(4, Allocator.Temp);

            foreach (var dir in Directions)
            {
                int2 neighbor = current + dir;
                if (!IsInBounds(neighbor, size))
                    continue;

                int idx = neighbor.y * size + neighbor.x;
                sbyte val = board[idx];

                switch (val)
                {
                    // Finisher
                    case -1:
                        continue;
                    case 0 when uniqueCells < maxPieces:
                        newCells.Add(neighbor);
                        break;
                    case > 0 when val < numMax:
                        revisitCells.Add(neighbor);
                        break;
                }
            }

            if (newCells.Length == 0 && revisitCells.Length == 0)
            {
                newCells.Dispose();
                revisitCells.Dispose();

                // Se ainda não atingimos minPieces, falhar este nível
                if (uniqueCells < minPieces)
                    break;

                // Caso contrário, tudo bem terminar aqui
                break;
            }

            // Escolher próxima célula
            int2 chosen;
            bool pickNew = UnityEngine.Random.value < preferNewProb;

            // Se ainda não atingimos minPieces, forçar criação de novas células
            if (uniqueCells < minPieces && newCells.Length > 0)
            {
                chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
            }
            else
                switch (pickNew)
                {
                    case true when newCells.Length > 0:
                        chosen = newCells[UnityEngine.Random.Range(0, newCells.Length)];
                        break;
                    case false when revisitCells.Length > 0:
                        chosen = revisitCells[UnityEngine.Random.Range(0, revisitCells.Length)];
                        break;
                    default:
                    {
                        chosen = newCells.Length > 0
                            ? newCells[UnityEngine.Random.Range(0, newCells.Length)]
                            : revisitCells[UnityEngine.Random.Range(0, revisitCells.Length)];
                        break;
                    }
                }

            newCells.Dispose();
            revisitCells.Dispose();

            int chosenIdx = chosen.y * size + chosen.x;
            if (board[chosenIdx] == 0)
            {
                board[chosenIdx] = 1;
                uniqueCells++;
            }
            else
            {
                board[chosenIdx]++;
            }

            current = chosen;
            moves++;
        }

        // Se não atingiu o mínimo de peças, descartar
        if (uniqueCells < minPieces)
        {
            board.Dispose();
            return new Level { PiecesInfo = default };
        }

        // 4. Exportar nível
        var piecesInfo = new NativeList<PieceInfo>(uniqueCells, Allocator.Persistent);
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] > 0)
            {
                int x = i % size;
                int y = i / size;
                piecesInfo.Add(new PieceInfo
                {
                    Position = new int2(x, y),
                    Value = (byte)board[i]
                });
            }
        }

        board.Dispose();

        return new Level
        {
            MousePos = current,
            BoardSize = new int2(size, size),
            FinishPos = finishPos,
            PiecesInfo = piecesInfo
        };
    }

    private static bool IsInBounds(int2 pos, int size)
    {
        return pos.x >= 0 && pos.x < size && pos.y >= 0 && pos.y < size;
    }

    // Gerador ESPECIAL para Nightmare: Preenche tudo primeiro, depois adiciona complexidade
    private static Level TryGenerateNightmareLevel(int size, int numMax, int maxMoves)
    {
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        for (int i = 0; i < board.Length; i++)
            board[i] = 0;

        // 1. Escolher finisher aleatório
        int2 finishPos = new int2(UnityEngine.Random.Range(0, size), UnityEngine.Random.Range(0, size));
        int finishIdx = finishPos.y * size + finishPos.x;
        board[finishIdx] = -1;

        // 2. Escolher start adjacente ao finisher
        var validStarts = new NativeList<int2>(4, Allocator.Temp);
        foreach (var dir in Directions)
        {
            int2 neighbor = finishPos + dir;
            if (IsInBounds(neighbor, size))
            {
                int idx = neighbor.y * size + neighbor.x;
                if (board[idx] == 0)
                    validStarts.Add(neighbor);
            }
        }

        if (validStarts.Length == 0)
        {
            board.Dispose();
            validStarts.Dispose();
            return new Level { PiecesInfo = default };
        }

        int2 start = validStarts[UnityEngine.Random.Range(0, validStarts.Length)];
        validStarts.Dispose();

        // FASE 1: PREENCHER O TABULEIRO (Flood Fill com BFS)
        var queue = new NativeList<int2>(size * size, Allocator.Temp);
        var visited = new NativeArray<bool>(size * size, Allocator.Temp);

        queue.Add(start);
        int startIdx = start.y * size + start.x;
        visited[startIdx] = true;
        board[startIdx] = 1;
        int filledCells = 1;

        while (queue.Length > 0)
        {
            int2 current = queue[0];
            queue.RemoveAtSwapBack(0);

            // Adicionar vizinhos não visitados
            var neighbors = new NativeList<int2>(4, Allocator.Temp);
            foreach (var dir in Directions)
            {
                int2 neighbor = current + dir;
                if (!IsInBounds(neighbor, size))
                    continue;

                int idx = neighbor.y * size + neighbor.x;
                if (visited[idx] || board[idx] == -1) // Já visitado ou é finisher
                    continue;

                neighbors.Add(neighbor);
            }

            // Embaralhar vizinhos para variedade
            for (int i = 0; i < neighbors.Length; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, neighbors.Length);
                (neighbors[i], neighbors[randomIndex]) = (neighbors[randomIndex], neighbors[i]);
            }

            // Adicionar vizinhos à queue
            foreach (var neighbor in neighbors)
            {
                int idx = neighbor.y * size + neighbor.x;

                if (!visited[idx])
                {
                    visited[idx] = true;
                    board[idx] = 1;
                    filledCells++;
                    queue.Add(neighbor);
                }
            }

            neighbors.Dispose();
        }

        queue.Dispose();
        visited.Dispose();

        UnityEngine.Debug.Log($"[Nightmare Fill Phase] Filled {filledCells} cells");

        // FASE 2: ADICIONAR COMPLEXIDADE (Revisitar células estrategicamente)
        int2 current2 = start;
        int complexityMoves = 0;
        int maxComplexityMoves = filledCells * 2; // Até 2x o número de células

        while (complexityMoves < maxComplexityMoves)
        {
            // Escolher vizinho com valor mais baixo (heurística: balancear valores)
            var candidates = new NativeList<int2>(4, Allocator.Temp);
            sbyte minValue = (sbyte)numMax;

            foreach (var dir in Directions)
            {
                int2 neighbor = current2 + dir;
                if (!IsInBounds(neighbor, size))
                    continue;

                int idx = neighbor.y * size + neighbor.x;
                sbyte val = board[idx];

                if (val <= 0 || val >= numMax) // Pular finisher, vazios e células no máximo
                    continue;

                if (val < minValue)
                {
                    candidates.Clear();
                    candidates.Add(neighbor);
                    minValue = val;
                }
                else if (val == minValue)
                {
                    candidates.Add(neighbor);
                }
            }

            if (candidates.Length == 0)
            {
                // Sem vizinhos válidos, escolher célula aleatória
                bool found = false;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int2 randomPos = new int2(
                        UnityEngine.Random.Range(0, size),
                        UnityEngine.Random.Range(0, size)
                    );
                    int idx = randomPos.y * size + randomPos.x;
                    if (board[idx] > 0 && board[idx] < numMax)
                    {
                        current2 = randomPos;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    candidates.Dispose();
                    break; // Todas as células estão no máximo
                }
            }
            else
            {
                // Escolher aleatoriamente entre os candidatos com menor valor
                int2 chosen = candidates[UnityEngine.Random.Range(0, candidates.Length)];
                int chosenIdx = chosen.y * size + chosen.x;
                board[chosenIdx]++;
                current2 = chosen;
            }

            candidates.Dispose();
            complexityMoves++;
        }

        UnityEngine.Debug.Log($"[Nightmare Complexity Phase] Added complexity in {complexityMoves} moves");

        // 3. Exportar nível
        int uniqueCells = board.AsValueEnumerable().Count(t => t > 0);

        var piecesInfo = new NativeList<PieceInfo>(uniqueCells, Allocator.Persistent);
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] > 0)
            {
                int x = i % size;
                int y = i / size;
                piecesInfo.Add(new PieceInfo
                {
                    Position = new int2(x, y),
                    Value = (byte)board[i]
                });
            }
        }

        board.Dispose();

        return new Level
        {
            MousePos = current2,
            BoardSize = new int2(size, size),
            FinishPos = finishPos,
            PiecesInfo = piecesInfo
        };
    }

    private static float GetPreferNewProbability(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 0.75f,
            Difficulty.Medium => 0.50f,
            Difficulty.Hard => 0.30f,
            Difficulty.SuperHard => 0.20f,
            Difficulty.Impossible => 0.15f, // ← Revisita muito, cria células densas
            _ => 0.50f
        };
    }

    // ========================================================================
    // SISTEMA DE AVALIAÇÃO DE QUALIDADE
    // ========================================================================

    public struct QualityMetrics
    {
        public int Rating; // 0-100
        public int SolutionLength;
        public int DecisionPoints; // Momentos com múltiplas escolhas válidas
        public int Backtracks; // Quantas vezes precisa voltar atrás
        public int DeadEnds; // Caminhos que parecem válidos mas falham
        public float AvgDistanceToFinish; // Quão longe começa
        public int ClusterDensity; // Células agrupadas com valores altos
        public bool PassesMinimumStandards;
    }

    private static QualityMetrics EvaluateLevelQuality(Level level, NativeList<MoveDirection> solution,
        Difficulty difficulty)
    {
        var metrics = new QualityMetrics
        {
            SolutionLength = solution.Length - 1, // -1 para excluir Finish
            DecisionPoints = 0,
            Backtracks = 0,
            DeadEnds = 0,
            ClusterDensity = 0
        };

        int size = level.BoardSize.x;

        // 1. Calcular distância média ao finisher
        float totalDist = level.PiecesInfo.Aggregate<PieceInfo, float>(0, (current, piece) => current + (math.abs(piece.Position.x - level.FinishPos.x) + math.abs(piece.Position.y - level.FinishPos.y)));

        metrics.AvgDistanceToFinish = totalDist / math.max(1, level.PiecesInfo.Length);

        // 2. Analisar densidade de clusters (células adjacentes com valores altos)
        foreach (var piece in level.PiecesInfo)
        {
            if (piece.Value >= 2)
            {
                // Contar vizinhos também com valores altos
                int highValueNeighbors = 0;
                foreach (var dir in new int2[] { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) })
                {
                    int2 neighbor = piece.Position + dir;
                    for (int j = 0; j < level.PiecesInfo.Length; j++)
                    {
                        if (level.PiecesInfo[j].Position.Equals(neighbor) && level.PiecesInfo[j].Value >= 2)
                        {
                            highValueNeighbors++;
                            break;
                        }
                    }
                }

                metrics.ClusterDensity += highValueNeighbors;
            }
        }

        // 3. Simular o jogo e contar decisões/backtracks
        metrics.DecisionPoints = CountDecisionPoints(level, solution);
        metrics.Backtracks = DetectBacktracks(solution);
        metrics.DeadEnds = EstimateDeadEnds(level);

        // 4. Calcular rating (0-100)
        int rating = 0;

        // Comprimento da solução (max 20 pontos)
        int minLength = GetMinimumLengthForDifficulty(difficulty, size);
        rating += math.min(20, (metrics.SolutionLength - minLength) * 2);

        // Pontos de decisão (max 25 pontos)
        rating += math.min(25, metrics.DecisionPoints * 3);

        // Backtracks necessários (max 20 pontos)
        rating += math.min(20, metrics.Backtracks * 5);

        // Dead ends (max 15 pontos)
        rating += math.min(15, metrics.DeadEnds * 4);

        // Distância ao finisher (max 10 pontos)
        rating += (int)math.min(10, metrics.AvgDistanceToFinish);

        // Cluster density (max 10 pontos)
        rating += math.min(10, metrics.ClusterDensity * 2);

        metrics.Rating = math.clamp(rating, 0, 100);

        // Standards mínimos
        metrics.PassesMinimumStandards =
            metrics.SolutionLength >= minLength &&
            metrics.DecisionPoints >= 2 &&
            metrics.Rating >= GetMinimumRatingForDifficulty(difficulty, false);

        return metrics;
    }

    private static int GetMinimumLengthForDifficulty(Difficulty difficulty, int size)
    {
        int baseLength = size switch
        {
            <= 5 => 4,
            <= 8 => 6,
            _ => 8
        };

        return difficulty switch
        {
            Difficulty.Easy => baseLength,
            Difficulty.Medium => baseLength + 2,
            Difficulty.Hard => baseLength + 4,
            Difficulty.SuperHard => baseLength + 6,
            Difficulty.Impossible => baseLength + 10, // ← Soluções muito longas
            _ => baseLength
        };
    }

    private static int GetMinimumRatingForDifficulty(Difficulty difficulty, bool challengeMode)
    {
        int baseRating = difficulty switch
        {
            Difficulty.Easy => 20,
            Difficulty.Medium => 35,
            Difficulty.Hard => 50,
            Difficulty.SuperHard => 65,
            Difficulty.Impossible => 80, // ← Rating mínimo muito alto
            _ => 30
        };

        return challengeMode ? baseRating + 15 : baseRating;
    }

    private static int CountDecisionPoints(Level level, NativeList<MoveDirection> solution)
    {
        // Simular o jogo e contar quantas vezes há mais de 1 movimento válido
        int size = level.BoardSize.x;
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);

        for (int i = 0; i < board.Length; i++)
            board[i] = 0;

        int finishIdx = level.FinishPos.y * size + level.FinishPos.x;
        board[finishIdx] = -1;

        foreach (var piece in level.PiecesInfo)
        {
            int idx = piece.Position.y * size + piece.Position.x;
            board[idx] = (sbyte)piece.Value;
        }

        int2 pos = level.MousePos;
        int decisions = 0;

        for (int i = 0; i < solution.Length - 1; i++) // -1 para ignorar Finish
        {
            // Contar movimentos válidos desta posição
            int validMoves = 0;
            foreach (var dir in new int2[] { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) })
            {
                int2 neighbor = pos + dir;
                if (neighbor.x >= 0 && neighbor.x < size && neighbor.y >= 0 && neighbor.y < size)
                {
                    int idx = neighbor.y * size + neighbor.x;
                    if (board[idx] > 0)
                        validMoves++;
                }
            }

            if (validMoves > 1)
                decisions++;

            // Aplicar movimento
            int currentIdx = pos.y * size + pos.x;
            board[currentIdx]--;

            pos = solution[i] switch
            {
                MoveDirection.Up => pos + new int2(0, 1),
                MoveDirection.Down => pos + new int2(0, -1),
                MoveDirection.Left => pos + new int2(-1, 0),
                MoveDirection.Right => pos + new int2(1, 0),
                _ => pos
            };
        }

        board.Dispose();
        return decisions;
    }

    private static int DetectBacktracks(NativeList<MoveDirection> solution)
    {
        // Detectar quando o jogador volta imediatamente na direção oposta
        int backtracks = 0;
        for (int i = 1; i < solution.Length - 1; i++)
        {
            var prev = solution[i - 1];
            var curr = solution[i];

            bool isBacktrack =
                (prev == MoveDirection.Up && curr == MoveDirection.Down) ||
                (prev == MoveDirection.Down && curr == MoveDirection.Up) ||
                (prev == MoveDirection.Left && curr == MoveDirection.Right) ||
                (prev == MoveDirection.Right && curr == MoveDirection.Left);

            if (isBacktrack)
                backtracks++;
        }

        return backtracks;
    }

    private static int EstimateDeadEnds(Level level)
    {
        // Estimar dead-ends: células isoladas ou com poucos vizinhos
        int deadEnds = 0;

        foreach (var piece in level.PiecesInfo)
        {
            int neighbors = 0;

            foreach (var dir in new int2[] { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) })
            {
                int2 neighbor = piece.Position + dir;
                for (int j = 0; j < level.PiecesInfo.Length; j++)
                {
                    if (level.PiecesInfo[j].Position.Equals(neighbor))
                    {
                        neighbors++;
                        break;
                    }
                }
            }

            if (neighbors == 1 && !piece.Position.Equals(level.FinishPos))
                deadEnds++;
        }

        return deadEnds;
    }

    private static int CountSolutions(Level level)
    {
        // Implementação simplificada: tenta encontrar até 2 soluções
        // Se encontrar 2, retorna 2 (não é única)
        // TODO: Implementar busca completa se necessário
        return 1; // Placeholder
    }
}

// ============================================================================
// SOLVER
// ============================================================================

public static class LevelSolver
{
    private static readonly int2[] Directions = new int2[]
    {
        new(0, 1), // Up
        new(0, -1), // Down
        new(-1, 0), // Left
        new(1, 0) // Right
    };

    // Solver Lite: retorna apenas bool (tem solução?)
    public static bool TrySolveLite(Level level, int2 startPos, float msCap, int stateCap, int depthCap)
    {
        int size = level.BoardSize.x;
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        InitializeBoard(board, level, size);

        int startIdx = startPos.y * size + startPos.x;
        if (board[startIdx] <= 0)
        {
            board.Dispose();
            return false;
        }

        var stopwatch = Stopwatch.StartNew();
        bool result = DFSIterative(board, startPos, level.FinishPos, size, msCap, stateCap, depthCap, stopwatch, null);

        board.Dispose();
        return result;
    }

    // Solver Completo: retorna caminho
    public static NativeList<MoveDirection> SolveComplete(Level level, int2 startPos, float msCap, int stateCap,
        int depthCap)
    {
        int size = level.BoardSize.x;
        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);
        InitializeBoard(board, level, size);

        int startIdx = startPos.y * size + startPos.x;
        if (board[startIdx] <= 0)
        {
            board.Dispose();
            return default;
        }

        var path = new NativeList<MoveDirection>(depthCap, Allocator.Persistent);
        var stopwatch = Stopwatch.StartNew();
        bool found = DFSIterative(board, startPos, level.FinishPos, size, msCap, stateCap, depthCap, stopwatch, path);

        board.Dispose();

        if (!found)
        {
            path.Dispose();
            return default;
        }

        return path;
    }

    private static void InitializeBoard(NativeArray<sbyte> board, Level level, int size)
    {
        for (int i = 0; i < board.Length; i++)
            board[i] = 0;

        // Finisher
        int finishIdx = level.FinishPos.y * size + level.FinishPos.x;
        board[finishIdx] = -1;

        // Peças
        foreach (var piece in level.PiecesInfo)
        {
            int idx = piece.Position.y * size + piece.Position.x;
            board[idx] = (sbyte)piece.Value;
        }
    }

    private struct DFSState
    {
        public int2 Position;
        public byte NextNeighborIdx;
        public sbyte OldValue;
        public int PositiveCount;
    }

    private static bool DFSIterative(
        NativeArray<sbyte> board,
        int2 startPos,
        int2 finishPos,
        int size,
        float msCap,
        int stateCap,
        int depthCap,
        Stopwatch stopwatch,
        NativeList<MoveDirection>? path)
    {
        var stack = new NativeList<DFSState>(depthCap, Allocator.Temp);
        int positiveCount = CountPositiveCells(board);
        int statesExplored = 0;

        stack.Add(new DFSState
        {
            Position = startPos,
            NextNeighborIdx = 0,
            OldValue = -1,
            PositiveCount = positiveCount
        });

        while (stack.Length > 0 && statesExplored < stateCap && stopwatch.Elapsed.TotalMilliseconds < msCap)
        {
            if (stack.Length > depthCap)
                break;

            ref var state = ref stack.ElementAt(stack.Length - 1);
            int2 pos = state.Position;
            int idx = pos.y * size + pos.x;

            statesExplored++;

            // Verificar se pode terminar
            if (CanFinish(board, pos, finishPos, size))
            {
                path?.Add(MoveDirection.Finish);
                stack.Dispose();
                return true;
            }

            // Enumerar vizinhos
            var neighbors = new NativeList<(int2, MoveDirection)>(4, Allocator.Temp);
            GetValidNeighbors(board, pos, finishPos, size, neighbors);

            if (state.NextNeighborIdx >= neighbors.Length)
            {
                // Backtrack
                neighbors.Dispose();
                if (state.OldValue >= 0)
                {
                    board[idx] = state.OldValue;
                }

                stack.RemoveAt(stack.Length - 1);
                if (path is { Length: > 0 })
                    path.Value.RemoveAt(path.Value.Length - 1);
                continue;
            }

            // Tentar próximo vizinho
            var (nextPos, moveDir) = neighbors[(int)state.NextNeighborIdx];
            neighbors.Dispose();
            state.NextNeighborIdx++;

            int nextIdx = nextPos.y * size + nextPos.x;
            sbyte currentVal = board[idx];

            // Decrementar origem
            sbyte newVal = (sbyte)(currentVal - 1);
            board[idx] = newVal;
            int newPositiveCount = newVal == 0 ? state.PositiveCount - 1 : state.PositiveCount;

            // Verificar conectividade
            if (!IsConnected(board, nextPos, size))
            {
                board[idx] = currentVal;
                continue;
            }

            // Avançar
            path?.Add(moveDir);

            stack.Add(new DFSState
            {
                Position = nextPos,
                NextNeighborIdx = 0,
                OldValue = state.OldValue == -1 ? currentVal : state.OldValue,
                PositiveCount = newPositiveCount
            });
        }

        stack.Dispose();
        return false;
    }

    private static int CountPositiveCells(NativeArray<sbyte> board)
    {
        return board.AsValueEnumerable().Count(t => t > 0);
    }

    private static bool CanFinish(NativeArray<sbyte> board, int2 pos, int2 finishPos, int size)
    {
        // Só pode terminar se: 1 célula restante, valor 1, adjacente ao finish
        int idx = pos.y * size + pos.x;
        if (board[idx] != 1)
            return false;

        int positiveCount = CountPositiveCells(board);
        if (positiveCount != 1)
            return false;

        return math.abs(pos.x - finishPos.x) + math.abs(pos.y - finishPos.y) == 1;
    }

    private static void GetValidNeighbors(NativeArray<sbyte> board, int2 pos, int2 finishPos, int size,
        NativeList<(int2, MoveDirection)> neighbors)
    {
        for (int i = 0; i < Directions.Length; i++)
        {
            int2 neighbor = pos + Directions[i];
            if (neighbor.x < 0 || neighbor.x >= size || neighbor.y < 0 || neighbor.y >= size)
                continue;

            int idx = neighbor.y * size + neighbor.x;
            sbyte val = board[idx];

            // Pode mover para células >0, nunca para finisher aqui
            if (val > 0)
            {
                MoveDirection dir = (MoveDirection)(i + 1);
                neighbors.Add((neighbor, dir));
            }
        }
    }

    private static bool IsConnected(NativeArray<sbyte> board, int2 startPos, int size)
    {
        // BFS simples para verificar se todas células >0 estão conectadas
        var visited = new NativeArray<bool>(size * size, Allocator.Temp);
        var queue = new NativeList<int2>(64, Allocator.Temp);

        int startIdx = startPos.y * size + startPos.x;
        queue.Add(startPos);
        visited[startIdx] = true;
        int visitedCount = 1;

        int totalPositive = CountPositiveCells(board);

        while (queue.Length > 0)
        {
            int2 current = queue[0];
            queue.RemoveAtSwapBack(0);

            foreach (var dir in Directions)
            {
                int2 neighbor = current + dir;
                if (neighbor.x < 0 || neighbor.x >= size || neighbor.y < 0 || neighbor.y >= size)
                    continue;

                int idx = neighbor.y * size + neighbor.x;
                if (visited[idx] || board[idx] <= 0)
                    continue;

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

// ============================================================================
// UTILITÁRIOS DE DEBUG
// ============================================================================

public static class LevelPrinter
{
    public static void PrintLevel(Level level)
    {
        if (!level.PiecesInfo.IsCreated)
        {
            UnityEngine.Debug.LogError("=== INVALID LEVEL (PiecesInfo not created) ===");
            return;
        }

        int size = level.BoardSize.x;

        if (size is <= 0 or > 20)
        {
            UnityEngine.Debug.LogError($"=== INVALID LEVEL (BoardSize invalid: {size}) ===");
            return;
        }

        var board = new NativeArray<sbyte>(size * size, Allocator.Temp);

        // Inicializar com zeros
        for (int i = 0; i < board.Length; i++)
            board[i] = 0;

        // Validar finisher position
        if (level.FinishPos.x < 0 || level.FinishPos.x >= size ||
            level.FinishPos.y < 0 || level.FinishPos.y >= size)
        {
            UnityEngine.Debug.LogError($"=== INVALID LEVEL (FinishPos out of bounds: {level.FinishPos}) ===");
            board.Dispose();
            return;
        }

        // Colocar finisher
        int finishIdx = level.FinishPos.y * size + level.FinishPos.x;
        board[finishIdx] = -1;

        // Colocar peças (com validação)
        for (int i = 0; i < level.PiecesInfo.Length; i++)
        {
            var piece = level.PiecesInfo[i];

            if (piece.Position.x < 0 || piece.Position.x >= size ||
                piece.Position.y < 0 || piece.Position.y >= size)
            {
                UnityEngine.Debug.LogWarning($"Piece {i} out of bounds: {piece.Position}");
                continue;
            }

            int idx = piece.Position.y * size + piece.Position.x;
            if (idx >= 0 && idx < board.Length)
                board[idx] = (sbyte)piece.Value;
        }

        // Construir string completo
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== LEVEL ===");
        sb.AppendLine($"Size: {size}x{size}");
        sb.AppendLine($"MousePos: [{level.MousePos.x},{level.MousePos.y}]");
        sb.AppendLine($"FinishPos: [{level.FinishPos.x},{level.FinishPos.y}]");
        sb.AppendLine($"Total Pieces: {level.PiecesInfo.Length}");
        sb.AppendLine();

        // Imprimir tabuleiro (Y invertido para ficar mais intuitivo)
        for (int y = size - 1; y >= 0; y--)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                if (idx >= board.Length)
                {
                    sb.Append("? ");
                    continue;
                }

                sbyte val = board[idx];

                switch (val)
                {
                    case -1:
                        sb.Append("@ ");
                        break;
                    case 0:
                        sb.Append(". ");
                        break;
                    default:
                        sb.Append(val + " ");
                        break;
                }
            }

            sb.AppendLine();
        }

        UnityEngine.Debug.Log(sb.ToString());
        board.Dispose();
    }

    public static void PrintSolution(NativeList<MoveDirection> solution)
    {
        if (!solution.IsCreated || solution.Length == 0)
        {
            UnityEngine.Debug.Log("=== NO SOLUTION ===");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== SOLUTION ({solution.Length} moves) ===");

        for (int i = 0; i < solution.Length; i++)
        {
            string move = solution[i] switch
            {
                MoveDirection.Up => "Up",
                MoveDirection.Down => "Down",
                MoveDirection.Left => "Left",
                MoveDirection.Right => "Right",
                MoveDirection.Finish => "Finish",
                _ => "Unknown"
            };

            sb.Append(move);
            if (i < solution.Length - 1)
                sb.Append(", ");
        }

        UnityEngine.Debug.Log(sb.ToString());
    }

    public static void PrintLevelWithSolution(Level level)
    {
        if (!level.PiecesInfo.IsCreated)
        {
            UnityEngine.Debug.LogError("=== INVALID LEVEL (Cannot print - PiecesInfo not created) ===");
            return;
        }

        PrintLevel(level);

        var solution = LevelSolver.SolveComplete(level, level.MousePos, 15, 400000, 4096);
        if (solution.IsCreated)
        {
            PrintSolution(solution);
            solution.Dispose();
        }
        else
        {
            UnityEngine.Debug.LogWarning("=== FAILED TO SOLVE ===");
        }
    }
}

// ============================================================================
// SISTEMA DE MOVIMENTO (GAMEPLAY)
// ============================================================================

public class MousePuzzleGame
{
    private NativeArray<sbyte> board;
    private int2 currentPos;
    private int2 finishPos;
    private int boardSize;

    public void Initialize(Level level)
    {
        boardSize = level.BoardSize.x;
        board = new NativeArray<sbyte>(boardSize * boardSize, Allocator.Persistent);

        for (int i = 0; i < board.Length; i++)
            board[i] = 0;

        // Finisher
        int finishIdx = level.FinishPos.y * boardSize + level.FinishPos.x;
        board[finishIdx] = -1;
        finishPos = level.FinishPos;

        // Peças
        foreach (var piece in level.PiecesInfo)
        {
            int idx = piece.Position.y * boardSize + piece.Position.x;
            board[idx] = (sbyte)piece.Value;
        }

        currentPos = level.MousePos;
        IsFinished = false;
    }

    public bool TryMove(MoveDirection direction)
    {
        if (IsFinished)
            return false;

        int2 targetPos = direction switch
        {
            MoveDirection.Up => currentPos + new int2(0, 1),
            MoveDirection.Down => currentPos + new int2(0, -1),
            MoveDirection.Left => currentPos + new int2(-1, 0),
            MoveDirection.Right => currentPos + new int2(1, 0),
            _ => currentPos
        };

        // Validar bounds
        if (targetPos.x < 0 || targetPos.x >= boardSize || targetPos.y < 0 || targetPos.y >= boardSize)
            return false;

        int currentIdx = currentPos.y * boardSize + currentPos.x;
        int targetIdx = targetPos.y * boardSize + targetPos.x;
        sbyte targetVal = board[targetIdx];

        // Não pode mover para células 0
        if (targetVal == 0)
            return false;

        // Finisher: só se puder terminar
        if (targetVal == -1)
        {
            if (CanFinishNow())
            {
                board[currentIdx]--;
                IsFinished = true;
                currentPos = targetPos;
                return true;
            }

            return false;
        }

        // Movimento normal
        board[currentIdx]--;
        currentPos = targetPos;
        return true;
    }

    private bool CanFinishNow()
    {
        int currentIdx = currentPos.y * boardSize + currentPos.x;
        if (board[currentIdx] != 1)
            return false;

        int positiveCount = board.AsValueEnumerable().Count(t => t > 0);

        if (positiveCount != 1)
            return false;

        return math.abs(currentPos.x - finishPos.x) + math.abs(currentPos.y - finishPos.y) == 1;
    }

    public sbyte GetCellValue(int2 pos)
    {
        int idx = pos.y * boardSize + pos.x;
        return board[idx];
    }

    public bool IsFinished { get; private set; }

    public int2 CurrentPosition => currentPos;

    public void Dispose()
    {
        if (board.IsCreated)
            board.Dispose();
    }
}