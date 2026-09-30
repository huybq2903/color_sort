/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonDecorateRequirement
    {
        ISet<Type> Dependencies { get; }
        ISingletonDecorateSupply ToSupply(Dictionary<Type, FactoryInfo> factoryInfos);
    }

    public interface ISingletonDecorateSupply
    {
        ISet<Type> Dependencies { get; }
        void Decorate(IMySingleton singleton);
    }

    public static class SingletonDecorators
    {
        public static IEnumerable<ISingletonDecorateRequirement> Scan(
            Type scanType,
            ISet<Type> consideringTypes,
            List<ISingletonParamResolver> paramResolvers
        )
        {
            foreach (var fieldInfo in scanType.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                         BindingFlags.NonPublic))
            {
                if (fieldInfo.GetCustomAttribute<InjectAttribute>() == null) continue;
                yield return new FieldDecorateRequirement(fieldInfo, consideringTypes, paramResolvers);
            }

            foreach (var propertyInfo in scanType.GetProperties(BindingFlags.Instance | BindingFlags.Public |
                                                                BindingFlags.NonPublic))
            {
                if (propertyInfo.GetCustomAttribute<InjectAttribute>() == null) continue;
                if (!propertyInfo.CanWrite)
                    throw new SingletonException(
                        $"Property {propertyInfo.Name} of InjectAttribute {scanType} is not writable");

                yield return new PropertyDecorateRequirement(propertyInfo, consideringTypes, paramResolvers);
            }
        }
    }

    public class FieldDecorateRequirement : ISingletonDecorateRequirement
    {
        public FieldDecorateRequirement(
            FieldInfo fieldInfo,
            ISet<Type> consideringTypes,
            List<ISingletonParamResolver> paramResolvers
        )
        {
            FieldInfo = fieldInfo;
            var fieldType = fieldInfo.FieldType;
            try
            {
                var paramContext = new ParamContext(fieldInfo);
                foreach (var resolver in paramResolvers)
                {
                    if (!resolver.TryResolveParamType(fieldType, paramContext, consideringTypes,
                            out var param)) continue;
                    ParamRequirement = param;
                    foreach (var dependentType in param.Dependencies) Dependencies.Add(dependentType);
                    break;
                }

                if (ParamRequirement == null)
                    throw new RequirementException(
                        $"Resolving field {fieldInfo.Name} of {fieldInfo.DeclaringType!.Name} failed: type {fieldType.Name} can't be resolved");
            }
            catch (RequirementException e)
            {
                throw new RequirementException(
                    $"Resolving field {fieldInfo.Name} of {fieldInfo.DeclaringType!.Name} failed: type {fieldType.Name} failed : {e.Message}");
            }
        }

        public ISingletonParamRequirement ParamRequirement { get; }
        public FieldInfo FieldInfo { get; }
        public ISet<Type> Dependencies { get; } = new HashSet<Type>();

        public ISingletonDecorateSupply ToSupply(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new FieldDecorateSupply(FieldInfo, ParamRequirement.ToResponse(factoryInfos));
        }
    }

    public class FieldDecorateSupply : ISingletonDecorateSupply
    {
        public FieldDecorateSupply(FieldInfo fieldInfo, ISingletonParamSupply paramSupply)
        {
            FieldInfo = fieldInfo;
            ParamSupply = paramSupply;
        }

        public FieldInfo FieldInfo { get; }
        public ISingletonParamSupply ParamSupply { get; }

        public ISet<Type> Dependencies => ParamSupply.Dependencies;

        public void Decorate(IMySingleton singleton)
        {
            FieldInfo.SetValue(singleton, ParamSupply.Create());
        }
    }

    public class PropertyDecorateRequirement : ISingletonDecorateRequirement
    {
        public PropertyDecorateRequirement(
            PropertyInfo propertyInfo,
            ISet<Type> consideringTypes,
            List<ISingletonParamResolver> paramResolvers
        )
        {
            PropertyInfo = propertyInfo;
            var propertyType = propertyInfo.PropertyType;
            try
            {
                var paramContext = new ParamContext(propertyInfo);
                foreach (var resolver in paramResolvers)
                {
                    if (!resolver.TryResolveParamType(propertyType, paramContext, consideringTypes,
                            out var param)) continue;
                    ParamRequirement = param;
                    foreach (var dependentType in param.Dependencies) Dependencies.Add(dependentType);
                    break;
                }

                if (ParamRequirement == null)
                    throw new RequirementException(
                        $"Resolving property {propertyInfo.Name} of {propertyInfo.DeclaringType!.Name} failed: type {propertyType.Name} can't be resolved");
            }
            catch (RequirementException e)
            {
                throw new RequirementException(
                    $"Resolving property {propertyInfo.Name} of {propertyInfo.DeclaringType!.Name} failed: type {propertyType.Name} failed : {e.Message}");
            }
        }

        public ISingletonParamRequirement ParamRequirement { get; }
        public PropertyInfo PropertyInfo { get; }
        public ISet<Type> Dependencies { get; } = new HashSet<Type>();

        public ISingletonDecorateSupply ToSupply(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new PropertyDecorateSupply(PropertyInfo, ParamRequirement.ToResponse(factoryInfos), Dependencies);
        }
    }

    public class PropertyDecorateSupply : ISingletonDecorateSupply
    {
        public PropertyDecorateSupply(PropertyInfo propertyInfo, ISingletonParamSupply paramSupply, ISet<Type> dependencies)
        {
            PropertyInfo = propertyInfo;
            ParamSupply = paramSupply;
            Dependencies = dependencies;
        }

        public PropertyInfo PropertyInfo { get; }
        public ISingletonParamSupply ParamSupply { get; }

        public ISet<Type> Dependencies { get; }

        public void Decorate(IMySingleton singleton)
        {
            PropertyInfo.SetValue(singleton, ParamSupply.Create());
        }
    }
}