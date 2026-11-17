using System.Collections.Generic;

namespace ResolverMaster.Core.Puzzle
{
    /// <summary>Last-in first-out list of moves for Undo. Generic: a move is whatever the puzzle needs to revert itself.</summary>
    public sealed class UndoStack<T>
    {
        readonly List<T> _items = new List<T>();

        public int Count => _items.Count;

        /// <summary>Oldest first.</summary>
        public IReadOnlyList<T> Items => _items;

        public void Push(T item) => _items.Add(item);

        public bool TryPop(out T item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }
            item = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            return true;
        }

        public bool TryPeek(out T item)
        {
            item = _items.Count > 0 ? _items[_items.Count - 1] : default;
            return _items.Count > 0;
        }

        public void Clear() => _items.Clear();
    }
}
