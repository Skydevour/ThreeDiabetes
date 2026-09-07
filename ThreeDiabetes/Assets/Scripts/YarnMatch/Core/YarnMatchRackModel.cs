using System;
using System.Collections.Generic;

public sealed class YarnMatchRackModel
{
    public const int Capacity = 8;
    public const int CellsPerSpool = 3;

    private readonly List<YarnMatchRackEntry> _entries = new List<YarnMatchRackEntry>();

    public IReadOnlyList<YarnMatchRackEntry> Entries => _entries;
    public int UnlockedSlots { get; private set; } = 7;
    public int FreeSlots => Math.Max(0, UnlockedSlots - _entries.Count);
    public bool IsFull => FreeSlots == 0;

    public YarnMatchRackEntry Find(YarnMatchColor color)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Color == color)
            {
                return _entries[index];
            }
        }
        return null;
    }

    public bool CanCreate(int count)
    {
        return count > 0 && count <= FreeSlots;
    }

    public YarnMatchRackEntry TryCreate(YarnMatchColor color)
    {
        return TryCreate(color, CellsPerSpool);
    }

    public YarnMatchRackEntry TryCreate(YarnMatchColor color, int capacity)
    {
        for (int slot = 0; slot < UnlockedSlots; slot++)
        {
            if (FindBySlot(slot) == null)
            {
                YarnMatchRackEntry entry = new YarnMatchRackEntry
                {
                    Color = color,
                    Slot = slot,
                    Progress = 0,
                    Capacity = Math.Max(1, Math.Min(CellsPerSpool, capacity))
                };
                _entries.Add(entry);
                return entry;
            }
        }
        return null;
    }

    public int GetNextCapacity(YarnMatchColor color, int remainingBoardCells)
    {
        int reservedCapacity = 0;
        for (int index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Color == color)
            {
                reservedCapacity += Math.Max(0, _entries[index].Capacity - _entries[index].Progress);
            }
        }

        int unreserved = Math.Max(1, remainingBoardCells - reservedCapacity);
        return Math.Min(CellsPerSpool, unreserved);
    }

    public void AddProgress(YarnMatchRackEntry entry)
    {
        if (entry != null)
        {
            entry.Progress = entry.Progress < entry.Capacity ? entry.Progress + 1 : entry.Capacity;
        }
    }

    public void Remove(YarnMatchRackEntry entry)
    {
        if (entry != null)
        {
            _entries.Remove(entry);
        }
    }

    public bool TryUnlockFinalSlot(int collectedCells, int totalCells)
    {
        if (UnlockedSlots >= Capacity || totalCells <= 0 || collectedCells < (totalCells + 1) / 2)
        {
            return false;
        }

        UnlockedSlots = Capacity;
        return true;
    }

    private YarnMatchRackEntry FindBySlot(int slot)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Slot == slot)
            {
                return _entries[index];
            }
        }
        return null;
    }
}
