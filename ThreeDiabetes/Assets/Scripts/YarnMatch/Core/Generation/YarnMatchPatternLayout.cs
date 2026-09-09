using System.Collections.Generic;

internal sealed class YarnMatchPatternLayout
{
    internal readonly int Width;
    internal readonly int Height;
    // Row-major, bottom-up. -1 is outside the subject, not a yarn color.
    internal readonly int[] Colors;
    internal int CellCount { get; }

    internal YarnMatchPatternLayout(int width, int height, int[] colors)
    {
        Width = width;
        Height = height;
        Colors = colors;
        foreach (int color in colors) if (color >= 0) CellCount++;
    }

    internal static YarnMatchPatternLayout Dense(IReadOnlyList<int> heights,
        IReadOnlyList<YarnMatchColor> colors)
    {
        int height = 0;
        foreach (int value in heights) height = System.Math.Max(height, value);
        int[] pixels = new int[heights.Count * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = -1;
        int cursor = 0;
        for (int x = 0; x < heights.Count; x++)
        for (int y = 0; y < heights[x]; y++) pixels[y * heights.Count + x] = (int)colors[cursor++];
        return new YarnMatchPatternLayout(heights.Count, height, pixels);
    }
}
