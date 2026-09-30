/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-24
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using Falcon.Helpers.FReflection;

	public abstract class AResource : IResource, IFReflection
	{
		public bool Add(int amount, string data)
		{
			bool added = AddInternal(amount, data);
			if (added)
			{
				OnChanged?.Invoke(amount, data);
			}
			return added;
		}
		
		public bool Remove(int amount, string data)
		{
			bool removed = RemoveInternal(amount, data);
			if (removed)
			{
				OnChanged?.Invoke(-amount, data);
			}
			return removed;
		}

		public int Set(int value, string data)
		{
			int valueChanged = SetInternal(value, data);
			OnChanged?.Invoke(valueChanged, data);
			return valueChanged;
		}

		public int Reset()
		{
			int value = ResetInternal();
			if (value > 0)
			{
				OnChanged?.Invoke(-value, null);
			}

			return value;
		}

		protected abstract bool AddInternal(int amount, string data);
		protected abstract bool RemoveInternal(int amount, string data);
		protected abstract int  SetInternal(int value, string data);
		protected abstract int  ResetInternal();
		
		public OnChanged OnChanged { get; set; }

		public virtual object Get    => null;
		public virtual int    GetInt => 0;
	}
}