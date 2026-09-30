/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-10
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.FReflection;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Falcon.Shared.BaseEvents
{
    public static class EventsRegister
    {
        private static Dictionary<string, Type> _dictUserData;
        private static Dictionary<string, Type> _dictConfig;

        private static List<Type> _listEventsLocal;

        private static void OnLogin(bool success)
        {
            if (success) new CSGetEvents().Send();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorBootstrap()
        {
            Register(GetTypesWrapperEventEditor());
        }

        private static IEnumerable<Type> GetTypesWrapperEventEditor()
        {
            var types = TypeCache.GetTypesDerivedFrom<IWrapperEvent>()
                .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true });
            return types;
        }
#endif
        public static Dictionary<string, Type> DictWrapper { get; private set; }

        public static Dictionary<string, string[]> DictWrapperNotifies { get; private set; }

        [RuntimeInitializeOnLoadMethod]
        private static async void Initialize()
        {
            WrapperTime.OnOverrideTime += RegisterEventResetTick;
            GameEvent<bool>.Register("falcon.modules.account.login", OnLogin, null);
            await Task.Yield();
            Register(GetTypesWrapperEvent());
            
#if UNITY_EDITOR
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            // Khởi tạo các wrapper có config cục bộ
            foreach (var wrapper in _listEventsLocal)
            {
                Center.GetOrCreate(wrapper);
            }
#endif
        }

        private static IEnumerable<Type> GetTypesWrapperEvent()
        {
            var wrapperType = typeof(IWrapperEvent);
            var types = FReflection.Instance.GetTypes()
                .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } && t.GetInterfaces().Contains(wrapperType));
            return types;
        }

        private static void Register(IEnumerable<Type> types)
        {
            _dictUserData = new Dictionary<string, Type>();
            _dictConfig = new Dictionary<string, Type>();
            DictWrapper = new Dictionary<string, Type>();
            DictWrapperNotifies = new Dictionary<string, string[]>();
            _listEventsLocal = new List<Type>();

            var allTypes = FReflection.Instance.GetTypes();

            foreach (var type in types)
            {
                if (type.BaseType == null) continue;

                var overrideType = allTypes.FirstOrDefault(t => t != type && type.IsAssignableFrom(t));
                var genericArgs = type.BaseType.GetGenericArguments();
                var configType = genericArgs[0];
                var userDataType = genericArgs[1];

                if (Activator.CreateInstance(type) is not IWrapperEvent instance) continue;
                var key = instance.Key;
#if UNITY_EDITOR
                if (instance.LocalConfig is { fakeDurationEvent: > 0 })
                {
                    _listEventsLocal.Add(type);
                }
#endif
                if (!string.IsNullOrEmpty(key))
                {
                    _dictUserData.TryAdd(key, userDataType);
                    _dictConfig.TryAdd(key, configType);
                    DictWrapper.TryAdd(key, overrideType ?? type);
                    DictWrapperNotifies.Add(key, instance.Notifies.Keys.ToArray());
                }
            }
        }

        internal static Type GetType(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            DictWrapper.TryGetValue(key, out var type);
            return type;
        }

        internal static (Type typeConfig, Type typeData) GetConfigAndDataType(string key)
        {
            _dictConfig.TryGetValue(key, out var typeConfig);
            _dictUserData.TryGetValue(key, out var typeData);
            return (typeConfig, typeData);
        }
        
        private const int RESET_HOUR_UTC = 8;
        private const string EVENT_RESET_KEY = "EventsNextReset";

        private static long GetNextEventResetSecond()
        {
            var current = WrapperTime.CurrentSecond;
            var currentDay = current / WrapperTime.SECOND_ONE_DAY;
            var todayReset = currentDay * WrapperTime.SECOND_ONE_DAY + RESET_HOUR_UTC * 3600;
            return current < todayReset
                ? todayReset
                : (currentDay + 1) * WrapperTime.SECOND_ONE_DAY + RESET_HOUR_UTC * 3600;
        }

        private static void RegisterEventResetTick()
        {
            WrapperTime.AddTick(EVENT_RESET_KEY, GetNextEventResetSecond(), null, () =>
            {
                new CSGetEvents().SendAfter(5f);
                RegisterEventResetTick();
            });
        }
    }
}