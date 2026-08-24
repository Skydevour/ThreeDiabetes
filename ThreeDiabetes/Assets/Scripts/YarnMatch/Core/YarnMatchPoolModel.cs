using System;
using System.Collections.Generic;

public sealed class YarnMatchPoolModel
{
    public const int DefaultColumns = 8;
    public const int DefaultRows = 6;
    public const int DefaultTunnelCount = 0;
    public const int DefaultTunnelQueueDepth = 0;

    private readonly List<YarnMatchPoolCell> _cells = new List<YarnMatchPoolCell>();
    private readonly List<YarnMatchTunnel> _tunnels = new List<YarnMatchTunnel>();
    private readonly List<YarnMatchSpoolToken> _tokens = new List<YarnMatchSpoolToken>();
    private int _columns = DefaultColumns;
    private int _rows = DefaultRows;
    private bool _hasRefreshed;

    public IReadOnlyList<YarnMatchPoolCell> Cells => _cells;
    public IReadOnlyList<YarnMatchTunnel> Tunnels => _tunnels;
    public IReadOnlyList<YarnMatchSpoolToken> Tokens => _tokens;
    public int Columns => _columns;
    public int Rows => _rows;

    public void Build(int seed)
    {
        Build(seed, DefaultColumns, DefaultRows, 8, DefaultTunnelCount, DefaultTunnelQueueDepth, 0);
    }

    public void Build(int seed, int columns, int rows, int colorCount, int tunnelCount, int tunnelQueueDepth)
    {
        Build(seed, columns, rows, colorCount, tunnelCount, tunnelQueueDepth, 0);
    }

    public void Build(int seed, int columns, int rows, int colorCount, int tunnelCount, int tunnelQueueDepth, int targetTokenCount)
    {
        Build(seed, columns, rows, colorCount, tunnelCount, tunnelQueueDepth, targetTokenCount, null);
    }

    public void Build(int seed, int columns, int rows, int colorCount, int tunnelCount, int tunnelQueueDepth, IReadOnlyList<int> targetSpoolsByColor)
    {
        Build(seed, columns, rows, colorCount, tunnelCount, tunnelQueueDepth, 0, targetSpoolsByColor);
    }

