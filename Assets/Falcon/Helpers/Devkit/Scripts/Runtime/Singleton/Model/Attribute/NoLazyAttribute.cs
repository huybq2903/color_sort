/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-07
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Method)]
    public class NoLazyAttribute : Attribute
    {
    }
}