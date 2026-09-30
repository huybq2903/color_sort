/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.FReflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [FReflection]
    public interface ISingletonLogicSource
    {
        public IEnumerable<T> SearchLogic<T>() where T : ISingletonLogic;
    }

    public class FReflectionSingletonLogicSource : ISingletonLogicSource
    {
        private readonly LazyVal<ISet<ISingletonLogic>> _lazyVal = new(() => FReflection.FReflection.Instance.GetTypes()
            .Where(type => typeof(ISingletonLogic).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<ISingletonLogic>().ToHashSet());

        public ISet<ISingletonLogic> Logics => _lazyVal.Value;

        public IEnumerable<T> SearchLogic<T>() where T : ISingletonLogic
        {
            return Logics.OfType<T>();
        }

        public void ResetLogics()
        {
            _lazyVal.Reset();
        }
    }

    public static class SingletonLogicSources
    {
        private static readonly LazyVal<ISet<ISingletonLogicSource>> kLazy = new(() => FReflection.FReflection
            .Instance.GetTypes()
            .Where(type => typeof(ISingletonLogicSource).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<ISingletonLogicSource>().ToHashSet());

        public static ISet<ISingletonLogicSource> LogicSources => kLazy.Value;

        public static void ResetLogicSources()
        {
            kLazy.Reset();
        }

        public static IEnumerable<T> SearchLogic<T>() where T : ISingletonLogic
        {
            return LogicSources.SelectMany(source => source.SearchLogic<T>()).OrderBy(x => x.Priority);
        }
    }
}