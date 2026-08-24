using System.Collections.Generic;

public enum YarnMatchColor
{
    Coral,
    Orange,
    Butter,
    Mint,
    Cyan,
    Blue,
    Violet,
    Pink,
    ReferenceCream,
    ReferencePaper,
    ReferenceCocoa,
    ReferenceGray,
    ReferenceTaupe,
    ReferencePeach,
    ReferenceDeepBlue,
    ReferenceIndigo,
    ReferenceWhite,
    ReferenceBlush,
    ReferenceSky,
    ReferenceApricot,
    ReferenceRose
}

public enum YarnMatchGameState
{
    Playing,
    Resolving,
    Won,
    Lost
}

public enum YarnMatchTunnelDirection
{
    Up,
    Right,
    Down,
    Left
}

public sealed class YarnMatchBoardCell
{
    public YarnMatchColor Color;
    public int Column;
    public int Row;
    public bool Active = true;
}

public sealed class YarnMatchSpoolToken
{
    public YarnMatchColor Color;
    public int Id;
    public bool Used;
    public YarnMatchPoolCell Cell;
}

public sealed class YarnMatchPoolCell
{
    public int Column;
    public int Row;
    public bool Unlocked;
    public YarnMatchSpoolToken Token;
    public YarnMatchTunnel Tunnel;
    public YarnMatchTunnel SourceTunnel;
}

public sealed class YarnMatchTunnel
{
    public YarnMatchPoolCell Target;
    public YarnMatchPoolCell OutputCell;
    public YarnMatchTunnelDirection Direction;
    public readonly List<YarnMatchSpoolToken> Queue = new List<YarnMatchSpoolToken>();
}

public sealed class YarnMatchPoolSelection
{
    public YarnMatchSpoolToken Token;
    public YarnMatchPoolCell SourceCell;
    public YarnMatchTunnel Tunnel;
}

public sealed class YarnMatchRackEntry
{
    public YarnMatchColor Color;
    public int Slot;
    public int Progress;
    public int Capacity = 3;
}

public static class YarnMatchRandom
{
    public static void Shuffle<T>(IList<T> list, int seed)
    {
        System.Random random = new System.Random(seed);
        for (int index = list.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            T value = list[index];
            list[index] = list[other];
            list[other] = value;
        }
    }
}
