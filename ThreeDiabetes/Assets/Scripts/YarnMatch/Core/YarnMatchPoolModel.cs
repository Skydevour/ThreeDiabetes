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

    public void Build(
        int seed,
        int columnsMin,
        int columnsMax,
        int rowsMin,
        int rowsMax,
        int colorCount,
        int tunnelCountMin,
        int tunnelCountMax,
        int tunnelQueueMin,
        int tunnelQueueMax,
        IReadOnlyList<int> targetSpoolsByColor)
    {
        int columns = SelectRandomInclusive(seed + 11, columnsMin, columnsMax);
        int rows = SelectRandomInclusive(seed + 23, rowsMin, rowsMax);
        int tunnelCount = SelectRandomInclusive(seed + 37, tunnelCountMin, tunnelCountMax);
        Build(seed, columns, rows, colorCount, tunnelCount, tunnelQueueMin, tunnelQueueMax, 0, targetSpoolsByColor);
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
        Build(seed, columns, rows, colorCount, tunnelCount, tunnelQueueDepth, tunnelQueueDepth, targetTokenCount, targetSpoolsByColor);
    }

    private void Build(
        int seed,
        int columns,
        int rows,
        int colorCount,
        int tunnelCount,
        int tunnelQueueMin,
        int tunnelQueueMax,
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

        int maximumTunnelCount = Math.Max(0, Math.Min(
            _cells.Count / 2,
            Math.Min((_rows - 1) * _columns, desiredTokenCount / 2)));
        int configuredTunnelCount = Math.Min(Math.Max(0, tunnelCount), maximumTunnelCount);
        int visibleTokenCount = Math.Min(
            Math.Max(0, desiredTokenCount - configuredTunnelCount),
            Math.Max(0, _cells.Count - configuredTunnelCount));
        TunnelLayout layout = ChooseTunnelLayout(visibleTokenCount, configuredTunnelCount, seed + 511);
        List<YarnMatchPoolCell> visibleCells = layout.VisibleCells;
        List<TunnelPlacement> tunnelPlacements = layout.Placements;
        configuredTunnelCount = tunnelPlacements.Count;
        int requestedQueueMin = Math.Max(1, tunnelQueueMin);
        int requestedQueueMax = Math.Max(requestedQueueMin, tunnelQueueMax);

        int hiddenBudget = Math.Max(0, desiredTokenCount - visibleCells.Count);
        List<int> queueCounts = BuildQueueCounts(hiddenBudget, tunnelPlacements.Count, requestedQueueMin, requestedQueueMax, seed + 733);
        int tokenCount = visibleCells.Count + hiddenBudget;
        List<YarnMatchColor> colors = BuildColors(tokenCount, visibleCells.Count, safeColorCount, targetSpoolsByColor, seed);
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
            placement.PipeCell.Unlocked = true;
            UnlockNeighbors(placement.PipeCell);
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
        return token != null
            && !token.Used
            && token.Cell != null
            && token.Cell.Tunnel == null
            && IsCellUnlocked(token.Cell);
    }

    private bool IsCellUnlocked(YarnMatchPoolCell cell)
    {
        if (cell.Unlocked)
        {
            return true;
        }

        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        for (int index = 0; index < directions.GetLength(0); index++)
        {
            YarnMatchPoolCell neighbor = FindCell(cell.Column + directions[index, 0], cell.Row + directions[index, 1]);
            if (neighbor != null && neighbor.Unlocked)
            {
                return true;
            }
        }

        return false;
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
        int visibleTokenCount,
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

        if (requestedTokenCount <= 0)
        {
            YarnMatchRandom.Shuffle(colors, seed);
            return colors;
        }

        EnsureEveryColorCanBeSeen(colors, visibleTokenCount, targetSpoolsByColor, seed);
        return colors;
    }

    private static void EnsureEveryColorCanBeSeen(
        List<YarnMatchColor> colors,
        int visibleTokenCount,
        IReadOnlyList<int> targetSpoolsByColor,
        int seed)
    {
        int safeVisibleCount = Math.Min(Math.Max(0, visibleTokenCount), colors.Count);
        if (safeVisibleCount <= 0)
        {
            return;
        }

        List<YarnMatchColor> visible = new List<YarnMatchColor>(safeVisibleCount);
        List<YarnMatchColor> hidden = new List<YarnMatchColor>(Math.Max(0, colors.Count - safeVisibleCount));
        for (int index = 0; index < colors.Count; index++)
        {
            if (index < safeVisibleCount)
            {
                visible.Add(colors[index]);
            }
            else
            {
                hidden.Add(colors[index]);
            }
        }

        List<YarnMatchColor> requiredVisibleColors = new List<YarnMatchColor>();
        for (int color = 0; color < targetSpoolsByColor.Count; color++)
        {
            if (targetSpoolsByColor[color] > 0)
            {
                requiredVisibleColors.Add((YarnMatchColor)color);
            }
        }

        if (requiredVisibleColors.Count > safeVisibleCount)
        {
            YarnMatchRandom.Shuffle(colors, seed);
            return;
        }

        HashSet<int> lockedVisibleIndices = new HashSet<int>();
        for (int colorIndex = 0; colorIndex < requiredVisibleColors.Count; colorIndex++)
        {
            YarnMatchColor requiredColor = requiredVisibleColors[colorIndex];
            int existingIndex = visible.IndexOf(requiredColor);
            if (existingIndex >= 0)
            {
                lockedVisibleIndices.Add(existingIndex);
                continue;
            }

            int hiddenIndex = hidden.IndexOf(requiredColor);
            if (hiddenIndex < 0)
            {
                continue;
            }

            int replacementIndex = -1;
            for (int visibleIndex = 0; visibleIndex < visible.Count; visibleIndex++)
            {
                if (!lockedVisibleIndices.Contains(visibleIndex))
                {
                    replacementIndex = visibleIndex;
                    break;
                }
            }

            if (replacementIndex < 0)
            {
                break;
            }

            YarnMatchColor displaced = visible[replacementIndex];
            visible[replacementIndex] = requiredColor;
            hidden[hiddenIndex] = displaced;
            lockedVisibleIndices.Add(replacementIndex);
        }

        YarnMatchRandom.Shuffle(visible, seed + 1);
        YarnMatchRandom.Shuffle(hidden, seed + 2);
        colors.Clear();
        colors.AddRange(visible);
        colors.AddRange(hidden);
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

    private static List<int> BuildQueueCounts(int hiddenBudget, int tunnelCount, int requestedMin, int requestedMax, int seed)
    {
        List<int> queueCounts = new List<int>(tunnelCount);
        if (tunnelCount <= 0)
        {
            return queueCounts;
        }

        int total = Math.Max(0, hiddenBudget);
        int safeMin = Math.Max(1, requestedMin);
        if ((long)safeMin * tunnelCount > total)
        {
            safeMin = Math.Max(0, total / tunnelCount);
        }

        int safeMax = Math.Max(safeMin, Math.Max(requestedMax, (total + tunnelCount - 1) / tunnelCount));
        safeMax = Math.Min(safeMax, total);
        for (int index = 0; index < tunnelCount; index++)
        {
            queueCounts.Add(safeMin);
        }

        int remaining = total - safeMin * tunnelCount;
        System.Random random = new System.Random(seed);
        int round = 0;
        while (remaining > 0)
        {
            List<int> candidates = new List<int>();
            for (int index = 0; index < queueCounts.Count; index++)
            {
                if (queueCounts[index] < safeMax)
                {
                    candidates.Add(index);
                }
            }
            if (candidates.Count == 0)
            {
                break;
            }

            YarnMatchRandom.Shuffle(candidates, seed + round * 37);
            int targetIndex = candidates[random.Next(candidates.Count)];
            int capacity = Math.Min(remaining, safeMax - queueCounts[targetIndex]);
            int increase = capacity <= 1 ? capacity : random.Next(1, capacity + 1);
            queueCounts[targetIndex] += increase;
            remaining -= increase;
            round++;
        }

        if (remaining > 0)
        {
            for (int index = 0; index < queueCounts.Count && remaining > 0; index++)
            {
                int capacity = safeMax - queueCounts[index];
                int increase = Math.Min(capacity, remaining);
                queueCounts[index] += increase;
                remaining -= increase;
            }
        }

        if (queueCounts.Count > 1 && safeMax > safeMin && AreQueueCountsEqual(queueCounts))
        {
            if (queueCounts[0] > safeMin)
            {
                queueCounts[0]--;
                queueCounts[1]++;
            }
            else if (queueCounts[0] < safeMax)
            {
                queueCounts[0]++;
                queueCounts[1]--;
            }
        }
        return queueCounts;
    }
    private static bool AreQueueCountsEqual(IReadOnlyList<int> queueCounts)
    {
        for (int index = 1; index < queueCounts.Count; index++)
        {
            if (queueCounts[index] != queueCounts[0])
            {
                return false;
            }
        }
        return true;
    }

    private void UnlockNeighbors(YarnMatchPoolCell source)
    {
        source.Unlocked = true;
        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        for (int index = 0; index < directions.GetLength(0); index++)
        {
            YarnMatchPoolCell neighbor = FindCell(source.Column + directions[index, 0], source.Row + directions[index, 1]);
            if (neighbor != null)
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

    private List<YarnMatchPoolCell> ChooseReachableCells(
        IReadOnlyList<YarnMatchPoolCell> allowed,
        int count,
        int seed,
        IReadOnlyList<TunnelPlacement> placements)
    {
        List<YarnMatchPoolCell> result = new List<YarnMatchPoolCell>(Math.Max(0, count));
        if (count <= 0)
        {
            return result;
        }

        HashSet<YarnMatchPoolCell> allowedSet = new HashSet<YarnMatchPoolCell>(allowed);
        HashSet<YarnMatchPoolCell> requiredSet = new HashSet<YarnMatchPoolCell>();
        if (placements != null)
        {
            for (int index = 0; index < placements.Count; index++)
            {
                if (placements[index].OutputCell != null)
                {
                    requiredSet.Add(placements[index].OutputCell);
                }
            }
        }
        List<YarnMatchPoolCell> roots = new List<YarnMatchPoolCell>();
        for (int index = 0; index < allowed.Count; index++)
        {
            if (allowed[index].Row == 0)
            {
                roots.Add(allowed[index]);
            }
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
                    if (neighbor != null && allowedSet.Contains(neighbor) && !result.Contains(neighbor) && !frontier.Contains(neighbor))
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

        if (placements != null)
        {
            for (int index = 0; index < placements.Count; index++)
            {
                YarnMatchPoolCell required = placements[index].OutputCell;
                if (required == null || !allowedSet.Contains(required) || result.Contains(required))
                {
                    continue;
                }
                if (result.Count < count)
                {
                    result.Add(required);
                    continue;
                }
                for (int replaceIndex = result.Count - 1; replaceIndex >= 0; replaceIndex--)
                {
                    if (requiredSet.Contains(result[replaceIndex]))
                    {
                        continue;
                    }
                    result[replaceIndex] = required;
                    break;
                }
            }
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

    private sealed class TunnelLayout
    {
        internal readonly List<YarnMatchPoolCell> VisibleCells = new List<YarnMatchPoolCell>();
        internal readonly List<TunnelPlacement> Placements = new List<TunnelPlacement>();
    }

    private TunnelLayout ChooseTunnelLayout(int visibleTokenCount, int tunnelCount, int seed)
    {
        TunnelLayout best = null;
        int attempts = tunnelCount > 0 ? 64 : 1;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            List<TunnelPlacement> placements = ChooseTunnelPlacements(tunnelCount, seed + attempt * 31);
            if (placements.Count < tunnelCount && best != null)
            {
                continue;
            }

            HashSet<YarnMatchPoolCell> pipeCells = new HashSet<YarnMatchPoolCell>();
            for (int index = 0; index < placements.Count; index++)
            {
                pipeCells.Add(placements[index].PipeCell);
            }

            List<YarnMatchPoolCell> available = new List<YarnMatchPoolCell>();
            for (int index = 0; index < _cells.Count; index++)
            {
                if (!pipeCells.Contains(_cells[index]))
                {
                    available.Add(_cells[index]);
                }
            }

            List<YarnMatchPoolCell> visible = ChooseReachableCells(
                available,
                Math.Min(visibleTokenCount, available.Count),
                seed + attempt * 47,
                placements);
            if (!AreTunnelOutputsVisible(visible, placements))
            {
                continue;
            }

            TunnelLayout layout = new TunnelLayout();
            layout.VisibleCells.AddRange(visible);
            layout.Placements.AddRange(placements);
            if (best == null
                || layout.Placements.Count > best.Placements.Count
                || layout.VisibleCells.Count > best.VisibleCells.Count)
            {
                best = layout;
            }
            if (placements.Count >= tunnelCount)
            {
                return layout;
            }
        }

        if (best != null)
        {
            return best;
        }

        TunnelLayout fallback = new TunnelLayout();
        fallback.VisibleCells.AddRange(ChooseReachableCells(visibleTokenCount, seed));
        return fallback;
    }

    private static bool AreTunnelOutputsVisible(
        IReadOnlyList<YarnMatchPoolCell> visible,
        IReadOnlyList<TunnelPlacement> placements)
    {
        if (visible.Count <= 0 && placements.Count > 0)
        {
            return false;
        }
        for (int index = 0; index < placements.Count; index++)
        {
            if (!ContainsCell(visible, placements[index].OutputCell))
            {
                return false;
            }
        }
        return true;
    }
    private List<TunnelPlacement> ChooseTunnelPlacements(int count, int seed)
    {
        List<TunnelPlacement> candidates = new List<TunnelPlacement>();
        int[,] offsets = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
        for (int pipeIndex = 0; pipeIndex < _cells.Count; pipeIndex++)
        {
            YarnMatchPoolCell pipe = _cells[pipeIndex];
            if (pipe.Row == 0)
            {
                continue;
            }
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                YarnMatchPoolCell output = FindCell(
                    pipe.Column + offsets[directionIndex, 0],
                    pipe.Row + offsets[directionIndex, 1]);
                if (output == null)
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
            if (usedPipes.Contains(candidate.OutputCell)
                || usedOutputs.Contains(candidate.PipeCell)
                || !usedPipes.Add(candidate.PipeCell)
                || !usedOutputs.Add(candidate.OutputCell))
            {
                continue;
            }
            result.Add(candidate);
        }
        return result;
    }    private static bool ContainsCell(IReadOnlyList<YarnMatchPoolCell> cells, YarnMatchPoolCell candidate)
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
    private static int SelectRandomInclusive(int seed, int min, int max)
    {
        int safeMin = Math.Min(min, max);
        int safeMax = Math.Max(min, max);
        return safeMin == safeMax ? safeMin : new System.Random(seed).Next(safeMin, safeMax + 1);
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
