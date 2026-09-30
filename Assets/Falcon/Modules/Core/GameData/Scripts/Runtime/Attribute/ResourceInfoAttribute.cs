/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-10
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using System;

	[AttributeUsage(AttributeTargets.Class)]
	public class ResourceInfoAttribute : Attribute
	{
		public string Id { get; }

		public ResourceInfoAttribute(string id)
		{
			Id = id;
		}
	}
}