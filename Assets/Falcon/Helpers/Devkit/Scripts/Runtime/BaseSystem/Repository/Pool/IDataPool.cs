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
    /// Represents a data pool interface for managing key-value pairs,
    /// providing thread-safe operations for retrieval, storage, and computation.
    /// This interface is designed to be a singleton within the application.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface should ensure thread safety for all operations,
    /// especially in multithreaded environments. It supports both reference types and nullable value types.
    /// </remarks>
    public interface IDataPool : IMySingleton
    {
        /// <summary>
        /// Retrieves the value associated with the specified key, or a default value if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="defaultValue">The value to return if the key does not exist.</param>
        /// <returns>
        /// The value associated with the specified key, or <paramref name="defaultValue"/>
        /// if the key is not found in the data pool.
        /// </returns>
        public T GetOrDefault<T>(string key, T defaultValue);

        /// <summary>
        /// Retrieves the value associated with the specified key. If the key does not exist,
        /// it adds the specified value to the data pool and returns it.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve or set.</typeparam>
        /// <param name="key">The key of the value to get or set.</param>
        /// <param name="valueIfNotExist">The value to add to the data pool if the key does not already exist.</param>
        /// <returns>
        /// The existing value associated with the key if found, or <paramref name="valueIfNotExist"/>
        /// if the key was not found and the value was successfully added.
        /// </returns>
        /// <remarks>
        /// This method is atomic, ensuring that if multiple threads attempt to add the same key simultaneously,
        /// only one value will be set, and all threads will receive the final (either existing or newly set) value.
        /// </remarks>
        public T GetOrSet<T>(string key, T valueIfNotExist);

        /// <summary>
        /// Determines whether the data pool contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the data pool.</param>
        /// <returns><c>true</c> if the data pool contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public bool HasKey(string key);

        /// <summary>
        /// Atomically computes and updates the value associated with the specified key for reference types.
        /// </summary>
        /// <typeparam name="T">The reference type of the value to compute. Must be a class type.</typeparam>
        /// <param name="key">The key of the value to compute.</param>
        /// <param name="function">A function that takes the existing value (or default <c>null</c> if not present)
        /// and returns the new computed value. The function is executed atomically with respect to other updates to the same key.</param>
        /// <returns>
        /// The new value after applying the <paramref name="function"/>.
        /// </returns>
        /// <remarks>
        /// If the key does not exist, the <paramref name="function"/> will be invoked with the default value for <typeparamref name="T"/> (which is <c>null</c> for reference types).
        /// The update operation is thread-safe and ensures that the <paramref name="function"/> is applied based on the latest value,
        /// even under concurrent modifications.
        /// </remarks>
        public T Compute<T>(string key, Func<T, T> function) where T : class;

        /// <summary>
        /// Atomically computes and updates the value associated with the specified key for nullable value types.
        /// </summary>
        /// <typeparam name="T">The value type of the value to compute. Must be a struct type.</typeparam>
        /// <param name="key">The key of the value to compute.</param>
        /// <param name="function">A function that takes the existing nullable value (or <c>null</c> if not present)
        /// and returns the new computed nullable value. The function is executed atomically with respect to other updates to the same key.</param>
        /// <returns>
        /// The new value after applying the <paramref name="function"/>.
        /// </returns>
        /// <remarks>
        /// If the key does not exist, the <paramref name="function"/> will be invoked with <c>null</c> for the nullable value type.
        /// This overload is specifically for structs and allows explicit handling of nullability for value types.
        /// The update operation is thread-safe and ensures that the <paramref name="function"/> is applied based on the latest value,
        /// even under concurrent modifications.
        /// </remarks>
        public T? Compute<T>(string key, Func<T?, T?> function) where T : struct;

        /// <summary>
        /// Stores or updates the value associated with the specified key in the data pool.
        /// </summary>
        /// <typeparam name="T">The type of the value to save.</typeparam>
        /// <param name="key">The key of the value to store.</param>
        /// <param name="value">The value to store.</param>
        /// <remarks>
        /// If the key already exists, its value will be overwritten.
        /// This operation should be thread-safe.
        /// </remarks>
        public void Save<T>(string key, T value);

        /// <summary>
        /// Removes the value with the specified key from the data pool.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <remarks>
        /// If the key does not exist, the operation should complete without throwing an exception.
        /// This operation should be thread-safe.
        /// </remarks>
        public void Delete(string key);

        /// <summary>
        /// Đẩy dữ liệu xuống ĐĨA ngay (best-effort, không chặn nếu đang có sync khác chạy).
        /// <br/>Vì sao phải có: pool chỉ tự sync lúc app pause + tick 5 phút — mọi <c>Compute</c>/
        /// <c>Save</c> giữa hai mốc đó nằm trong RAM, force-kill là mất. Đường ghi nào tự nhận là
        /// KILL-SAFE (cụm banner, sổ PENDING txn) phải gọi cái này sau khi ghi, không thì
        /// kill-safe chỉ là kill-safe trên giấy.
        /// </summary>
        /// <returns>false nếu nhường lượt vì một sync khác đang chạy.</returns>
        public bool TrySync();
    
#if UNITY_EDITOR
        public void Clear();
#endif
    }
}