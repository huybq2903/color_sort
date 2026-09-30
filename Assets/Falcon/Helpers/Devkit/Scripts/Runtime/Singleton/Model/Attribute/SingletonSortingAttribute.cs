/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */
using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class SingletonSortingAttribute : Attribute
    {
        public SortingOrder SortingOrder { get; set; }

        public SingletonSortingAttribute(SortingOrder sortingOrder)
        {
            SortingOrder = sortingOrder;
        }
    }

    public enum SortingOrder
    {
        CREATING, DESTROYING
    }
}