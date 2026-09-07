using System;
using System.Collections.Generic;

internal static class YarnMatchBoardPatternGenerator
{
    internal static IReadOnlyList<YarnMatchColor> Generate(
        IReadOnlyList<int> columnHeights,
        int colorCount,
        int seed)
    {
        return Generate(columnHeights, colorCount, seed, false);
    }

    internal static IReadOnlyList<YarnMatchColor> Generate(
        IReadOnlyList<int> columnHeights,
        int colorCount,
        int seed,
        bool useGraphicPattern)
    {
        int safeColorCount = Math.Max(1, Math.Min(colorCount, Enum.GetValues(typeof(YarnMatchColor)).Length));
        int totalCells = CountCells(columnHeights);
        int groupCount = (totalCells + YarnMatchRackModel.CellsPerSpool - 1) / YarnMatchRackModel.CellsPerSpool;
        int[] groupColors = useGraphicPattern
            ? BuildGraphicGroupPattern(columnHeights, safeColorCount, groupCount, seed)
            : BuildRandomGroupPattern(safeColorCount, groupCount, seed);
        List<YarnMatchColor> pattern = ExpandGroups(groupColors, totalCells);

        if (!useGraphicPattern)
        {
            YarnMatchRandom.Shuffle(pattern, seed + 97);
        }

        return pattern;
    }

    private static List<YarnMatchColor> ExpandGroups(int[] groupColors, int totalCells)
    {
        List<YarnMatchColor> pattern = new List<YarnMatchColor>(totalCells);
        for (int groupIndex = 0; groupIndex < groupColors.Length && pattern.Count < totalCells; groupIndex++)
        {
            for (int cellIndex = 0; cellIndex < YarnMatchRackModel.CellsPerSpool && pattern.Count < totalCells; cellIndex++)
            {
                pattern.Add((YarnMatchColor)groupColors[groupIndex]);
            }
        }
        return pattern;
    }

    private static int[] BuildRandomGroupPattern(int colorCount, int groupCount, int seed)
    {
        int[] colorOrder = BuildColorOrder(colorCount, seed);
        int[] groupColors = new int[groupCount];
        int baseGroupsPerColor = groupCount / colorCount;
        int extraGroups = groupCount % colorCount;
        int cursor = 0;

        for (int orderIndex = 0; orderIndex < colorOrder.Length; orderIndex++)
        {
            int groups = baseGroupsPerColor + (orderIndex < extraGroups ? 1 : 0);
            for (int group = 0; group < groups; group++)
            {
                groupColors[cursor++] = colorOrder[orderIndex];
            }
        }

        return groupColors;
    }

    private static int[] BuildGraphicGroupPattern(
        IReadOnlyList<int> columnHeights,
        int colorCount,
        int groupCount,
        int seed)
    {
        int[] groupColors = new int[groupCount];
        int cursor = 0;
        int column = 0;
        int columnStart = 0;
        while (column < columnHeights.Count && columnHeights[column] <= 0)
        {
            columnStart += Math.Max(0, columnHeights[column]);
            column++;
        }

        for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
        {
            while (column < columnHeights.Count
                && cursor >= columnStart + Math.Max(0, columnHeights[column]))
            {
                columnStart += Math.Max(0, columnHeights[column]);
                column++;
            }

            if (column >= columnHeights.Count)
            {
                groupColors[groupIndex] = groupIndex % Math.Max(1, colorCount);
                continue;
            }

            int columnHeight = Math.Max(1, columnHeights[column]);
            int macroRows = (columnHeight + YarnMatchRackModel.CellsPerSpool - 1) / YarnMatchRackModel.CellsPerSpool;
            int macroRow = Math.Max(0, (cursor - columnStart) / YarnMatchRackModel.CellsPerSpool);
            float x = columnHeights.Count <= 1 ? 0.5f : column / (float)(columnHeights.Count - 1);
            float y = macroRows <= 1 ? 0.5f : macroRow / (float)(macroRows - 1);
            float ring = MathfLikeDistance(x, y);
            int band = (int)(ring * Math.Max(1, colorCount) * 1.8f);
            int stripe = ((column * 7 + macroRow * 11 + seed) & int.MaxValue) % 3;
            int color = (band + stripe + groupIndex / Math.Max(1, groupCount / Math.Max(1, colorCount))) % colorCount;
            if (groupIndex < colorCount)
            {
                color = groupIndex;
            }
            groupColors[groupIndex] = color;
            cursor += YarnMatchRackModel.CellsPerSpool;
        }

        return groupColors;
    }

    private static float MathfLikeDistance(float x, float y)
    {
        float centeredX = Math.Abs(x - 0.5f) * 2f;
        float centeredY = Math.Abs(y - 0.5f) * 2f;
        return Math.Min(1f, (centeredX * 0.62f + centeredY * 0.38f));
    }

    private static int CountCells(IReadOnlyList<int> columnHeights)
    {
        int total = 0;
        for (int index = 0; index < columnHeights.Count; index++)
        {
            total += Math.Max(0, columnHeights[index]);
        }
        return total;
    }

    private static int[] BuildColorOrder(int colorCount, int seed)
    {
        int[] result = new int[colorCount];
        for (int index = 0; index < colorCount; index++)
        {
            result[index] = index;
        }

        System.Random random = new System.Random(seed);
        for (int index = result.Length - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            int value = result[index];
            result[index] = result[other];
            result[other] = value;
        }
        return result;
    }
}
