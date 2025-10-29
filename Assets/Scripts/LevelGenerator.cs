using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class LevelGenerator
{
    private static Random _random = new();
    
    private static readonly int[] _neighborBuffer = new int[4];
    private static readonly Dictionary<long, int> _pieceDict = new(256);

    public static void SetSeed(int seed)
    {
        _random = new Random(seed);
    }

    public static Level GenerateLevel(
        int minPieces, 
        int maxPieces, 
        int numMax, 
        int maxMoves, 
        int size, 
        bool useDirectionalPieces,
        bool avoidBacktracking = true,
        int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Level level = TryGenerateLevel(minPieces, maxPieces, numMax, maxMoves, size, 
                useDirectionalPieces, avoidBacktracking);
            
            // Validar se o nível gerado é aceitável
            if (IsValidLevel(level, minPieces))
            {
                return level;
            }
        }
        
        // Fallback: retornar último nível gerado mesmo que não seja ideal
        return TryGenerateLevel(minPieces, maxPieces, numMax, maxMoves, size, 
            useDirectionalPieces, avoidBacktracking);
    }

    private static Level TryGenerateLevel(
        int minPieces, 
        int maxPieces, 
        int numMax, 
        int maxMoves, 
        int size, 
        bool useDirectionalPieces,
        bool avoidBacktracking)
    {
        Level level = new Level(size, maxPieces);
        _pieceDict.Clear();
        
        // 1. Escolher posição do Finish
        level.FinishPos = new Position(_random.Next(size), _random.Next(size));
        
        // 2. Escolher célula adjacente ao Finish como ponto de partida
        int neighborCount = GetValidNeighbors(level.FinishPos, size, _neighborBuffer);
        if (neighborCount == 0)
        {
            level.FinishPos = new Position(size / 2, size / 2);
            neighborCount = GetValidNeighbors(level.FinishPos, size, _neighborBuffer);
        }
        
        Position startPos = UnpackPosition(_neighborBuffer[_random.Next(neighborCount)]);
        
        // 3. Dicionário de peças usando long como chave (packed position)
        long startKey = PackPosition(startPos);
        _pieceDict[startKey] = 1;
        
        // 4. Random Walk
        Position currentPos = startPos;
        byte lastDir = 255; // Inválido
        int moveCount = 0;
        int targetPieces = _random.Next(minPieces, maxPieces + 1);
        
        Span<int> validNeighbors = stackalloc int[4];
        
        while (moveCount < maxMoves && _pieceDict.Count < targetPieces)
        {
            int validCount = GetValidNeighborsForWalk(
                currentPos, 
                size, 
                level.FinishPos, 
                lastDir, 
                avoidBacktracking,
                validNeighbors);
            
            if (validCount == 0)
                break;
            
            // Escolher próximo movimento
            Position nextPos = ChooseNextPosition(validNeighbors.Slice(0, validCount), numMax);
            long nextKey = PackPosition(nextPos);
            
            // Atualizar ou criar peça
            if (_pieceDict.TryGetValue(nextKey, out int val))
            {
                if (val < numMax)
                    _pieceDict[nextKey] = val + 1;
            }
            else
            {
                _pieceDict[nextKey] = 1;
            }
            
            lastDir = (byte)GetDirection(currentPos, nextPos);
            currentPos = nextPos;
            moveCount++;
        }
        
        // 5. Posição inicial do mouse
        level.MousePos = currentPos;
        
        // 6. Converter dicionário para array de PieceInfo
        int pieceIndex = 0;
        foreach (var kvp in _pieceDict)
        {
            level.PiecesInfo[pieceIndex++] = new PieceInfo(
                UnpackPosition(kvp.Key), 
                kvp.Value);
        }
        
        // Redimensionar array para tamanho exato
        Array.Resize(ref level.PiecesInfo, pieceIndex);
        
        // 7. Crop do board para remover espaços vazios
        level = CropLevel(level);
        
        // 8. Aplicar peças direcionais (se solicitado)
        if (useDirectionalPieces && level.PiecesInfo.Length >= 3)
        {
            // TODO: Implementar peças direcionais no futuro
            // Por enquanto mantém array vazio
        }
        
        return level;
    }

    private static Level CropLevel(Level level)
    {
        if (level.PiecesInfo.Length == 0)
            return level;
        
        // Encontrar bounds (min/max) de todas as peças + mouse + finish
        int minX = level.BoardSize;
        int maxX = 0;
        int minY = level.BoardSize;
        int maxY = 0;
        
        // Verificar peças
        for (int i = 0; i < level.PiecesInfo.Length; i++)
        {
            Position pos = level.PiecesInfo[i].Position;
            if (pos.X < minX) minX = pos.X;
            if (pos.X > maxX) maxX = pos.X;
            if (pos.Y < minY) minY = pos.Y;
            if (pos.Y > maxY) maxY = pos.Y;
        }
        
        // Verificar mouse position
        if (level.MousePos.X < minX) minX = level.MousePos.X;
        if (level.MousePos.X > maxX) maxX = level.MousePos.X;
        if (level.MousePos.Y < minY) minY = level.MousePos.Y;
        if (level.MousePos.Y > maxY) maxY = level.MousePos.Y;
        
        // Verificar finish position
        if (level.FinishPos.X < minX) minX = level.FinishPos.X;
        if (level.FinishPos.X > maxX) maxX = level.FinishPos.X;
        if (level.FinishPos.Y < minY) minY = level.FinishPos.Y;
        if (level.FinishPos.Y > maxY) maxY = level.FinishPos.Y;
        
        // Adicionar margem de 1 célula (opcional, ajustável)
        int margin = 1;
        minX = Math.Max(0, minX - margin);
        minY = Math.Max(0, minY - margin);
        maxX = Math.Min(level.BoardSize - 1, maxX + margin);
        maxY = Math.Min(level.BoardSize - 1, maxY + margin);
        
        // Calcular novo tamanho do board
        int newWidth = maxX - minX + 1;
        int newHeight = maxY - minY + 1;
        sbyte newSize = (sbyte)Math.Max(newWidth, newHeight);
        
        // Se não há mudança significativa, retornar original
        if (newSize >= level.BoardSize - 2)
            return level;
        
        // Criar novo level com tamanho ajustado
        Level croppedLevel = new Level(newSize, level.PiecesInfo.Length);
        
        // Ajustar posições das peças
        for (int i = 0; i < level.PiecesInfo.Length; i++)
        {
            croppedLevel.PiecesInfo[i] = new PieceInfo(
                new Position(
                    level.PiecesInfo[i].Position.X - minX,
                    level.PiecesInfo[i].Position.Y - minY
                ),
                level.PiecesInfo[i].Value
            );
        }
        
        // Ajustar mouse position
        croppedLevel.MousePos = new Position(
            level.MousePos.X - minX,
            level.MousePos.Y - minY
        );
        
        // Ajustar finish position
        croppedLevel.FinishPos = new Position(
            level.FinishPos.X - minX,
            level.FinishPos.Y - minY
        );
        
        // Redimensionar array para tamanho exato
        Array.Resize(ref croppedLevel.PiecesInfo, level.PiecesInfo.Length);
        
        // Copiar directional pieces (quando implementado)
        croppedLevel.DirectionalPiecesInfo = level.DirectionalPiecesInfo;
        
        return croppedLevel;
    }

    private static bool IsValidLevel(Level level, int minPieces)
    {
        // Verificar se tem peças suficientes
        if (level.PiecesInfo.Length < minPieces)
            return false;
        
        // Verificar se mouse não está no finish
        if (level.MousePos.Equals(level.FinishPos))
            return false;
        
        // Verificar se mouse está numa peça válida
        bool mouseOnPiece = false;
        for (int i = 0; i < level.PiecesInfo.Length; i++)
        {
            if (level.PiecesInfo[i].Position.Equals(level.MousePos) && level.PiecesInfo[i].Value > 0)
            {
                mouseOnPiece = true;
                break;
            }
        }
        
        if (!mouseOnPiece)
            return false;
        
        // Verificar se finish tem pelo menos um vizinho com peça
        bool hasNeighborPiece = false;
        Span<Position> neighbors = stackalloc Position[4];
        int neighborCount = 0;
        
        if (level.FinishPos.Y > 0) 
            neighbors[neighborCount++] = new Position(level.FinishPos.X, level.FinishPos.Y - 1);
        if (level.FinishPos.Y < level.BoardSize - 1) 
            neighbors[neighborCount++] = new Position(level.FinishPos.X, level.FinishPos.Y + 1);
        if (level.FinishPos.X > 0) 
            neighbors[neighborCount++] = new Position(level.FinishPos.X - 1, level.FinishPos.Y);
        if (level.FinishPos.X < level.BoardSize - 1) 
            neighbors[neighborCount++] = new Position(level.FinishPos.X + 1, level.FinishPos.Y);
        
        for (int i = 0; i < neighborCount; i++)
        {
            for (int j = 0; j < level.PiecesInfo.Length; j++)
            {
                if (level.PiecesInfo[j].Position.Equals(neighbors[i]))
                {
                    hasNeighborPiece = true;
                    break;
                }
            }
            if (hasNeighborPiece) break;
        }
        
        return hasNeighborPiece;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long PackPosition(Position pos)
    {
        return ((long)pos.X << 32) | (uint)pos.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Position UnpackPosition(long packed)
    {
        return new Position((int)(packed >> 32), (int)(packed & 0xFFFFFFFF));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Position UnpackPosition(int packed)
    {
        return new Position(packed >> 16, packed & 0xFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PackPositionInt(Position pos)
    {
        return (pos.X << 16) | (pos.Y & 0xFFFF);
    }

    private static int GetValidNeighbors(Position pos, int size, int[] buffer)
    {
        int count = 0;
        
        if (pos.Y > 0) 
            buffer[count++] = PackPositionInt(new Position(pos.X, pos.Y - 1));
        if (pos.Y < size - 1) 
            buffer[count++] = PackPositionInt(new Position(pos.X, pos.Y + 1));
        if (pos.X > 0) 
            buffer[count++] = PackPositionInt(new Position(pos.X - 1, pos.Y));
        if (pos.X < size - 1) 
            buffer[count++] = PackPositionInt(new Position(pos.X + 1, pos.Y));
        
        return count;
    }

    private static int GetValidNeighborsForWalk(
        Position pos, 
        int size, 
        Position finishPos, 
        byte lastDirection, 
        bool avoidBacktracking,
        Span<int> buffer)
    {
        int count = 0;
        
        // Up
        var backtracingChance = 0.60;
        if (pos.Y > 0)
        {
            Position n = new Position(pos.X, pos.Y - 1);
            // Permitir backtrack 35% das vezes para criar mais loops
            bool allowMove = !n.Equals(finishPos) && 
                           (!avoidBacktracking || lastDirection == 255 || lastDirection != 1 || _random.NextDouble() < backtracingChance);
            if (allowMove)
                buffer[count++] = PackPositionInt(n);
        }
        // Down
        if (pos.Y < size - 1)
        {
            Position n = new Position(pos.X, pos.Y + 1);
            bool allowMove = !n.Equals(finishPos) && 
                           (!avoidBacktracking || lastDirection == 255 || lastDirection != 0 || _random.NextDouble() < backtracingChance);
            if (allowMove)
                buffer[count++] = PackPositionInt(n);
        }
        // Left
        if (pos.X > 0)
        {
            Position n = new Position(pos.X - 1, pos.Y);
            bool allowMove = !n.Equals(finishPos) && 
                           (!avoidBacktracking || lastDirection == 255 || lastDirection != 3 || _random.NextDouble() < backtracingChance);
            if (allowMove)
                buffer[count++] = PackPositionInt(n);
        }
        // Right
        if (pos.X < size - 1)
        {
            Position n = new Position(pos.X + 1, pos.Y);
            bool allowMove = !n.Equals(finishPos) && 
                           (!avoidBacktracking || lastDirection == 255 || lastDirection != 2 || _random.NextDouble() < backtracingChance);
            if (allowMove)
                buffer[count++] = PackPositionInt(n);
        }
        
        return count;
    }

    private static Position ChooseNextPosition(Span<int> candidates, int numMax)
    {
        bool chooseNew = _random.NextDouble() < 0.15;
        
        int newCount = 0;
        int existingCount = 0;
        Span<int> newCells = stackalloc int[4];
        Span<int> existingCells = stackalloc int[4];
        
        foreach (var candidate in candidates)
        {
            long key = PackPosition(UnpackPosition(candidate));
            if (_pieceDict.TryGetValue(key, out int val))
            {
                if (val < numMax)
                    existingCells[existingCount++] = candidate;
            }
            else
            {
                newCells[newCount++] = candidate;
            }
        }
        
        if (chooseNew && newCount > 0)
            return UnpackPosition(newCells[_random.Next(newCount)]);
        
        if (existingCount > 0)
            return UnpackPosition(existingCells[_random.Next(existingCount)]);
        
        if (newCount > 0)
            return UnpackPosition(newCells[_random.Next(newCount)]);
        
        return UnpackPosition(candidates[_random.Next(candidates.Length)]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Direction GetDirection(Position from, Position to)
    {
        if (to.Y < from.Y) return Direction.Up;
        if (to.Y > from.Y) return Direction.Down;
        if (to.X < from.X) return Direction.Left;
        return Direction.Right;
    }
}