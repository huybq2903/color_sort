/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-09
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.FReflection;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Quản lý danh sách đăng ký của tất cả các instance IWrapperPack trong dự án.
    /// Cung cấp các phương thức để tạo, truy xuất và xóa wrapper pack theo loại,
    /// cũng như liệt kê và quản lý vòng đời của chúng.
    /// 
    /// Sử dụng PacksManager để truy cập hoặc khởi tạo bất kỳ wrapper nhóm pack nào trong game.
    ///
    /// Đăng ký các nhóm pack trong toàn hệ thống.
    /// Quét toàn bộ assembly để lấy ra các WrapperPack hợp lệ và ánh xạ chúng với idGroup tương ứng.
    /// Từ đó có thể lấy ra đúng kiểu dữ liệu Config, UserData và Wrapper.
    /// </summary>
    public static class PacksManager
    {
        private static readonly Dictionary<Type, IWrapperPack> _dic = new();

        /// <summary>
        /// Custom cho phép module khác sửa hàm Initialize
        /// Gán ở method trước khi AfterSceneLoad
        /// </summary>
        public static Action onInitialize = () =>
        {
            GameEvent<bool>.Register(PacksConstant.EVENT_LOGIN, OnLogin, null);
        };
        
        private static void OnLogin(bool success)
        {
            if (success) new CSGetAllPacksData().Send();
        }
        
        /// <summary>
        /// Lấy wrapper pack của loại được chỉ định, hoặc null nếu không tìm thấy.
        /// </summary>
        /// <typeparam name="T">Loại của wrapper pack.</typeparam>
        /// <returns>Instance của wrapper pack, hoặc null nếu chưa đăng ký.</returns>
        public static T Get<T>() where T : class, IWrapperPack
        {
            var t = _dic.ContainsKey(typeof(T)) ? _dic[typeof(T)] as T : null;
            return t;
        }
        
        /// <summary>
        /// Lấy wrapper pack hiện có theo Type, hoặc tạo và khởi tạo nó nếu chưa tồn tại.
        /// </summary>
        /// <param name="type">Loại của wrapper pack.</param>
        /// <returns>Instance của wrapper pack.</returns>
        public static IWrapperPack GetOrCreate(Type type)
        {
            if (!typeof(IWrapperPack).IsAssignableFrom(type))
                throw new ArgumentException("Type must implement IWrapperPack", nameof(type));

            IWrapperPack instance;
            if (_dic.TryGetValue(type, out var wrapper))
            {
                instance = wrapper;
            }
            else
            {
                instance = Activator.CreateInstance(type) as IWrapperPack;
                if (instance == null) return null;
                
                var key = instance.Key;
                var overrideType = _dictWrapper[key];
                if (overrideType != type)
                {
                    instance = Activator.CreateInstance(overrideType) as IWrapperPack;
                }
                if (instance == null) return null;
                
                _dic.Add(type, instance);
                instance.OnInitialize();
            }
            return instance;
        }

        /// <summary>
        /// Xóa wrapper pack đã đăng ký của loại T khỏi danh sách đăng ký.
        /// </summary>
        /// <typeparam name="T">Loại của wrapper pack cần xóa.</typeparam>
        public static void Remove<T>() where T : class, IWrapperPack
        {
            if (_dic.ContainsKey(typeof(T)))
                _dic.Remove(typeof(T));
        }

        /// <summary>
        /// Lấy gói config từ module khác
        /// </summary>
        /// <param name="idPack"></param>
        /// <returns></returns>
        public static ABaseElementPackConfig GetConfigCommon(string idPack)
        {
            return Get<WrapperPacksCommon>().DictConfigs.GetValueOrDefault(idPack);
        }
        
        private static Dictionary<string, Type> _dictUserData;
        private static Dictionary<string, Type> _dictConfig;
        private static Dictionary<string, Type> _dictWrapper;

        private static List<Type> _listWrapperHadLocalConfig;

        /// <summary>
        /// Được gọi tự động khi khởi động game (trước khi tải scene).
        /// Kết nối với AccountManager để tự động gửi request dữ liệu khi login thành công.
        /// Đăng ký tất cả các loại nhóm pack và khởi tạo các instance wrapper có asset cấu hình cục bộ.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static async void Initialize()
        {
            try
            {
                await Task.Yield();
                onInitialize?.Invoke();
                BuildAllDictionaries();

                // Khởi tạo các wrapper có config cục bộ
                foreach (var wrapper in _listWrapperHadLocalConfig)
                {
                    try
                    {
                        GetOrCreate(wrapper);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Tìm và ánh xạ tất cả các wrapper pack hợp lệ từ assembly vào từ điển nội bộ.
        /// </summary>
        private static void BuildAllDictionaries()
        {
            _dictUserData = new Dictionary<string, Type>();
            _dictConfig = new Dictionary<string, Type>();
            _dictWrapper = new Dictionary<string, Type>();
            _listWrapperHadLocalConfig = new List<Type>();

            var wrapperType = typeof(IWrapperPack);

            var types = FReflection.Instance.GetTypes()
                .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } && t.GetInterfaces().Contains(wrapperType));

            foreach (var type in types)
            {
                if (type.BaseType == null) continue;
                
                var overrideType = FReflection.Instance.GetTypes().FirstOrDefault(t => t != type && type.IsAssignableFrom(t));
                var genericArgs = type.BaseType.GetGenericArguments();
                var configType = genericArgs[0];
                var userDataType = genericArgs[1];

                if (Activator.CreateInstance(type) is not IWrapperPack instance) continue;
                
                var key = instance.Key;
                if (instance.LocalConfig)
                {
                    _listWrapperHadLocalConfig.Add(type);
                }

                if (!string.IsNullOrEmpty(key))
                {
                    _dictUserData.TryAdd(key, userDataType);
                    _dictConfig.TryAdd(key, configType);
                    _dictWrapper.TryAdd(key, overrideType ?? type);
                }
            }
        }

        /// <summary>
        /// Trả về kiểu dữ liệu người dùng tương ứng với key nhóm pack (idGroup).
        /// </summary>
        internal static Type GetUserDataType(string key)
        {
            _dictUserData.TryGetValue(key, out var type);
            return type;
        }

        /// <summary>
        /// Trả về kiểu config tương ứng với key nhóm pack (idGroup).
        /// </summary>
        internal static Type GetConfigType(string key)
        {
            _dictConfig.TryGetValue(key, out var type);
            return type;
        }

        /// <summary>
        /// Trả về kiểu wrapper tương ứng với key nhóm pack (idGroup).
        /// </summary>
        internal static Type GetWrapperType(string key)
        {
            _dictWrapper.TryGetValue(key, out var type);
            return type;
        }
    }
}