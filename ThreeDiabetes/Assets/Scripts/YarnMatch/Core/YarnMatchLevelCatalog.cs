using System;
using System.Collections.Generic;

public static class YarnMatchLevelCatalog
{
    private const int InitialPoolCellCount = 48;
    private const int InitialPoolColumns = 8;
    private const int ExpandedPoolColumns = 10;
    private const int PoolCellsAddedPerMilestone = 10;
    private const int FirstBoardSize = 5;
    private const int MinimumNormalColorCount = 4;
    private const int MaximumNormalColorCount = 9;
    public const int ReferencePatternLevel = 110;
    private const int ReferencePatternColumns = 48;
    private const int ReferencePatternRows = 40;
    private const int ReferencePoolColumns = 12;
    private const int ReferencePoolRows = 7;
    private const int ReferenceTunnelCountMin = 20;
    private const int ReferenceTunnelCountMax = 30;
    private const int ReferenceTunnelQueueMin = 10;
    private const int ReferenceTunnelQueueMax = 40;
    public const int LevelsPerChapter = 50;
    private static readonly Dictionary<int, IReadOnlyList<YarnMatchLevelConfig>> ChapterCache = new Dictionary<int, IReadOnlyList<YarnMatchLevelConfig>>();

    public static IReadOnlyList<YarnMatchLevelConfig> All => GetChapter(0);

    public static YarnMatchLevelConfig Get(int level)
    {
        return BuildLevel(Math.Max(1, level));
    }

    public static IReadOnlyList<YarnMatchLevelConfig> GetChapter(int chapterIndex)
    {
        int safeChapterIndex = Math.Max(0, chapterIndex);
        if (ChapterCache.TryGetValue(safeChapterIndex, out IReadOnlyList<YarnMatchLevelConfig> cached))
        {
            return cached;
        }

        List<YarnMatchLevelConfig> levels = new List<YarnMatchLevelConfig>(LevelsPerChapter);
        int firstLevel = safeChapterIndex * LevelsPerChapter + 1;
        for (int chapterLevel = 1; chapterLevel <= LevelsPerChapter; chapterLevel++)
        {
            levels.Add(BuildLevel(firstLevel + chapterLevel - 1));
        }
        ChapterCache.Add(safeChapterIndex, levels);
        return levels;
    }

    public static int GetChapterIndex(int level)
    {
        return Math.Max(0, (Math.Max(1, level) - 1) / LevelsPerChapter);
    }

    public static int GetChapterStartLevel(int level)
    {
        return GetChapterIndex(level) * LevelsPerChapter + 1;
    }

    private static YarnMatchLevelConfig BuildLevel(int level)
    {
        if (level == ReferencePatternLevel)
        {
            return BuildReferencePatternLevel(level);
        }

        int chapterIndex = GetChapterIndex(level);
        int chapterLevel = (level - 1) % LevelsPerChapter + 1;
        GetBoardShape(chapterLevel, out int boardColumns, out int boardRows);
        GetPoolShape(chapterLevel, out int poolColumns, out int poolRows);
        int totalCells = boardColumns * boardRows;
        int colorCount = GetColorCount(chapterLevel);
        GetTunnelRanges(chapterLevel, out int tunnelCountMin, out int tunnelCountMax, out int tunnelQueueMin, out int tunnelQueueMax);
        int tunnelCount = tunnelCountMin;
        int tunnelQueueDepth = tunnelQueueMin;
        int seed = CreateSeed(chapterIndex, chapterLevel);
        return new YarnMatchLevelConfig(
            level,
            colorCount,
            poolColumns,
            poolRows,
            tunnelCount,
            tunnelQueueDepth,
            poolColumns,
            poolColumns,
            poolRows,
            poolRows,
            tunnelCountMin,
            tunnelCountMax,
            tunnelQueueMin,
            tunnelQueueMax,
            chapterLevel >= 10,
            false,
            seed,
            CreateRectangle(boardColumns, boardRows));
    }

    private static void GetBoardShape(int chapterLevel, out int columns, out int rows)
    {
        int size = Math.Max(FirstBoardSize, chapterLevel);
        columns = Math.Max(FirstBoardSize, size);
        rows = columns;

        // A few long boards teach the player that the lower part is revealed by falling.
        // Milestone levels keep the requested scale; the final row may grow slightly so every color is made of complete three-cell groups.
        if (chapterLevel >= 15 && chapterLevel % 10 == 5)
        {
            rows = columns * 2;
        }

        EnsureSpoolGroupCompatibleArea(columns, ref rows);
    }

