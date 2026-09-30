/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class MonoBehaviourExtensions
    {
        public static MonoBehaviour GetOrAddComponent(this GameObject gameObject, Type type)
        {
            return (MonoBehaviour)(gameObject.GetComponent(type) ?? gameObject.AddComponent(type));
        }

        public static MonoBehaviour GetOrAddComponent(this MonoBehaviour monoBehaviour, Type type)
        {
            return GetOrAddComponent(monoBehaviour.gameObject, type);
        }
        
        public static T GetOrAddComponent<T>(this GameObject gameObject)
        {
            return (gameObject.GetComponent(typeof(T)) ?? gameObject.AddComponent(typeof(T))).As<T>();
        }

        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour)
        {
            return GetOrAddComponent<T>(monoBehaviour.gameObject);
        }
    }
}