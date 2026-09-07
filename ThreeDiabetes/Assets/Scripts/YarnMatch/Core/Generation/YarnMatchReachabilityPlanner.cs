using System;
using System.Collections.Generic;

internal static class YarnMatchReachabilityPlanner
{
    internal static List<YarnMatchPoolCell> GrowVisibleCells(
        IReadOnlyList<YarnMatchPoolCell> cells, int columns, int rows, int count,
        HashSet<YarnMatchPoolCell> pipes, List<YarnMatchPoolCell> outputs, int seed)
    {
        List<YarnMatchPoolCell> result = new List<YarnMatchPoolCell>(outputs);
        HashSet<YarnMatchPoolCell> selected = new HashSet<YarnMatchPoolCell>(outputs);
        List<YarnMatchPoolCell> frontier = new List<YarnMatchPoolCell>();
        Random random = new Random(seed);
        while (result.Count < count)
        {
            frontier.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                YarnMatchPoolCell cell = cells[i];
                if (pipes.Contains(cell) || selected.Contains(cell)) continue;
                bool reachable = cell.Row == 0;
                foreach (YarnMatchPoolCell neighbor in Neighbors(cells, columns, rows, cell, false))
                    reachable |= pipes.Contains(neighbor) || selected.Contains(neighbor);
                if (reachable) frontier.Add(cell);
            }
            YarnMatchPoolCell next = frontier[random.Next(frontier.Count)];
            selected.Add(next);
            result.Add(next);
        }
        YarnMatchRandom.Shuffle(result, seed + 1);
        return result;
    }

    // This models access only, with distinct visible sources. It never mutates the live pool.
    internal static bool[] CollectReachable(IReadOnlyList<YarnMatchPoolCell> cells,
        int columns, int rows, YarnMatchGeneratedMechanicLayout layout, bool allowThaw)
    {
        int count = cells.Count;
        bool[] unlocked = new bool[count];
        bool[] collected = new bool[count];
        int[] frozen = new int[count];
        int[] partners = new int[count];
        for (int i = 0; i < count; i++)
        {
            unlocked[i] = cells[i].Unlocked;
            partners[i] = -1;
        }
        foreach (YarnMatchPoolCell cell in layout.FreezeCells) frozen[Index(cell, columns)] = 3;
        foreach (YarnMatchMechanicPair pair in layout.ChainPairs)
        {
            int first = Index(pair.First, columns);
            int second = Index(pair.Second, columns);
            partners[first] = second;
            partners[second] = first;
        }
        bool changed;
        do
        {
            changed = false;
            for (int i = 0; i < count; i++)
            {
                if (!Ready(i, cells, unlocked, collected, frozen)) continue;
                int partner = partners[i];
                if (partner >= 0 && !Ready(partner, cells, unlocked, collected, frozen)) continue;
                Take(i);
                if (partner >= 0) Take(partner);
                changed = true;
            }
        } while (changed);
        return collected;

        void Take(int index)
        {
            collected[index] = true;
            foreach (YarnMatchPoolCell neighbor in Neighbors(cells, columns, rows, cells[index], false))
                unlocked[Index(neighbor, columns)] = true;
            if (!allowThaw) return;
            foreach (YarnMatchPoolCell neighbor in Neighbors(cells, columns, rows, cells[index], true))
            {
                int other = Index(neighbor, columns);
                if (frozen[other] > 0 && --frozen[other] == 0) unlocked[other] = true;
            }
        }
    }

    internal static bool IsLayoutReachable(IReadOnlyList<YarnMatchPoolCell> cells,
        int columns, int rows, YarnMatchGeneratedMechanicLayout layout)
    {
        bool[] independent = CollectReachable(cells, columns, rows, layout, false);
        foreach (YarnMatchPoolCell freeze in layout.FreezeCells)
        {
            int sources = 0;
            foreach (YarnMatchPoolCell neighbor in Neighbors(cells, columns, rows, freeze, true))
                if (independent[Index(neighbor, columns)]) sources++;
            if (sources < 4) return false;
        }
        bool[] reachable = layout.FreezeCells.Count == 0
            ? independent : CollectReachable(cells, columns, rows, layout, true);
        for (int i = 0; i < cells.Count; i++)
            if (cells[i].Token != null && !reachable[i]) return false;
        return true;
    }

    private static bool Ready(int index, IReadOnlyList<YarnMatchPoolCell> cells,
        bool[] unlocked, bool[] collected, int[] frozen)
    {
        return cells[index].Token != null && cells[index].Tunnel == null
            && unlocked[index] && !collected[index] && frozen[index] == 0;
    }

    internal static IEnumerable<YarnMatchPoolCell> Neighbors(
        IReadOnlyList<YarnMatchPoolCell> cells, int columns, int rows,
        YarnMatchPoolCell cell, bool diagonals)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if ((dx == 0 && dy == 0) || (!diagonals && Math.Abs(dx) + Math.Abs(dy) != 1)) continue;
            int x = cell.Column + dx, y = cell.Row + dy;
            if (x >= 0 && x < columns && y >= 0 && y < rows) yield return cells[y * columns + x];
        }
    }

    private static int Index(YarnMatchPoolCell cell, int columns) => cell.Row * columns + cell.Column;
}
