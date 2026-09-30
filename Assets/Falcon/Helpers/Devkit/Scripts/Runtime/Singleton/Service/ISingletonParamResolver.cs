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
    public interface ISingletonParamResolver : ISingletonLogic
    {
        bool TryResolveParamType(Type type, ParamContext paramContext, ISet<Type> consideringTypes,
            out ISingletonParamRequirement paramRequirement);
    }

    public class DirectParamResolver : ISingletonParamResolver
    {
        public int Priority => 0;

        public bool TryResolveParamType(Type type, ParamContext paramContext, ISet<Type> consideringTypes,
            out ISingletonParamRequirement paramRequirement)
        {
            if (!typeof(IMySingleton).IsAssignableFrom(type))
            {
                paramRequirement = null;
                return false;
            }

            paramRequirement = new DirectParamRequirement(type, paramContext.nullable, consideringTypes);
            return true;
        }
    }

    public class ArrayParamResolver : ISingletonParamResolver
    {
        public int Priority => 10;

        public bool TryResolveParamType(Type type, ParamContext paramContext, ISet<Type> consideringTypes,
            out ISingletonParamRequirement paramRequirement)
        {
            if (!type.IsArray)
            {
                paramRequirement = null;
                return false;
            }

            paramRequirement = new ArrayParamRequirement(type.GetElementType(), paramContext.sortingOrder,
                consideringTypes);
            return true;
        }
    }


    public class DictionaryParamResolver : ISingletonParamResolver
    {
        public int Priority => 20;

        public bool TryResolveParamType(Type type, ParamContext paramContext, ISet<Type> consideringTypes,
            out ISingletonParamRequirement paramRequirement)
        {
            if (!type.IsGenericType || !typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                paramRequirement = null;
                return false;
            }

            var genericDef = type.GetGenericTypeDefinition();

            if (genericDef != typeof(IDictionary<,>))
            {
                paramRequirement = null;
                return false;
            }

            var keyType = type.GetGenericArguments()[0];
            var componentType = type.GetGenericArguments()[1];

            if (keyType != typeof(Type) || !typeof(IMySingleton).IsAssignableFrom(componentType))
            {
                paramRequirement = null;
                return false;
            }

            paramRequirement = new DictionaryParamRequirement(componentType, type, consideringTypes);
            return true;
        }
    }

    public class CollectionParamResolver : ISingletonParamResolver
    {
        public int Priority => 30;

        public bool TryResolveParamType(Type type, ParamContext paramContext, ISet<Type> consideringTypes,
            out ISingletonParamRequirement paramRequirement)
        {
            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (enumerableInterface == null)
            {
                paramRequirement = null;
                return false;
            }

            paramRequirement = new CollectionParamRequirement(
                enumerableInterface.GetGenericArguments()[0],
                type,
                paramContext.sortingOrder,
                consideringTypes
            );
            return true;
        }
    }
}