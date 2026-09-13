using System;

// An initial layout only. Live game objects must never be stored in this record.
[Serializable]
internal sealed class YarnMatchLevelSnapshot
{
    public int Number;
    public int Seed;
    public int ColorCount;
    public bool Special;
    public bool FitToViewport;
    public int Width;
    public int Height;
    public int[] Colors;
    public YarnMatchPoolSnapshot Pool;

    internal static YarnMatchLevelSnapshot Generate(YarnMatchLevelConfig config)
    {
        YarnMatchPatternLayout pattern = config.Pattern;
        var round = new YarnMatchRoundData(config, pattern);
        return new YarnMatchLevelSnapshot
        {
            Number = config.Number, Seed = config.Seed, ColorCount = config.ColorCount,
            Special = config.UsesReferencePattern, FitToViewport = round.Board.FitToViewport,
            Width = pattern.Width, Height = pattern.Height, Colors = (int[])pattern.Colors.Clone(),
            Pool = YarnMatchPoolSnapshot.Capture(round.Pool)
        };
    }

    internal YarnMatchPatternLayout CreatePattern() => new YarnMatchPatternLayout(Width, Height, Colors);

}
