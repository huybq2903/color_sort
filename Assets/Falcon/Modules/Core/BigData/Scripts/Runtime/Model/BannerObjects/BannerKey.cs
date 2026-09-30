/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public readonly struct BannerKey : IEquatable<BannerKey>
    {
        public string AdWhere { get; }
        public string AdPrecision { get; }
        public string AdCountry { get; }
        public string AdNetwork { get; }
        public string AdMediation { get; }
        
        public int CurrentLevel { get; }

        public BannerKey(string adWhere, string adPrecision, string adCountry, string adNetwork, string adMediation, int currentLevel)
        {
            AdWhere = adWhere;
            AdPrecision = adPrecision;
            AdCountry = adCountry;
            AdNetwork = adNetwork;
            AdMediation = adMediation;
            CurrentLevel = currentLevel;
        }

        public bool Equals(BannerKey other)
        {
            return AdWhere == other.AdWhere && AdPrecision == other.AdPrecision && AdCountry == other.AdCountry && AdNetwork == other.AdNetwork && AdMediation == other.AdMediation && CurrentLevel == other.CurrentLevel;
        }

        public override bool Equals(object obj)
        {
            return obj is BannerKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AdWhere, AdPrecision, AdCountry, AdNetwork, AdMediation, CurrentLevel);
        }

        public static bool operator ==(BannerKey left, BannerKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BannerKey left, BannerKey right)
        {
            return !left.Equals(right);
        }
    }
}