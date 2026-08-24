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
        int tunnelCount = GetTunnelCount(chapterLevel, poolColumns, poolRows);
        int totalSpools = GetRequiredSpoolCount(totalCells);
        int tunnelQueueDepth = GetTunnelQueueDepth(totalSpools, poolColumns, poolRows, tunnelCount);
        int seed = CreateSeed(chapterIndex, chapterLevel);
        return new YarnMatchLevelConfig(
            level,
            colorCount,
            poolColumns,
            poolRows,
            tunnelCount,
            tunnelQueueDepth,
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
        // The milestone levels requested by the design remain square: 20x20 and 40x40.
        if (chapterLevel >= 15 && chapterLevel % 10 == 5)
        {
            rows = columns * 2;
        }
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

    private static int GetTunnelCount(int chapterLevel, int poolColumns, int poolRows)
    {
        if (chapterLevel < 10)
        {
            return 0;
        }

        int configured = 2 + (chapterLevel - 10) / 2;
        return Math.Min(GetMaximumTunnelCount(poolColumns, poolRows), configured);
    }

    private static int GetMaximumTunnelCount(int poolColumns, int poolRows)
    {
        int pipeCellLimit = Math.Max(0, (poolRows - 1) * poolColumns);
        int independentOutputLimit = Math.Max(0, poolColumns * poolRows / 2);
        return Math.Min(pipeCellLimit, independentOutputLimit);
    }

    private static int GetTunnelQueueDepth(int totalSpools, int poolColumns, int poolRows, int tunnelCount)
    {
        if (tunnelCount <= 0)
        {
            return 0;
        }

        int poolCellCount = Math.Max(1, poolColumns * poolRows);
        int hiddenSpools = tunnelCount + Math.Max(0, totalSpools - poolCellCount);
        return (hiddenSpools + tunnelCount - 1) / tunnelCount;
    }

    private static YarnMatchLevelConfig BuildReferencePatternLevel(int level)
    {
        int totalSpools = GetRequiredSpoolCount(ReferencePatternColumns * ReferencePatternRows);
        int chapterIndex = GetChapterIndex(level);
        int chapterLevel = (level - 1) % LevelsPerChapter + 1;
        GetPoolShape(chapterLevel, out int poolColumns, out int poolRows);
        int tunnelCount = GetMaximumTunnelCount(poolColumns, poolRows);
        int seed = CreateSeed(chapterIndex, chapterLevel);
        return new YarnMatchLevelConfig(
            level,
            Enum.GetValues(typeof(YarnMatchColor)).Length,
            poolColumns,
            poolRows,
            tunnelCount,
            GetTunnelQueueDepth(totalSpools, poolColumns, poolRows, tunnelCount),
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
