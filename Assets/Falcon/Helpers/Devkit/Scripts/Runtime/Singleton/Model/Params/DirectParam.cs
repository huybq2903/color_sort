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
    public class DirectParamRequirement : ISingletonParamRequirement
    {
        public DirectParamRequirement(Type requireType, bool nullable, ISet<Type> consideringTypes)
        {
            var set = consideringTypes.Where(requireType.IsAssignableFrom).ToHashSet();
            var primaries = new Dictionary<Type, int>();
            foreach (var type in set)
            {
                var primaryAttribute = type.GetCustomAttribute<PrimaryAttribute>();
                if (primaryAttribute != null) primaries.Add(type, primaryAttribute.Priority);
            }

            switch (primaries.Count)
            {
                case > 1:
                    var maxPriority = primaries.Values.Max();
                    var tops = primaries.Where(pair => pair.Value == maxPriority).Select(pair => pair.Key).ToList();
                    if (tops.Count > 1)
                        throw new RequirementException(
                            $"Can't decide when multiple custom singletons are founded suitable for {requireType.Name}: {string.Join(", ", tops.Select(type => type.Name))}");
                    ActualType = tops[0];
                    break;
                case 1:
                    ActualType = primaries.First().Key;
                    break;
                default:
                    switch (set.Count)
                    {
                        case > 1:
                            throw new RequirementException(
                                $"Can't decide when multiple singletons are founded suitable for {requireType.Name}: {string.Join(", ", set.Select(type => type.Name))}");
                        case 1:
                            ActualType = set.First();
                            break;
                        default:
                        {
                            if (nullable) ActualType = null;
                            else
                                throw new RequirementException(
                                    $"No singletons are founded suitable for {requireType.Name}");
                            break;
                        }
                    }

                    break;
            }
            Dependencies = new HashSet<Type> { ActualType };
        }

        public Type ActualType { get; }

        public ISet<Type> Dependencies { get; }

        public ISingletonParamSupply ToResponse(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return factoryInfos.TryGetValue(ActualType, out var factoryInfo)
                ? new DirectParamSupply(factoryInfo.Factory)
                : new NullParamSupply();
        }
    }

    public class NullParamSupply : ISingletonParamSupply
    {
        public ISet<Type> Dependencies { get; }= new HashSet<Type>();

        public object Create()
        {
            return null;
        }
    }

    public class DirectParamSupply : ISingletonParamSupply
    {
        public DirectParamSupply(ISingletonFactory factory)
        {
            Factory = factory;
            Dependencies = new HashSet<Type> { Factory.InstanceType };
        }

        public ISingletonFactory Factory { get; }

        public ISet<Type> Dependencies { get; }
        public object Create()
        {
            return Factory.Instance;
        }
    }
}