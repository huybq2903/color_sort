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
    public class NodeInfo
    {
        public NodeInfo(ISingletonNode node)
        {
            Node = node;
            Dependents = Node.Dependencies;
        }

        public ISingletonNode Node { get; }
        public ISet<Type> Dependents { get; }
        public int DependencyOrder { get; set; }
    }
}