/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-20
 */

using System;
using System.Collections.Generic;

namespace Falcon.Manager.Shared
{
    [Serializable]
    public class GameTemplateConfig
    {
        public List<GameTemplatePackage> templates = new List<GameTemplatePackage>();
    }
}
