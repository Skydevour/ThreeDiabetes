using System;
using System.Collections.Generic;

internal static class YarnMatchTemplatePalette
{
    // Existing spool colors, with contrasting neutrals for outlines and image backgrounds.
    private static readonly YarnMatchColor[] GameColors =
    {
        YarnMatchColor.Coral, YarnMatchColor.Orange, YarnMatchColor.Butter,
        YarnMatchColor.Mint, YarnMatchColor.Cyan, YarnMatchColor.Blue,
        YarnMatchColor.Violet, YarnMatchColor.Pink, YarnMatchColor.ReferenceCream,
        YarnMatchColor.ReferenceCocoa, YarnMatchColor.ReferenceWhite, YarnMatchColor.ReferenceSky
    };
    private static readonly int[] Rgb =
    {
        0xF5384D, 0xFC7D14, 0xFFBF0D, 0x1AB861, 0x08B3E6, 0x1F5CE0,
        0x8038D1, 0xF23394, 0xFFEEAD, 0x753832, 0xFEFFFF, 0x7CC4FF
    };

    internal static int[] Build(int[] source, IReadOnlyList<int> symbols, int colorCount,
        out List<YarnMatchColor> palette)
    {
        int[] frequencies = new int[source.Length];
        foreach (int symbol in symbols) frequencies[symbol]++;
        int dominant = 0;
        for (int i = 1; i < frequencies.Length; i++)
            if (frequencies[i] > frequencies[dominant]) dominant = i;

        var representatives = new List<int> { dominant };
        while (representatives.Count < colorCount)
        {
            int best = -1;
            double bestScore = -1;
            for (int i = 0; i < source.Length; i++)
            {
                if (frequencies[i] == 0 || representatives.Contains(i)) continue;
                int distance = int.MaxValue;
                foreach (int other in representatives)
                    distance = Math.Min(distance, Distance(source[i], source[other]));
                double score = distance * Math.Sqrt(frequencies[i]);
                if (score > bestScore) { best = i; bestScore = score; }
            }
            if (best < 0) break;
            representatives.Add(best);
        }

        // Distinct retained regions get distinct spool colors before any shades are merged.
        var assigned = new List<int>();
        int[] map = new int[source.Length];
        palette = new List<YarnMatchColor>();
        foreach (int symbol in representatives)
        {
            int best = -1, distance = int.MaxValue;
            for (int color = 0; color < Rgb.Length; color++)
            {
                if (assigned.Contains(color)) continue;
                int candidate = Distance(source[symbol], Rgb[color]);
                if (candidate < distance) { best = color; distance = candidate; }
            }
            assigned.Add(best);
            map[symbol] = (int)GameColors[best];
            palette.Add(GameColors[best]);
        }
        for (int i = 0; i < source.Length; i++)
        {
            if (representatives.Contains(i)) continue;
            int best = dominant, distance = int.MaxValue;
            foreach (int other in representatives)
            {
                int candidate = Distance(source[i], source[other]);
                if (candidate < distance) { best = other; distance = candidate; }
            }
            map[i] = map[best];
        }

        return map;
    }

    private static int Distance(int a, int b)
    {
        int r = (a >> 16 & 255) - (b >> 16 & 255);
        int g = (a >> 8 & 255) - (b >> 8 & 255);
        int blue = (a & 255) - (b & 255);
        return 2 * r * r + 4 * g * g + 3 * blue * blue;
    }
}
