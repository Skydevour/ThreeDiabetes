using System;

[Serializable]
internal sealed class YarnMatchPatternProfile
{
    public int cells;
    public int colors;
    public int[] pressure;
}

[Serializable]
internal sealed class YarnMatchPatternTemplate
{
    public string id;
    public string title;
    public string category;
    public int minSize;
    public string[] palette;
    public string[] rows;
    public string[] mask;
    public YarnMatchPatternProfile[] profiles;
    [NonSerialized] internal int Left, Top, Width, Height;
    [NonSerialized] internal int[] Rgb;

    internal void Prepare()
    {
        Left = rows[0].Length;
        Top = rows.Length;
        int right = 0, bottom = 0;
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < rows[y].Length; x++)
        {
            if (mask[y][x] != '1') continue;
            Left = Math.Min(Left, x);
            Top = Math.Min(Top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }
        Width = right - Left + 1;
        Height = bottom - Top + 1;
        Rgb = new int[palette.Length];
        for (int i = 0; i < Rgb.Length; i++) Rgb[i] = Convert.ToInt32(palette[i], 16);
    }
}

[Serializable]
internal sealed class YarnMatchPatternPack
{
    public YarnMatchPatternTemplate[] patterns;
}
