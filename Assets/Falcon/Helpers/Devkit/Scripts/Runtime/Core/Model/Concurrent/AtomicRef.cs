/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// <para>Represents a reference to a value of type <typeparamref name="T"/> that can be
    /// accessed and modified atomically (thread-safe) using an internal lock.</para>
    /// <para>This class is useful for managing a single shared mutable value in multithreaded environments,
    /// providing controlled access and conditional update operations.</para>
    /// </summary>
    /// <remarks>
    /// All access (reads and writes) to the internal value via the <see cref="Value"/> property or
    /// any `Set` or `Compute` methods are protected by an internal lock, ensuring thread safety and
    /// visibility across concurrent operations.
    /// <para>Be cautious when performing complex operations or calling external code within `Func` or `Action` delegates,
    /// as long-running or blocking operations inside the lock can degrade performance or lead to deadlocks.</para>
    /// </remarks>
    /// <typeparam name="T">The type of the value to be stored atomically.
    /// For best performance and correctness in equality checks, ensure that <typeparamref name="T"/> provides
    /// a meaningful implementation of equality (e.g., overrides <see cref="object.Equals(object)"/>
    /// and <see cref="object.GetHashCode"/>, or implements <see cref="IEquatable{T}"/>).</typeparam>
    public sealed class AtomicRef<T>
    {
        private readonly object _lock = new();
        private T _val;

        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicRef{T}"/> class with the default value of <typeparamref name="T"/>.
        /// </summary>
        public AtomicRef()
        {
            _val = default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicRef{T}"/> class with a specified initial value.
        /// </summary>
        /// <param name="val">The initial value for the atomic reference.</param>
        public AtomicRef(T val)
        {
            _val = val;
        }

        /// <summary>
        /// Atomically gets or sets the current value of the atomic reference.
        /// </summary>
        /// <remarks>
        /// Both the getter and setter are protected by an internal lock, ensuring that
        /// reads and writes are atomic and visible across different threads.
        /// </remarks>
        public T Value
        {
            get
            {
                lock (_lock) // CRITICAL FIX: Ensure reads are also under lock for full thread-safety and visibility
                {
                    return _val;
                }
            }
            set
            {
                lock (_lock)
                {
                    _val = value;
                }
            }
        }

        /// <summary>
        /// Atomically sets the value to a new value only if it is different from the specified value.
        /// Uses a double-checked locking pattern for efficiency.
        /// </summary>
        /// <param name="other">The value to compare with the current value. The value is set if they are not equal.</param>
        /// <returns><see langword="true"/> if the value was successfully set (i.e., it was different and updated); otherwise, <see langword="false"/>.</returns>
        public bool SetIfNotEq(T other)
        {
            // First check outside the lock for early exit optimization (reads via thread-safe 'Value' property)
            if (AreValuesEqual(Value, other)) return false;

            lock (_lock)
            {
                // Second check inside the lock to ensure atomicity and consistency
                // (value might have changed between the first check and lock acquisition)
                if (AreValuesEqual(_val, other)) return false; // _val is directly accessed under lock here
                _val = other;
                return true;
            }
        }
        
        public bool CompareAndSet(T expect, T update)
        {
            if (!AreValuesEqual(Value, expect)) return false;

            lock (_lock)
            {
                if (!AreValuesEqual(_val, expect)) return false;
                _val = update;
                return true;
            }
        }

        /// <summary>
        /// Atomically updates the current value by applying a computation function to it.
        /// The result of the computation becomes the new value.
        /// </summary>
        /// <param name="computation">The function to apply to the current value. It takes the current value and returns the new value.</param>
        /// <returns>The newly computed and set value.</returns>
        /// <remarks>
        /// The <paramref name="computation"/> delegate is invoked while holding the internal lock.
        /// Avoid performing long-running or blocking operations inside the <paramref name="computation"/>
        /// to prevent performance degradation or deadlocks in multi-threaded scenarios.
        /// </remarks>
        public T Compute(Func<T, T> computation)
        {
            lock (_lock)
            {
                return _val = computation.Invoke(_val);
            }
        }

        /// <summary>
        /// Atomically updates the current value using a computation function,
        /// but only if the current value is equal to a specified comparison value.
        /// Uses a double-checked locking pattern.
        /// </summary>
        /// <param name="comparisonValue">The value to compare with the current value. The computation only proceeds if they are equal.</param>
        /// <param name="computation">The function to apply to the current value to produce the new value.</param>
        /// <param name="result">When this method returns, contains the final value of the <see cref="AtomicRef{T}"/>
        /// after the operation (either the new computed value or the original value if no update occurred).</param>
        /// <returns><see langword="true"/> if the value was successfully computed and updated; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The <paramref name="computation"/> delegate is invoked while holding the internal lock.
        /// Avoid performing long-running or blocking operations inside the <paramref name="computation"/>
        /// to prevent performance degradation or deadlocks.
        /// </remarks>
        public bool ComputeIfEqual(T comparisonValue, Func<T, T> computation, out T result)
        {
            // First check (unlocked) for early exit optimization (reads via thread-safe 'Value' property)
            result = Value; // This read is now thread-safe due to 'Value.get' fix
            if (!AreValuesEqual(result, comparisonValue))
            {
                return false;
            }

            lock (_lock)
            {
                // Second check inside the lock to ensure atomicity and consistency
                if (!AreValuesEqual(_val, comparisonValue)) // _val is directly accessed under lock here
                {
                    result = _val; // Update result with the current value under lock if condition fails
                    return false;
                }
                _val = computation.Invoke(_val);
                result = _val; // Update result with the new value under lock
                return true;
            }
        }

        /// <summary>
        /// Atomically executes a specified action, but only if the current value of the atomic reference
        /// is equal to the specified comparison value. This method does not modify the internal value of the AtomicRef.
        /// Uses a double-checked locking pattern.
        /// </summary>
        /// <param name="comparisonValue">The value to compare with the current value. The action only proceeds if they are equal.</param>
        /// <param name="action">The action to execute if the values are equal.</param>
        /// <returns><see langword="true"/> if the action was executed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// **Important:** The <paramref name="action"/> delegate is invoked while holding the internal lock.
        /// **Avoid performing any long-running, blocking, or re-entrant operations (especially
        /// those that might try to acquire the same or related locks) inside this <paramref name="action"/>,
        /// as this can severely degrade performance or lead to deadlocks.**
        /// </remarks>
        public bool ComputeIfEqual(T comparisonValue, Action action)
        {
            // First check (unlocked) for early exit optimization (reads via thread-safe 'Value' property)
            if (!AreValuesEqual(Value, comparisonValue)) return false;

            lock (_lock)
            {
                // Second check (under lock) to ensure atomicity and consistency
                if (!AreValuesEqual(_val, comparisonValue)) return false;
                action.Invoke(); // Action is invoked inside the lock
                return true;
            }
        }

        /// <summary>
        /// Atomically updates the current value using a computation function,
        /// but only if its current value is different from a specified comparison value.
        /// Uses a double-checked locking pattern.
        /// </summary>
        /// <param name="comparisonValue">The value to compare with the current value. The computation only proceeds if they are different.</param>
        /// <param name="computation">The function to apply to the current value to produce the new value.</param>
        /// <param name="result">When this method returns, contains the final value of the <see cref="AtomicRef{T}"/>
        /// after the operation (either the new computed value or the original value if no update occurred).</param>
        /// <returns><see langword="true"/> if the value was successfully computed and updated; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The <paramref name="computation"/> delegate is invoked while holding the internal lock.
        /// Avoid performing long-running or blocking operations inside the <paramref name="computation"/>
        /// to prevent performance degradation or deadlocks.
        /// </remarks>
        public bool ComputeIfDifferent(T comparisonValue, Func<T, T> computation, out T result)
        {
            // First check (unlocked) for early exit optimization (reads via thread-safe 'Value' property)
            result = Value; // This read is now thread-safe due to 'Value.get' fix
            if (AreValuesEqual(result, comparisonValue))
            {
                return false;
            }

            lock (_lock)
            {
                // Second check (under lock) to ensure atomicity and consistency
                if (AreValuesEqual(_val, comparisonValue))
                {
                    result = _val; // Update result with the current value under lock if condition fails
                    return false;
                }
                _val = computation.Invoke(_val);
                result = _val; // Update result with the new value under lock
                return true;
            }
        }

        /// <summary>
        /// Atomically executes a specified action, but only if the current value of the atomic reference
        /// is different from a specified comparison value. This method does not modify the internal value of the AtomicRef.
        /// Uses a double-checked locking pattern.
        /// </summary>
        /// <param name="comparisonValue">The value to compare with the current value. The action only proceeds if they are different.</param>
        /// <param name="action">The action to execute if the values are different.</param>
        /// <returns><see langword="true"/> if the action was executed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// **Important:** The <paramref name="action"/> delegate is invoked while holding the internal lock.
        /// **Avoid performing any long-running, blocking, or re-entrant operations (especially
        /// those that might try to acquire the same or related locks) inside this <paramref name="action"/>,
        /// as this can severely degrade performance or lead to deadlocks.**
        /// </remarks>
        public bool ComputeIfDifferent(T comparisonValue, Action action)
        {
            // First check (unlocked) for early exit optimization (reads via thread-safe 'Value' property)
            if (AreValuesEqual(Value, comparisonValue)) return false;

            lock (_lock)
            {
                // Second check (under lock) to ensure atomicity and consistency
                if (AreValuesEqual(_val, comparisonValue)) return false;
                action.Invoke();
                return true;
            }
        }

        /// <summary>
        /// Helper method to compare two values of type <typeparamref name="T"/> for equality.
        /// Uses <see cref="EqualityComparer{T}.Default"/> for robust and optimized comparison,
        /// handling nulls and considering <see cref="IEquatable{T}"/> implementations.
        /// </summary>
        /// <param name="val1">The first value to compare.</param>
        /// <param name="val2">The second value to compare.</param>
        /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
        private static bool AreValuesEqual(T val1, T val2)
        {
            // Using EqualityComparer<T>.Default is the most robust way to compare T values
            return EqualityComparer<T>.Default.Equals(val1, val2);
        }
    }
}