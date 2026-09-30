    /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-10-01
     */


namespace Falcon.Modules.Core.SystemInformation.Runtime
{
    [System.Serializable]
    public class AppLibInfoSave
    {
        public int    numberLibFiles;
        public long   totalLibFileSize;
        public string libFileNameList;
        public string libFolder;
        public string libMD5;
    }
}
