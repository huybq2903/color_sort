/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using JetBrains.Annotations;

// Assuming InAppData is defined here

// Used for [CanBeNull]

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Defines the contract for a repository that manages player in-app purchase (IAP) related data.
    /// This includes metrics such as IAP Lifetime Value, purchase counts, and details about the first purchase.
    /// </summary>
    /// <remarks>
    /// As an <see cref="IMySingleton"/>, implementers of this interface should ensure a single, consistent source
    /// for player IAP data across the application, facilitating analytics and monetization tracking.
    /// </remarks>
    public interface IFPlayerIapRepository : IMySingleton
    {
        /// <summary>
        /// Gets the player's in-app purchase lifetime value (IAP LTV) data.
        /// This property provides aggregated information about the total value of purchases.
        /// </summary>
        /// <value>An instance of <see cref="InAppData"/> representing the IAP LTV, or <see langword="null"/> if no IAP data is available.</value>
        [CanBeNull] 
        InAppData InAppLtv { get; }

        /// <summary>
        /// Gets or sets the total number of in-app purchase transactions made by the player.
        /// </summary>
        int InAppCount { get; }

        /// <summary>
        /// Gets or sets the game level at which the player made their very first in-app purchase.
        /// This value is <see langword="null"/> if no purchase has been made yet.
        /// </summary>
        int? FirstInAppLv { get; }

        /// <summary>
        /// Gets or sets the date and time of the player's very first in-app purchase.
        /// This value is <see langword="null"/> if no purchase has been made yet.
        /// </summary>
        DateTime? FirstInAppDate { get; }

        /// <summary>
        /// Gets a string representation of the date of the player's first in-app purchase.
        /// This property is derived from <see cref="FirstInAppDate"/> and might be formatted for logging or display.
        /// </summary>
        /// <value>A formatted string representing the first IAP date, or an empty string/specific placeholder if no purchase has been made.</value>
        string FirstInAppDateStr { get; }

        /// <summary>
        /// Gets or sets the product identifier of the item purchased in the player's very first in-app transaction.
        /// </summary>
        string FirstInAppProduct{ get; }
        
        int? LastInAppLv { get; }
        DateTime? LastInAppDate { get; }
        string LastInAppDateStr { get; }
        string LastInAppProduct{ get; }

        void RecordNewIap(string iapProduct, decimal amount, string isoCountryCode, int maxPassedLevel);
    }
}