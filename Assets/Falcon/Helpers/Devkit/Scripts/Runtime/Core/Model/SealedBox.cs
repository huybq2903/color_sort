/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Provides static factory methods and extension methods for creating and manipulating <see cref="SealedBox{T}"/> instances.
    /// </summary>
    public static class SealedBox
    {
        /// <summary>
        /// Creates an empty <see cref="SealedBox{T}"/> instance.
        /// </summary>
        /// <typeparam name="TG">The type of the value the box would contain.</typeparam>
        /// <returns>A new <see cref="SealedBox{T}"/> instance that indicates the absence of a value.</returns>
        public static SealedBox<TG> Empty<TG>()
        {
            return SealedBox<TG>.Instance;
        }

        /// <summary>
        /// Creates a <see cref="SealedBox{T}"/> containing the specified value.
        /// If the provided value is <see langword="null"/> (for reference types), an empty box will be returned.
        /// </summary>
        /// <typeparam name="TG">The type of the value to box.</typeparam>
        /// <param name="t">The value to store in the box.</param>
        /// <returns>A <see cref="SealedBox{T}"/> containing <paramref name="t"/> if <paramref name="t"/> is not <see langword="null"/>;
        /// otherwise, an empty <see cref="SealedBox{T}"/>.</returns>
        public static SealedBox<TG> Of<TG>(TG t)
        {
            return Equals(t, null) ? Empty<TG>() : new SealedBox<TG>(t);
        }
        
        /// <summary>
        /// Creates a <see cref="SealedBox{T}"/> from a nullable value type.
        /// If the nullable value type has no value, an empty box will be returned.
        /// </summary>
        /// <typeparam name="TG">The value type (must be a struct).</typeparam>
        /// <param name="t">The nullable value type to box.</param>
        /// <returns>A <see cref="SealedBox{T}"/> containing <paramref name="t"/>.Value if <paramref name="t"/> has a value;
        /// otherwise, an empty <see cref="SealedBox{T}"/>.</returns>
        public static SealedBox<TG> OfNullable<TG>(TG? t) where TG : struct
        {
            return t.HasValue ? new SealedBox<TG>(t.Value) : Empty<TG>();
        }

        /// <summary>
        /// Transforms the value inside a <see cref="SealedBox{TIn}"/> using a provided mapping function.
        /// If the input box is empty, the mapping function is not applied, and an empty box of the output type is returned.
        /// </summary>
        /// <typeparam name="TIn">The input type of the value in the sealed box.</typeparam>
        /// <typeparam name="TOut">The output type of the value after transformation.</typeparam>
        /// <param name="sealedBox">The input <see cref="SealedBox{TIn}"/> instance.</param>
        /// <param name="map">The function to apply to the value if it is present.</param>
        /// <returns>
        /// A new <see cref="SealedBox{TOut}"/> containing the transformed value if the input box has a value;
        /// otherwise, an empty <see cref="SealedBox{TOut}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="map"/> is <see langword="null"/>.</exception>
        public static SealedBox<TOut> Map<TIn, TOut>(this SealedBox<TIn> sealedBox, Func<TIn, TOut> map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            return sealedBox.TryGetValue(out var value) ? new SealedBox<TOut>(map(value)) : Empty<TOut>();
        }

        /// <summary>
        /// Converts a <see cref="SealedBox{T}"/> of a struct type into its equivalent nullable struct type.
        /// </summary>
        /// <typeparam name="T">The struct type of the value in the sealed box.</typeparam>
        /// <param name="sealedBox">The input <see cref="SealedBox{T}"/> instance containing a struct value.</param>
        /// <returns>
        /// The struct value as a nullable type if the box has a value; otherwise, <see langword="null"/>.
        /// </returns>
        public static T? AsNullable<T>(this SealedBox<T> sealedBox) where T : struct
        {
            return sealedBox.TryGetValue(out var value) ? value : null;
        }
    }

    /// <summary>
    /// Represents an immutable container for a value that may or may not be present.
    /// This struct is analogous to an "Optional" or "Maybe" type, providing a robust way to handle
    /// the presence or absence of a value without relying on <see langword="null"/> references directly.
    /// </summary>
    /// <remarks>
    /// Being a <see langword="readonly struct"/>, <see cref="SealedBox{T}"/> offers performance benefits
    /// by minimizing heap allocations. It correctly handles reference types where <see langword="null"/>
    /// is interpreted as an empty state.
    /// </remarks>
    /// <typeparam name="T">The type of the value contained within the box.</typeparam>
    [Serializable]
    public readonly struct SealedBox<T> : IEquatable<SealedBox<T>>
    {
        /// <summary>
        /// Represents the singleton instance of an empty <see cref="SealedBox{T}"/>.
        /// Use this when you need an instance that indicates the absence of a value.
        /// </summary>
        public static readonly SealedBox<T> Instance = new();
        
        // The actual value stored in the box. Its default value is used if hasValue is false.
        private readonly T _val;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="SealedBox{T}"/> struct with the specified value.
        /// If <paramref name="value"/> is <see langword="null"/> (for reference types), the box will be considered empty.
        /// </summary>
        /// <param name="value">The value to store in the box.</param>
        internal SealedBox(T value)
        {
            // For reference types, a null value means no value is present.
            // For value types, default(T) is considered a value.
            HasValue = !Equals(value, null);
            _val = value;
        }

        /// <summary>
        /// Gets the value contained in the box.
        /// </summary>
        /// <value>The contained value.</value>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="HasValue"/> is <see langword="false"/> (i.e., the box is empty).</exception>
        /// <remarks>
        /// This property is ignored during JSON serialization by <see cref="Newtonsoft.Json.JsonIgnoreAttribute"/>
        /// to prevent exceptions when trying to serialize an empty box.
        /// </remarks>
        [JsonIgnore]
        public T Value => HasValue ? _val : throw new InvalidOperationException("Box is empty");

        /// <summary>
        /// Gets a value indicating whether the <see cref="SealedBox{T}"/> contains a value.
        /// </summary>
        /// <value><see langword="true"/> if the box contains a value; otherwise, <see langword="false"/>.</value>
        public bool HasValue { get; }

        /// <summary>
        /// Attempts to get the value contained in the box without throwing an exception.
        /// </summary>
        /// <param name="value">
        /// When this method returns, contains the value from the box if <see cref="HasValue"/> is <see langword="true"/>;
        /// otherwise, the default value of <typeparamref name="T"/>. This parameter is passed uninitialized.
        /// </param>
        /// <returns><see langword="true"/> if the box contains a value; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue(out T value)
        {
            value = _val; // Assigns default(T) if hasValue is false
            return HasValue;
        }

        /// <summary>
        /// Determines whether this <see cref="SealedBox{T}"/> instance is equal to another specified <see cref="SealedBox{T}"/> instance.
        /// </summary>
        /// <param name="other">The <see cref="SealedBox{T}"/> instance to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
        public bool Equals(SealedBox<T> other)
        {
            // If both don't have value, they are equal.
            // If one has value and other doesn't, they are not equal.
            // If both have value, compare their values.
            return HasValue == other.HasValue &&
                   (!HasValue || EqualityComparer<T>.Default.Equals(_val, other._val));
        }

        /// <summary>
        /// Determines whether this <see cref="SealedBox{T}"/> instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified object is a <see cref="SealedBox{T}"/> and is equal to the current instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is SealedBox<T> other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            // Combine hash codes of value (if present) and hasValue flag.
            return HashCode.Combine(_val, HasValue);
        }

        /// <summary>
        /// Compares two <see cref="SealedBox{T}"/> instances for equality.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(SealedBox<T> left, SealedBox<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="SealedBox{T}"/> instances for inequality.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(SealedBox<T> left, SealedBox<T> right)
        {
            return !left.Equals(right);
        }
    }
}