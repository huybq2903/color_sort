/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-10
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using Newtonsoft.Json;

	public delegate void OnChanged(int amount, string data);

	public interface IResource
	{
		bool Add(int amount, string data);
		bool Remove(int amount, string data);
		int  Set(int amount, string data);
		int  Reset();

		[JsonIgnore] OnChanged OnChanged { get; set; }
		[JsonIgnore] object    Get       { get; }
	}
}