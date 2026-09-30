/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-28
 */

using System;
using System.Reflection;
using Falcon.Modules.Core.GameData.Runtime;
using Sirenix.OdinInspector;

namespace Falcon.Shared.BaseBooster
{
    /// <summary>Số lượt của 1 booster. Class con chỉ cần khai [ResourceInfo("booster_x")] là xong.</summary>
    public abstract class ABoosterResource : AResource
    {
        [ShowInInspector] public int Quantity { get; set; }
        [ShowInInspector] public bool IsFree { get; set; }

        public virtual void ResetDefault()
        {
            Quantity = BoosterConfig.DefaultQuantity(GetType().GetCustomAttribute<ResourceInfoAttribute>().Id);
            IsFree = true;
        }

        protected override bool AddInternal(int amount, string data)
        {
            if (amount <= 0) return false;
            Quantity += amount;
            return true;
        }

        protected override bool RemoveInternal(int amount, string data)
        {
            if (amount <= 0) return false;
            Quantity = Math.Max(Quantity - amount, 0);
            return true;
        }

        protected override int SetInternal(int value, string data)
        {
            if (value <= 0) return 0;
            Quantity = value;
            return value;
        }

        protected override int ResetInternal()
        {
            var before = Quantity;
            Quantity = 0;
            return before;
        }

        public override object Get => Quantity;
        public override int GetInt => Quantity;
    }
}
