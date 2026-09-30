/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-10
     */

using Sirenix.OdinInspector;

namespace Falcon.Modules.Core.GameData.Runtime
{
	[ResourceInfo("gold")]
	public class GoldResource : AResource
	{
		[ShowInInspector] public int Quantity { get; set; } = 200;

		protected override bool AddInternal(int amount, string data)
		{
			if (amount > 0)
			{
				Quantity += amount;
				return true;
			}
			return false;
		}
		
		protected override bool RemoveInternal(int amount, string data)
		{
			if (amount > 0 && amount <= Quantity)
			{
				Quantity -= amount;
				return true;
			}
			return false;
		}
		
		protected override int SetInternal(int value, string data)
		{
			if (value < 0) return 0;
			
			int valueChanged = value - Quantity;
			Quantity = value;
			return valueChanged;
		}

		protected override int ResetInternal()
		{
			int before = Quantity;
			Quantity = 0;
			return before;
		}

		public override string ToString()
		{
			return "Q: " + Quantity;
		}

		public override object Get    => Quantity;
		public override int    GetInt => Quantity;
	}
}