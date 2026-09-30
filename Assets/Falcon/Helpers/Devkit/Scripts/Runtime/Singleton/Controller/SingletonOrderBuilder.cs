/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class SingletonOrderBuilder
    {
        private readonly Dictionary<Type, NodeInfo> _nodes;

        public SingletonOrderBuilder()
        {
            ISet<ISingletonShell> shells = SingletonLogicSources.SearchLogic<ISingletonSource>()
                .SelectMany(source => source.GenerateShells()).ToHashSet();

            foreach (var editor in SingletonLogicSources.SearchLogic<ISingletonShellEditor>())
                editor.EditSingletonsShells(shells);

            var typeToShells = new Dictionary<Type, ISingletonShell>();
            foreach (var shell in shells)
            {
                if (typeToShells.TryGetValue(shell.GenerateType, out var occupyingShell))
                    throw new ArithmeticException(
                        $"Multiple sources generating shell of type {shell.GenerateType} : {shell.SourceType} and {occupyingShell.SourceType}");
                typeToShells[shell.GenerateType] = shell;
            }

            var supportingTypes = typeToShells.Keys.ToHashSet();
            _nodes = typeToShells.ToDictionary(entry => entry.Key,
                entry => new NodeInfo(entry.Value.BuildNode(supportingTypes,
                    SingletonLogicSources.SearchLogic<ISingletonParamResolver>().ToList())));
        }

        /// <summary>
        ///     Sắp xếp thứ tự khởi tạo các Type dựa trên phụ thuộc và kiểm tra deadlock.
        /// </summary>
        private void SortTasksAndCheckDeadlock()
        {
            Queue<NodeInfo> queue = new(_nodes.OrderBy(entry => entry.Value.Dependents.Count)
                .Select(entry => entry.Value));
            var resolvedTypes = new HashSet<Type>();
            while (queue.Count > 0)
            {
                var doneSmt = false;
                for (var queueIndex = 0; queueIndex < queue.Count; queueIndex++)
                {
                    var info = queue.Dequeue();
                    if (!resolvedTypes.IsSupersetOf(info.Dependents))
                    {
                        queue.Enqueue(info);
                        continue;
                    }

                    info.DependencyOrder = _nodes.Count - queue.Count;
                    resolvedTypes.Add(info.Node.GenerateType);
                    doneSmt = true;
                }

                if (!doneSmt) CheckDeadLock(new HashSet<NodeInfo>(queue));
            }
        }

        private void CheckDeadLock(ISet<NodeInfo> remaining)
        {
            foreach (var info in remaining) CheckDeadLockRecursion(info.Node.GenerateType, new List<Type>());
            SingletonLogger.Instance.Error("No deadlock found, wtf!!!");
        }

        private void CheckDeadLockRecursion(Type current, List<Type> tracks)
        {
            tracks.Add(current);
            var dependentTypes = _nodes[current].Dependents;
            foreach (var dependentType in dependentTypes)
                if (!tracks.Contains(dependentType))
                {
                    CheckDeadLockRecursion(dependentType, new List<Type>(tracks));
                }
                else
                {
                    tracks.Add(dependentType);
                    throw new SingletonException(
                        $"Singleton deadlock detected: {string.Join("->", tracks.Select(track => track.Name))}");
                }
        }

        public Dictionary<Type, FactoryInfo> Factories()
        {
            SortTasksAndCheckDeadlock();
            Dictionary<Type, FactoryInfo> factories = new();
            foreach (var nodeInfo in _nodes.Values.OrderBy(info => info.DependencyOrder).ToList())
            {
                var factory = nodeInfo.Node.BuildFactory(factories);
                factories[nodeInfo.Node.GenerateType] = new FactoryInfo(factory, nodeInfo.DependencyOrder);
            }

            return factories;
        }
    }
}