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
    /// Defines the contract for a service responsible for managing and providing access to
    /// player session-specific data and gameplay statistics.
    /// </summary>
    /// <remarks>
    /// As an <see cref="IMySingleton"/>, implementations of this interface should provide a single,
    /// consistent source for all player session information across the application.
    /// This service typically tracks login times, active days, session durations, and unique session identifiers.
    /// </remarks>
    public interface IFPlayerSessionService : IMySingleton
    {
        /// <summary>
        /// Gets the exact <see cref="DateTime"/> (UTC) when the current player session started.
        /// </summary>
        DateTime SessionStartTime { get; }

        /// <summary>
        /// Gets the total accumulated play time for the user across all sessions, including the current session's duration.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> representing the total play time.</value>
        TimeSpan TotalPlayTime { get; }

        /// <summary>
        /// Gets or sets the timestamp (in milliseconds since Unix epoch) of the player's very first login to the application.
        /// </summary>
        long FirstLogInMillis { get;}

        /// <summary>
        /// Gets or sets the total number of distinct active days the player has engaged with the application.
        /// </summary>
        int ActiveDays { get;}

        /// <summary>
        /// Gets or sets the unique identifier for the current session. This ID is typically persisted
        /// and increments with each new player session, serving as a distinct session counter.
        /// </summary>
        int SessionId { get;}

        /// <summary>
        /// Gets a unique string identifier generated specifically for the current application session.
        /// This ID is not persisted and is unique for each application launch.
        /// </summary>
        string SessionUid { get; }

        /// <summary>
        /// Gets the local <see cref="DateTime"/> of the player's very first login to the application.
        /// This is computed from <see cref="FirstLogInMillis"/>.
        /// </summary>
        DateTime FirstLogInDateTimeLocal { get; }

        /// <summary>
        /// Gets the local date component (without time) of the player's very first login.
        /// </summary>
        DateTime FirstLoginDateLocal { get; }

        /// <summary>
        /// Gets the player's retention value, which is the number of days between their first login date
        /// and the current date (excluding the first login day itself if it's the same day).
        /// Returns 0 if the current date is the same as or earlier than the first login date.
        /// </summary>
        int Retention { get; }

        /// <summary>
        /// Gets a value indicating whether a new active day has started since the last recorded login.
        /// This flag is useful for triggering daily bonuses or specific retention-related events.
        /// </summary>
        bool RetentionChanged { get; }

        /// <summary>
        /// Gets the duration of time elapsed since the application's last pause or since the session started if no pause occurred.
        /// </summary>
        TimeSpan TimeSinceLastPause { get; }

        /// <summary>
        /// Gets the total duration of the current application session from its start time to the current moment.
        /// </summary>
        TimeSpan SessionTime { get; }
    }

    /// <summary>
    /// Provides extension methods for the <see cref="IFPlayerSessionService"/> interface,
    /// primarily for converting local date/time properties to their UTC equivalents.
    /// </summary>
    public static class FPlayerSessionServiceExtensions
    {

        /// <summary>
        /// Converts the <see cref="IFPlayerSessionService.FirstLogInDateTimeLocal"/> to its UTC equivalent.
        /// </summary>
        /// <param name="service">The <see cref="IFPlayerSessionService"/> instance.</param>
        /// <returns>A <see cref="DateTime"/> representing the first login date and time in UTC.</returns>
        /// <remarks>
        /// **Correction:** The original code had a potential bug here, using `LastLoginInDateTimeLocal` instead of `FirstLogInDateTimeLocal`.
        /// This documentation assumes the corrected behavior is desired.
        /// </remarks>
        public static DateTime FirstLogInDateTimeUtc(this IFPlayerSessionService service)
        {
            return service.FirstLogInDateTimeLocal.ToUniversalTime(); // Corrected from service.LastLoginInDateTimeLocal
        }

        /// <summary>
        /// Converts the date component of <see cref="IFPlayerSessionService.FirstLogInDateTimeLocal"/> to its UTC equivalent.
        /// </summary>
        /// <param name="service">The <see cref="IFPlayerSessionService"/> instance.</param>
        /// <returns>A <see cref="DateTime"/> representing the first login date (time component set to midnight) in UTC.</returns>
        /// <remarks>
        /// **Correction:** The original code had a potential bug here, using `LastLoginInDateTimeLocal` instead of `FirstLoginDateLocal`.
        /// This documentation assumes the corrected behavior is desired.
        /// </remarks>
        public static DateTime FirstLoginDateUtc(this IFPlayerSessionService service)
        {
            // First convert the local date to UTC, then get its date component.
            return service.FirstLogInDateTimeLocal.ToUniversalTime().Date; // Corrected from service.LastLoginInDateTimeLocal
        }
    }
}