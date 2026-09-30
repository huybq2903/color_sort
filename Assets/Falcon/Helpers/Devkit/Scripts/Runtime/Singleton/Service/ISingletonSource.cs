/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonSource : ISingletonLogic
    {
        IEnumerable<ISingletonShell> GenerateShells();
    }

    public class MySingletonImplementationSource : ISingletonSource
    {
        public IEnumerable<ISingletonShell> GenerateShells()
        {
            return FReflection.FReflection.Instance.GetTypes()
                .Where(type => typeof(IMySingleton).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                .Select<Type, ISingletonShell>(type => typeof(MonoBehaviour).IsAssignableFrom(type)
                    ? new MonoSingletonShell(type)
                    : new ConstructorSingletonShell(type));
        }

        public int Priority => 0;
    }

    public class SingletonSupplierSource : ISingletonSource
    {
        public IEnumerable<ISingletonShell> GenerateShells()
        {
            return FReflection.FReflection.Instance.GetTypes()
                .Where(type => typeof(ISingletonSupplier).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                .SelectMany(ExtractShells);
        }

        public int Priority => -10;

        private static IEnumerable<ISingletonShell> ExtractShells(Type type)
        {
            return type.GetMethods()
                .Where(method =>
                {
                    if (method.GetParameters().Length == 0) return false;

                    var returnParameter = method.ReturnParameter;
                    return returnParameter != null &&
                           typeof(IMySingleton).IsAssignableFrom(returnParameter.ParameterType);
                })
                .Select(methodInfo => new MethodCallSingletonShell(methodInfo, type));
        }
    }
}