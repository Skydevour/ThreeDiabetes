public sealed class YarnMatchCollectionJob
{
    public YarnMatchSpoolToken Token;
    public YarnMatchPoolCell SourceCell;
    public YarnMatchRackEntry Entry;
    public int PendingCells;
    public bool Completing;
    public bool Ready;
}
