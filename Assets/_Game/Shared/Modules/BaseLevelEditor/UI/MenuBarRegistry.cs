// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.FReflection;

namespace Falcon.Shared.BaseLevelEditor
{
    public class MenuNode
    {
        public readonly string Label;
        public IMenuItem Contributor; // non-null only on leaf nodes
        public readonly List<MenuNode> Children = new();
        public bool IsLeaf => Children.Count == 0;

        public MenuNode(string label) => Label = label;
    }

    public static class MenuBarRegistry
    {
        private static List<MenuNode> _topLevel;

        public static IReadOnlyList<MenuNode> TopLevel
        {
            get
            {
                if (_topLevel == null) Initialize();
                return _topLevel;
            }
        }

        public static void Initialize()
        {
            var rootOrder = new List<string>();
            var rootMap   = new Dictionary<string, MenuNode>();

            var sorted = FReflection.Instance.GetTypes()
                .Where(t => typeof(IMenuItem).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => (attr: t.GetCustomAttribute<MenuItemAttribute>(), type: t))
                .Where(x => x.attr != null)
                .OrderBy(x => x.attr.Order)
                .ToList();

            foreach (var (attr, type) in sorted)
            {
                var parts    = attr.Label.Split('/');
                var instance = (IMenuItem)Activator.CreateInstance(type);

                if (!rootMap.ContainsKey(parts[0]))
                {
                    rootMap[parts[0]] = new MenuNode(parts[0]);
                    rootOrder.Add(parts[0]);
                }

                var node = rootMap[parts[0]];

                for (var i = 1; i < parts.Length - 1; i++)
                {
                    var found = node.Children.Find(n => n.Label == parts[i]);
                    if (found == null)
                    {
                        found = new MenuNode(parts[i]);
                        node.Children.Add(found);
                    }
                    node = found;
                }

                if (parts.Length == 1)
                    node.Contributor = instance;
                else
                    node.Children.Add(new MenuNode(parts[^1]) { Contributor = instance });
            }

            _topLevel = rootOrder.Select(k => rootMap[k]).ToList();
        }
    }
}
