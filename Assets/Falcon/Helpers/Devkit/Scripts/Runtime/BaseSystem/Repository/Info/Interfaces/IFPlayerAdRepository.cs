/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Defines the contract for a repository that manages player advertising-related data.
    /// This includes metrics such as Ad LTV (Lifetime Value) and counts of various ad types displayed to the player.
    /// </summary>
    /// <remarks>
    /// As an <see cref="IMySingleton"/>, implementers of this interface should ensure a single, consistent source
    /// for player ad data across the application, facilitating analytics and ad monetization tracking.
    /// </remarks>
    public interface IFPlayerAdRepository : IMySingleton
    {
        /// <summary>
        /// Gets or sets the estimated advertising lifetime value (Ad LTV) for the player.
        /// This value can be <see langword="null"/> if not yet calculated or available.
        /// </summary>
        double? AdLtv { get; }

        /// <summary>
        /// Retrieves the current count of a specific type of advertisement displayed to the player.
        /// </summary>
        /// <param name="adType">The <see cref="AdType"/> for which to retrieve the count.</param>
        /// <returns>The total number of times the specified ad type has been displayed.</returns>
        int AdCountOf(AdType adType);
        
        (int typeWatched, double? adLtv) NewAdWatched(AdType adType, double? adRev);
    }
}