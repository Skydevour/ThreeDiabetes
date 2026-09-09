using System;
using System.Collections.Generic;

public sealed class YarnMatchBoardModel
{
    private readonly List<List<YarnMatchBoardCell>> _columns = new List<List<YarnMatchBoardCell>>();
    private readonly List<YarnMatchBoardCell> _allCells = new List<YarnMatchBoardCell>();
    private readonly int[] _colorCounts = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
    private readonly int[] _requiredSpoolsByColor = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
    private readonly int[] _remainingByColor = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];

    public IReadOnlyList<List<YarnMatchBoardCell>> Columns => _columns;
    public IReadOnlyList<YarnMatchBoardCell> AllCells => _allCells;
    public IReadOnlyList<int> ColorCounts => _colorCounts;
    public IReadOnlyList<int> RequiredSpoolsByColor => _requiredSpoolsByColor;
    public int TotalCells { get; private set; }
    public int CollectedCells { get; private set; }
    public int RequiredSpoolCount { get; private set; }
    public int InitialRows { get; private set; }
    public bool FitToViewport { get; private set; }

    public void Build(YarnMatchLevelConfig config)
    {
        Build(config.Pattern);
        FitToViewport = !config.UsesReferencePattern && config.BoardRows <= config.BoardColumns * 1.25f;
    }

    internal static YarnMatchPatternLayout GeneratePattern(YarnMatchLevelConfig config) => config.Pattern;

    public void Build(IReadOnlyList<int> columnHeights, int colorCount, int cellsPerColor, int seed)
    {
        Build(columnHeights, colorCount, seed);
    }

    public void Build(IReadOnlyList<int> columnHeights, int colorCount, int seed)
    {
        Build(columnHeights, colorCount, seed, false);
    }

    public void Build(IReadOnlyList<int> columnHeights, int colorCount, int seed, bool useGraphicPattern)
    {
        Build(columnHeights, colorCount, seed, useGraphicPattern, false);
    }

    public void Build(
        IReadOnlyList<int> columnHeights,
        int colorCount,
        int seed,
        bool useGraphicPattern,
        bool useReferencePattern)
    {
        IReadOnlyList<YarnMatchColor> colors = useReferencePattern
            ? YarnMatchReferencePatternGenerator.Generate(columnHeights)
            : YarnMatchBoardPatternGenerator.Generate(columnHeights, colorCount, seed, useGraphicPattern);

        Build(columnHeights, colors);
    }

    public void Build(IReadOnlyList<int> columnHeights, IReadOnlyList<YarnMatchColor> colors)
    {
        Build(YarnMatchPatternLayout.Dense(columnHeights, colors));
    }

    internal void Build(YarnMatchPatternLayout layout, bool fitToViewport = false)
    {
        _columns.Clear();
        _allCells.Clear();
        TotalCells = 0;
        CollectedCells = 0;
        Array.Clear(_colorCounts, 0, _colorCounts.Length);
        Array.Clear(_requiredSpoolsByColor, 0, _requiredSpoolsByColor.Length);
        RequiredSpoolCount = 0;

        InitialRows = layout.Height;
        FitToViewport = fitToViewport || layout.Width == layout.Height;
        for (int column = 0; column < layout.Width; column++)
        {
            List<YarnMatchBoardCell> stack = new List<YarnMatchBoardCell>();
            for (int row = 0; row < layout.Height; row++)
            {
                int color = layout.Colors[row * layout.Width + column];
                if (color < 0) continue;
                YarnMatchBoardCell cell = new YarnMatchBoardCell
                {
                    Color = (YarnMatchColor)color,
                    Column = column,
                    Row = row
                };
                stack.Add(cell);
                _allCells.Add(cell);
                _colorCounts[(int)cell.Color]++;
            }
            _columns.Add(stack);
        }

        TotalCells = _allCells.Count;
        Array.Copy(_colorCounts, _remainingByColor, _colorCounts.Length);
        for (int color = 0; color < _colorCounts.Length; color++)
        {
            _requiredSpoolsByColor[color] = (_colorCounts[color] + YarnMatchRackModel.CellsPerSpool - 1)
                / YarnMatchRackModel.CellsPerSpool;
            RequiredSpoolCount += _requiredSpoolsByColor[color];
        }
    }

    public int RemainingCells(YarnMatchColor color)
    {
        return _remainingByColor[(int)color];
    }

    public YarnMatchBoardCell FindFirstExposed(YarnMatchColor color)
    {
        for (int column = 0; column < _columns.Count; column++)
        {
            List<YarnMatchBoardCell> stack = _columns[column];
            if (stack.Count > 0 && stack[0].Color == color)
            {
                return stack[0];
            }
        }

        return null;
    }

    public int ExposedCount(YarnMatchColor color)
    {
        int count = 0;
        for (int column = 0; column < _columns.Count; column++)
        {
            List<YarnMatchBoardCell> stack = _columns[column];
            if (stack.Count > 0 && stack[0].Color == color)
            {
                count++;
            }
        }
        return count;
    }

    public bool IsExposed(YarnMatchBoardCell cell)
    {
        if (cell == null || cell.Column < 0 || cell.Column >= _columns.Count)
        {
            return false;
        }

        List<YarnMatchBoardCell> stack = _columns[cell.Column];
        return stack.Count > 0 && stack[0] == cell;
    }

    public bool Remove(YarnMatchBoardCell cell)
    {
        if (cell == null || !IsExposed(cell))
        {
            return false;
        }

        List<YarnMatchBoardCell> stack = _columns[cell.Column];
        stack.RemoveAt(0);
        cell.Active = false;
        _remainingByColor[(int)cell.Color]--;
        CollectedCells++;
        ReindexColumn(cell.Column);
        return true;
    }

    private void ReindexColumn(int column)
    {
        List<YarnMatchBoardCell> stack = _columns[column];
        for (int row = 0; row < stack.Count; row++)
        {
            // Preserve authored holes and column offsets; only the removed yarn's height falls.
            stack[row].Row--;
        }
    }
}
