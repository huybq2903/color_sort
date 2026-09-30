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
    ///     Represents a data wrapper for a single item within an <see cref="IDataPool" />.
    ///     It provides thread-safe access to a cached value, reducing repeated data pool lookups.
    /// </summary>
    /// <typeparam name="T">The type of data this pool entry holds.</typeparam>
    /// <remarks>
    ///     This class maintains a local cache of the data pool item using <see cref="SealedBox"/>.
    ///     Operations are synchronized to ensure thread safety for the local cache and interactions with the underlying data pool.
    /// </remarks>
    public class BasicPoolData<T>
    {
        private readonly IDataPool _dataPool;
        private readonly T _defaultVal;
        private readonly object _lock = new();

        private readonly string _poolKey;
        // Using `_box` instead of `_t` (from previous version)
        // Initialize _box to an empty state, so TryGetValue correctly indicates no value initially.
        private SealedBox<T> _box = SealedBox.Empty<T>(); 

        /// <summary>
        ///     Initializes a new instance of the <see cref="BasicPoolData{T}" /> class.
        /// </summary>
        /// <param name="dataPool">The underlying <see cref="IDataPool" /> instance to interact with.</param>
        /// <param name="poolKey">The unique key for this data item within the data pool.</param>
        /// <param name="defaultVal">The default value to use if the key is not found in the data pool.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     Thrown if <paramref name="dataPool"/> or <paramref name="poolKey"/> is <c>null</c>.
        ///     If <typeparamref name="T"/> is a reference type, this is also thrown if <paramref name="defaultVal"/> is <c>null</c>.
        /// </exception>
        public BasicPoolData(IDataPool dataPool, string poolKey, T defaultVal)
        {
            _dataPool = dataPool ?? throw new ArgumentNullException(nameof(dataPool));
            _poolKey = poolKey ?? throw new ArgumentNullException(nameof(poolKey));

            // For T, the defaultVal check should consider if T is a reference type.
            // If T is a value type (e.g., int, bool), defaultVal cannot be null.
            // If T is a nullable value type (e.g., int?), defaultVal can be null.
            // So, this check is most relevant for reference types.
            if (Equals(defaultVal, null) && typeof(T).IsClass) // Check only if T is a reference type
            {
                 throw new ArgumentNullException(nameof(defaultVal), "Default value cannot be null for a reference type.");
            }
            _defaultVal = defaultVal;
        }

        /// <summary>
        ///     Gets or sets the current value.
        ///     When getting, it attempts to load the value from the data pool if not already cached.
        ///     When setting, it updates both the local cache and the underlying data pool.
        /// </summary>
        /// <returns>The cached or loaded value for this pool entry.</returns>
        public T Value
        {
            get
            {
                // Optimistic read without lock (double-checked locking for the box content)
                // This read _temp ensures that if _box is reassigned by another thread while we're processing,
                // we don't end up with a torn read.
                var temp = _box;
                if (temp.TryGetValue(out var value)) return value;

                // Fallback to locking if value is not present
                lock (_lock)
                {
                    // Double check inside the lock to avoid redundant data pool access
                    if (_box.TryGetValue(out var now)) return now;

                    T newVal = _dataPool.GetOrSet(_poolKey, _defaultVal);
                    _box = SealedBox.Of(newVal); // Atomically update the box reference
                    return newVal;
                }
            }
            set
            {
                lock (_lock)
                {
                    // Using SealedBox.Of for atomic update of the reference
                    _box = SealedBox.Of(value);
                    _dataPool.Save(_poolKey, value);
                }
            }
        }

        /// <summary>
        ///     Saves the current cached value to the underlying data pool.
        ///     This ensures the data pool is synchronized with the latest cached value.
        /// </summary>
        /// <remarks>
        ///     This method will first ensure the value is loaded into the local cache if it isn't already,
        ///     before saving it to the data pool.
        /// </remarks>
        public void SaveSync()
        {
            lock (_lock)
            {
                // Accessing the Value property will ensure it's loaded from _dataPool if not cached.
                _dataPool.Save(_poolKey, Value);
            }
        }

        /// <summary>
        ///     Deletes the entry from both the local cache and the underlying data pool.
        /// </summary>
        /// <remarks>
        ///     The local cache (<see cref="Value"/>) will be cleared to an empty state,
        ///     and the item associated with <see cref="_poolKey"/> will be removed from <see cref="_dataPool"/>.
        /// </remarks>
        public void Delete()
        {
            lock (_lock)
            {
                _box = SealedBox.Empty<T>();  // Clear local cache
                _dataPool.Delete(_poolKey);
            }
        }

        /// <summary>
        ///     Atomically computes a new value based on the current value, updates the local cache,
        ///     and saves the new value to the underlying data pool.
        /// </summary>
        /// <param name="computation">A function that takes the current value and returns the computed new value.</param>
        /// <returns>The newly computed value.</returns>
        /// <remarks>
        ///     If the value is not already cached, it will first be loaded from the data pool.
        ///     The <paramref name="computation"/> function will then be applied to this loaded value.
        /// </remarks>
        public T Compute(Func<T, T> computation)
        {
            lock (_lock)
            {
                // Ensure value is loaded into cache before computation
                // Using .Value directly here is fine as it has the load logic
                T currentValue = Value; // This will trigger load from data pool if not cached

                T newValue = computation.Invoke(currentValue); // Apply the computation function
                
                if(Equals(currentValue, newValue)) return newValue;
                _box = SealedBox.Of(newValue); // Update local cache
                _dataPool.Save(_poolKey, newValue); // Save the new value to data pool
                return newValue; // Return the new value
            }
        }

        /// <summary>
        ///     Clears the locally cached value, forcing a reload from the data pool on the next <see cref="Value" /> access.
        ///     The value in the underlying data pool remains unchanged.
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _box = SealedBox.Empty<T>();
            }
        }
        
        
    }
}