using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Text; // Necessário para StringBuilder

struct CellDisplay
{
    public char ValueChar;
    public char InChar;
    public char OutChar;
    public bool IsDirectional;
}

public static class LevelPrinter
{
    // Mapeamento de flags de Direção para caracteres de seta Unicode
    private static char GetArrowChar(Direction dir)
    {
        bool up = (dir & Direction.Up) != 0;
        bool down = (dir & Direction.Down) != 0;
        bool left = (dir & Direction.Left) != 0;
        bool right = (dir & Direction.Right) != 0;

        // Prioridade para combinações comuns (desenham melhor)
        if (up && down && left && right) return '╬'; // Todas
        if (up && down && left) return '╣';
        if (up && down && right) return '╠';
        if (up && left && right) return '╩';
        if (down && left && right) return '╦';
        if (up && down) return '║';
        if (left && right) return '═';
        if (up && right) return '╚';
        if (up && left) return '╝';
        if (down && right) return '╔';
        if (down && left) return '╗';
        // Direções únicas
        if (up) return '↑';
        if (down) return '↓';
        if (left) return '←';
        if (right) return '→';
        if (dir == Direction.None) return 'X'; // Nenhuma direção (útil para AllowedOut do Finisher)
        return '?'; // Caso inesperado
    }

    public static void PrintLevel(Level level)
    {
        // Validações básicas
        if (level.BoardSize.x <= 0 || level.BoardSize.y <= 0)
        {
            UnityEngine.Debug.LogError("=== INVALID LEVEL (BoardSize invalid) ===");
            return;
        }

        if (!level.PiecesInfo.IsCreated || !level.DirectionalPiecesInfo.IsCreated)
        {
            UnityEngine.Debug.LogError("=== INVALID LEVEL (Piece lists not created/initialized) ===");
            return;
        }

        int size = level.BoardSize.x;
        // Estrutura temporária para guardar a informação de display de cada célula

        // Usar Allocator.Temp pois este array só existe durante a execução desta função
        var displayBoard = new NativeArray<CellDisplay>(size * size, Allocator.Temp);

        // 1. Inicializa o tabuleiro de display com células vazias
        for (int i = 0; i < displayBoard.Length; i++)
            displayBoard[i] = new CellDisplay { ValueChar = '.', InChar = ' ', OutChar = ' ', IsDirectional = false };

        // 2. Coloca o Finisher
        if (!IsInBounds(level.FinishPos, size))
        {
            Debug.LogError($"=== INVALID LEVEL (FinishPos out of bounds: {level.FinishPos}) ===");
            displayBoard.Dispose();
            return;
        }

        int finishIdx = level.FinishPos.y * size + level.FinishPos.x;
        displayBoard[finishIdx] = new CellDisplay { ValueChar = '@', InChar = ' ', OutChar = ' ' };

        // 3. Coloca as Peças Normais
        foreach (var piece in level.PiecesInfo)
        {
            if (!IsInBounds(piece.Position, size))
            {
                Debug.LogWarning($"Normal Piece out of bounds: {piece.Position}");
                continue;
            }

            int idx = piece.Position.y * size + piece.Position.x;
            if (idx == finishIdx)
            {
                Debug.LogWarning($"Overlap at Finisher Position {piece.Position}");
                continue;
            } // Não sobrescrever o finisher

            if (displayBoard[idx].ValueChar != '.')
            {
                Debug.LogWarning($"Overlap at {piece.Position}");
            } // Avisa se houver sobreposição

            // Assume que valores são 1-9. Se forem maiores, pode truncar.
            displayBoard[idx] = new CellDisplay { ValueChar = piece.Value.ToString()[0], InChar = ' ', OutChar = ' ' };
        }

        // 4. Coloca as Peças Direcionais
        foreach (var piece in level.DirectionalPiecesInfo)
        {
            if (!IsInBounds(piece.Position, size))
            {
                Debug.LogWarning($"Directional Piece out of bounds: {piece.Position}");
                continue;
            }

            int idx = piece.Position.y * size + piece.Position.x;
            if (idx == finishIdx)
            {
                Debug.LogWarning($"Overlap at Finisher Position {piece.Position}");
                continue;
            }

            if (displayBoard[idx].ValueChar != '.')
            {
                Debug.LogWarning($"Overlap at {piece.Position}");
            }

            // Assume que valores são 1-9.
            displayBoard[idx] = new CellDisplay
            {
                ValueChar = piece.Value.ToString()[0],
                InChar = GetArrowChar(piece.AllowedIn),
                OutChar = GetArrowChar(piece.AllowedOut),
                IsDirectional = true
            };
        }

        // 5. Constrói a string de saída
        var sb = new StringBuilder();
        sb.AppendLine($"=== LEVEL ({size}x{size}) ===");
        sb.AppendLine(
            $"Start: [{level.MousePos.x},{level.MousePos.y}] Finish: [{level.FinishPos.x},{level.FinishPos.y}]");
        sb.AppendLine($"Pieces: {level.PiecesInfo.Length} normal, {level.DirectionalPiecesInfo.Length} directional");
        sb.AppendLine();
        sb.AppendLine("Legenda:");
        sb.AppendLine("  .  : Vazio");
        sb.AppendLine("  @  : Finish");
        sb.AppendLine(" #   : Peça Normal (Valor #)"); // Centralizado
        sb.AppendLine("#←║ : Peça Direcional (Valor #, Entrada ←, Saída ║)"); // Exemplo
        sb.AppendLine(" --- Tabuleiro (Y Invertido) ---");

        // Desenha o tabuleiro linha a linha, de cima para baixo
        for (int y = size - 1; y >= 0; y--)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                var cell = displayBoard[idx];

                if (cell.IsDirectional)
                {
                    // Formato: Valor + Seta Entrada + Seta Saída + Espaço
                    sb.Append($"{cell.ValueChar}{cell.InChar}{cell.OutChar} ");
                }
                else if (cell.ValueChar == '@')
                {
                    sb.Append($" @  "); // Arroba centralizado
                }
                else if (cell.ValueChar == '.')
                {
                    sb.Append($" .  "); // Ponto centralizado
                }
                else // Peça normal
                {
                    sb.Append($" {cell.ValueChar}  "); // Valor centralizado
                }
            }

