/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System.Reflection;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public struct ParamContext
    {
        public readonly bool nullable;
        public readonly SortingOrder? sortingOrder;

        public ParamContext(bool nullable = false, SortingOrder? sortingOrder = null)
        {
            this.nullable = nullable;
            this.sortingOrder = sortingOrder;
        }

        public ParamContext(ParameterInfo parameterInfo) :
            this(
                parameterInfo.IsDefined(typeof(CanBeNullAttribute), true),
                parameterInfo.GetCustomAttribute<SingletonSortingAttribute>()?.SortingOrder
            )
        {
        }
        
        public ParamContext(MemberInfo memberInfo) :
            this(
                memberInfo.IsDefined(typeof(CanBeNullAttribute), true),
                memberInfo.GetCustomAttribute<SingletonSortingAttribute>()?.SortingOrder
            )
        {
        }
    }
}