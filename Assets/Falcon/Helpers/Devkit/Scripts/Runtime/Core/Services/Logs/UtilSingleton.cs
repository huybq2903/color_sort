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
    public abstract class UtilSingleton<T> where T : UtilSingleton<T>, new()
    {
        private static readonly Lazy<T> Lazy = new(() => new T());
        // ReSharper disable once MemberCanBeProtected.Global
        public static T Instance => Lazy.Value;
    }
}