    private void Build(
        int seed,
        int columns,
        int rows,
        int colorCount,
        int tunnelCount,
        int tunnelQueueDepth,
        int targetTokenCount,
        IReadOnlyList<int> targetSpoolsByColor)
    {
        _cells.Clear();
        _tunnels.Clear();
        _tokens.Clear();
        _hasRefreshed = false;
        _columns = Math.Max(1, columns);
        _rows = Math.Max(2, rows);
        int safeColorCount = Math.Max(1, Math.Min(colorCount, Enum.GetValues(typeof(YarnMatchColor)).Length));

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                _cells.Add(new YarnMatchPoolCell
                {
                    Column = column,
                    Row = row,
                    Unlocked = row == 0
                });
            }
        }

        int requestedTokenCount = CountRequestedTokens(targetSpoolsByColor);
        int desiredTokenCount = requestedTokenCount > 0
            ? requestedTokenCount
            : targetTokenCount > 0 ? targetTokenCount : _cells.Count;
        desiredTokenCount = Math.Max(0, desiredTokenCount);

        int maximumTunnelCount = Math.Max(0, (_rows - 1) * _columns);
        int configuredTunnelCount = Math.Min(
            Math.Max(0, tunnelCount),
            Math.Min(maximumTunnelCount, Math.Max(0, desiredTokenCount - 1)));
        int visibleTokenCount = Math.Min(
            Math.Max(0, desiredTokenCount - configuredTunnelCount),
            Math.Max(0, _cells.Count - configuredTunnelCount));
        List<YarnMatchPoolCell> visibleCells = ChooseReachableCells(visibleTokenCount, seed + 300);
        List<TunnelPlacement> tunnelPlacements = ChooseTunnelPlacements(
            visibleCells,
            configuredTunnelCount,
            seed + 511);
        while (configuredTunnelCount > 0 && tunnelPlacements.Count == 0)
        {
            configuredTunnelCount--;
            visibleTokenCount = Math.Min(
                Math.Max(0, desiredTokenCount - configuredTunnelCount),
                Math.Max(0, _cells.Count - configuredTunnelCount));
            visibleCells = ChooseReachableCells(visibleTokenCount, seed + 300 + configuredTunnelCount);
            tunnelPlacements = ChooseTunnelPlacements(visibleCells, configuredTunnelCount, seed + 511);
        }

        int hiddenBudget = Math.Max(0, desiredTokenCount - visibleCells.Count);
        List<int> queueCounts = BuildQueueCounts(hiddenBudget, tunnelPlacements.Count, tunnelQueueDepth);
        int tokenCount = visibleCells.Count + hiddenBudget;
        List<YarnMatchColor> colors = BuildColors(tokenCount, safeColorCount, targetSpoolsByColor, seed);
        for (int index = 0; index < tokenCount; index++)
        {
            _tokens.Add(new YarnMatchSpoolToken
            {
                Id = index,
                Color = colors[index]
            });
        }

        for (int index = 0; index < visibleCells.Count; index++)
        {
            AttachToken(visibleCells[index], _tokens[index]);
        }

        int queueOffset = 0;
        for (int index = 0; index < tunnelPlacements.Count; index++)
        {
            TunnelPlacement placement = tunnelPlacements[index];
            YarnMatchTunnel tunnel = new YarnMatchTunnel
            {
                Target = placement.PipeCell,
                OutputCell = placement.OutputCell,
                Direction = placement.Direction
            };
            placement.PipeCell.Tunnel = tunnel;
            placement.OutputCell.SourceTunnel = tunnel;
            int queueStart = visibleCells.Count + queueOffset;
            int queueCount = queueCounts[index];
            for (int queueIndex = 0; queueIndex < queueCount; queueIndex++)
            {
                tunnel.Queue.Add(_tokens[queueStart + queueIndex]);
            }
            queueOffset += queueCount;
            _tunnels.Add(tunnel);
        }
    }
    public bool IsSelectable(YarnMatchSpoolToken token)
    {
        return token != null && !token.Used && token.Cell != null && token.Cell.Unlocked;
    }

    public YarnMatchPoolSelection Consume(YarnMatchSpoolToken token)
    {
        if (!IsSelectable(token))
        {
            return null;
        }

        YarnMatchPoolCell source = token.Cell;
        token.Used = true;
        source.Token = null;
        token.Cell = null;
        UnlockNeighbors(source);
        return new YarnMatchPoolSelection
        {
            Token = token,
            SourceCell = source,
            Tunnel = source.SourceTunnel
        };
    }

    public YarnMatchSpoolToken Replenish(YarnMatchPoolCell cell)
    {
        if (cell == null || cell.Token != null || cell.SourceTunnel == null || cell.SourceTunnel.Queue.Count == 0)
        {
            return null;
        }

        YarnMatchSpoolToken token = cell.SourceTunnel.Queue[0];
        cell.SourceTunnel.Queue.RemoveAt(0);
        AttachToken(cell, token);
        return token;
    }

    public bool Refresh(int seed)
    {
        if (_hasRefreshed)
        {
            return false;
        }

        _hasRefreshed = true;
        List<YarnMatchPoolCell> visibleCells = new List<YarnMatchPoolCell>();
        List<YarnMatchSpoolToken> visibleTokens = new List<YarnMatchSpoolToken>();
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchPoolCell cell = _cells[index];
            if (cell.Token != null && !cell.Token.Used)
            {
                visibleCells.Add(cell);
                visibleTokens.Add(cell.Token);
            }
        }

        List<YarnMatchSpoolToken> hiddenTokens = new List<YarnMatchSpoolToken>();
        for (int index = 0; index < _tunnels.Count; index++)
        {
            hiddenTokens.AddRange(_tunnels[index].Queue);
        }

        YarnMatchRandom.Shuffle(visibleTokens, seed);
        YarnMatchRandom.Shuffle(hiddenTokens, seed + 1);
        for (int index = 0; index < visibleCells.Count; index++)
        {
            visibleCells[index].Token = visibleTokens[index];
            visibleTokens[index].Cell = visibleCells[index];
        }

        int cursor = 0;
        for (int tunnelIndex = 0; tunnelIndex < _tunnels.Count; tunnelIndex++)
        {
            List<YarnMatchSpoolToken> queue = _tunnels[tunnelIndex].Queue;
            for (int queueIndex = 0; queueIndex < queue.Count; queueIndex++)
            {
                queue[queueIndex] = hiddenTokens[cursor++];
                queue[queueIndex].Cell = null;
            }
        }

        return true;
    }

    public List<YarnMatchSpoolToken> GetSelectableTokens()
    {
        List<YarnMatchSpoolToken> result = new List<YarnMatchSpoolToken>();
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchSpoolToken token = _cells[index].Token;
            if (IsSelectable(token))
            {
                result.Add(token);
            }
        }
        return result;
    }

    private List<YarnMatchColor> BuildColors(
        int tokenCount,
        int safeColorCount,
        IReadOnlyList<int> targetSpoolsByColor,
        int seed)
    {
        int requestedTokenCount = CountRequestedTokens(targetSpoolsByColor);
        List<YarnMatchColor> colors = new List<YarnMatchColor>(tokenCount);
        if (requestedTokenCount > 0)
        {
            for (int color = 0; color < targetSpoolsByColor.Count; color++)
            {
                int requested = Math.Max(0, targetSpoolsByColor[color]);
                for (int count = 0; count < requested; count++)
                {
                    colors.Add((YarnMatchColor)color);
                }
            }
        }
        else
        {
            for (int index = 0; index < tokenCount; index++)
            {
                colors.Add((YarnMatchColor)(index % safeColorCount));
            }
        }

        while (colors.Count < tokenCount)
        {
            colors.Add((YarnMatchColor)(colors.Count % safeColorCount));
        }
        if (colors.Count > tokenCount)
        {
            colors.RemoveRange(tokenCount, colors.Count - tokenCount);
        }
        YarnMatchRandom.Shuffle(colors, seed);
        return colors;
    }

    private void AttachToken(YarnMatchPoolCell cell, YarnMatchSpoolToken token)
    {
        cell.Token = token;
        token.Cell = cell;
    }

    private static int CountRequestedTokens(IReadOnlyList<int> targetSpoolsByColor)
    {
        if (targetSpoolsByColor == null)
        {
            return 0;
        }

        int total = 0;
        for (int index = 0; index < targetSpoolsByColor.Count; index++)
        {
            total += Math.Max(0, targetSpoolsByColor[index]);
        }
        return total;
    }

    private static List<int> BuildQueueCounts(int hiddenBudget, int tunnelCount, int configuredDepth)
    {
        List<int> queueCounts = new List<int>(tunnelCount);
        if (tunnelCount <= 0)
        {
            return queueCounts;
        }

        int remaining = Math.Max(0, hiddenBudget);
        int depthHint = Math.Max(0, configuredDepth);
        for (int index = 0; index < tunnelCount; index++)
        {
            int remainingTunnels = tunnelCount - index;
            int queueCount = (remaining + remainingTunnels - 1) / remainingTunnels;
            if (depthHint > queueCount && remaining >= depthHint * remainingTunnels)
            {
                queueCount = depthHint;
            }
            queueCounts.Add(queueCount);
            remaining -= queueCount;
        }
        return queueCounts;
    }

    private void UnlockNeighbors(YarnMatchPoolCell source)
    {
        source.Unlocked = true;
        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        for (int index = 0; index < directions.GetLength(0); index++)
        {
            YarnMatchPoolCell neighbor = FindCell(source.Column + directions[index, 0], source.Row + directions[index, 1]);
            if (neighbor != null && neighbor.Token != null)
            {
                neighbor.Unlocked = true;
            }
        }
    }

    private List<YarnMatchPoolCell> ChooseReachableCells(int count, int seed)
    {
        List<YarnMatchPoolCell> result = new List<YarnMatchPoolCell>(Math.Max(0, count));
        if (count <= 0)
        {
            return result;
        }

        List<YarnMatchPoolCell> roots = new List<YarnMatchPoolCell>();
        for (int column = 0; column < _columns; column++)
        {
            roots.Add(_cells[column]);
        }
        YarnMatchRandom.Shuffle(roots, seed);
        for (int index = 0; index < roots.Count && result.Count < count; index++)
        {
            result.Add(roots[index]);
        }

        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        while (result.Count < count)
        {
            List<YarnMatchPoolCell> frontier = new List<YarnMatchPoolCell>();
            for (int index = 0; index < result.Count; index++)
            {
                YarnMatchPoolCell current = result[index];
                for (int direction = 0; direction < directions.GetLength(0); direction++)
                {
                    YarnMatchPoolCell neighbor = FindCell(current.Column + directions[direction, 0], current.Row + directions[direction, 1]);
                    if (neighbor != null && !result.Contains(neighbor) && !frontier.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }

            if (frontier.Count == 0)
            {
                break;
            }
            YarnMatchRandom.Shuffle(frontier, seed + result.Count * 17);
            result.Add(frontier[0]);
        }
        return result;
    }

    private static int CountLowerCells(IReadOnlyList<YarnMatchPoolCell> cells)
    {
        int count = 0;
        for (int index = 0; index < cells.Count; index++)
        {
            if (cells[index].Row > 0)
            {
                count++;
            }
        }
        return count;
    }

    private sealed class TunnelPlacement
    {
        internal YarnMatchPoolCell PipeCell;
        internal YarnMatchPoolCell OutputCell;
        internal YarnMatchTunnelDirection Direction;
    }

    private List<TunnelPlacement> ChooseTunnelPlacements(
        IReadOnlyList<YarnMatchPoolCell> visibleCells,
        int count,
        int seed)
    {
        List<TunnelPlacement> candidates = new List<TunnelPlacement>();

        int[,] offsets = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
        for (int outputIndex = 0; outputIndex < visibleCells.Count; outputIndex++)
        {
            YarnMatchPoolCell output = visibleCells[outputIndex];
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                YarnMatchPoolCell pipe = FindCell(
                    output.Column - offsets[directionIndex, 0],
                    output.Row - offsets[directionIndex, 1]);
                if (pipe == null || ContainsCell(visibleCells, pipe))
                {
                    continue;
                }

                candidates.Add(new TunnelPlacement
                {
                    PipeCell = pipe,
                    OutputCell = output,
                    Direction = (YarnMatchTunnelDirection)directionIndex
                });
            }
        }

        YarnMatchRandom.Shuffle(candidates, seed);
        List<TunnelPlacement> result = new List<TunnelPlacement>(Math.Max(0, count));
        HashSet<YarnMatchPoolCell> usedPipes = new HashSet<YarnMatchPoolCell>();
        HashSet<YarnMatchPoolCell> usedOutputs = new HashSet<YarnMatchPoolCell>();
        for (int index = 0; index < candidates.Count && result.Count < count; index++)
        {
            TunnelPlacement candidate = candidates[index];
            if (!usedPipes.Add(candidate.PipeCell) || !usedOutputs.Add(candidate.OutputCell))
            {
                continue;
            }
            result.Add(candidate);
        }
        return result;
    }
    private static bool ContainsCell(IReadOnlyList<YarnMatchPoolCell> cells, YarnMatchPoolCell candidate)
    {
        for (int index = 0; index < cells.Count; index++)
        {
            if (cells[index] == candidate)
            {
                return true;
            }
        }
        return false;
    }
    private YarnMatchPoolCell FindCell(int column, int row)
    {
        if (column < 0 || column >= _columns || row < 0 || row >= _rows)
        {
            return null;
        }
        return _cells[row * _columns + column];
    }
}
