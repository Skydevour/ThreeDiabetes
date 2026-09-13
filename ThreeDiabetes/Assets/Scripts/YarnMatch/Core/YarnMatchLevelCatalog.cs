using System;

public static class YarnMatchLevelCatalog
{
    public const int LevelsPerChapter = 50;
    public const int ReferencePatternLevel = 110;
    private const int MaximumPoolColumns = 12;
    private const int MaximumPoolRows = 8;
    private const int InitialPoolCells = 48;
    private const int PoolCellsPerMilestone = 10;
    private static readonly Random RoundSeeds = new Random();
    private static readonly YarnMatchTemplateDeck TemplateDeck = new YarnMatchTemplateDeck();

    internal static YarnMatchLevelConfig Generate(int level)
    {
        level = Math.Max(1, level);
        var difficulty = new YarnMatchLevelDifficulty(level);
        int template = TemplateDeck.Draw(difficulty, RoundSeeds, out int width, out int height);
        return BuildLevel(level, RoundSeeds.Next(), template, width, height);
    }

    public static int GetChapterIndex(int level) => (Math.Max(1, level) - 1) / LevelsPerChapter;
    public static int GetChapterStartLevel(int level) => GetChapterIndex(level) * LevelsPerChapter + 1;
    public static int GetChapterEndLevel(int level) => GetChapterStartLevel(level) + LevelsPerChapter - 1;
    public static int GetColorCount(int level) => Math.Min(9, 4 + (Math.Max(1, level) - 1) / 3);

    private static YarnMatchLevelConfig BuildLevel(int level, int seed, int template, int size, int rows)
    {
        GetPoolShape(level, out int poolColumns, out int poolRows);
        int colorCount = GetColorCount(level);
        GetTunnelRanges(level, out int countMin, out int countMax, out int queueMax);
        GetGeneratedMechanicCounts(level, seed, out int freezes, out int chains);
        return new YarnMatchLevelConfig(level, colorCount, poolColumns, poolRows,
            countMin, countMax, countMin > 0 ? 5 : 0, queueMax,
            freezes, chains, seed, false, Rectangle(size, rows), template);
    }

    private static void GetPoolShape(int level, out int columns, out int rows)
    {
        int maximumCells = MaximumPoolColumns * MaximumPoolRows;
        int maximumMilestone = (maximumCells - InitialPoolCells + PoolCellsPerMilestone - 1)
            / PoolCellsPerMilestone;
        int milestone = Math.Min(maximumMilestone, (level - 1) / 10);
        int targetCells = Math.Min(maximumCells, InitialPoolCells + milestone * PoolCellsPerMilestone);
        int columnsForRowLimit = (targetCells + MaximumPoolRows - 1) / MaximumPoolRows;
        columns = Math.Min(MaximumPoolColumns, Math.Max(level <= 10 ? 8 : 10, columnsForRowLimit));
        rows = (targetCells + columns - 1) / columns;
    }

    internal static YarnMatchLevelConfig GenerateSpecial()
    {
        int seed = RoundSeeds.Next();
        Random random = new Random(seed);
        return new YarnMatchLevelConfig(ReferencePatternLevel, 14,
            random.Next(11, 14), random.Next(7, 9), 20, 30, 10, 40,
            0, 0, seed, true, Rectangle(48, 40));
    }

    internal static void GetGeneratedMechanicCounts(int level, int seed, out int freezes, out int chains)
    {
        freezes = 0;
        chains = 0;
        if (level < 10) return;
        int milestone = Math.Min(3, (level - 10) / 5);
        int target = 2 + milestone * 7;
        Random random = new Random(seed ^ 1401);
        int total = random.Next(Math.Max(2, target - 3), Math.Min(23, target + 3) + 1);
        chains = random.Next(Math.Max(1, total - 15), Math.Min(8, Math.Max(1, total / 2)) + 1);
        freezes = Math.Min(15, total - chains);
    }

    private static void GetTunnelRanges(int level, out int min, out int max, out int queueMax)
    {
        min = 0;
        max = 0;
        queueMax = 0;
        if (level < 10) return;
        int stage = Math.Max(0, Math.Min(level, LevelsPerChapter) / 10 - 1);
        min = 5 + stage * 5;
        max = 15 + stage * 5;
        queueMax = 15 + stage * 5;
    }

    private static int[] Rectangle(int columns, int rows)
    {
        int[] result = new int[columns];
        for (int i = 0; i < result.Length; i++) result[i] = rows;
        return result;
    }
}
