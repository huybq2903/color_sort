/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Defines the contract for a repository that manages general player information.
    /// This interface is intended to be implemented by a singleton class to ensure a single, consistent source
    /// for core player data within the BigData module.
    /// </summary>
    /// <remarks>
    /// As an <see cref="IMySingleton"/>, implementers of this interface should provide a mechanism
    /// to ensure only one instance exists application-wide, facilitating centralized access to player data.
    /// </remarks>
    public interface IFPlayerGeneralRepository : IMySingleton
    {
        /// <summary>
        /// Gets or sets the unique account identifier for the player.
        /// </summary>
        string AccountID { get; }

        /// <summary>
        /// Gets or sets the maximum level the player has successfully completed in the game.
        /// </summary>
        int MaxPassedLevel { get; set; }

        /// <summary>
        /// Gets or sets the version of the application when it was initially installed.
        /// </summary>
        string InstallVersion { get;}

        /// <summary>
        /// Gets or sets the advertising identifier associated with the player's device.
        /// This is often used for analytics and ad attribution.
        /// </summary>
        [CanBeNull] string AdvertisingID { get; }
    }
}