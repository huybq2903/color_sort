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
    /// Defines the contract for a repository that manages player session-specific data and gameplay statistics.
    /// This includes tracking session durations, login times, and active days.
    /// </summary>
    /// <remarks>
    /// This interface is intended to be implemented by a singleton class, as indicated by its inheritance from <see cref="IMySingleton"/>.
    /// This ensures a single, consistent source for player session information across the application,
    /// which is crucial for accurate analytics and user behavior tracking.
    /// </remarks>
    public interface IFPlayerSessionRepository : IMySingleton
    {
        /// <summary>
        /// Retrieves the total accumulated seconds a player has spent in a specific game mode.
        /// </summary>
        /// <param name="gameMode">The identifier or name of the game mode.</param>
        /// <returns>The total number of seconds played in the specified <paramref name="gameMode"/>.</returns>
        long GetModeTotalSec(string gameMode);
        
        long IncreaseModeTotalSec(string gameMode, long seconds);

        /// <summary>
        /// Gets or sets the timestamp (in milliseconds) of the player's very first login to the application.
        /// </summary>
        long FirstLogInMillis { get; }

        /// <summary>
        /// Gets or sets the total number of distinct active days the player has engaged with the application.
        /// </summary>
        int ActiveDays { get; }

        /// <summary>
        /// Gets or sets the unique identifier for the current session.
        /// This ID typically increments with each new player session.
        /// </summary>
        int SessionId { get; }
        
        bool RetentionChanged { get; }
    }
}