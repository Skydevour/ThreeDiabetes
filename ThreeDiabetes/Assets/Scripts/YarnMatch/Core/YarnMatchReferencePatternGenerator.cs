using System;
using System.Collections.Generic;

internal static class YarnMatchReferencePatternGenerator
{
    private const int Columns = 48;
    private const int Rows = 40;

    // The matrix is baked from the supplied workbook: one character is one filled cell.
    private static readonly string[] ReferenceMap =
    {
        ".aa................bbbbbb.......................",
        ".aa.............bbbccccccbb.....................",
        ".aa...........bbcccddddddccbb...................",
        ".aa..........bccdddddddddddcccb.................",
        ".aa.........bcddddddddddddddddcb................",
        ".aa........bcddddddddddddddddddcb...............",
        ".aa.......bcddddddddddddddddddddccb.............",
        ".aa......bcdddddddddddddddddddddedcb............",
        ".aa.....bcddddddddfdddddddfddddddedcb...........",
        ".aa....bcdddddddddddddddddddddddddddcb..........",
        ".aa...bcdddddddddddddddddggdddedcb..............",
        ".aa..bcdddedddddddddddddddddggggddedcb..........",
        ".....bcdddedddddhgddddddddddiiggdjeddcb.........",
        "....bcdddeddddddgggdddddddddiiggdjeddcb.........",
        "....bcdddeddddddigghddddddddggkhddedddcb........",
        "...bcddddedddjddggghdddddffddkkdllledddcbbb.....",
        "..bcdddddeddddddgkkhddddldfddddlllleddddcccbb...",
        "..bcdddddejdddddkkkhdddddfdddddllldeddddddfccb..",
        ".bcddddddejjdmmmmhhdddddddddddddddjeddddddfffb..",
        ".bcdddddddejjmmlllddddddddddddddddejdddddddffcb.",
        ".bcdddddddejjmlllldddddddddddddddeejdddddddfffcb",
        ".bcddddddddejjjmdddddddddeeemmmdcjjjjddddddfffcb",
        ".bcdddddddddejjjmdddedddedddeefccjjjjjjddddfffcb",
        ".bcdddddddddeejjjjjedemdeddddccbbcjjjjdddddfffcb",
        "bcdddddddddddcccfjedddeledddddcb.bcjjjjdddfffcb.",
        "bcdddddddddddcbbccddddejeddddddcb.bcjjjjjffffcb.",
        "bcddddddddddffcbcddddddejedddddcb..bccjffffccb..",
        "bcdddddddddfffcbcddmeddejjdddddccb..bbcccccbb...",
        "bcfddddddfffffcbcddmmeefjjdddddejcb...bbbbb.....",
        "bcffffffffffffcbcdmmmmmddedddddedjcb............",
        "bcffffffffffffcbcdmmmmmdededddddddcb............",
        ".cfffffffffffcbbcdmmmmmeddedddddddcb............",
        ".bfffffffffffcbbcddmmmedddedddddfcb.............",
        ".bcfffffffffcbbcjedddddddeddddeccb..............",
        ".bcfffffffffcbbcjdeddddddddecccbb...............",
        "..bccfffffccb.bcjddeeeddddccbbb.................",
        "...bbcccccbb..bcjjdjjjccccbb....................",
        ".....bbbbb.....bccccccbbbb......................",
        "................bbbbbb..........................",
        "................................................",
    };

    internal static IReadOnlyList<YarnMatchColor> Generate(IReadOnlyList<int> columnHeights)
    {
        if (columnHeights.Count != Columns)
        {
            throw new ArgumentException("The reference level requires 48 board columns.", nameof(columnHeights));
        }

        for (int column = 0; column < columnHeights.Count; column++)
        {
            if (columnHeights[column] != Rows)
            {
                throw new ArgumentException("The reference level requires 40 rows per column.", nameof(columnHeights));
            }
        }

        List<YarnMatchColor> pattern = new List<YarnMatchColor>(Columns * Rows);
        for (int column = 0; column < Columns; column++)
        {
            for (int row = 0; row < Rows; row++)
            {
                char symbol = ReferenceMap[Rows - 1 - row][column];
                pattern.Add(MapSymbol(symbol));
            }
        }

        BalanceSpoolCounts(pattern);
        return pattern;
    }

    private static YarnMatchColor MapSymbol(char symbol)
    {
        switch (symbol)
        {
            case '.': return YarnMatchColor.Butter;
            case 'a': return YarnMatchColor.ReferenceCream;
            case 'b': return YarnMatchColor.ReferencePaper;
            case 'c': return YarnMatchColor.ReferenceCocoa;
            case 'd': return YarnMatchColor.ReferenceGray;
            case 'e': return YarnMatchColor.ReferenceTaupe;
            case 'f': return YarnMatchColor.ReferencePeach;
            case 'g': return YarnMatchColor.ReferenceDeepBlue;
            case 'h': return YarnMatchColor.ReferenceIndigo;
            case 'i': return YarnMatchColor.ReferenceWhite;
            case 'j': return YarnMatchColor.ReferenceBlush;
            case 'k': return YarnMatchColor.ReferenceSky;
            case 'l': return YarnMatchColor.ReferenceApricot;
            case 'm': return YarnMatchColor.ReferenceRose;
            default: return YarnMatchColor.Butter;
        }
    }

    private static void BalanceSpoolCounts(List<YarnMatchColor> pattern)
    {
        int[] counts = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
        for (int index = 0; index < pattern.Count; index++)
        {
            counts[(int)pattern[index]]++;
        }

        int baseColor = (int)YarnMatchColor.Butter;
        for (int color = 0; color < counts.Length; color++)
        {
            if (color == baseColor)
            {
                continue;
            }

            int remainder = counts[color] % YarnMatchRackModel.CellsPerSpool;
            for (int index = pattern.Count - 1; index >= 0 && remainder > 0; index--)
            {
                if ((int)pattern[index] == color)
                {
                    pattern[index] = (YarnMatchColor)baseColor;
                    remainder--;
                }
            }
        }
    }
}
