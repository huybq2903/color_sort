    /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-29
     */


namespace Falcon.Modules.Core.SaveLoad.Runtime
{
    public interface ISaveLoadConfigs
    {
        /// <summary>
        /// Encrypted save load pass
        /// </summary>
        public string SaveLoadDefaultPass { get; }
        
        /// <summary>
        /// Random string for generate key
        /// </summary>
        public string SaveLoadRandomKey   { get; }
    }
}
