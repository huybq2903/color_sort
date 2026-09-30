/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class MySingletonService
    {
        private static readonly LazyVal<Dictionary<Type, FactoryInfo>> kLazy = new (() => new SingletonOrderBuilder().Factories());
        
        private static Dictionary<Type, FactoryInfo> Infos => kLazy.Value;

        public static void SortInitOrder<T>(List<T> list) where T : IMySingleton
        {
            list.Sort((a, b) =>
            {
                var dependencyOrder = Infos[a.GetType()].DependencyOrder;
                SingletonLogger.Instance.Info($"Order of {a.GetType().Name}: {dependencyOrder}");
                return dependencyOrder.CompareTo(Infos[b.GetType()].DependencyOrder);
            });
        }

        public static void SortInitOrder<T>(T[] array) where T : IMySingleton
        {
            Array.Sort(array,
                (a, b) => Infos[a.GetType()].DependencyOrder
                    .CompareTo(Infos[b.GetType()].DependencyOrder));
        }

        public static void SortDestroyOrder<T>(List<T> list) where T : IMySingleton
        {
            list.Sort((a, b) => -Infos[a.GetType()].DependencyOrder
                .CompareTo(Infos[b.GetType()].DependencyOrder));
        }

        public static void SortDestroyOrder<T>(T[] array) where T : IMySingleton
        {
            Array.Sort(array,
                (a, b) => -Infos[a.GetType()].DependencyOrder
                    .CompareTo(Infos[b.GetType()].DependencyOrder));
        }

        public static T Instance<T>() where T : IMySingleton
        {
            return Instance(typeof(T)).As<T>();
        }

        public static IMySingleton Instance(Type type)
        {
            return TryGetInstance(type, out var instance)
                ? instance
                : throw new SingletonException($"No Singleton of type {type.Name} can by found");
        }

        public static bool TryGetInstance<T>(out T singleton) where T : IMySingleton
        {
            if (TryGetInstance(typeof(T), out var mySingleton))
            {
                singleton = (T)mySingleton;
                return true;
            }

            singleton = default;
            return false;
        }

        public static bool TryGetInstance(Type type, out IMySingleton singleton)
        {
            if (Infos.TryGetValue(type, out var info))
            {
                singleton = info.Factory.Instance;
                return true;
            }

            try
            {
                var actualType = new DirectParamRequirement(type, true, Infos.Keys.ToHashSet()).ActualType;
                if (actualType != null)
                {
                    singleton = Infos[actualType].Factory.Instance;
                    return true;
                }
            }
            catch (RequirementException e)
            {
                SingletonLogger.Instance.Warning(e);
            }

            singleton = null;
            return false;
        }

        public static IEnumerable<T> ChildInstancesInitOrder<T>() where T : IMySingleton
        {
            var type = typeof(T);
            return Infos.Values
                .Where(info => type.IsAssignableFrom(info.Factory.InstanceType))
                .OrderBy(info => info.DependencyOrder)
                .Select(info => info.Factory.Instance.As<T>());
        }

        public static IEnumerable<T> ChildInstancesDestroyOrder<T>() where T : IMySingleton
        {
            var type = typeof(T);
            return Infos.Values
                .Where(info => type.IsAssignableFrom(info.Factory.InstanceType))
                .OrderBy(info => -info.DependencyOrder)
                .Select(info => info.Factory.Instance.As<T>());
        }
    }
}