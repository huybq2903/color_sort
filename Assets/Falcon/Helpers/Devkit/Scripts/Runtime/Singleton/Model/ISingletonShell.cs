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
    public interface ISingletonShell
    {
        Type GenerateType { get; }
        Type SourceType { get; }
        ISingletonNode BuildNode(ISet<Type> supportingTypes, List<ISingletonParamResolver> paramResolvers);
    }

    public class MonoSingletonShell : ISingletonShell
    {
        public MonoSingletonShell(Type sourceType)
        {
            SourceType = sourceType;
        }

        public Type GenerateType => SourceType;
        public Type SourceType { get; }

        public ISingletonNode BuildNode(ISet<Type> supportingTypes, List<ISingletonParamResolver> paramResolvers)
        {
            return new MonoSingletonNode(SourceType,
                SingletonDecorators.Scan(SourceType, supportingTypes, paramResolvers).ToList());
        }
    }

    public class ConstructorSingletonShell : ISingletonShell
    {
        public ConstructorSingletonShell(Type sourceType)
        {
            SourceType = sourceType;
        }

        public Type GenerateType => SourceType;
        public Type SourceType { get; }

        public ISingletonNode BuildNode(ISet<Type> supportingTypes, List<ISingletonParamResolver> paramResolvers)
        {
            return new ConstructorSingletonNode(
                new ConstructorCreateRequirement(SourceType, supportingTypes, paramResolvers),
                SingletonDecorators.Scan(SourceType, supportingTypes, paramResolvers).ToList()
            );
        }
    }

    public class MethodCallSingletonShell : ISingletonShell
    {
        public MethodCallSingletonShell(MethodInfo methodInfo, Type sourceType)
        {
            MethodInfo = methodInfo;
            SourceType = sourceType;
            GenerateType = MethodInfo.ReturnType;
        }

        public MethodInfo MethodInfo { get; }
        public Type GenerateType { get; }
        public Type SourceType { get; }

        public ISingletonNode BuildNode(ISet<Type> supportingTypes, List<ISingletonParamResolver> paramResolvers)
        {
            return new MethodCallSingletonNode(SourceType, MethodInfo);
        }
    }
}