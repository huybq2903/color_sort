/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-09


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.FReflection;

namespace Falcon.Modules.Core.AccountData
{
    public class FGameDataRegistry
    {
        private static FGameDataRegistry _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, Type> _map = new();

        private FGameDataRegistry()
        {
        }

        public static FGameDataRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FGameDataRegistry();
                            _instance.Init();
                        }
                    }
                }

                return _instance;
            }
        }

        public void Register(string typeName, Type type) =>
            _map[typeName] = type;

        public Type Get(string typeName) =>
            _map.TryGetValue(typeName, out var type) ? type : null;

        public List<string> GetAllTypeNames() =>
            _map.Keys.ToList();

        public List<Type> GetAllTypes() =>
            _map.Values.ToList();

        public FGameData GetGameDataInstance(string typeName)
        {
            Type type = Get(typeName);
            FGameData fGameData = (FGameData)Activator.CreateInstance(type);
            fGameData.__type = typeName;
            return fGameData;
        }

        private void Init()
        {
            var gameDataTypes = FReflection.Instance.GetTypes()
                .Where(t => typeof(FGameData).IsAssignableFrom(t) && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<FGameDataTypeAttribute>() != null);

            foreach (var type in gameDataTypes)
            {
                var attr = type.GetCustomAttribute<FGameDataTypeAttribute>();
                if (attr == null || attr.Skip)
                    continue;
                Register(attr.TypeName, type);
            }
        }
    }
}