using NUnit.Framework;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class NetworkTypeTests
    {
        [Test]
        public void Lan_MapsToWifi()
        {
            Assert.AreEqual(NetworkType.Wifi,
                NetworkTypeExtensions.FromReachability(NetworkReachability.ReachableViaLocalAreaNetwork));
        }

        [Test]
        public void CarrierData_MapsToCellular()
        {
            Assert.AreEqual(NetworkType.Cellular,
                NetworkTypeExtensions.FromReachability(NetworkReachability.ReachableViaCarrierDataNetwork));
        }

        [Test]
        public void NotReachable_MapsToNone()
        {
            Assert.AreEqual(NetworkType.None,
                NetworkTypeExtensions.FromReachability(NetworkReachability.NotReachable));
        }
    }
}
