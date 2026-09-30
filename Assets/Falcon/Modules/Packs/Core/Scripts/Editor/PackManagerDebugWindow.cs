using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Packs.Core.Runtime;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Editor
{
    public class PackManagerDebugWindow : OdinEditorWindow
    {
        [MenuItem("Falcon/Modules/Pack/Manager Debug")]
        private static void OpenWindow()
        {
            GetWindow<PackManagerDebugWindow>("Pack Manager Debug").Show();
        }
                
        [PropertyOrder(0)]
        [Button]
        private void Refresh()
        {
            _registeredPacks = GetRegisteredPacks();
            OnSelectPackChanged(); // reload dữ liệu nếu đang chọn pack
        }

        private Dictionary<Type, IWrapperPack> _registeredPacks;
        private IWrapperPack _selectedPack;

        [PropertyOrder(1)]
        [ValueDropdown(nameof(GetPackChoices))]
        [LabelText("Chọn pack")]
        [OnValueChanged(nameof(OnSelectPackChanged))]
        public Type selectedType;

        [PropertyOrder(2)]
        [ShowInInspector, ReadOnly, InlineProperty, HideReferenceObjectPicker]
        [ShowIf(nameof(HasSelectAndApplicationPlaying))]
        private PacksConfig _config;

        [PropertyOrder(3)]
        [ShowInInspector, InlineProperty, HideReferenceObjectPicker]
        [ShowIf(nameof(IsUserDataValid))]
        private IPackUserData _userData;

        [PropertyOrder(4)]
        [ShowInInspector, ReadOnly, LabelText("UserData")]
        [ShowIf(nameof(IsUserDataInvalid))]
        private string NullUserDataView => "<null>";

        [PropertyOrder(5)]
        [Button]
        [ShowIf(nameof(HasSelected))]
        private void SaveAndSend()
        {
            if (_selectedPack == null) return;

            if (Application.isPlaying)
            {
                var configProp = _selectedPack.GetType().GetProperty("Config");
                var userDataProp = _selectedPack.GetType().GetProperty("UserData");

                configProp?.SetValue(_selectedPack, _config);
                userDataProp?.SetValue(_selectedPack, _userData);

                var saveMethod = _selectedPack.GetType().GetMethod("AddSequenceAndSave", BindingFlags.Instance | BindingFlags.NonPublic);
                saveMethod?.Invoke(_selectedPack, null);

                var sendMethod = _selectedPack.GetType().GetMethod("SendToServer", BindingFlags.Instance | BindingFlags.NonPublic);
                sendMethod?.Invoke(_selectedPack, null);

                Debug.Log($"[Save & Send] {_selectedPack.Key}");
            }
            else
            {
                var key = _selectedPack.Key;
                if (_userData is ABasePackUserData abpu)
                    abpu.sequence++;
                SaveLoadHandler.Save(key + "_user_data", JsonConvert.SerializeObject(_userData));
                Debug.Log($"[Editor Save] UserData for {key} saved.");
            }
        }

        // ================= Internal =================

        private bool HasSelected() => _selectedPack != null;
        private bool IsUserDataValid() => _userData != null && _userData.GetType().Name != nameof(NullPackUserData);
        private bool IsUserDataInvalid() => !IsUserDataValid();
        
        private bool HasSelectAndApplicationPlaying => HasSelected() && Application.isPlaying;
        
        private void OnSelectPackChanged()
        {
            UnregisterUserDataListener();
            UpdateData();
            RegisterUserDataListener();
        }

        private void UpdateData()
        {
            if (selectedType != null && _registeredPacks != null && _registeredPacks.TryGetValue(selectedType, out var pack))
            {
                _selectedPack = pack;

                var configProp = selectedType.GetProperty("Config");
                var userDataProp = selectedType.GetProperty("UserData");

                _config = configProp?.GetValue(_selectedPack) as PacksConfig;
                _userData = userDataProp?.GetValue(_selectedPack) as IPackUserData;
            }
            else
            {
                _selectedPack = null;
                _config = null;
                _userData = null;
            }
            Repaint();
        }

        private IEnumerable<ValueDropdownItem<Type>> GetPackChoices()
        {
            if (_registeredPacks == null || _registeredPacks.Count == 0)
            {
                _registeredPacks = GetRegisteredPacks();
            }

            return _registeredPacks.Keys
                .Where(k => k != null)
                .Select(k => new ValueDropdownItem<Type>(k.Name, k));
        }

        private Dictionary<Type, IWrapperPack> GetRegisteredPacks()
        {
            if (Application.isPlaying)
            {
                var field = typeof(PacksManager).GetField("_dic", BindingFlags.Static | BindingFlags.NonPublic);
                return field?.GetValue(null) as Dictionary<Type, IWrapperPack> ?? new Dictionary<Type, IWrapperPack>();
            }

            var result = new Dictionary<Type, IWrapperPack>();

            foreach (var type in GetAllWrapperPackTypes())
            {
                if (type.IsAbstract || !typeof(IWrapperPack).IsAssignableFrom(type))
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is not IWrapperPack instance)
                {
                    continue;
                }

                var keyProp = type.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var key = keyProp?.GetValue(instance) as string;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var hasConfig = SaveLoadHandler.ExistsKey(key + "_config") || instance.LocalConfig;
                var hasUserData = SaveLoadHandler.ExistsKey(key + "_user_data");

                if (!hasConfig)
                {
                    continue;
                }
                
                var userDataProp = type.GetProperty("UserData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (hasUserData && userDataProp != null)
                {
                    var json = SaveLoadHandler.Load<string>(key + "_user_data");
                    var userDataType = userDataProp.PropertyType;
                    var userDataObj = JsonUtility.FromJson(json, userDataType);
                    userDataProp.SetValue(instance, userDataObj);
                }

                result[type] = instance;
            }

            return result;
        }

        private List<Type> GetAllWrapperPackTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.BaseType is { IsGenericType: true } &&
                    t.GetInterfaces().Contains(typeof(IWrapperPack))
                )
                .ToList();
        }

        private Delegate _userDataListener;

        private void RegisterUserDataListener()
        {
            if (_userData == null) return;

            var userDataType = _userData.GetType();
            var method = GetType().GetMethod(nameof(GenericOnUserDataChanged), BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                var genericMethod = method.MakeGenericMethod(userDataType);
                _userDataListener = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(userDataType), this, genericMethod);
                
                // Register the listener - specify parameter types to avoid ambiguity
                var actionType = typeof(Action<>).MakeGenericType(userDataType);
                var registerMethod = typeof(GameEvent<>)
                    .MakeGenericType(userDataType)
                    .GetMethod("Register", BindingFlags.Public | BindingFlags.Static, null, 
                        new[] { typeof(string), actionType, typeof(Component) }, null);
                registerMethod?.Invoke(null, new object[] { PacksConstant.EVENT_CHANGE_DATA, _userDataListener, null });
            }
        }
        
        private void UnregisterUserDataListener()
        {
            if (_userData == null || _userDataListener == null) return;

            var userDataType = _userData.GetType();
            var actionType = typeof(Action<>).MakeGenericType(userDataType);
            var unregisterMethod = typeof(GameEvent<>)
                .MakeGenericType(userDataType)
                .GetMethod("Unregister", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(string), actionType, typeof(Component) }, null);

            unregisterMethod?.Invoke(null, new object[] { PacksConstant.EVENT_CHANGE_DATA, _userDataListener, null });
            _userDataListener = null;
        }
        
        private void GenericOnUserDataChanged<T>(T data) => UpdateData();

        [OnInspectorInit]
        private void AutoRefreshOdin()
        {
            Refresh();
        }
    }
}