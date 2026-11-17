using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Reuses components (cells, pieces, effects) instead of Instantiate/Destroy (GUIDELINES 8). Released items are deactivated and
    /// kept under their parent; <see cref="Get"/> reactivates one or creates it with the factory.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        readonly Func<T> _create;
        readonly Stack<T> _free = new Stack<T>();
        readonly List<T> _active = new List<T>();

        public IReadOnlyList<T> Active => _active;
        public int Created { get; private set; }

        public ComponentPool(Func<T> create)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
        }

        public T Get()
        {
            T item;
            if (_free.Count > 0) item = _free.Pop();
            else
            {
                item = _create();
                Created++;
            }
            item.gameObject.SetActive(true);
            _active.Add(item);
            return item;
        }

        public void Release(T item)
        {
            if (!_active.Remove(item)) return;
            item.gameObject.SetActive(false);
            _free.Push(item);
        }

        public void ReleaseAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].gameObject.SetActive(false);
                _free.Push(_active[i]);
            }
            _active.Clear();
        }
    }
}
