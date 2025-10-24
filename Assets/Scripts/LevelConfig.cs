using UnityEngine;

public static class LevelConfig
{
    // Configuration for how levels scale with level number
    public static LevelParameters GetParametersForLevel(int levelNumber)
    {
        // Scale difficulty based on level number
        int tier = (levelNumber - 1) / 10; // Every 10 levels increases tier

        // Base parameters that scale up
        int baseSize = Mathf.Clamp(5 + (tier / 2), 5, 11);
        int minPieces = Mathf.Clamp(8 + (tier * 2), 8, 30);
        int maxPieces = Mathf.Clamp(15 + (tier * 3), 15, 50);
        int numMax = Mathf.Clamp(3 + (tier / 2), 3, 9);
        int maxMoves = Mathf.Clamp(20 + (tier * 5), 20, 100);

        // Difficulty progression
        Difficulty difficulty = tier switch
        {
            0 => Difficulty.Easy,
            1 => Difficulty.Medium,
            2 => Difficulty.Hard,
            3 => Difficulty.SuperHard,
            _ => Difficulty.Impossible
        };

        // Generation mode - use faster modes for early levels
        GenerationMode mode = levelNumber <= 5 ? GenerationMode.FastSafe : GenerationMode.Premium;

        return new LevelParameters
        {
            MinPieces = minPieces,
            MaxPieces = maxPieces,
            NumMax = numMax,
            MaxMoves = maxMoves,
            Size = baseSize,
            Difficulty = difficulty,
            Mode = mode,
            RequireUniqueSolution = false
        };
    }
}

public struct LevelParameters
{
    public int MinPieces;
    public int MaxPieces;
    public int NumMax;
    public int MaxMoves;
    public int Size;
    public Difficulty Difficulty;
    public GenerationMode Mode;
    public bool RequireUniqueSolution;
}