    private static void EnsureSpoolGroupCompatibleArea(int columns, ref int rows)
    {
        int safeColumns = Math.Max(1, columns);
        int safeRows = Math.Max(1, rows);
        while ((safeColumns * safeRows) % YarnMatchRackModel.CellsPerSpool != 0)
        {
            safeRows++;
        }

        rows = safeRows;
    }

    private static void GetPoolShape(int chapterLevel, out int columns, out int rows)
    {
        int milestone = Math.Max(0, chapterLevel - 1) / 10;
        int targetCellCount = InitialPoolCellCount + milestone * PoolCellsAddedPerMilestone;
        columns = chapterLevel <= 10 ? InitialPoolColumns : ExpandedPoolColumns;
        rows = Math.Max(1, (targetCellCount + columns - 1) / columns);
    }

    private static int GetColorCount(int chapterLevel)
    {
        int progressiveCount = MinimumNormalColorCount
            + Math.Max(0, chapterLevel - 1) / 3;
        return Math.Min(MaximumNormalColorCount, progressiveCount);
    }

    private static void GetTunnelRanges(
        int chapterLevel,
        out int tunnelCountMin,
        out int tunnelCountMax,
        out int tunnelQueueMin,
        out int tunnelQueueMax)
    {
        if (chapterLevel < 5)
        {
            tunnelCountMin = 0;
            tunnelCountMax = 0;
            tunnelQueueMin = 0;
            tunnelQueueMax = 0;
            return;
        }

        if (chapterLevel < 20)
        {
            tunnelCountMin = 5;
            tunnelCountMax = 15;
            tunnelQueueMin = 5;
            tunnelQueueMax = 15;
            return;
        }

        if (chapterLevel < 30)
        {
            tunnelCountMin = 10;
            tunnelCountMax = 20;
            tunnelQueueMin = 5;
            tunnelQueueMax = 20;
            return;
        }

        if (chapterLevel <= 40)
        {
            tunnelCountMin = 15;
            tunnelCountMax = 25;
            tunnelQueueMin = 5;
            tunnelQueueMax = 25;
            return;
        }

        int stage = (chapterLevel - 30) / 10;
        tunnelCountMin = 15 + stage * 5;
        tunnelCountMax = 25 + stage * 5;
        tunnelQueueMin = 5;
        tunnelQueueMax = 25 + stage * 5;
    }

    private static YarnMatchLevelConfig BuildReferencePatternLevel(int level)
    {
        int chapterIndex = GetChapterIndex(level);
        int chapterLevel = (level - 1) % LevelsPerChapter + 1;
        int poolColumns = ReferencePoolColumns;
        int poolRows = ReferencePoolRows;
        int seed = CreateSeed(chapterIndex, chapterLevel);
        return new YarnMatchLevelConfig(
            level,
            Enum.GetValues(typeof(YarnMatchColor)).Length,
            poolColumns,
            poolRows,
            ReferenceTunnelCountMin,
            ReferenceTunnelQueueMin,
            11,
            13,
            7,
            8,
            ReferenceTunnelCountMin,
            ReferenceTunnelCountMax,
            ReferenceTunnelQueueMin,
            ReferenceTunnelQueueMax,
            false,
            true,
            seed,
            CreateRectangle(ReferencePatternColumns, ReferencePatternRows));
    }

    private static int[] CreateRectangle(int columns, int rows)
    {
        int[] heights = new int[Math.Max(1, columns)];
        for (int index = 0; index < heights.Length; index++)
        {
            heights[index] = Math.Max(1, rows);
        }
        return heights;
    }

    private static int GetRequiredSpoolCount(int totalCells)
    {
        return (Math.Max(0, totalCells) + YarnMatchRackModel.CellsPerSpool - 1) / YarnMatchRackModel.CellsPerSpool;
    }

    private static int CreateSeed(int chapterIndex, int chapterLevel)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + chapterIndex * 1000003;
            hash = hash * 31 + chapterLevel * 7919;
            hash ^= 0x5F3759DF;
            return hash & int.MaxValue;
        }
    }
}
