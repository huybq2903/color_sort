/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-11
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using Falcon.Helpers.FReflection;

	// FReflection để ResourceInjector tự quét được các injector con.
	[FReflection]
	public abstract class AResourceInject
	{
		public abstract void Inject(Injection injection);
	}
}