/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-11
 */

using System;
using UnityEngine;

namespace Falcon.Shared.Common
{
    public static class Messenger<T>
    {
        private static Action<T> _handlers;

        public static void Register(Action<T> handler)
        {
            _handlers += handler;
        }

        public static void Unregister(Action<T> handler)
        {
            _handlers -= handler;
        }

        public static void Emit(T message = default)
        {
            try
            {
                _handlers?.Invoke(message);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}