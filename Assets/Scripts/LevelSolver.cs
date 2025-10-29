// ============================================================================
// SOLVER - BFS Simples e Correto
// ============================================================================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class LevelSolver
{
    // Estado do jogo num momento específico
    private class GameState
    {
        public Position MousePos;
        public int[] PieceValues; // Array com valores atuais das peças
        public List<Direction> Moves; // Caminho até aqui
        
        public GameState(Position mousePos, int[] values, List<Direction> moves)
        {
            MousePos = mousePos;
            PieceValues = values;
            Moves = moves;
        }
        
        // Hash para detectar estados repetidos
        public long GetHash()
        {
            long hash = ((long)MousePos.X << 32) | (uint)MousePos.Y;
            for (int i = 0; i < PieceValues.Length; i++)
            {
                hash ^= ((long)PieceValues[i] << (i * 4));
            }
            return hash;
        }
    }
    
    public static List<Direction> Solve(Level level)
    {
        // Fila para BFS
        Queue<GameState> queue = new Queue<GameState>();
        HashSet<long> visited = new HashSet<long>();
        
        // Estado inicial (SEM MODIFICAÇÕES!)
        int[] initialValues = new int[level.PiecesInfo.Length];
        for (int i = 0; i < level.PiecesInfo.Length; i++)
        {
            initialValues[i] = level.PiecesInfo[i].Value;
        }
        
        GameState initial = new GameState(level.MousePos, initialValues, new List<Direction>());
        queue.Enqueue(initial);
        visited.Add(initial.GetHash());
        
        // BFS
        while (queue.Count > 0)
        {
            GameState current = queue.Dequeue();
            
            // Limite de segurança
            if (current.Moves.Count >= 100)
                continue;
            
            // Tentar as 4 direções (incluindo movimento para finish)
            TryMove(current, Direction.Up, level, queue, visited);
            TryMove(current, Direction.Down, level, queue, visited);
            TryMove(current, Direction.Left, level, queue, visited);
            TryMove(current, Direction.Right, level, queue, visited);
        }
        
        // Sem solução
        return new List<Direction>();
    }
    
    private static void TryMove(GameState current, Direction dir, Level level, 
        Queue<GameState> queue, HashSet<long> visited)
    {
        // Calcular nova posição
        Position newPos = GetNewPosition(current.MousePos, dir);
        
        // Verificar limites do board
        if (newPos.X < 0 || newPos.X >= level.BoardSize || 
            newPos.Y < 0 || newPos.Y >= level.BoardSize)
            return;
        
        // Verificar se é o finish
        bool movingToFinish = newPos.Equals(level.FinishPos);
        
        if (movingToFinish)
        {
            // Para mover para finish: mouse deve estar em peça com value 1
            // e todas outras peças devem estar a 0
            int currentPieceIndex = FindPieceIndex(current.MousePos, level.PiecesInfo);
            if (currentPieceIndex == -1 || current.PieceValues[currentPieceIndex] != 1)
                return;
            
            // Verificar se todas outras peças estão a 0
            for (int i = 0; i < current.PieceValues.Length; i++)
            {
                if (i != currentPieceIndex && current.PieceValues[i] > 0)
                    return;
            }
            
            // VITÓRIA! Criar estado final
            int[] finalValues = new int[current.PieceValues.Length];
            Array.Copy(current.PieceValues, finalValues, current.PieceValues.Length);
            finalValues[currentPieceIndex] = 0; // Decrementar ao sair
            
            List<Direction> winMoves = new List<Direction>(current.Moves) { dir };

            GameState winState = new GameState(newPos, finalValues, winMoves);
            queue.Clear(); // Limpar fila
            queue.Enqueue(winState); // Adicionar estado de vitória
            
            // Verificar condição de vitória final
            if (IsWinState(winState, level))
            {
                // Retornar através de exception (hack para sair do BFS)
                throw new SolutionFoundException(winState.Moves);
            }
            return;
        }
        
        // Movimento normal para outra peça
        
        // Verificar se há peça válida na nova posição
        int targetPieceIndex = FindPieceIndex(newPos, level.PiecesInfo);
        if (targetPieceIndex == -1)
            return;
        
        // A peça de destino DEVE ter value > 0
        if (current.PieceValues[targetPieceIndex] <= 0)
            return;
        
        // Criar novo estado
        int[] newValues = new int[current.PieceValues.Length];
        Array.Copy(current.PieceValues, newValues, current.PieceValues.Length);
        
        // SEMPRE decrementar a peça de onde estamos a sair
        int currentPieceIndex2 = FindPieceIndex(current.MousePos, level.PiecesInfo);
        if (currentPieceIndex2 != -1)
        {
            newValues[currentPieceIndex2]--;
        }
        
        List<Direction> newMoves = new List<Direction>(current.Moves) { dir };

        GameState newState = new GameState(newPos, newValues, newMoves);
        long hash = newState.GetHash();
        
        // Adicionar à fila se ainda não visitamos este estado
        if (visited.Add(hash))
        {
            queue.Enqueue(newState);
        }
    }
    
    private static bool IsWinState(GameState state, Level level)
    {
        // Mouse está no finish?
        if (!state.MousePos.Equals(level.FinishPos))
            return false;
        
        // Todas as peças estão a 0?
        foreach (var pieceValue in state.PieceValues)
        {
            if (pieceValue > 0)
                return false;
        }
        
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindPieceIndex(Position pos, PieceInfo[] pieces)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].Position.Equals(pos))
                return i;
        }
        return -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Position GetNewPosition(Position pos, Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return new Position(pos.X, pos.Y + 1);
            case Direction.Down: return new Position(pos.X, pos.Y - 1);
            case Direction.Left: return new Position(pos.X - 1, pos.Y);
            case Direction.Right: return new Position(pos.X + 1, pos.Y);
            default: return pos;
        }
    }
    
    // Exception para sair do BFS quando encontrar solução
    private class SolutionFoundException : Exception
    {
        public List<Direction> Solution { get; }
        
        public SolutionFoundException(List<Direction> solution)
        {
            Solution = solution;
        }
    }
    
    // Wrapper público que captura a exception
    public static List<Direction> SolveWithException(Level level)
    {
        try
        {
            Solve(level);
            return new List<Direction>(); // Sem solução
        }
        catch (SolutionFoundException ex)
        {
            return ex.Solution;
        }
    }
}