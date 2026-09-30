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
    public class FactoryInfo
    {
        public FactoryInfo(ISingletonFactory factory, int dependencyOrder)
        {
            Factory = factory;
            DependencyOrder = dependencyOrder;
        }

        public ISingletonFactory Factory { get; }
        public ISet<Type> Dependencies => Factory.Dependencies;
        public int DependencyOrder { get;}
    }
}