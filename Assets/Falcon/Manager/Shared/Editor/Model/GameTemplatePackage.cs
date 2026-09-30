/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-20
 */

using System;

namespace Falcon.Manager.Shared
{
    [Serializable]
    public class GameTemplatePackage
    {
        public string fileName;
        public string displayName;
        public string version;
        public string date;
        public string changelog;
    }
}
