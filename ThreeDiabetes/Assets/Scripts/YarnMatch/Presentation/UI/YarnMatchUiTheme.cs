using UnityEngine;

internal static class YarnMatchUiTheme
{
    internal static readonly Color[] Palette =
    {
        new Color(0.96f, 0.22f, 0.30f),
        new Color(0.99f, 0.49f, 0.08f),
        new Color(1.00f, 0.75f, 0.05f),
        new Color(0.10f, 0.72f, 0.38f),
        new Color(0.03f, 0.70f, 0.90f),
        new Color(0.12f, 0.36f, 0.88f),
        new Color(0.50f, 0.22f, 0.82f),
        new Color(0.95f, 0.20f, 0.58f),
        new Color(1.00f, 0.933f, 0.678f),
        new Color(0.992f, 0.984f, 1.00f),
        new Color(0.459f, 0.220f, 0.196f),
        new Color(0.945f, 0.929f, 0.929f),
        new Color(0.647f, 0.529f, 0.404f),
        new Color(0.996f, 0.761f, 0.651f),
        new Color(0.059f, 0.329f, 0.753f),
        new Color(0.467f, 0.525f, 0.898f),
        new Color(0.996f, 1.00f, 1.00f),
        new Color(0.992f, 0.827f, 0.800f),
        new Color(0.486f, 0.769f, 1.00f),
        new Color(0.902f, 0.612f, 0.475f),
        new Color(1.00f, 0.820f, 0.729f)
    };

    private static readonly string[] ColorNames =
    {
        "\u7ea2\u8272", "\u6a59\u8272", "\u9ec4\u8272", "\u7eff\u8272", "\u9752\u8272", "\u84dd\u8272", "\u7d2b\u8272", "\u7c89\u8272",
        "\u56fe\u6848\u5976\u6cb9", "\u56fe\u6848\u767d", "\u56fe\u6848\u53ef\u53ef", "\u56fe\u6848\u7070", "\u56fe\u6848\u7070\u8910", "\u56fe\u6848\u6843", "\u56fe\u6848\u6df1\u84dd", "\u56fe\u6848\u975b\u84dd", "\u56fe\u6848\u4e73\u767d", "\u56fe\u6848\u7c89\u767d", "\u56fe\u6848\u5929\u84dd", "\u56fe\u6848\u674f\u8272", "\u56fe\u6848\u6d45\u6843"
    };

    internal const int BoardColumns = 8;
    internal const int BoardStrandsPerCell = 10;
    internal const float BoardCellSize = 82f;
    internal const float BoardHorizontalStep = BoardCellSize;
    internal const float BoardVerticalStep = BoardCellSize;
    internal const int BoardRows = 18;
    internal const int RackCapacity = 8;
    internal const float BoardViewportWidth = 700f;
    internal const float BoardViewportHeight = 500f;
    internal const float PoolViewportWidth = 700f;
    internal const float PoolViewportHeight = 350f;
    internal const float PoolViewportCenterY = -25f;
    internal const float PoolSlotGap = 5f;
    internal const float PoolMaxCellSize = 68f;
    internal static int BoardStrandCountForSize(float cellSize)
    {
        if (cellSize < 14f)
        {
            return 1;
        }
        if (cellSize < 28f)
        {
            return 3;
        }
        if (cellSize < 48f)
        {
            return 5;
        }
        return BoardStrandsPerCell;
    }

    internal static string GetColorName(YarnMatchColor color)
    {
        int index = Mathf.Clamp((int)color, 0, ColorNames.Length - 1);
        return ColorNames[index];
    }

    internal static float BoardCellSizeFor(int columns, int rows)
    {
        return BoardCellSizeFor(columns, rows, false);
    }

    internal static float BoardCellSizeFor(int columns, int rows, bool fitSquareToViewport)
    {
        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);
        float widthFit = BoardViewportWidth / safeColumns;
        float heightFit = fitSquareToViewport ? BoardViewportHeight / safeRows : BoardCellSize;
        return Mathf.Min(BoardCellSize, widthFit, heightFit);
    }

    internal static Vector2 BoardPosition(int column, int row)
    {
        return BoardPosition(column, row, BoardColumns, BoardRows, false);
    }

    internal static Vector2 BoardPosition(int column, int row, int columns, int rows)
    {
        return BoardPosition(column, row, columns, rows, false);
    }

    internal static Vector2 BoardPosition(int column, int row, int columns, int rows, bool fitSquareToViewport)
    {
        float cellSize = BoardCellSizeFor(columns, rows, fitSquareToViewport);
        return new Vector2(
            (column - (columns - 1) * 0.5f) * cellSize,
            -BoardViewportHeight * 0.5f + cellSize * 0.5f + row * cellSize);
    }

    internal static Vector2 PoolSlotPosition(int column, int row)
    {
        return PoolSlotPosition(column, row, YarnMatchPoolModel.DefaultColumns, YarnMatchPoolModel.DefaultRows);
    }

    internal static float PoolCellSize(int columns, int rows)
    {
        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);
        float widthFit = (PoolViewportWidth - (safeColumns - 1) * PoolSlotGap) / safeColumns;
        float heightFit = (PoolViewportHeight - (safeRows - 1) * PoolSlotGap) / safeRows;
        return Mathf.Max(22f, Mathf.Min(PoolMaxCellSize, widthFit, heightFit));
    }

    internal static Vector2 PoolSlotPosition(int column, int row, int columns, int rows)
    {
        float cellSize = PoolCellSize(columns, rows);
        float step = cellSize + PoolSlotGap;
        float x = (column - (columns - 1) * 0.5f) * step;
        float y = PoolViewportCenterY + (rows - 1) * 0.5f * step - row * step;
        return new Vector2(x, y);
    }

    internal static Vector2 DirectionVector(YarnMatchTunnelDirection direction)
    {
        switch (direction)
        {
            case YarnMatchTunnelDirection.Right: return Vector2.right;
            case YarnMatchTunnelDirection.Down: return Vector2.down;
            case YarnMatchTunnelDirection.Left: return Vector2.left;
            default: return Vector2.up;
        }
    }



    internal static float EaseInOut(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
