using System.Collections.Generic;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>
    /// Priority queue for small non-negative integer priorities (move counts): one bucket per priority, last-in first-out inside a bucket.
    /// Fully deterministic, used by the puzzle solvers' best-first searches.
    /// </summary>
    public sealed class BucketQueue<T>
    {
        readonly List<List<T>> _buckets = new List<List<T>>();
        int _lowest;

        public int Count { get; private set; }

        public void Push(T item, int priority)
        {
            if (priority < 0) priority = 0;
            while (_buckets.Count <= priority) _buckets.Add(new List<T>());
            _buckets[priority].Add(item);
            if (priority < _lowest) _lowest = priority;
            Count++;
        }

        public bool TryPop(out T item, out int priority)
        {
            while (_lowest < _buckets.Count)
            {
                var bucket = _buckets[_lowest];
                if (bucket.Count > 0)
                {
                    item = bucket[bucket.Count - 1];
                    bucket.RemoveAt(bucket.Count - 1);
                    priority = _lowest;
                    Count--;
                    return true;
                }
                _lowest++;
            }
            item = default;
            priority = -1;
            return false;
        }

        public void Clear()
        {
            foreach (var b in _buckets) b.Clear();
            _lowest = 0;
            Count = 0;
        }
    }
}