            sb.AppendLine(); // Fim da linha
        }

        // Linha inferior para delimitar
        sb.Append("----");
        for (int i = 0; i < size; i++) sb.Append("----"); // Adapta ao tamanho
        sb.AppendLine();

        // 6. Imprime na consola e liberta a memória temporária
        UnityEngine.Debug.Log(sb.ToString());
        displayBoard.Dispose();
    }

    private static bool IsInBounds(int2 pos, int size)
    {
        return pos.x >= 0 && pos.x < size && pos.y >= 0 && pos.y < size;
    }

    // --- Funções Auxiliares (Opcionais) ---

    public static void PrintSolution(NativeList<MoveDirection> solution)
    {
        if (!solution.IsCreated || solution.Length == 0)
        {
            UnityEngine.Debug.Log("=== NO SOLUTION FOUND ===");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"=== SOLUTION ({solution.Length} moves) ===");
        for (int i = 0; i < solution.Length; i++)
        {
            sb.Append(solution[i].ToString());
            if (i < solution.Length - 1) sb.Append(" -> ");
        }

        UnityEngine.Debug.Log(sb.ToString());
    }

    public static void PrintLevelWithSolution(Level level)
    {
        if (!level.PiecesInfo.IsCreated || !level.DirectionalPiecesInfo.IsCreated)
        {
            UnityEngine.Debug.LogError("=== INVALID LEVEL (Cannot print - Piece lists not created) ===");
            return;
        }

        PrintLevel(level); // Usa a nova função de impressão

        // Tenta resolver e imprimir a solução
        var solution =
            LevelSolver.SolveComplete(level, level.MousePos, 30f, 800000, 8192); // Ajuste os limites se necessário
        if (solution.IsCreated)
        {
            PrintSolution(solution);
            solution.Dispose();
        }
        else
        {
            UnityEngine.Debug.LogWarning("=== FAILED TO SOLVE (for printing solution) ===");
        }
    }
}