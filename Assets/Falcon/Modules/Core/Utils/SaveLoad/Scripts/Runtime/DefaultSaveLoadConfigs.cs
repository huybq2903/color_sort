/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-03
     */


namespace Falcon.Modules.Core.SaveLoad.Runtime
{
	public sealed class DefaultSaveLoadConfigs : ISaveLoadConfigs
	{
		public string SaveLoadDefaultPass => "Default_Pass";
		public string SaveLoadRandomKey   => "Random_Key";
	}
}