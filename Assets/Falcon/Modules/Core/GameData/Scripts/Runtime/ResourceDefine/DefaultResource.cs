/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-27
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	[ResourceInfo("default_resource")]
	public class DefaultResource : AResource
	{
		protected override bool AddInternal(int amount, string data)
		{
			return false;
		}
		
		protected override bool RemoveInternal(int amount, string data)
		{
			return false;
		}
		
		protected override int SetInternal(int value, string data)
		{
			return 0;
		}

		protected override int ResetInternal()
		{
			return 0;
		}

		public override object Get => 0;
	}
}