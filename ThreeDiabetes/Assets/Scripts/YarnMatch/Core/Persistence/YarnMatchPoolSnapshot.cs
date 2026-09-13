using System;
using System.Collections.Generic;

[Serializable]
internal sealed class YarnMatchPoolSnapshot
{
    public int Columns;
    public int Rows;
    public TokenData[] Tokens;
    public CellData[] Cells;
    public TunnelData[] Tunnels;
    public ChainData[] Chains;

    internal static YarnMatchPoolSnapshot Capture(YarnMatchPoolModel pool)
    {
        var snapshot = new YarnMatchPoolSnapshot
        {
            Columns = pool.Columns, Rows = pool.Rows,
            Tokens = new TokenData[pool.Tokens.Count], Cells = new CellData[pool.Cells.Count],
            Tunnels = new TunnelData[pool.Tunnels.Count], Chains = new ChainData[pool.Chains.Count]
        };
        var tokens = new Dictionary<YarnMatchSpoolToken, int>();
        for (int i = 0; i < pool.Tokens.Count; i++)
        {
            var token = pool.Tokens[i];
            tokens.Add(token, i);
            snapshot.Tokens[i] = new TokenData { Id = token.Id, Color = token.Color, Capacity = token.Capacity };
        }
        for (int i = 0; i < pool.Cells.Count; i++)
        {
            var cell = pool.Cells[i];
            snapshot.Cells[i] = new CellData
            {
                Token = cell.Token == null ? -1 : tokens[cell.Token],
                Unlocked = cell.Unlocked, FreezeHits = cell.FreezeHitsRemaining
            };
        }
        for (int i = 0; i < pool.Tunnels.Count; i++)
        {
            var tunnel = pool.Tunnels[i];
            int[] queue = new int[tunnel.Queue.Count];
            for (int j = 0; j < queue.Length; j++) queue[j] = tokens[tunnel.Queue[j]];
            snapshot.Tunnels[i] = new TunnelData
            {
                Target = Index(tunnel.Target, pool.Columns), Output = Index(tunnel.OutputCell, pool.Columns),
                Direction = tunnel.Direction, Queue = queue
            };
        }
        for (int i = 0; i < pool.Chains.Count; i++)
        {
            var chain = pool.Chains[i];
            snapshot.Chains[i] = new ChainData
            {
                Id = chain.Id, First = Index(chain.First, pool.Columns), Second = Index(chain.Second, pool.Columns)
            };
        }
        return snapshot;
    }

    private static int Index(YarnMatchPoolCell cell, int columns) => cell.Row * columns + cell.Column;

    [Serializable]
    internal sealed class TokenData
    {
        public int Id;
        public YarnMatchColor Color;
        public int Capacity;
    }

    [Serializable]
    internal sealed class CellData
    {
        public int Token;
        public bool Unlocked;
        public int FreezeHits;
    }

    [Serializable]
    internal sealed class TunnelData
    {
        public int Target;
        public int Output;
        public YarnMatchTunnelDirection Direction;
        public int[] Queue;
    }

    [Serializable]
    internal sealed class ChainData
    {
        public int Id;
        public int First;
        public int Second;
    }
}
