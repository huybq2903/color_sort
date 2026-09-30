/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Represents aggregated data for in-app purchases (IAP), typically used to track a player's
    /// cumulative spending, highest single purchase, and total transaction count within a specific currency.
    /// </summary>
    /// <remarks>
    /// This class is designed to be serializable, making it suitable for persistence or transfer.
    /// It provides methods to update purchase statistics incrementally.
    /// </remarks>
    [Serializable] // Indicates that this class can be serialized (e.g., to JSON).
    public class InAppData
    {
        /// <summary>
        /// The total number of in-app purchase transactions.
        /// </summary>
        public int count;

        /// <summary>
        /// The ISO 4217 currency code for the monetary values (e.g., "USD", "EUR").
        /// </summary>
        public string isoCurrencyCode;

        /// <summary>
        /// The maximum single purchase amount recorded.
        /// </summary>
        public decimal max;

        /// <summary>
        /// The total cumulative amount spent across all purchases in the specified currency.
        /// </summary>
        public decimal total;

        /// <summary>
        /// Initializes a new instance of the <see cref="InAppData"/> class.
        /// This parameterless constructor is typically used by serializers (e.g., JSON.NET) to create an instance
        /// before populating its fields.
        /// </summary>
        [Preserve] // Prevents Unity's code stripping from removing this constructor, crucial for serialization.
        public InAppData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InAppData"/> class with specified initial values.
        /// </summary>
        /// <param name="total">The initial total cumulative amount spent.</param>
        /// <param name="max">The initial maximum single purchase amount.</param>
        /// <param name="count">The initial total number of transactions.</param>
        /// <param name="isoCurrencyCode">The ISO 4217 currency code for these values.</param>
        public InAppData(decimal total, decimal max, int count, string isoCurrencyCode)
        {
            this.total = total;
            this.max = max;
            this.count = count;
            this.isoCurrencyCode = isoCurrencyCode;
        }

        /// <summary>
        /// Updates the in-app purchase statistics with a new purchase amount.
        /// </summary>
        /// <param name="amount">The amount of the new purchase to add.</param>
        /// <remarks>
        /// This method increments the <see cref="count"/>, adds the <paramref name="amount"/> to the <see cref="total"/>,
        /// and updates the <see cref="max"/> purchase amount if the new <paramref name="amount"/> is higher.
        /// </remarks>
        public void Update(decimal amount)
        {
            total += amount;
            max = Math.Max(max, amount);
            count++;
        }
    }
}