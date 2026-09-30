/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{

    /// <summary>
    /// Represents a data wrapper that manages a single item within an <see cref="IDataPool"/>,
    /// with custom delegate logic for retrieving and setting the value.
    /// It provides thread-safe access to a cached value, reducing repeated data pool lookups.
    /// </summary>
    /// <typeparam name="T">The type of data this pool entry holds.</typeparam>
    /// <remarks>
    /// This class maintains a local cache of the data pool item using <see cref="SealedBox"/>.
    /// Operations are synchronized to ensure thread safety for the local cache and interactions with the underlying data pool.
    /// The <c>getDelegate</c> is used to initialize the value if it's not in the cache or data pool.
    /// The <c>setDelegate</c> is used to apply custom logic when updating the value,
    /// potentially allowing for transformations or even deletion by returning an empty <see cref="SealedBox{T}"/>.
    /// </remarks>
    public class DelegatePoolData<T>
    {
        private readonly IDataPool _dataPool;
        private readonly string _poolKey;
        private readonly Func<T> _getDelegate;
        private readonly Func<T, T, SealedBox<T>> _setDelegate;

        // Initialize _box to an empty state so TryGetValue correctly indicates no value initially.
        private SealedBox<T> _box = SealedBox.Empty<T>(); 
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatePoolData{T}"/> class.
        /// </summary>
        /// <param name="dataPool">The underlying <see cref="IDataPool"/> instance to interact with.</param>
        /// <param name="poolKey">The unique key for this data item within the data pool.</param>
        /// <param name="getDelegate">A delegate that provides the value to be used if the key is not found in the data pool or cache.</param>
        /// <param name="setDelegate">A delegate that takes the current value and the new value, returning a <see cref="SealedBox{T}"/>
        /// indicating the value to be stored (or an empty box to delete).</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="dataPool"/>, <paramref name="poolKey"/>,
        /// <paramref name="getDelegate"/>, or <paramref name="setDelegate"/> is <c>null</c>.
        /// </exception>
        public DelegatePoolData(
            IDataPool dataPool,
            string poolKey,
            Func<T> getDelegate,
            Func<T, T, SealedBox<T>> setDelegate
        )
        {
            _dataPool = dataPool ?? throw new ArgumentNullException(nameof(dataPool));
            _poolKey = poolKey ?? throw new ArgumentNullException(nameof(poolKey));
            _getDelegate = getDelegate ?? throw new ArgumentNullException(nameof(getDelegate));
            _setDelegate = setDelegate ?? throw new ArgumentNullException(nameof(setDelegate));
        }

        /// <summary>
        /// Gets or sets the current value.
        /// <para>When getting, it retrieves the value, loading it from the data pool if not already cached.
        /// If the key is not in the data pool, the <c>getDelegate</c> is invoked to provide a default value.
        /// This getter guarantees a non-null return value if <typeparamref name="T"/> is a non-nullable reference type
        /// and <c>getDelegate</c> provides one.</para>
        /// <para>When setting, it applies the custom <c>setDelegate</c> logic. The <c>setDelegate</c> determines
        /// the actual value to store based on the current and new values. If <c>setDelegate</c> returns an empty
        /// <see cref="SealedBox{T}"/>, the entry will be deleted from the underlying data pool.
        /// Otherwise, the value provided by <c>setDelegate</c> is saved.</para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <c>getDelegate</c> or underlying data pool operation returns a <c>null</c> value
        /// which cannot be assigned to <typeparamref name="T"/> (e.g., if <typeparamref name="T"/> is a non-nullable value type).
        /// </exception>
        public T Value
        {
            get
            {
                // Optimistic read without lock (double-checked locking for the box content)
                // This read `temp` ensures that if `_box` is reassigned by another thread while we're processing,
                // we don't end up with a torn read.
                var temp = _box;
                if (temp.TryGetValue(out var value)) return value;

                // Fallback to locking if value is not present
                lock (_lock)
                {
                    // Double check inside the lock to avoid redundant data pool access
                    if (_box.TryGetValue(out var now)) return now;

                    T newVal = _dataPool.GetOrSet(_poolKey, _getDelegate.Invoke());
                    _box = SealedBox.Of(newVal); // Atomically update the box reference
                    return newVal;
                }
            }
            set
            {
                lock (_lock)
                {
                    // Apply custom set logic using the delegate.
                    // Value property getter ensures `_box` is loaded from data pool if needed.
                    var appliedBox = _setDelegate.Invoke(Value, value);

                    // Check if the delegate's intention was to delete the entry
                    if (!appliedBox.TryGetValue(out var newVal))
                    {
                        _dataPool.Delete(_poolKey); // Delete from data pool
                        _box = SealedBox.Empty<T>(); // Clear local cache
                    }
                    else
                    {
                        // Update local cache with the box provided by the delegate
                        _box = appliedBox;
                        // Save the extracted value to the data pool
                        _dataPool.Save(_poolKey, newVal);
                    }
                }
            }
        }

        /// <summary>
        /// Synchronizes the current cached value with the underlying data pool.
        /// If the cached value is currently empty (not present), the entry is deleted from the data pool.
        /// Otherwise, the cached value is saved to the data pool.
        /// </summary>
        /// <remarks>
        /// This method checks the state of the internal cache. It does not force a reload from the data pool
        /// before saving, so it reflects the last known value in the cache.
        /// </remarks>
        public void SaveSync()
        {
            lock (_lock)
            {
                if (!_box.TryGetValue(out var valueToSave))
                {
                    _dataPool.Delete(_poolKey);
                }
                else
                {
                    _dataPool.Save(_poolKey, valueToSave);
                }
            }
        }

        /// <summary>
        /// Static factory methods for creating <see cref="DelegatePoolData{T}"/> instances.
        /// </summary>
        public static class Factory
        {
            /// <summary>
            /// Creates a <see cref="DelegatePoolData{T}"/> with a custom getter and a default setter
            /// that simply assigns the new value (i.e., the <c>setDelegate</c> returns a <see cref="SealedBox{T}"/>
            /// containing the new value).
            /// </summary>
            /// <param name="dataPool">The underlying <see cref="IDataPool"/>.</param>
            /// <param name="poolKey">The key for the data item.</param>
            /// <param name="getDelegate">The delegate to get the initial value if not found in data pool.</param>
            /// <returns>A new <see cref="DelegatePoolData{T}"/> instance.</returns>
            /// <exception cref="System.ArgumentNullException">
            /// Thrown if <paramref name="dataPool"/>, <paramref name="poolKey"/>, or <paramref name="getDelegate"/> is <c>null</c>.
            /// </exception>
            public static DelegatePoolData<T> GetDelegate(
                IDataPool dataPool,
                string poolKey,
                Func<T> getDelegate
            )
            {
                // Default setDelegate: simply create a SealedBox from the new value
                return new DelegatePoolData<T>(dataPool, poolKey, getDelegate, (_, newT) => SealedBox.Of(newT));
            }

            /// <summary>
            /// Creates a <see cref="DelegatePoolData{T}"/> with a custom setter and a default getter
            /// that uses <see cref="IDataPool.GetOrSet{T}(string, T)"/> with a provided default value.
            /// </summary>
            /// <param name="dataPool">The underlying <see cref="IDataPool"/>.</param>
            /// <param name="poolKey">The key for the data item.</param>
            /// <param name="defaultVal">The default value to use if the key is not found in the data pool.</param>
            /// <param name="setDelegate">The delegate to apply when setting a value.</param>
            /// <returns>A new <see cref="DelegatePoolData{T}"/> instance.</returns>
            /// <exception cref="System.ArgumentNullException">
            /// Thrown if <paramref name="dataPool"/>, <paramref name="poolKey"/>, or <paramref name="setDelegate"/> is <c>null</c>.
            /// </exception>
            public static DelegatePoolData<T> SetDelegate(
                IDataPool dataPool,
                string poolKey,
                T defaultVal,
                Func<T, T, SealedBox<T>> setDelegate
            )
            {
                // Null check for defaultVal only if T is a reference type.
                if (Equals(defaultVal, null) && typeof(T).IsClass)
                {
                    throw new ArgumentNullException(nameof(defaultVal), "Default value cannot be null for a reference type.");
                }

                // Default getDelegate: use dataPool.GetOrSet with the provided defaultVal
                Func<T> getDelegate = () => dataPool.GetOrSet(poolKey, defaultVal);

                return new DelegatePoolData<T>(dataPool, poolKey, getDelegate, setDelegate);
            }
        }
    }
}