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
    /// Represents a value that is initialized lazily (on first access) and can be reset, forcing re-computation on subsequent access.
    /// This class is thread-safe for both initialization and reset operations.
    /// </summary>
    /// <remarks>
    /// The value is computed by the provided <see cref="Func{T}"/> supplier only when it's first accessed
    /// via the <see cref="Value"/> property, or after a call to <see cref="Reset()"/>.
    /// It employs a double-checked locking pattern for efficient and thread-safe initialization.
    /// <para>
    /// This implementation relies on the immutability and atomic assignment properties of <see cref="SealedBox{T}"/>.
    /// If the <ref name="_supplier"/> throws an exception, the state remains uninitialized,
    /// and subsequent calls to <see cref="Value"/> will attempt to re-invoke the supplier.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value that is being lazily initialized.</typeparam>
    public class LazyVal<T>
    {
        // _box holds the initialized value. SealedBox.Empty indicates not yet initialized.
        private SealedBox<T> _box = SealedBox.Empty<T>();
        
        // The function that provides the value when it needs to be computed.
        private readonly Func<T> _supplier;
        
        // The lock object to ensure thread-safe access and modification of _box.
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyVal{T}"/> class with the specified value supplier.
        /// The value will not be computed until its first access.
        /// </summary>
        /// <param name="supplier">
        /// The <see cref="Func{T}"/> that will be invoked to produce the value when it's first needed.
        /// This function will be invoked at most once per initialization cycle (i.e., until <see cref="Reset()"/> is called).
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="supplier"/> is <see langword="null"/>.</exception>
        public LazyVal(Func<T> supplier)
        {
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
        }

        /// <summary>
        /// Gets the lazily initialized value.
        /// The value is computed only on the first call to this property (or after a <see cref="Reset()"/>).
        /// Subsequent calls return the same computed value without re-invoking the supplier.
        /// </summary>
        /// <value>The value produced by the supplier function.</value>
        /// <remarks>
        /// This getter uses a double-checked locking pattern to ensure that the value is computed
        /// exactly once in a thread-safe manner per initialization cycle.
        /// If the <see cref="_supplier"/> throws an exception during computation, the state remains uninitialized,
        /// and subsequent calls to <see cref="Value"/> will attempt to re-invoke the supplier.
        /// </remarks>
        public T Value
        {
            get
            {
                // First check without locking for performance (common case: value already computed)
                var temp = _box;
                if(temp.TryGetValue(out var value)) 
                    return value;
                
                // If not yet computed, acquire lock to ensure only one thread computes
                lock (_lock)
                {
                    // Second check inside the lock (value might have been computed by another thread
                    // while this thread was waiting for the lock)
                    if(_box.TryGetValue(out var now)) 
                        return now;
                    
                    // Value not yet computed, so invoke the supplier
                    T newVal = _supplier();
                    
                    // Atomically set the box with the new value.
                    // This relies on SealedBox.Of returning an immutable instance and the assignment being atomic.
                    _box = SealedBox.Of(newVal);
                    return newVal;
                }
            }
        }

        /// <summary>
        /// Resets the lazily initialized value, effectively discarding the current computed value
        /// and forcing re-computation on the next access to the <see cref="Value"/> property.
        /// </summary>
        /// <remarks>
        /// This operation is thread-safe. After a reset, the next access to <see cref="Value"/>
        /// will cause the <see cref="_supplier"/> function to be invoked again to generate a new value.
        /// </remarks>
        public void Reset()
        {
            lock (_lock)
            {
                // Atomically set the box back to an empty state.
                _box = SealedBox.Empty<T>();
            }
        }
    }
}