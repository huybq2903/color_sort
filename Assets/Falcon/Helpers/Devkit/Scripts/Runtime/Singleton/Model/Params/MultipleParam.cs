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
    public abstract class MultipleParamRequirement : ISingletonParamRequirement
    {
        protected readonly Type _componentType;

        protected MultipleParamRequirement(Type componentType, ISet<Type> consideringTypes)
        {
            _componentType = componentType;
            Dependencies = consideringTypes.Where(componentType.IsAssignableFrom).ToHashSet();
        }

        public ISet<Type> Dependencies { get; }
        public abstract ISingletonParamSupply ToResponse(Dictionary<Type, FactoryInfo> factoryInfos);

        public IEnumerable<FactoryInfo> Scan(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return Dependencies.Select(type => factoryInfos[type]);
        }
    }

    public class ArrayParamRequirement : MultipleParamRequirement
    {
        private readonly SortingOrder? _sortingOrder;

        public ArrayParamRequirement(Type componentType, SortingOrder? sortingOrder, ISet<Type> consideringTypes) : base(
            componentType, consideringTypes)
        {
            _sortingOrder = sortingOrder;
        }

        public override ISingletonParamSupply ToResponse(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new ArrayParamSupply(Scan(factoryInfos).ToArray(), _componentType, _sortingOrder, Dependencies);
        }
    }

    public class ArrayParamSupply : ISingletonParamSupply
    {
        private readonly FactoryInfo[] _factoryInfos;
        private readonly Type _componentType;

        public ArrayParamSupply(FactoryInfo[] factoryInfos, Type componentType, SortingOrder? sortingOrder,
            ISet<Type> dependencies)
        {
            _factoryInfos = factoryInfos;
            _componentType = componentType;
            Dependencies = dependencies;
            switch (sortingOrder)
            {
                case SortingOrder.CREATING:
                    Array.Sort(_factoryInfos, (i1, i2) => i1.DependencyOrder.CompareTo(i2.DependencyOrder));
                    break;
                case SortingOrder.DESTROYING:
                    Array.Sort(_factoryInfos, (i1, i2) => -i1.DependencyOrder.CompareTo(i2.DependencyOrder));
                    break;
            }
        }

        public ISet<Type> Dependencies { get; }

        public object Create()
        {
            var array = Array.CreateInstance(_componentType, _factoryInfos.Length);
            for (var i = 0; i < _factoryInfos.Length; i++) array.SetValue(_factoryInfos[i].Factory.Instance, i);

            return array;
        }
    }

    public class DictionaryParamRequirement : MultipleParamRequirement
    {
        private readonly Type _dictionaryType;

        public DictionaryParamRequirement(Type componentType, Type dictionaryType, ISet<Type> consideringTypes) : base(
            componentType, consideringTypes)
        {
            _dictionaryType = dictionaryType;
        }

        public override ISingletonParamSupply ToResponse(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new DictionaryParamSupply(_componentType, _dictionaryType, Scan(factoryInfos), Dependencies);
        }
    }

    public class DictionaryParamSupply : ISingletonParamSupply
    {
        private readonly Type _componentType;
        private readonly IEnumerable<FactoryInfo> _factoryInfos;
        private readonly Type _dictionaryType;

        public DictionaryParamSupply(Type componentType, Type dictionaryType, IEnumerable<FactoryInfo> factoryInfos,
            ISet<Type> dependencies)
        {
            _componentType = componentType;
            _dictionaryType = dictionaryType;
            _factoryInfos = factoryInfos;
            Dependencies = dependencies;
        }

        public ISet<Type> Dependencies { get; }

        public object Create()
        {
            var collection = Activator.CreateInstance(_dictionaryType);
            var methodInfo = _dictionaryType.GetMethod("Put", new[] { typeof(Type), _componentType });
            if (methodInfo == null)
                throw new SingletonException(
                    $"Can't find method Put(Type, {_componentType.Name}) on type {_dictionaryType.Name}");
            foreach (var factoryInfo in _factoryInfos)
                methodInfo.Invoke(collection,
                    new object[] { factoryInfo.Factory.InstanceType, factoryInfo.Factory.Instance });

            return collection;
        }
    }

    public class CollectionParamRequirement : MultipleParamRequirement
    {
        private readonly Type _collectionType;
        private readonly SortingOrder? _sortingOrder;

        public CollectionParamRequirement(Type componentType, Type collectionType, SortingOrder? sortingOrder,
            ISet<Type> consideringTypes) : base(componentType, consideringTypes)
        {
            _collectionType = collectionType;
            _sortingOrder = sortingOrder;
        }

        public override ISingletonParamSupply ToResponse(Dictionary<Type, FactoryInfo> factoryInfos)
        {
            return new CollectionParamSupply(_componentType, _collectionType, Scan(factoryInfos), _sortingOrder,
                Dependencies);
        }
    }

    public class CollectionParamSupply : ISingletonParamSupply
    {
        private readonly Type _componentType;
        private readonly IEnumerable<FactoryInfo> _factoryInfos;
        private readonly Type _collectionType;

        public CollectionParamSupply(Type componentType, Type collectionType, IEnumerable<FactoryInfo> factoryInfos,
            SortingOrder? sortingOrder, ISet<Type> dependencies)
        {
            _componentType = componentType;
            _collectionType = collectionType;

            switch (sortingOrder)
            {
                case SortingOrder.CREATING:
                    factoryInfos = factoryInfos.OrderBy(info => info.DependencyOrder);
                    break;
                case SortingOrder.DESTROYING:
                    factoryInfos = factoryInfos.OrderByDescending(info => info.DependencyOrder);
                    break;
            }

            _factoryInfos = factoryInfos;
            Dependencies = dependencies;
        }


        public ISet<Type> Dependencies { get; }

        public object Create()
        {
            var collection = Activator.CreateInstance(_collectionType);
            var methodInfo = _collectionType.GetMethod("Add", new[] { _componentType });
            if (methodInfo == null)
                throw new SingletonException(
                    $"Can't find method Add({_componentType.Name}) on type {_collectionType.Name}");
            foreach (var factoryInfo in _factoryInfos)
                methodInfo.Invoke(collection, new object[] { factoryInfo.Factory.Instance });

            return collection;
        }
    }
}