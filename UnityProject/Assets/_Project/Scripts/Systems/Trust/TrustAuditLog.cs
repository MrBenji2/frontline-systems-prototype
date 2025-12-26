using System;
using System.Collections.Generic;

namespace Frontline.Trust
{
    public sealed class TrustAuditLog
    {
        public const int DefaultCapacity = 200;

        public sealed class Entry
        {
            public long utc;
            public string playerId = "";
            public FactionId faction;
            public string actionType = "";
            public string payload = "";
        }

        private readonly int _capacity;
        private readonly Queue<Entry> _entries;

        public TrustAuditLog(int capacity = DefaultCapacity)
        {
            _capacity = Math.Max(1, capacity);
            _entries = new Queue<Entry>(_capacity);
        }

        public IReadOnlyCollection<Entry> Entries => _entries;

        public void Add(string playerId, FactionId faction, string actionType, string payload)
        {
            if (_entries.Count >= _capacity)
                _entries.Dequeue();

            _entries.Enqueue(new Entry
            {
                utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                playerId = playerId ?? "",
                faction = faction,
                actionType = actionType ?? "",
                payload = payload ?? "",
            });
        }
    }
}

