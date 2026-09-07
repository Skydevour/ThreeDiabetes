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
    private readonly List<YarnMatchChain> _chains = new List<YarnMatchChain>();
    private readonly List<YarnMatchSpoolToken> _tokens = new List<YarnMatchSpoolToken>();
    private readonly int[] _createdTokensByColor = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
    private int _columns = DefaultColumns;
    private int _rows = DefaultRows;

    public IReadOnlyList<YarnMatchPoolCell> Cells => _cells;
    public IReadOnlyList<YarnMatchTunnel> Tunnels => _tunnels;
    public IReadOnlyList<YarnMatchChain> Chains => _chains;
    public IReadOnlyList<YarnMatchSpoolToken> Tokens => _tokens;
    public int Columns => _columns;
    public int Rows => _rows;
    public int RemainingTokenCount
    {
        get
        {
            int count = 0;
            for (int index = 0; index < _tokens.Count; index++)
            {
                if (!_tokens[index].Used)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public void Build(YarnMatchLevelConfig config)
    {
        Build(config, null);
    }

    public void Build(YarnMatchLevelConfig config, IReadOnlyList<int> boardColorCounts)
    {
        if (config == null)
        {
            Build(0);
            return;
        }

        Build(
            config.Seed + 500,
            config.PoolColumnsMin,
            config.PoolColumnsMax,
            config.PoolRowsMin,
            config.PoolRowsMax,
            config.ColorCount,
            config.TunnelCountMin,
            config.TunnelCountMax,
            config.TunnelQueueMin,
            config.TunnelQueueMax,
            null,
            boardColorCounts);
        ApplyGeneratedMechanics(config);
    }

    public void Build(int seed)
    {
        Build(seed, DefaultColumns, DefaultRows, 8, DefaultTunnelCount, DefaultTunnelQueueDepth, 0);
    }

    public void Build(int seed, int columns, int rows, int colorCount, int tunnelCount, int tunnelQueueDepth)
    {
        Build(
            seed,
            columns,
            columns,
            rows,
            rows,
            colorCount,
            tunnelCount,
            tunnelCount,
            tunnelQueueDepth,
            tunnelQueueDepth,
            null,
            null);
    }

    public void Build(int seed, int columns, int rows, int colorCount, int tunnelCount, int tunnelQueueDepth, int targetTokenCount)
    {
        BuildGenerated(
            seed,
            columns,
            rows,
            colorCount,
            tunnelCount,
            tunnelQueueDepth,
            tunnelQueueDepth,
            targetTokenCount,
            null,
            null);
    }

    public void Build(int seed, int columns, int rows, int colorCount, int tunnelCount, int tunnelQueueDepth, IReadOnlyList<int> targetSpoolsByColor)
    {
        Build(
            seed,
            columns,
            columns,
            rows,
            rows,
            colorCount,
            tunnelCount,
            tunnelCount,
            tunnelQueueDepth,
            tunnelQueueDepth,
            targetSpoolsByColor,
            null);
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
        Build(
            seed,
            columnsMin,
            columnsMax,
            rowsMin,
            rowsMax,
            colorCount,
            tunnelCountMin,
            tunnelCountMax,
            tunnelQueueMin,
            tunnelQueueMax,
            targetSpoolsByColor,
            null);
    }

    private void Build(
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
        IReadOnlyList<int> targetSpoolsByColor,
        IReadOnlyList<int> boardColorCounts)
    {
        int columns = SelectRandomInclusive(seed + 11, columnsMin, columnsMax);
        int rows = SelectRandomInclusive(seed + 23, rowsMin, rowsMax);
        int tunnelCount = SelectRandomInclusive(seed + 37, tunnelCountMin, tunnelCountMax);
        int targetTokenCount = CountRequestedTokens(targetSpoolsByColor);
        BuildGenerated(
            seed,
            columns,
            rows,
            colorCount,
            tunnelCount,
            tunnelQueueMin,
            tunnelQueueMax,
            targetTokenCount,
            targetSpoolsByColor,
            boardColorCounts);
    }

    private void BuildGenerated(
        int seed,
        int columns,
        int rows,
        int colorCount,
        int tunnelCount,
        int tunnelQueueMin,
        int tunnelQueueMax,
        int targetTokenCount,
        IReadOnlyList<int> targetSpoolsByColor,
        IReadOnlyList<int> boardColorCounts)
    {
        Clear();
        _columns = Math.Max(1, columns);
        _rows = Math.Max(2, rows);
        CreateCells();

        int safeColorCount = Math.Max(1, Math.Min(colorCount, Enum.GetValues(typeof(YarnMatchColor)).Length));
        int boardTokenCount = CountRequiredTokens(boardColorCounts);
        int desiredTokenCount = targetTokenCount > 0
            ? targetTokenCount
            : boardTokenCount > 0 ? boardTokenCount : _cells.Count;
        List<YarnMatchColor> tokenColors = BuildTokenColors(
            desiredTokenCount,
            safeColorCount,
            targetSpoolsByColor,
            boardColorCounts,
            seed);
        desiredTokenCount = tokenColors.Count;

        int maximumTunnelCount = Math.Max(0, Math.Min(_cells.Count / 2, desiredTokenCount / 2));
        int configuredTunnelCount = Math.Min(Math.Max(0, tunnelCount), maximumTunnelCount);
        List<TunnelPlacement> placements = ChooseTunnelPlacements(configuredTunnelCount, seed + 511);

        int visibleTokenCount = Math.Min(
            Math.Max(0, desiredTokenCount - placements.Count),
            Math.Max(0, _cells.Count - placements.Count));
        List<YarnMatchPoolCell> visibleCells = ChooseVisibleCells(visibleTokenCount, placements, seed + 601);
        int hiddenBudget = Math.Max(0, desiredTokenCount - visibleCells.Count);
        List<int> queueCounts = BuildQueueCounts(
            hiddenBudget,
            placements.Count,
            tunnelQueueMin,
            tunnelQueueMax,
            seed + 733);

        List<YarnMatchSpoolToken> tokens = CreateTokens(tokenColors, boardColorCounts);
        for (int index = 0; index < visibleCells.Count; index++)
        {
            AttachToken(visibleCells[index], tokens[index]);
        }

        int queueOffset = visibleCells.Count;
        for (int index = 0; index < placements.Count; index++)
        {
            TunnelPlacement placement = placements[index];
            YarnMatchTunnel tunnel = new YarnMatchTunnel
            {
                Target = placement.PipeCell,
                OutputCell = placement.OutputCell,
                Direction = placement.Direction
            };
            placement.PipeCell.Tunnel = tunnel;
            placement.OutputCell.SourceTunnel = tunnel;
            for (int queueIndex = 0; queueIndex < queueCounts[index]; queueIndex++)
            {
                tunnel.Queue.Add(tokens[queueOffset + queueIndex]);
            }
            queueOffset += queueCounts[index];
            _tunnels.Add(tunnel);
        }

        SetInitialUnlocks();
    }

    private void ApplyGeneratedMechanics(YarnMatchLevelConfig config)
    {
        if (config == null || config.UsesReferencePattern || config.Number < 10)
        {
            return;
        }

        int freezeCount = config.FreezeCount;
        int chainCount = config.ChainCount;
        if (freezeCount <= 0 && chainCount <= 0)
        {
            return;
        }

        YarnMatchGeneratedMechanicLayout layout = YarnMatchMechanicLayoutGenerator.Generate(
            _cells,
            _columns,
            _rows,
            freezeCount,
            chainCount,
            config.Seed + 1703);
        for (int index = 0; index < layout.FreezeCells.Count; index++)
        {
            layout.FreezeCells[index].FreezeHitsRemaining = 3;
        }

        for (int index = 0; index < layout.ChainPairs.Count; index++)
        {
            YarnMatchMechanicPair pair = layout.ChainPairs[index];
            YarnMatchChain chain = new YarnMatchChain
            {
                Id = _chains.Count,
                First = pair.First,
                Second = pair.Second
            };
            pair.First.ChainId = chain.Id;
            pair.Second.ChainId = chain.Id;
            _chains.Add(chain);
        }

        SetInitialUnlocks();
    }

    private void Clear()
    {
        _cells.Clear();
        _tunnels.Clear();
        _chains.Clear();
        _tokens.Clear();
        Array.Clear(_createdTokensByColor, 0, _createdTokensByColor.Length);
    }

    private void CreateCells()
    {
        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                _cells.Add(new YarnMatchPoolCell
                {
                    Column = column,
                    Row = row,
                    Unlocked = false
                });
            }
        }
    }

    private void SetInitialUnlocks()
    {
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchPoolCell cell = _cells[index];
            cell.Unlocked = cell.Tunnel != null
                || (cell.Row == 0 && cell.Token != null && cell.Tunnel == null);
        }

        for (int index = 0; index < _cells.Count; index++)
        {
            if (_cells[index].Tunnel != null)
            {
                UnlockNeighbors(_cells[index]);
            }
        }
    }

    public bool IsSelectable(YarnMatchSpoolToken token)
    {
        if (token == null || !IsSelectableCell(token.Cell))
        {
            return false;
        }

        YarnMatchPoolCell cell = token.Cell;
        if (cell.ChainId < 0)
        {
            return true;
        }

        YarnMatchChain chain = FindChain(cell.ChainId);
        YarnMatchPoolCell partner = GetPartner(chain, cell);
        return IsSelectableCell(partner);
    }

    public int GetSelectionCount(YarnMatchSpoolToken token)
    {
        if (!IsSelectable(token))
        {
            return 0;
        }

        return token.Cell.ChainId < 0 ? 1 : 2;
    }

    public YarnMatchPoolSelection Consume(YarnMatchSpoolToken token)
    {
        return Consume(PreviewSelection(token));
    }

    public YarnMatchPoolSelection PreviewSelection(YarnMatchSpoolToken token)
    {
        List<YarnMatchSpoolToken> selectedTokens = GetSelectionTokens(token);
        if (selectedTokens.Count == 0)
        {
            return null;
        }

        YarnMatchPoolSelection selection = new YarnMatchPoolSelection
        {
            Token = selectedTokens[0],
            SourceCell = selectedTokens[0].Cell,
            Tunnel = selectedTokens[0].Cell.SourceTunnel
        };
        for (int index = 0; index < selectedTokens.Count; index++)
        {
            selection.Tokens.Add(selectedTokens[index]);
            selection.SourceCells.Add(selectedTokens[index].Cell);
        }

        return selection;
    }

    public YarnMatchPoolSelection Consume(YarnMatchPoolSelection selection)
    {
        if (!CanConsume(selection))
        {
            return null;
        }

        for (int index = 0; index < selection.Tokens.Count; index++)
        {
            YarnMatchSpoolToken selected = selection.Tokens[index];
            YarnMatchPoolCell source = selection.SourceCells[index];
            selected.Used = true;
            source.Token = null;
            source.ChainId = -1;
            selected.Cell = null;
        }

        for (int index = 0; index < selection.SourceCells.Count; index++)
        {
            YarnMatchPoolCell source = selection.SourceCells[index];
            UnlockNeighbors(source);
            DamageAdjacentFreezes(source, selection.FreezeChangedCells);
        }
        return selection;
    }

    private bool CanConsume(YarnMatchPoolSelection selection)
    {
        if (selection == null
            || selection.Tokens.Count == 0
            || selection.Tokens.Count != selection.SourceCells.Count
            || !IsSelectable(selection.Tokens[0]))
        {
            return false;
        }

        for (int index = 0; index < selection.Tokens.Count; index++)
        {
            YarnMatchSpoolToken token = selection.Tokens[index];
            YarnMatchPoolCell source = selection.SourceCells[index];
            if (token == null || source == null || token.Used || token.Cell != source || source.Token != token)
            {
                return false;
            }
            if (index > 0 && !IsSelectableCell(source))
            {
                return false;
            }
        }

        return true;
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

    public bool HasRemainingToken(YarnMatchColor color)
    {
        for (int index = 0; index < _tokens.Count; index++)
        {
            if (!_tokens[index].Used && _tokens[index].Color == color)
            {
                return true;
            }
        }
        return false;
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

    public bool Refresh(int seed)
    {
        List<YarnMatchPoolCell> occupiedCells = new List<YarnMatchPoolCell>();
        List<YarnMatchSpoolToken> visibleTokens = new List<YarnMatchSpoolToken>();
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchPoolCell cell = _cells[index];
            if (cell.Token != null && !cell.Token.Used && cell.Tunnel == null)
            {
                occupiedCells.Add(cell);
                visibleTokens.Add(cell.Token);
            }
        }

        YarnMatchRandom.Shuffle(visibleTokens, seed);
        for (int index = 0; index < occupiedCells.Count; index++)
        {
            AttachToken(occupiedCells[index], visibleTokens[index]);
        }

        List<YarnMatchSpoolToken> queued = new List<YarnMatchSpoolToken>();
        List<int> queueSizes = new List<int>();
        for (int tunnelIndex = 0; tunnelIndex < _tunnels.Count; tunnelIndex++)
        {
            List<YarnMatchSpoolToken> queue = _tunnels[tunnelIndex].Queue;
            queueSizes.Add(queue.Count);
            queued.AddRange(queue);
            queue.Clear();
        }
        YarnMatchRandom.Shuffle(queued, seed + 1);
        int cursor = 0;
        for (int tunnelIndex = 0; tunnelIndex < _tunnels.Count; tunnelIndex++)
        {
            for (int queueIndex = 0; queueIndex < queueSizes[tunnelIndex]; queueIndex++)
            {
                _tunnels[tunnelIndex].Queue.Add(queued[cursor++]);
            }
        }
        return occupiedCells.Count > 1 || queued.Count > 1;
    }

    public YarnMatchSpoolToken MakeColorSelectable(YarnMatchColor color)
    {
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchSpoolToken token = _cells[index].Token;
            if (token != null && token.Color == color && token.Cell.ChainId < 0 && IsSelectable(token))
            {
                return token;
            }
        }

        YarnMatchSpoolToken target = null;
        for (int index = 0; index < _tokens.Count; index++)
        {
            YarnMatchSpoolToken token = _tokens[index];
            if (!token.Used && token.Color == color)
            {
                target = token;
                break;
            }
        }
        if (target == null)
        {
            return null;
        }

        YarnMatchPoolCell destination = FindRefreshCell(target.Cell == null);
        if (destination == null)
        {
            return null;
        }

        if (target.Cell != null)
        {
            AttachToken(target.Cell, destination.Token);
        }
        else if (!ReplaceInQueue(target, destination.Token))
        {
            return null;
        }
        AttachToken(destination, target);
        return target;
    }

    private YarnMatchPoolCell FindRefreshCell(bool allowEmpty)
    {
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchPoolCell cell = _cells[index];
            if ((cell.Token != null || allowEmpty)
                && cell.Tunnel == null
                && cell.ChainId < 0
                && cell.FreezeHitsRemaining == 0
                && cell.Unlocked)
            {
                return cell;
            }
        }
        return null;
    }

    private bool ReplaceInQueue(YarnMatchSpoolToken target, YarnMatchSpoolToken replacement)
    {
        for (int tunnelIndex = 0; tunnelIndex < _tunnels.Count; tunnelIndex++)
        {
            List<YarnMatchSpoolToken> queue = _tunnels[tunnelIndex].Queue;
            int index = queue.IndexOf(target);
            if (index >= 0)
            {
                if (replacement == null) queue.RemoveAt(index);
                else
                {
                    queue[index] = replacement;
                    replacement.Cell = null;
                }
                return true;
            }
        }
        return false;
    }

    private List<YarnMatchSpoolToken> GetSelectionTokens(YarnMatchSpoolToken token)
    {
        List<YarnMatchSpoolToken> result = new List<YarnMatchSpoolToken>();
        if (!IsSelectable(token))
        {
            return result;
        }

        result.Add(token);
        if (token.Cell.ChainId < 0)
        {
            return result;
        }

        YarnMatchChain chain = FindChain(token.Cell.ChainId);
        YarnMatchPoolCell partner = GetPartner(chain, token.Cell);
        if (partner == null || partner.Token == null)
        {
            result.Clear();
            return result;
        }
        result.Add(partner.Token);
        return result;
    }

    private bool IsSelectableCell(YarnMatchPoolCell cell)
    {
        return cell != null
            && cell.Tunnel == null
            && cell.Token != null
            && !cell.Token.Used
            && cell.Unlocked
            && cell.FreezeHitsRemaining <= 0;
    }

    private void UnlockNeighbors(YarnMatchPoolCell source)
    {
        if (source == null)
        {
            return;
        }
        source.Unlocked = true;
        UnlockCell(source.Column, source.Row + 1);
        UnlockCell(source.Column + 1, source.Row);
        UnlockCell(source.Column, source.Row - 1);
        UnlockCell(source.Column - 1, source.Row);
    }

    private void UnlockCell(int column, int row)
    {
        YarnMatchPoolCell cell = FindCell(column, row);
        if (cell == null)
        {
            return;
        }
        if (cell.Tunnel != null || cell.Token != null || cell.SourceTunnel != null)
        {
            cell.Unlocked = true;
        }
    }

    private void DamageAdjacentFreezes(YarnMatchPoolCell source, List<YarnMatchPoolCell> changed)
    {
        for (int row = source.Row - 1; row <= source.Row + 1; row++)
        {
            for (int column = source.Column - 1; column <= source.Column + 1; column++)
            {
                if (column == source.Column && row == source.Row)
                {
                    continue;
                }

                YarnMatchPoolCell neighbor = FindCell(column, row);
                if (neighbor == null || neighbor.FreezeHitsRemaining <= 0)
                {
                    continue;
                }

                neighbor.FreezeHitsRemaining--;
                if (neighbor.FreezeHitsRemaining == 0)
                {
                    // Breaking the last ice layer is itself an unlock. This is
                    // important for diagonal hits where ordinary four-way
                    // propagation would not reach the cell.
                    neighbor.Unlocked = true;
                }
                if (!changed.Contains(neighbor))
                {
                    changed.Add(neighbor);
                }
            }
        }
    }

    private YarnMatchChain FindChain(int id)
    {
        for (int index = 0; index < _chains.Count; index++)
        {
            if (_chains[index].Id == id)
            {
                return _chains[index];
            }
        }
        return null;
    }

    private static YarnMatchPoolCell GetPartner(YarnMatchChain chain, YarnMatchPoolCell cell)
    {
        if (chain == null || cell == null)
        {
            return null;
        }
        return chain.First == cell ? chain.Second : chain.First;
    }

    private List<YarnMatchSpoolToken> CreateTokens(
        IReadOnlyList<YarnMatchColor> colors,
        IReadOnlyList<int> boardColorCounts)
    {
        List<YarnMatchSpoolToken> result = new List<YarnMatchSpoolToken>(colors.Count);
        for (int index = 0; index < colors.Count; index++)
        {
            result.Add(CreateToken(index, colors[index], boardColorCounts));
        }
        return result;
    }

    private YarnMatchSpoolToken CreateToken(
        int id,
        YarnMatchColor color,
        IReadOnlyList<int> boardColorCounts)
    {
        int capacity = YarnMatchRackModel.CellsPerSpool;
        if (boardColorCounts != null && (int)color < boardColorCounts.Count)
        {
            int ordinal = _createdTokensByColor[(int)color];
            int remaining = boardColorCounts[(int)color] - ordinal * YarnMatchRackModel.CellsPerSpool;
            capacity = Math.Max(1, Math.Min(YarnMatchRackModel.CellsPerSpool, remaining));
        }

        YarnMatchSpoolToken token = new YarnMatchSpoolToken
        {
            Id = id,
            Color = color,
            Capacity = capacity
        };
        _tokens.Add(token);
        _createdTokensByColor[(int)color]++;
        return token;
    }

    private void AttachToken(YarnMatchPoolCell cell, YarnMatchSpoolToken token)
    {
        cell.Token = token;
        token.Cell = cell;
    }

    private List<YarnMatchColor> BuildTokenColors(
        int targetTokenCount,
        int safeColorCount,
        IReadOnlyList<int> targetSpoolsByColor,
        IReadOnlyList<int> boardColorCounts,
        int seed)
    {
        List<YarnMatchColor> colors = new List<YarnMatchColor>(Math.Max(0, targetTokenCount));
        if (CountRequestedTokens(targetSpoolsByColor) > 0)
        {
            for (int color = 0; color < targetSpoolsByColor.Count; color++)
            {
                for (int count = 0; count < Math.Max(0, targetSpoolsByColor[color]); count++)
                {
                    colors.Add((YarnMatchColor)color);
                }
            }
        }
        else if (CountRequiredTokens(boardColorCounts) > 0)
        {
            int colorLimit = Math.Min(boardColorCounts.Count, Enum.GetValues(typeof(YarnMatchColor)).Length);
            for (int color = 0; color < colorLimit; color++)
            {
                int spoolCount = (Math.Max(0, boardColorCounts[color]) + YarnMatchRackModel.CellsPerSpool - 1)
                    / YarnMatchRackModel.CellsPerSpool;
                for (int count = 0; count < spoolCount; count++)
                {
                    colors.Add((YarnMatchColor)color);
                }
            }
        }
        else
        {
            for (int index = 0; index < targetTokenCount; index++)
            {
                colors.Add((YarnMatchColor)(index % safeColorCount));
            }
        }
        YarnMatchRandom.Shuffle(colors, seed);
        return colors;
    }

    private static int CountRequiredTokens(IReadOnlyList<int> boardColorCounts)
    {
        if (boardColorCounts == null)
        {
            return 0;
        }

        int total = 0;
        for (int index = 0; index < boardColorCounts.Count; index++)
        {
            total += (Math.Max(0, boardColorCounts[index]) + YarnMatchRackModel.CellsPerSpool - 1)
                / YarnMatchRackModel.CellsPerSpool;
        }
        return total;
    }

    private List<YarnMatchPoolCell> ChooseVisibleCells(
        int count,
        IReadOnlyList<TunnelPlacement> placements,
        int seed)
    {
        List<YarnMatchPoolCell> result = new List<YarnMatchPoolCell>(Math.Max(0, count));
        HashSet<YarnMatchPoolCell> pipeCells = new HashSet<YarnMatchPoolCell>();
        for (int index = 0; index < placements.Count; index++)
        {
            pipeCells.Add(placements[index].PipeCell);
            if (result.Count < count && !result.Contains(placements[index].OutputCell))
            {
                result.Add(placements[index].OutputCell);
            }
        }

        return YarnMatchReachabilityPlanner.GrowVisibleCells(_cells, _columns, _rows,
            count, pipeCells, result, seed);
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
        List<int> counts = new List<int>(Math.Max(0, tunnelCount));
        if (tunnelCount <= 0)
        {
            return counts;
        }

        int total = Math.Max(0, hiddenBudget);
        int safeMinimum = Math.Max(0, requestedMin);
        int safeMaximum = Math.Max(safeMinimum, requestedMax);
        int minimum = Math.Min(safeMinimum, total / tunnelCount);
        if (safeMinimum == 0 && total >= tunnelCount)
        {
            minimum = 1;
        }
        int averageMaximum = (total + tunnelCount - 1) / tunnelCount;
        int variationLimit = Math.Max(1, averageMaximum / 2);
        int balancedMaximum = averageMaximum + variationLimit;
        int maximum = Math.Max(
            minimum,
            Math.Max(averageMaximum, Math.Min(safeMaximum, balancedMaximum)));
        for (int index = 0; index < tunnelCount; index++)
        {
            counts.Add(minimum);
        }

        int remaining = total - minimum * tunnelCount;
        System.Random random = new System.Random(seed);
        List<int> candidates = new List<int>(tunnelCount);
        while (remaining > 0)
        {
            candidates.Clear();
            for (int index = 0; index < counts.Count; index++)
            {
                if (counts[index] < maximum)
                {
                    candidates.Add(index);
                }
            }
            if (candidates.Count == 0)
            {
                maximum++;
                continue;
            }
            int target = candidates[random.Next(candidates.Count)];
            int increase = Math.Min(remaining, Math.Max(1, random.Next(1, maximum - counts[target] + 1)));
            counts[target] += increase;
            remaining -= increase;
        }
        return counts;
    }

    private List<TunnelPlacement> ChooseTunnelPlacements(int count, int seed)
    {
        List<TunnelPlacement> candidates = new List<TunnelPlacement>();
        int[,] offsets = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
        for (int index = 0; index < _cells.Count; index++)
        {
            YarnMatchPoolCell pipe = _cells[index];
            if (pipe.Row == 0)
            {
                continue;
            }
            for (int direction = 0; direction < 4; direction++)
            {
                YarnMatchPoolCell output = FindCell(
                    pipe.Column + offsets[direction, 0],
                    pipe.Row + offsets[direction, 1]);
                if (output != null)
                {
                    candidates.Add(new TunnelPlacement
                    {
                        PipeCell = pipe,
                        OutputCell = output,
                        Direction = (YarnMatchTunnelDirection)direction
                    });
                }
            }
        }
        YarnMatchRandom.Shuffle(candidates, seed);
        List<TunnelPlacement> result = new List<TunnelPlacement>(Math.Max(0, count));
        HashSet<YarnMatchPoolCell> usedPipes = new HashSet<YarnMatchPoolCell>();
        HashSet<YarnMatchPoolCell> usedOutputs = new HashSet<YarnMatchPoolCell>();
        for (int index = 0; index < candidates.Count && result.Count < count; index++)
        {
            TunnelPlacement candidate = candidates[index];
            if (usedPipes.Contains(candidate.PipeCell)
                || usedPipes.Contains(candidate.OutputCell)
                || usedOutputs.Contains(candidate.PipeCell)
                || usedOutputs.Contains(candidate.OutputCell))
            {
                continue;
            }
            usedPipes.Add(candidate.PipeCell);
            usedOutputs.Add(candidate.OutputCell);
            result.Add(candidate);
        }
        return result;
    }

    private YarnMatchPoolCell FindCellByIndex(int index)
    {
        return index < 0 || index >= _cells.Count ? null : _cells[index];
    }

    private YarnMatchPoolCell FindCell(int column, int row)
    {
        if (column < 0 || column >= _columns || row < 0 || row >= _rows)
        {
            return null;
        }
        return _cells[row * _columns + column];
    }

    private static int SelectRandomInclusive(int seed, int min, int max)
    {
        int safeMin = Math.Min(min, max);
        int safeMax = Math.Max(min, max);
        return safeMin == safeMax ? safeMin : new System.Random(seed).Next(safeMin, safeMax + 1);
    }

    private sealed class TunnelPlacement
    {
        internal YarnMatchPoolCell PipeCell;
        internal YarnMatchPoolCell OutputCell;
        internal YarnMatchTunnelDirection Direction;
    }
}
