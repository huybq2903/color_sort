/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-03-11
     */


namespace Falcon.Manager.Shared
{
	using System;
	using System.Runtime.InteropServices;

	[Serializable]
	[StructLayout(LayoutKind.Auto)]
	public struct GoogleAuthKey
	{
		public string idToken;

		public GoogleAuthKey(string idToken)
		{
			this.idToken = idToken;
		}
	}
}