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
    
    [AttributeUsage(AttributeTargets.Class)]
    public class PrimaryAttribute : System.Attribute
    {
        public PrimaryAttribute(int priority = 0)
        {
            Priority = priority;
        }

        public int Priority { get; set; }
    }
}