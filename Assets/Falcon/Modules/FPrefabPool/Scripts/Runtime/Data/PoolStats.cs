/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

namespace Falcon.Modules.FPrefabPool.Runtime
{
    public readonly struct PoolStats
    {
        // Tổng số instance ĐÃ Instantiate suốt vòng đời pool (lifetime-cumulative, không bao giờ giảm).
        public readonly int lifetimeCreated;
        public readonly int inactive;
        public readonly int active;
        public readonly int peakActive;

        public PoolStats(int lifetimeCreated, int inactive, int active, int peakActive)
        {
            this.lifetimeCreated = lifetimeCreated;
            this.inactive = inactive;
            this.active = active;
            this.peakActive = peakActive;
        }
    }
}