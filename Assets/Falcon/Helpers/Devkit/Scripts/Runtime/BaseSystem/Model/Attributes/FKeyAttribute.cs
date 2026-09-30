/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FKeyAttribute : Attribute
    {
        public bool Ignore { get; set; }
        public string Name { get; set; }
        public bool RemoveIfNull { get; set; }
    }
}