/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-08-04
     */


namespace Falcon.Manager.Shared
{
	using UnityEditor;

	public abstract class ACMSValidatedController : AViewController
	{
		public abstract void SetCMSService(CMSService service);
	}
}