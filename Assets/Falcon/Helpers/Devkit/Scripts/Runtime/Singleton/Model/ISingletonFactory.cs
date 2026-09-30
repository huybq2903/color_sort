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
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISingletonFactory
    {
        ISet<Type> Dependencies { get; }
        IMySingleton Instance { get; }
        Type InstanceType { get; }
    }

    public abstract class SelfCreateSingletonFactory : ISingletonFactory
    {
        private readonly Lazy<IMySingleton> _lazy;

        protected SelfCreateSingletonFactory(ISingletonCreateSupply createSupply,
            List<ISingletonDecorateSupply> decorateSupplies)
        {
            CreateSupply = createSupply;
            DecorateSupplies = decorateSupplies;
            Dependencies = new HashSet<Type>(createSupply.Dependencies);
            Dependencies.UnionWith(decorateSupplies.SelectMany(decorate => decorate.Dependencies).ToHashSet());
            _lazy = new Lazy<IMySingleton>(() =>
            {
                var singleton = CreateSupply.Create();
                if(singleton.GetType().Name == "AppFlowService") Debug.Log("Here");
                foreach (var decorateSupply in DecorateSupplies) decorateSupply.Decorate(singleton);

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (singleton is IPostConstruct postInjectSingleton) postInjectSingleton.OnPostConstruct();

                return singleton;
            });

            if(InstanceType.Name == "AppFlowService") Debug.Log("Here");
            if (InstanceType.GetCustomAttribute<NoLazyAttribute>() != null) _ = _lazy.Value;
        }

        public ISingletonCreateSupply CreateSupply { get; }
        public List<ISingletonDecorateSupply> DecorateSupplies { get; }
        public ISet<Type> Dependencies { get; }
        public IMySingleton Instance => _lazy.Value;
        public Type InstanceType => CreateSupply.InstanceType;
    }

    public class ConstructorSingletonFactory : SelfCreateSingletonFactory
    {
        public ConstructorSingletonFactory(ConstructorCreateSupply createSupply,
            List<ISingletonDecorateSupply> decorateSupplies) : base(createSupply, decorateSupplies)
        {
        }
    }

    public class MonoSingletonFactory : SelfCreateSingletonFactory
    {
        public MonoSingletonFactory(MonoCreateSupply createSupply, List<ISingletonDecorateSupply> decorateSupplies) :
            base(createSupply, decorateSupplies)
        {
        }
        
        public MonoSingletonFactory(Type monoType, List<ISingletonDecorateSupply> decorateSupplies) :
            this(new MonoCreateSupply(monoType), decorateSupplies)
        {
        }
    }

    public class MethodCallSingletonFactory : ISingletonFactory
    {
        private readonly Lazy<IMySingleton> _lazy;

        public MethodCallSingletonFactory(ISingletonFactory baseFactory, MethodInfo singletonMethod)
        {
            BaseFactory = baseFactory;
            SingletonMethod = singletonMethod;
            _lazy = new Lazy<IMySingleton>(() =>
            {
                var mySingleton = singletonMethod.Invoke(BaseFactory.Instance, null).As<IMySingleton>();
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (mySingleton is IPostConstruct postInjectSingleton) postInjectSingleton.OnPostConstruct();
                return mySingleton;
            });
            if (SingletonMethod.IsDefined(typeof(NoLazyAttribute), true)) _ = _lazy.Value;
        }

        public ISingletonFactory BaseFactory { get; }

        public MethodInfo SingletonMethod { get; }
        public ISet<Type> Dependencies => BaseFactory.Dependencies;
        public IMySingleton Instance => _lazy.Value;
        public Type InstanceType => SingletonMethod.ReturnType;
    }
}