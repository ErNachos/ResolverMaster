using System;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// Index of 64-bit packed puzzle states for the exhaustive searches of the solvers (TJ-001): each new key gets the next dense index
    /// (0, 1, 2... in insertion order), so the searches keep their per-state data (distance, parent...) in plain arrays.
    /// Open addressing with linear probing, no allocation per lookup, fully deterministic. Pure logic, not thread-safe (one per search).
    /// </summary>
    public sealed class StateIndex
    {
        ulong[] _keys;
        /// <summary>Slot -> index + 1 (0 = empty).</summary>
        int[] _slots;
        int _mask;

        public int Count { get; private set; }

        public StateIndex(int capacity = 1024)
        {
            int slots = 16;
            while (slots < capacity * 2) slots <<= 1;
            _slots = new int[slots];
            _mask = slots - 1;
            _keys = new ulong[Math.Max(16, capacity)];
        }

        /// <summary>The key with dense index <paramref name="index"/>.</summary>
        public ulong KeyAt(int index) => _keys[index];

        /// <summary>The dense index of <paramref name="key"/>, or -1.</summary>
        public int IndexOf(ulong key)
        {
            int slot = (int)Hash(key) & _mask;
            while (true)
            {
                int v = _slots[slot];
                if (v == 0) return -1;
                if (_keys[v - 1] == key) return v - 1;
                slot = (slot + 1) & _mask;
            }
        }

        /// <summary>The index of <paramref name="key"/>, adding it when new (<paramref name="added"/> true).</summary>
        public int Add(ulong key, out bool added)
        {
            int slot = (int)Hash(key) & _mask;
            while (true)
            {
                int v = _slots[slot];
                if (v == 0) break;
                if (_keys[v - 1] == key)
                {
                    added = false;
                    return v - 1;
                }
                slot = (slot + 1) & _mask;
            }
            if (Count == _keys.Length) Array.Resize(ref _keys, _keys.Length * 2);
            _keys[Count] = key;
            _slots[slot] = Count + 1;
            Count++;
            added = true;
            if (Count * 2 > _slots.Length) Grow();
            return Count - 1;
        }

        void Grow()
        {
            _slots = new int[_slots.Length * 2];
            _mask = _slots.Length - 1;
            for (int i = 0; i < Count; i++)
            {
                int slot = (int)Hash(_keys[i]) & _mask;
                while (_slots[slot] != 0) slot = (slot + 1) & _mask;
                _slots[slot] = i + 1;
            }
        }

        /// <summary>SplitMix64 finalizer: packed states differ in few bits, this spreads them over the table.</summary>
        static ulong Hash(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
