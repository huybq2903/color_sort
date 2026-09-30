/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
namespace Falcon.Helpers.Devkit
{
    public readonly struct EvictionInfo<TK, TV>
    {
        public readonly EvictionReason reason;
        public readonly TK key;
        public readonly TV before;
        public readonly TV after;

        public EvictionInfo(EvictionReason reason, TK key, TV before, TV after = default)
        {
            this.reason = reason;
            this.key = key;
            this.before = before;
            this.after = after;
        }
    }
}