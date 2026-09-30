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
    public static class GeneralExtensions
    {
        public static T As<T>(this object obj)
        {
            if (obj is T t)
            {
                return t;
            }
            throw new InvalidCastException($"Value {obj} is not of type {typeof(T).Name}");
        }
    }
}