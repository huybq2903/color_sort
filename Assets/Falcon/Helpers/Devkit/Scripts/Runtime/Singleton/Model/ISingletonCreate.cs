/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonCreateRequirement
    {
        ISet<Type> Dependencies { get; }
        public Type InstanceType { get; }
        ISingletonCreateSupply ToSupply(Dictionary<Type, FactoryInfo> factoryInfos);
    }

    public interface ISingletonCreateSupply
    {
        ISet<Type> Dependencies { get; }
        public Type InstanceType { get; }
        IMySingleton Create();
    }

    public class MonoSingletonCreateRequirement : ISingletonCreateRequirement
    {
        public MonoSingletonCreateRequirement(Type instanceType)
        {
            InstanceType = instanceType;
        }

        public Type InstanceType { get; }
        public ISet<Type> Dependencies { get; } = new HashSet<Type>();

        public ISingletonCreateSupply ToSupply(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new MonoCreateSupply(InstanceType);
        }
    }

    public class MonoCreateSupply : ISingletonCreateSupply
    {
        public MonoCreateSupply(Type instanceType)
        {
            InstanceType = instanceType;
        }

        public Type InstanceType { get; }

        public ISet<Type> Dependencies { get; } = new HashSet<Type>();

        public IMySingleton Create()
        {
            return MainGameObj.Instance.GetOrAddComponent(InstanceType).As<IMySingleton>();
        }
    }

    public class ConstructorCreateRequirement : ISingletonCreateRequirement
    {
        public ConstructorCreateRequirement(Type instanceType, ISet<Type> consideringTypes,
            List<ISingletonParamResolver> paramResolvers)
        {
            InstanceType = instanceType;
            var constructorInfos = instanceType.GetConstructors();

            foreach (var info in constructorInfos)
                if (info.GetCustomAttribute<SingletonConstructorAttribute>(false) != null)
                {
                    ConstructorInfo = info;
                    break;
                }

            if (ConstructorInfo == null && constructorInfos.Length > 0) ConstructorInfo ??= constructorInfos[0];

            ScanConstructorParams(consideringTypes, paramResolvers);
        }

        public List<ISingletonParamRequirement> ParamRequirements { get; } = new();
        [CanBeNull] public ConstructorInfo ConstructorInfo { get; }
        public ISet<Type> Dependencies { get; } = new HashSet<Type>();
        public Type InstanceType { get; }

        public ConstructorCreateSupply ToSupply(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new ConstructorCreateSupply(InstanceType, ConstructorInfo,
                ParamRequirements.Select(requirement => requirement.ToResponse(factoryInfos)).ToList(), Dependencies);
        }

        ISingletonCreateSupply ISingletonCreateRequirement.ToSupply(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return ToSupply(factoryInfos);
        }

        private void ScanConstructorParams(ISet<Type> consideringTypes, List<ISingletonParamResolver> paramResolvers)
        {
            if (ConstructorInfo == null) return;
            foreach (var parameterInfo in ConstructorInfo.GetParameters())
                try
                {
                    var type = parameterInfo.ParameterType;
                    var paramContext = new ParamContext(parameterInfo);
                    var resolved = false;
                    foreach (var resolver in paramResolvers)
                    {
                        if (!resolver.TryResolveParamType(type, paramContext, consideringTypes,
                                out var param)) continue;
                        ParamRequirements.Add(param);
                        foreach (var dependentType in param.Dependencies) Dependencies.Add(dependentType);
                        resolved = true;
                        break;
                    }

                    if (!resolved)
                        throw new RequirementException(
                            $"Resolving constructor {ConstructorInfo.ConstructorName} of {InstanceType.Name} failed: type {parameterInfo.ParameterType.Name} of field {parameterInfo.Name} can't be resolved");
                }
                catch (RequirementException e)
                {
                    throw new RequirementException(
                        $"Resolving constructor {ConstructorInfo.ConstructorName} of {InstanceType.Name} failed: type {parameterInfo.ParameterType.Name} of field {parameterInfo.Name} failed : {e.Message}");
                }
        }
    }

    public class ConstructorCreateSupply : ISingletonCreateSupply
    {
        public ConstructorCreateSupply(Type instanceType, [CanBeNull] ConstructorInfo constructorInfo,
            List<ISingletonParamSupply> paramSupplies, ISet<Type> dependencies)
        {
            InstanceType = instanceType;
            ConstructorInfo = constructorInfo;
            ParamSupplies = paramSupplies;
            Dependencies = dependencies;
        }

        public Type InstanceType { get; }
        [CanBeNull] public ConstructorInfo ConstructorInfo { get; }
        public List<ISingletonParamSupply> ParamSupplies { get; }

        public ISet<Type> Dependencies { get; }

        public IMySingleton Create()
        {
            return ConstructorInfo == null
                ? Activator.CreateInstance(InstanceType).As<IMySingleton>()
                : ConstructorInfo.Invoke(ParamSupplies.Select(param => param.Create()).ToArray()).As<IMySingleton>();
        }
    }
}