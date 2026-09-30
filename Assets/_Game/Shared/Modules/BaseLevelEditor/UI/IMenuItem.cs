// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-26

using System;
using Falcon.Helpers.FReflection;

namespace Falcon.Shared.BaseLevelEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MenuItemAttribute : Attribute
    {
        public readonly string Label;
        public readonly int Order;

        public MenuItemAttribute(string label, int order = 0)
        {
            Label = label;
            Order = order;
        }
    }

    [FReflection]
    public interface IMenuItem
    {
        void OnClick();
    }
}
