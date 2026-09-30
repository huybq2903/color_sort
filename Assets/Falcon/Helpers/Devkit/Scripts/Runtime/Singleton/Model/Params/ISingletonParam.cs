/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonParamRequirement
    {
        ISet<Type> Dependencies { get; }
        ISingletonParamSupply ToResponse(Dictionary<Type, FactoryInfo> factoryInfos);
    }

    public interface ISingletonParamSupply
    {
        ISet<Type> Dependencies { get; }
        object Create();
    }
}