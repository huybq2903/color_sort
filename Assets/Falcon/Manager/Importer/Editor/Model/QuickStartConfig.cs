/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-02-25
     */


using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    [CreateAssetMenu(fileName = "SO_QuickStartConfig", menuName = "Falcon/Quick Start Config")]
    public class QuickStartConfig : ScriptableObject
    {
        [Serializable]
        public class ModuleGroup
        {
            public string groupName;
            public List<string> moduleNames;
        }

        public List<ModuleGroup> groups = new();
    }
}
