using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Tracker.Common
{
    /// <summary>
    /// Extension methods for ObservableCollection to improve performance
    /// by reducing unnecessary UI notifications during bulk operations.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Replaces all items in the collection with the new items.
        /// Uses optimized bulk update if collection is BulkObservableCollection,
        /// otherwise falls back to standard Clear() + Add() pattern.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to update.</param>
        /// <param name="newItems">The new items to populate the collection with.</param>
        public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(newItems);

            if (collection is BulkObservableCollection<T> bulkCollection)
            {
                // Use optimized bulk replacement
                bulkCollection.ReplaceAllSuppressed(newItems);
            }
            else
            {
                // Standard fallback - still N+1 notifications but preserves reference
                collection.Clear();
                foreach (var item in newItems)
                {
                    collection.Add(item);
                }
            }
        }

        /// <summary>
        /// Adds multiple items to the collection.
        /// Uses optimized bulk add if collection is BulkObservableCollection,
        /// otherwise adds items one at a time.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to add items to.</param>
        /// <param name="items">The items to add.</param>
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(items);

            if (collection is BulkObservableCollection<T> bulkCollection)
            {
                // Use optimized bulk add
                bulkCollection.AddRangeSuppressed(items);
            }
            else
            {
                // Standard fallback
                foreach (var item in items)
                {
                    collection.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// An ObservableCollection that supports bulk operations with a single notification.
    /// Use this for collections that are frequently updated with multiple items at once.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotifications;

        /// <summary>
        /// Creates an empty BulkObservableCollection.
        /// </summary>
        public BulkObservableCollection() : base() { }

        /// <summary>
        /// Creates a BulkObservableCollection with the specified items.
        /// </summary>
        /// <param name="items">Initial items.</param>
        public BulkObservableCollection(IEnumerable<T> items) : base(items) { }

        /// <summary>
        /// Replaces all items with new items, firing only a single Reset notification.
        /// </summary>
        /// <param name="newItems">The new items to populate the collection with.</param>
        public void ReplaceAllSuppressed(IEnumerable<T> newItems)
        {
            ArgumentNullException.ThrowIfNull(newItems);

            _suppressNotifications = true;
            try
            {
                Items.Clear();
                foreach (var item in newItems)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotifications = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Adds multiple items with a single Reset notification.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRangeSuppressed(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            _suppressNotifications = true;
            try
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotifications = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <inheritdoc/>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotifications)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
