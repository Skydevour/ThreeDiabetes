using System;
using System.Collections.Generic;

public sealed class YarnMatchLevelConfig
{
    private readonly int[] _columnHeights;

    public int Number { get; }
    public int Chapter { get; }
    public int ChapterLevel { get; }
    public int ColorCount { get; }
    public int PoolColumns { get; }
    public int PoolRows { get; }
    public int TunnelCount { get; }
    public int TunnelQueueDepth { get; }
    public int BoardColumns { get; }
    public int BoardRows { get; }
    public bool UsesGraphicPattern { get; }
    public bool UsesReferencePattern { get; }
    public int Seed { get; }
    public IReadOnlyList<int> ColumnHeights => _columnHeights;

    internal YarnMatchLevelConfig(
        int number,
        int colorCount,
        int poolColumns,
        int poolRows,
        int tunnelCount,
        int tunnelQueueDepth,
        bool usesGraphicPattern,
        bool usesReferencePattern,
        int seed,
        params int[] columnHeights)
    {
        Number = number;
        ColorCount = colorCount;
        PoolColumns = poolColumns;
        PoolRows = poolRows;
        TunnelCount = tunnelCount;
        TunnelQueueDepth = tunnelQueueDepth;
        UsesGraphicPattern = usesGraphicPattern;
        UsesReferencePattern = usesReferencePattern;
        Seed = seed;
        Chapter = (number - 1) / YarnMatchLevelCatalog.LevelsPerChapter + 1;
        ChapterLevel = (number - 1) % YarnMatchLevelCatalog.LevelsPerChapter + 1;
        _columnHeights = (int[])columnHeights.Clone();
        BoardColumns = _columnHeights.Length;
        BoardRows = 1;
        for (int index = 0; index < _columnHeights.Length; index++)
        {
            BoardRows = Math.Max(BoardRows, _columnHeights[index]);
        }
    }

    public int TotalBoardCells
    {
        get
        {
            int total = 0;
            for (int index = 0; index < _columnHeights.Length; index++)
            {
                total += _columnHeights[index];
            }
            return total;
        }
    }

    public string Summary
    {
        get
        {
            string tunnelSummary = TunnelCount > 0 ? "  ·  管道 " + TunnelCount : string.Empty;
            return ColorCount + " 色  ·  " + PoolColumns + "×" + PoolRows + tunnelSummary;
        }
    }
}
