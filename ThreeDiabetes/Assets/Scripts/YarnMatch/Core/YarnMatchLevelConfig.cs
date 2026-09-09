using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class YarnMatchLevelConfig
{
    private readonly int[] _columnHeights;
    private YarnMatchPatternLayout _pattern;
    private Task<YarnMatchPatternLayout> _preparation;
    internal int TemplateIndex { get; }
    internal YarnMatchPatternLayout Pattern => _pattern ?? (_pattern = UsesReferencePattern
        ? YarnMatchPatternLayout.Dense(ColumnHeights, YarnMatchReferencePatternGenerator.Generate(ColumnHeights))
        : YarnMatchTemplateSampler.Generate(this));
    internal Task<YarnMatchPatternLayout> PreparePatternAsync()
    {
        if (_preparation == null)
            _preparation = _pattern != null ? Task.FromResult(_pattern) : Task.Run(() => UsesReferencePattern
                ? YarnMatchPatternLayout.Dense(ColumnHeights, YarnMatchReferencePatternGenerator.Generate(ColumnHeights))
                : YarnMatchTemplateSampler.Generate(this));
        return _preparation;
    }
    internal void RetainPattern(YarnMatchPatternLayout pattern) => _pattern = pattern;
    internal void ReleasePattern()
    {
        _pattern = null;
        _preparation = null;
    }
    public int Number { get; }
    public int Chapter => YarnMatchLevelCatalog.GetChapterIndex(Number) + 1;
    public int ChapterLevel => (Number - 1) % YarnMatchLevelCatalog.LevelsPerChapter + 1;
    public int ColorCount { get; }
    public int PoolColumns { get; }
    public int PoolRows { get; }
    public int PoolColumnsMin => PoolColumns;
    public int PoolColumnsMax => PoolColumns;
    public int PoolRowsMin => PoolRows;
    public int PoolRowsMax => PoolRows;
    public int TunnelCountMin { get; }
    public int TunnelCountMax { get; }
    public int TunnelQueueMin { get; }
    public int TunnelQueueMax { get; }
    public int FreezeCount { get; }
    public int ChainCount { get; }
    public int BoardColumns => _columnHeights.Length;
    public int BoardRows { get; }
    public bool UsesReferencePattern { get; }
    public int Seed { get; }
    public IReadOnlyList<int> ColumnHeights => _columnHeights;
    public int TotalBoardCells => Pattern.CellCount;
    public string Summary => ColorCount + " 色 · " + BoardColumns + " × " + BoardRows;

    internal YarnMatchLevelConfig(int number, int colorCount, int poolColumns, int poolRows,
        int tunnelCountMin, int tunnelCountMax, int tunnelQueueMin, int tunnelQueueMax,
        int freezeCount, int chainCount, int seed, bool referencePattern, int[] columnHeights,
        int templateIndex = 0)
    {
        Number = number;
        ColorCount = colorCount;
        PoolColumns = poolColumns;
        PoolRows = poolRows;
        TunnelCountMin = tunnelCountMin;
        TunnelCountMax = tunnelCountMax;
        TunnelQueueMin = tunnelQueueMin;
        TunnelQueueMax = tunnelQueueMax;
        FreezeCount = freezeCount;
        ChainCount = chainCount;
        Seed = seed;
        UsesReferencePattern = referencePattern;
        TemplateIndex = templateIndex;
        _columnHeights = (int[])columnHeights.Clone();
        for (int i = 0; i < _columnHeights.Length; i++)
        {
            BoardRows = Math.Max(BoardRows, _columnHeights[i]);
        }
    }
}
