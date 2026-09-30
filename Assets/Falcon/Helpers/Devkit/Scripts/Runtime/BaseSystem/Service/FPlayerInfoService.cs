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
    /// Provides a centralized access point for all player-related information and data repositories.
    /// This service acts as a facade, consolidating access to various player data categories
    /// such as advertising, general profile, in-app purchases, custom properties, and session details.
    /// </summary>
    /// <remarks>
    /// As a singleton inheriting from <see cref="MySingleton{T}"/>, <see cref="FPlayerInfoService"/>
    /// ensures that there's only one instance providing consistent access to player data throughout the application.
    /// This design promotes modularity and makes it easier to manage player data dependencies.
    /// </remarks>
    public class FPlayerInfoService : MySingleton<FPlayerInfoService>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FPlayerInfoService"/> class
        /// by injecting the necessary player data repositories and services.
        /// </summary>
        /// <param name="ad">The repository for player advertising data (<see cref="IFPlayerAdRepository"/>).</param>
        /// <param name="general">The repository for general player profile information (<see cref="IFPlayerGeneralRepository"/>).</param>
        /// <param name="iap">The repository for player in-app purchase data (<see cref="IFPlayerIapRepository"/>).</param>
        /// <param name="custom">The repository for player-defined custom properties (<see cref="IFPlayerSelfDefineRepository"/>).</param>
        /// <param name="session">The service for player session management (<see cref="IFPlayerSessionService"/>).</param>
        public FPlayerInfoService(IFPlayerAdRepository ad, IFPlayerGeneralRepository general,
            IFPlayerIapRepository iap, CustomInfoService custom, IFPlayerSessionService session)
        {
            Ad = ad;
            General = general;
            Iap = iap;
            Custom = custom;
            Session = session;
        }

        /// <summary>
        /// Gets the repository for managing player advertising-related data.
        /// </summary>
        public IFPlayerAdRepository Ad { get; }

        /// <summary>
        /// Gets the repository for managing general player profile information.
        /// </summary>
        public IFPlayerGeneralRepository General { get; }

        /// <summary>
        /// Gets the repository for managing player in-app purchase (IAP) data.
        /// </summary>
        public IFPlayerIapRepository Iap { get; }

        /// <summary>
        /// Gets the repository for managing player-specific custom properties.
        /// </summary>
        public CustomInfoService Custom { get; }

        /// <summary>
        /// Gets the service for managing player session data and gameplay statistics.
        /// </summary>
        public IFPlayerSessionService Session { get; }
    }
}