/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-29


using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.FReflection
{
    public class FReflection
    {
        //Tạo singleton 
        private static FReflection _instance;
        private readonly Dictionary<string, Type> _name2Type = new();
        private readonly Dictionary<string, Type> _simpleName2Type = new();

        private readonly HashSet<Type> _types = new();

        private FReflection()
        {
        }

        public static FReflection Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new FReflection();
                _instance.ScanAssemblies();
                return _instance;
            }
        }


        private void ScanAssemblies()
        {
            var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                         where assembly.GetName().FullName.Contains("Falcon") || assembly.GetName().Name == "Assembly-CSharp"
                         from type in assembly.GetTypes()
                         select type).ToList();

            var baseTypes = types.Where(t => t.IsDefined(typeof(FReflectionAttribute), true)).ToList();
            baseTypes.Add(typeof(IFReflection));
            _types.AddRange(baseTypes);

            foreach (var type in types)
                if (baseTypes.Any(baseType => baseType.IsAssignableFrom(type)))
                    AssignType(type);

        }

        private void AssignType(Type type)
        {
            _types.Add(type);
            _simpleName2Type.TryAdd(type.Name, type);
            _name2Type.TryAdd(type.FullName, type);
        }

        public Type[] GetTypes()
        {
            return _types.ToArray();
        }

        public Type GetTypeBySimpleName(string simpleName)
        {
            return _simpleName2Type.GetValueOrDefault(simpleName);
        }

        public Type GetTypeByFullName(string name)
        {
            return _name2Type.GetValueOrDefault(name);
        }

        public Type[] GetTypes(Type baseType) => _types.Where(type => baseType.IsAssignableFrom(type)).ToArray();
    }
}