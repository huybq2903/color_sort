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

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonNode
    {
        ISet<Type> Dependencies { get; }
        Type GenerateType { get; }
        ISingletonFactory BuildFactory(Dictionary<Type, FactoryInfo> factoryInfos);
    }

    public class MonoSingletonNode : ISingletonNode
    {
        public MonoSingletonNode(Type generateType, List<ISingletonDecorateRequirement> decorateRequirements)
        {
            GenerateType = generateType;
            DecorateRequirements = decorateRequirements;
            Dependencies = decorateRequirements.SelectMany(requirement => requirement.Dependencies).ToHashSet();
        }

        public List<ISingletonDecorateRequirement> DecorateRequirements { get; }

        public ISet<Type> Dependencies { get; }
        public Type GenerateType { get; }

        public ISingletonFactory BuildFactory(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new MonoSingletonFactory(
                new MonoCreateSupply(GenerateType),
                DecorateRequirements
                    .Select(decorateRequirement => decorateRequirement.ToSupply(factoryInfos))
                    .ToList()
            );
        }
    }

    public class ConstructorSingletonNode : ISingletonNode
    {
        public ConstructorSingletonNode(ConstructorCreateRequirement createRequirement,
            List<ISingletonDecorateRequirement> decorateRequirements)
        {
            CreateRequirement = createRequirement;
            DecorateRequirements = decorateRequirements;
            Dependencies = new HashSet<Type>(createRequirement.Dependencies);
            Dependencies.UnionWith(decorateRequirements.SelectMany(decorate => decorate.Dependencies).ToHashSet());
        }

        public ConstructorCreateRequirement CreateRequirement { get; }
        public List<ISingletonDecorateRequirement> DecorateRequirements { get; }
        public ISet<Type> Dependencies { get; }
        public Type GenerateType => CreateRequirement.InstanceType;

        public ISingletonFactory BuildFactory(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new ConstructorSingletonFactory(
                CreateRequirement.ToSupply(factoryInfos),
                DecorateRequirements
                    .Select(decorateRequirement => decorateRequirement.ToSupply(factoryInfos))
                    .ToList()
            );
        }
    }

    public class MethodCallSingletonNode : ISingletonNode
    {
        public MethodCallSingletonNode(Type sourceType, MethodInfo singletonMethod)
        {
            SourceType = sourceType;
            SingletonMethod = singletonMethod;
            Dependencies = new HashSet<Type> { SourceType };
        }

        public Type SourceType { get; }
        public MethodInfo SingletonMethod { get; }
        public ISet<Type> Dependencies { get; }
        public Type GenerateType => SingletonMethod.ReturnType;

        public ISingletonFactory BuildFactory(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new MethodCallSingletonFactory(
                factoryInfos[SourceType].Factory,
                SingletonMethod
            );
        }
    }
}