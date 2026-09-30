/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-11
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using System;
	using System.Linq;
	using Falcon.Helpers.FReflection;

	public delegate void Injection(AResource resource);

	/// <summary>
	/// Use this class to inject your custom resources into <see cref="GameDataCore"/>
	/// </summary>
	public static class ResourceInjector
	{
		/// <summary>
		/// Will call injection add of <see cref="GameDataCore"/> for initialize.
		/// </summary>
		/// <param name="injection">Injection</param>
		public static void Injection(Injection injection)
		{
			if (injection == null) return;

			var injectTypes = FReflection.Instance.GetTypes()
				.Where(t => typeof(AResourceInject).IsAssignableFrom(t) && !t.IsAbstract);

			foreach (var type in injectTypes)
			{
				((AResourceInject)Activator.CreateInstance(type)).Inject(injection);
			}
		}
	}
}