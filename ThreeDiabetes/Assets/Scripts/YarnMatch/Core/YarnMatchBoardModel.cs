using System;
using System.Collections.Generic;

public sealed class YarnMatchBoardModel
{
    private readonly List<List<YarnMatchBoardCell>> _columns = new List<List<YarnMatchBoardCell>>();
    private readonly List<YarnMatchBoardCell> _allCells = new List<YarnMatchBoardCell>();
    private readonly int[] _colorCounts = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
    private readonly int[] _requiredSpoolsByColor = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];

    public IReadOnlyList<List<YarnMatchBoardCell>> Columns => _columns;
    public IReadOnlyList<YarnMatchBoardCell> AllCells => _allCells;
    public IReadOnlyList<int> ColorCounts => _colorCounts;
    public IReadOnlyList<int> RequiredSpoolsByColor => _requiredSpoolsByColor;
    public int TotalCells { get; private set; }
    public int CollectedCells { get; private set; }
    public int RequiredSpoolCount { get; private set; }

    public void Build(YarnMatchLevelConfig config)
    {
        if (config != null)
        {
            IReadOnlyList<YarnMatchColor> colors = config.UsesReferencePattern
                ? YarnMatchReferencePatternGenerator.Generate(config.ColumnHeights)
                : YarnMatchTemplateSampler.Generate(config.ColumnHeights, config.ColorCount, config.Seed);
            Build(config.ColumnHeights, colors);
            return;
        }

        Build(
            config == null ? new[] { 1 } : config.ColumnHeights,
            config == null ? 1 : config.ColorCount,
            config == null ? 0 : config.Seed,
            false,
            config != null && config.UsesReferencePattern);
    }

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
        _columns.Clear();
        _allCells.Clear();
        TotalCells = 0;
        CollectedCells = 0;
        Array.Clear(_colorCounts, 0, _colorCounts.Length);
        Array.Clear(_requiredSpoolsByColor, 0, _requiredSpoolsByColor.Length);
        RequiredSpoolCount = 0;

        IReadOnlyList<YarnMatchColor> colors = useReferencePattern
            ? YarnMatchReferencePatternGenerator.Generate(columnHeights)
            : YarnMatchBoardPatternGenerator.Generate(columnHeights, colorCount, seed, useGraphicPattern);

        int cursor = 0;
        for (int column = 0; column < columnHeights.Count; column++)
        {
            List<YarnMatchBoardCell> stack = new List<YarnMatchBoardCell>();
            for (int row = 0; row < columnHeights[column]; row++)
            {
                YarnMatchBoardCell cell = new YarnMatchBoardCell
                {
                    Color = colors[cursor],
                    Column = column,
                    Row = row
                };
                cursor++;
                stack.Add(cell);
                _allCells.Add(cell);
                _colorCounts[(int)cell.Color]++;
            }
            _columns.Add(stack);
        }

        TotalCells = cursor;
        for (int color = 0; color < _colorCounts.Length; color++)
        {
            _requiredSpoolsByColor[color] = (_colorCounts[color] + 2) / 3;
            RequiredSpoolCount += _requiredSpoolsByColor[color];
        }
    }

    public void Build(IReadOnlyList<int> columnHeights, IReadOnlyList<YarnMatchColor> colors)
    {
        _columns.Clear();
        _allCells.Clear();
        TotalCells = 0;
        CollectedCells = 0;
        Array.Clear(_colorCounts, 0, _colorCounts.Length);
        Array.Clear(_requiredSpoolsByColor, 0, _requiredSpoolsByColor.Length);
        RequiredSpoolCount = 0;

        int cursor = 0;
        for (int column = 0; column < columnHeights.Count; column++)
        {
            List<YarnMatchBoardCell> stack = new List<YarnMatchBoardCell>();
            int height = Math.Max(0, columnHeights[column]);
            for (int row = 0; row < height; row++)
            {
                YarnMatchBoardCell cell = new YarnMatchBoardCell
                {
                    Color = colors[cursor],
                    Column = column,
                    Row = row
                };
                cursor++;
                stack.Add(cell);
                _allCells.Add(cell);
                _colorCounts[(int)cell.Color]++;
            }
            _columns.Add(stack);
        }

        TotalCells = cursor;
        for (int color = 0; color < _colorCounts.Length; color++)
        {
            _requiredSpoolsByColor[color] = (_colorCounts[color] + YarnMatchRackModel.CellsPerSpool - 1)
                / YarnMatchRackModel.CellsPerSpool;
            RequiredSpoolCount += _requiredSpoolsByColor[color];
        }
    }

    public int RemainingCells(YarnMatchColor color)
    {
        int count = 0;
        for (int index = 0; index < _allCells.Count; index++)
        {
            if (_allCells[index].Active && _allCells[index].Color == color)
            {
                count++;
            }
        }
        return count;
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
        CollectedCells++;
        ReindexColumn(cell.Column);
        return true;
    }

    private void ReindexColumn(int column)
    {
        List<YarnMatchBoardCell> stack = _columns[column];
        for (int row = 0; row < stack.Count; row++)
        {
            stack[row].Row = row;
        }
    }
}
