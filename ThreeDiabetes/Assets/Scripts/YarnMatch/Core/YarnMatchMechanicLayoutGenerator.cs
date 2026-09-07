using System;
using System.Collections.Generic;

internal sealed class YarnMatchMechanicPair
{
    internal YarnMatchPoolCell First;
    internal YarnMatchPoolCell Second;
}

internal sealed class YarnMatchGeneratedMechanicLayout
{
    internal readonly List<YarnMatchPoolCell> FreezeCells = new List<YarnMatchPoolCell>();
    internal readonly List<YarnMatchMechanicPair> ChainPairs = new List<YarnMatchMechanicPair>();
}

internal static class YarnMatchMechanicLayoutGenerator
{
    internal static YarnMatchGeneratedMechanicLayout Generate(
        IReadOnlyList<YarnMatchPoolCell> cells, int columns, int rows,
        int freezeCount, int chainCount, int seed)
    {
        var layout = new YarnMatchGeneratedMechanicLayout();
        var candidates = new List<YarnMatchPoolCell>();
        foreach (YarnMatchPoolCell cell in cells)
            if (cell.Token != null && cell.Tunnel == null && cell.Row > 0) candidates.Add(cell);
        YarnMatchRandom.Shuffle(candidates, seed);
        var paired = new HashSet<YarnMatchPoolCell>();
        foreach (YarnMatchPoolCell first in candidates)
        {
            if (layout.ChainPairs.Count >= chainCount) break;
            if (paired.Contains(first)) continue;
            var neighbors = new List<YarnMatchPoolCell>(
                YarnMatchReachabilityPlanner.Neighbors(cells, columns, rows, first, false));
            YarnMatchRandom.Shuffle(neighbors, seed + first.Row * columns + first.Column);
            foreach (YarnMatchPoolCell second in neighbors)
            {
                if (second.Token == null || second.Row == 0 || paired.Contains(second)) continue;
                var pair = new YarnMatchMechanicPair { First = first, Second = second };
                layout.ChainPairs.Add(pair);
                if (!YarnMatchReachabilityPlanner.IsLayoutReachable(cells, columns, rows, layout))
                {
                    layout.ChainPairs.RemoveAt(layout.ChainPairs.Count - 1);
                    continue;
                }
                paired.Add(first);
                paired.Add(second);
                break;
            }
        }

        YarnMatchRandom.Shuffle(candidates, seed + 23);
        foreach (YarnMatchPoolCell candidate in candidates)
        {
            if (layout.FreezeCells.Count >= freezeCount) break;
            bool adjacentFreeze = false;
            foreach (YarnMatchPoolCell other in layout.FreezeCells)
                adjacentFreeze |= Math.Abs(other.Column - candidate.Column) <= 1
                    && Math.Abs(other.Row - candidate.Row) <= 1;
            if (adjacentFreeze) continue;
            layout.FreezeCells.Add(candidate);
            if (!YarnMatchReachabilityPlanner.IsLayoutReachable(cells, columns, rows, layout))
                layout.FreezeCells.RemoveAt(layout.FreezeCells.Count - 1);
        }
        return layout;
    }
}
