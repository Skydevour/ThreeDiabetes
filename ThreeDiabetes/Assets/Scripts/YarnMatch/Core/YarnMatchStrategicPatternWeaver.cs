using System;
using System.Collections.Generic;

internal static class YarnMatchStrategicPatternWeaver
{
    private const int HeldColorLayerLimit = 2;

    internal static IReadOnlyList<YarnMatchColor> Weave(
        IReadOnlyList<YarnMatchColor> source,
        IReadOnlyList<int> columnHeights,
        int colorCount,
        int seed,
        bool preservePattern)
    {
        int safeColorCount = Math.Max(1, Math.Min(colorCount, Enum.GetValues(typeof(YarnMatchColor)).Length));
        int[] remaining = CountColors(source);
        int[] columnStarts = BuildColumnStarts(columnHeights);
        YarnMatchColor[] result = new YarnMatchColor[source.Count];
        int maxRows = GetMaximumHeight(columnHeights);
        int heldColorCount = safeColorCount > 1
            ? Math.Max(1, Math.Min(safeColorCount - 1, safeColorCount / 2))
            : 0;
        int rotationOffset = (seed & int.MaxValue) % safeColorCount;
        System.Random random = new System.Random(seed + 193);

        for (int row = 0; row < maxRows; row++)
        {
            List<int> columns = GetColumnsAtRow(columnHeights, row);
            if (!preservePattern)
            {
                YarnMatchRandom.Shuffle(columns, seed + row * 101);
            }

            bool[] heldColors = BuildHeldColors(safeColorCount, heldColorCount, rotationOffset, row);
            int[] layerCounts = new int[remaining.Length];
            for (int position = 0; position < columns.Count; position++)
            {
                int column = columns[position];
                int index = columnStarts[column] + row;
                int belowColor = row > 0 ? (int)result[index - 1] : -1;
                bool enforceHeldLimit = HasCandidate(remaining, layerCounts, heldColors, -1, true);
                bool avoidVerticalMatch = HasCandidate(remaining, layerCounts, heldColors, belowColor, enforceHeldLimit);
                int selectedColor = SelectColor(
                    source[index],
                    remaining,
                    layerCounts,
                    heldColors,
                    belowColor,
                    enforceHeldLimit,
                    avoidVerticalMatch,
                    preservePattern,
                    random);

                result[index] = (YarnMatchColor)selectedColor;
                remaining[selectedColor]--;
                layerCounts[selectedColor]++;
            }
        }

        return result;
    }

    private static int SelectColor(
        YarnMatchColor preferredColor,
        IReadOnlyList<int> remaining,
        IReadOnlyList<int> layerCounts,
        IReadOnlyList<bool> heldColors,
        int belowColor,
        bool enforceHeldLimit,
        bool avoidVerticalMatch,
        bool preservePattern,
        System.Random random)
    {
        int selected = -1;
        int bestScore = int.MinValue;
        for (int color = 0; color < remaining.Count; color++)
        {
            if (remaining[color] <= 0
                || (enforceHeldLimit && heldColors[color] && layerCounts[color] >= HeldColorLayerLimit)
                || (avoidVerticalMatch && color == belowColor))
            {
                continue;
            }

            int score = remaining[color] * 3 - layerCounts[color] * 80 + random.Next(29);
            if (heldColors[color] && layerCounts[color] < HeldColorLayerLimit)
            {
                score += 24;
            }
            if (preservePattern && color == (int)preferredColor)
            {
                score += 140;
            }

            if (score > bestScore)
            {
                bestScore = score;
                selected = color;
            }
        }

        if (selected >= 0)
        {
            return selected;
        }

        for (int color = 0; color < remaining.Count; color++)
        {
            if (remaining[color] > 0)
            {
                return color;
            }
        }

        return 0;
    }

    private static bool HasCandidate(
        IReadOnlyList<int> remaining,
        IReadOnlyList<int> layerCounts,
        IReadOnlyList<bool> heldColors,
        int excludedColor,
        bool enforceHeldLimit)
    {
        for (int color = 0; color < remaining.Count; color++)
        {
            if (remaining[color] <= 0 || color == excludedColor)
            {
                continue;
            }
            if (!enforceHeldLimit || !heldColors[color] || layerCounts[color] < HeldColorLayerLimit)
            {
                return true;
            }
        }

        return false;
    }

    private static bool[] BuildHeldColors(int colorCount, int heldColorCount, int rotationOffset, int row)
    {
        bool[] result = new bool[Enum.GetValues(typeof(YarnMatchColor)).Length];
        int firstColor = (rotationOffset + row * Math.Max(1, heldColorCount)) % colorCount;
        for (int index = 0; index < heldColorCount; index++)
        {
            result[(firstColor + index) % colorCount] = true;
        }
        return result;
    }

    private static int[] CountColors(IReadOnlyList<YarnMatchColor> source)
    {
        int[] result = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
        for (int index = 0; index < source.Count; index++)
        {
            result[(int)source[index]]++;
        }
        return result;
    }

    private static int[] BuildColumnStarts(IReadOnlyList<int> columnHeights)
    {
        int[] result = new int[columnHeights.Count];
        int cursor = 0;
        for (int column = 0; column < columnHeights.Count; column++)
        {
            result[column] = cursor;
            cursor += Math.Max(0, columnHeights[column]);
        }
        return result;
    }

    private static int GetMaximumHeight(IReadOnlyList<int> columnHeights)
    {
        int result = 0;
        for (int column = 0; column < columnHeights.Count; column++)
        {
            result = Math.Max(result, Math.Max(0, columnHeights[column]));
        }
        return result;
    }

    private static List<int> GetColumnsAtRow(IReadOnlyList<int> columnHeights, int row)
    {
        List<int> result = new List<int>();
        for (int column = 0; column < columnHeights.Count; column++)
        {
            if (row < Math.Max(0, columnHeights[column]))
            {
                result.Add(column);
            }
        }
        return result;
    }
}
