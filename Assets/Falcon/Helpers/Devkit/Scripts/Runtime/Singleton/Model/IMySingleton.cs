/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Falcon.Helpers.FReflection;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    
    [FReflection]
    public interface IMySingleton
    {
    }

    public abstract class MySingleton<T> : IMySingleton where T : MySingleton<T>
    {
        public static T Instance => MySingletonService.Instance<T>();
    }

    public abstract class MonoSingleton : MonoBehaviour, IMySingleton
    {
    }

    public abstract class MonoSingleton<T> : MonoSingleton where T : MonoSingleton<T>
    {
        public static T Instance => MySingletonService.Instance<T>();
    }

    public interface IPostConstruct : IMySingleton
    {
        void OnPostConstruct();
    }
}