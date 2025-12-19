namespace Tracker.Help.Services
{
    /// <summary>
    /// A simple Least Recently Used (LRU) cache implementation.
    /// Automatically evicts the oldest items when the cache reaches capacity.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public class LruCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new();

        private class CacheItem
        {
            public TKey Key { get; set; } = default!;
            public TValue Value { get; set; } = default!;
            public DateTime AddedAt { get; set; }
        }

        /// <summary>
        /// Creates a new LRU cache with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of items to store.</param>
        public LruCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Gets the current number of items in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <summary>
        /// Tries to get a value from the cache.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value if found.</param>
        /// <returns>True if the key was found.</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default!;
                return false;
            }
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    // Update existing
                    existingNode.Value.Value = value;
                    existingNode.Value.AddedAt = DateTime.UtcNow;
                    _lruList.Remove(existingNode);
                    _lruList.AddFirst(existingNode);
                    return;
                }

                // Evict if at capacity
                while (_cache.Count >= _capacity)
                {
                    var oldest = _lruList.Last;
                    if (oldest != null)
                    {
                        _cache.Remove(oldest.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                // Add new item
                var item = new CacheItem
                {
                    Key = key,
                    Value = value,
                    AddedAt = DateTime.UtcNow
                };

                var node = new LinkedListNode<CacheItem>(item);
                _lruList.AddFirst(node);
                _cache[key] = node;
            }
        }

        /// <summary>
        /// Removes an item from the cache.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the item was removed.</returns>
        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _cache.Remove(key);
                    _lruList.Remove(node);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }

        /// <summary>
        /// Gets all keys currently in the cache.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Keys.ToList();
                }
            }
        }
    }
}

