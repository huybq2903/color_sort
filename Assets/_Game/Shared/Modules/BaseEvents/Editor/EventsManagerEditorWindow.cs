/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-14
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace Falcon.Shared.BaseEvents
{
    public class EventsManagerEditorWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Events/Manager Editor")]
        private static void OpenWindow()
        {
            GetWindow<EventsManagerEditorWindow>("Events Manager Editor").Show();
        }
        
        private Dictionary<Type, IWrapperEvent> _registeredEvents;
        private IWrapperEvent _selectedEvent;
        private readonly Dictionary<string, object[]> _buttonArguments = new();
        
        // [Button]
        private void Refresh()
        {
            _registeredEvents = GetRegisteredEvents();
            OnSelectEventsChanged();
        }
        
        [LabelText("Chọn event")]
        [ValueDropdown(nameof(GetListEventsCanShow))]
        [OnValueChanged(nameof(OnSelectEventsChanged))]
        public Type selectedType;
        
        [ShowInInspector, ReadOnly, InlineProperty, HideReferenceObjectPicker]
        private ABaseEventConfig _config;
        
        [ShowInInspector, InlineProperty, HideReferenceObjectPicker]
        private ABaseEventUserData _userData;
        
        private void OnSelectEventsChanged()
        {
            UnregisterUserDataListener();
            _buttonArguments.Clear();
            UpdateData();
            RegisterUserDataListener();
        }

        private void UpdateData()
        {
            if (selectedType != null && _registeredEvents != null && _registeredEvents.TryGetValue(selectedType, out var @event))
            {
                _selectedEvent = @event;

                var configProp = selectedType.GetProperty("Config");
                var userDataProp = selectedType.GetProperty("UserData");

                _config = configProp?.GetValue(_selectedEvent) as ABaseEventConfig;
                _userData = userDataProp?.GetValue(_selectedEvent) as ABaseEventUserData;
            }
            else
            {
                _selectedEvent = null;
                _config = null;
                _userData = null;
            }
            Repaint();
        }

        private IEnumerable<ValueDropdownItem<Type>> GetListEventsCanShow()
        {
            if (_registeredEvents == null || _registeredEvents.Count == 0)
            {
                _registeredEvents = GetRegisteredEvents();
            }

            return _registeredEvents.Select(k => new ValueDropdownItem<Type>(k.Value.Key, k.Key));
        }

        private Dictionary<Type, IWrapperEvent> GetRegisteredEvents()
        {
            var result = new Dictionary<Type, IWrapperEvent>();
            
            if (Application.isPlaying)
            {
                var allEvents = Center.All<IWrapperEvent>();
                foreach (var @event in allEvents)
                {
                    result[@event.GetType()] = @event;
                }

                return result;
            }
            
            var settings = CreateJsonSettings();
            
            foreach (var type in EventsRegister.DictWrapper.Values)
            {
                if (Activator.CreateInstance(type) is not IWrapperEvent instance)
                    continue;

                var keyProp = type.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var key = keyProp?.GetValue(instance) as string;
                if (string.IsNullOrEmpty(key))
                    continue;
                
                var userDataProp = type.GetProperty("UserData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var configProp = type.GetProperty("Config", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (userDataProp != null)
                {
                    var json = SaveLoadHandler.Load<string>(key + "_user_data");
                    var userDataObj = string.IsNullOrEmpty(json)
                        ? Activator.CreateInstance(userDataProp.PropertyType)
                        : JsonConvert.DeserializeObject(json, userDataProp.PropertyType, settings);
                    GetPrivateSetter(userDataProp)?.Invoke(instance, new[] { userDataObj });
                }

                if (configProp != null)
                {
                    var json = SaveLoadHandler.Load<string>(key + "_config");
                    var configObj = string.IsNullOrEmpty(json)
                        ? instance.LocalConfig
                        : JsonConvert.DeserializeObject(json, configProp.PropertyType, settings);
                    GetPrivateSetter(configProp)?.Invoke(instance, new object[] { configObj });
                }

                result[type] = instance;
            }

            return result;
        }

        // Setter private khai báo ở base: PropertyInfo lấy qua type dẫn xuất không thấy accessor này.
        private static MethodInfo GetPrivateSetter(PropertyInfo prop)
        {
            return prop.DeclaringType
                ?.GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetSetMethod(true);
        }

        private Delegate _userDataListener;

        private void RegisterUserDataListener()
        {
            if (_userData == null || _selectedEvent == null) return;

            var key = _selectedEvent.Key;
            _userDataListener = new Action<object>(GenericOnUserDataChanged);
            GameEvent<object>.Register(key, (Action<object>)_userDataListener, null);
        }
        
        private void UnregisterUserDataListener()
        {
            if (_userData == null || _userDataListener == null || _selectedEvent == null) return;

            var key = _selectedEvent.Key;
            GameEvent<object>.Unregister(key, (Action<object>)_userDataListener, null);
            _userDataListener = null;
        }
        
        private void GenericOnUserDataChanged(object data) => UpdateData();

        [OnInspectorInit]
        private void AutoRefreshOdin()
        {
            Refresh();
        }

        private void OnFocus()
        {
            Refresh();
        }

        [OnInspectorGUI]
        private void DrawSelectedEventOdinButtons()
        {
            if (_selectedEvent == null || !Application.isPlaying) return;

            var methods = GetSelectedEventButtonMethods(_selectedEvent.GetType());
            if (methods.Count == 0) return;

            EditorGUILayout.Space(8);

            foreach (var method in methods)
            {
                var buttonAttr = method.GetCustomAttribute<ButtonAttribute>(true);
                var label = string.IsNullOrEmpty(buttonAttr?.Name)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : buttonAttr.Name;
                var parameters = method.GetParameters();
                var canInvoke = true;
                var args = GetOrCreateButtonArguments(method);

                if (parameters.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (TryDrawParameterField(parameters[i], args[i], out var updated))
                        {
                            args[i] = updated;
                            continue;
                        }

                        canInvoke = false;
                        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(parameters[i].Name), $"Unsupported: {parameters[i].ParameterType.Name}");
                    }

                    if (!canInvoke)
                    {
                        EditorGUILayout.HelpBox("Method has unsupported parameter type(s).", MessageType.Warning);
                    }
                    EditorGUILayout.EndVertical();
                }

                if (!GUILayout.Button(label)) continue;
                if (!canInvoke)
                {
                    Debug.LogWarning($"Cannot invoke '{method.Name}' because it has unsupported parameters.");
                    continue;
                }

                try
                {
                    method.Invoke(_selectedEvent, args);
                    ShowNotification(new GUIContent(label));
                    UpdateData();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static List<MethodInfo> GetSelectedEventButtonMethods(Type eventType)
        {
            return eventType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.GetCustomAttribute<ButtonAttribute>(true) != null)
                .OrderBy(m => m.MetadataToken)
                .ToList();
        }

        [Button("Save User Data")]
        private void SaveSelectedEvent()
        {
            if (_selectedEvent == null) return;

            var key = _selectedEvent.Key;
            var settings = CreateJsonSettings();

            if (_userData != null)
            {
                _userData.sequence++;
                var json = JsonConvert.SerializeObject(_userData, settings);
                SaveLoadHandler.Save(key + "_user_data", json);
                GameEvent<object>.Emit(key, _userData);
                GameEvent.Emit(key + "_notify");
            }

            ShowNotification(new GUIContent("Event saved successfully"));
        }
        
        private object[] GetOrCreateButtonArguments(MethodInfo method)
        {
            var key = GetMethodCacheKey(method);
            if (_buttonArguments.TryGetValue(key, out var cached)) return cached;

            var parameters = method.GetParameters();
            var values = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                values[i] = GetDefaultParameterValue(parameters[i]);
            }

            _buttonArguments[key] = values;
            return values;
        }

        private static string GetMethodCacheKey(MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName}:{method.MetadataToken}";
        }

        private static object GetDefaultParameterValue(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue && parameter.DefaultValue != DBNull.Value)
            {
                return parameter.DefaultValue;
            }

            var type = parameter.ParameterType;
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static bool TryDrawParameterField(ParameterInfo parameter, object value, out object updated)
        {
            var type = parameter.ParameterType;
            var label = ObjectNames.NicifyVariableName(parameter.Name);

            if (type.IsByRef || parameter.IsOut)
            {
                updated = value;
                return false;
            }

            if (type == typeof(string))
            {
                updated = EditorGUILayout.TextField(label, value as string ?? string.Empty);
                return true;
            }

            if (type == typeof(int))
            {
                updated = EditorGUILayout.IntField(label, value is int v ? v : 0);
                return true;
            }

            if (type == typeof(float))
            {
                updated = EditorGUILayout.FloatField(label, value is float v ? v : 0f);
                return true;
            }

            if (type == typeof(double))
            {
                updated = EditorGUILayout.DoubleField(label, value is double v ? v : 0d);
                return true;
            }

            if (type == typeof(long))
            {
                updated = EditorGUILayout.LongField(label, value is long v ? v : 0L);
                return true;
            }

            if (type == typeof(bool))
            {
                updated = EditorGUILayout.Toggle(label, value is bool v && v);
                return true;
            }

            if (type.IsEnum)
            {
                var enumValue = value as Enum ?? (Enum)Enum.GetValues(type).GetValue(0);
                updated = EditorGUILayout.EnumPopup(label, enumValue);
                return true;
            }

            if (type == typeof(Vector2))
            {
                updated = EditorGUILayout.Vector2Field(label, value is Vector2 v ? v : Vector2.zero);
                return true;
            }

            if (type == typeof(Vector3))
            {
                updated = EditorGUILayout.Vector3Field(label, value is Vector3 v ? v : Vector3.zero);
                return true;
            }

            if (type == typeof(Vector4))
            {
                updated = EditorGUILayout.Vector4Field(label, value is Vector4 v ? v : Vector4.zero);
                return true;
            }

            if (type == typeof(Color))
            {
                updated = EditorGUILayout.ColorField(label, value is Color v ? v : Color.white);
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                updated = EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);
                return true;
            }

            updated = value;
            return false;
        }

        private static JsonSerializerSettings CreateJsonSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ObscuredIntConverter());
            settings.Converters.Add(new ObscuredFloatConverter());
            return settings;
        }
    }
    
    public class ObscuredIntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(ObscuredInt) || objectType == typeof(ObscuredInt?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return default(ObscuredInt);

            try
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Integer:
                        checked { return (ObscuredInt)(int)Convert.ToInt64(reader.Value); }
                    case JsonToken.Float:
                        checked { return (ObscuredInt)(int)Math.Truncate(Convert.ToDouble(reader.Value)); }
                    case JsonToken.String:
                        var s = (string)reader.Value;
                        if (int.TryParse(s, out var i)) return (ObscuredInt)i;
                        if (long.TryParse(s, out var l)) { checked { return (ObscuredInt)(int)l; } }
                        break;
                }
            }
            catch (OverflowException ex)
            {
                throw new JsonSerializationException($"Giá trị '{reader.Value}' vượt phạm vi Int32 cho ObscuredInt.", ex);
            }

            throw new JsonSerializationException($"Không thể convert token {reader.TokenType} sang ObscuredInt.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null) writer.WriteValue((ObscuredInt)value);
        }
    }

    public class ObscuredFloatConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(ObscuredFloat) || objectType == typeof(ObscuredFloat?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return default(ObscuredFloat);

            try
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Integer:
                    case JsonToken.Float:
                        return (ObscuredFloat)Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
                    case JsonToken.String:
                        var s = (string)reader.Value;
                        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f))
                            return (ObscuredFloat)f;
                        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out f))
                            return (ObscuredFloat)f;
                        break;
                }
            }
            catch (OverflowException ex)
            {
                throw new JsonSerializationException($"Giá trị '{reader.Value}' vượt phạm vi Single cho ObscuredFloat.", ex);
            }
            catch (FormatException ex)
            {
                throw new JsonSerializationException($"Giá trị '{reader.Value}' không hợp lệ cho ObscuredFloat.", ex);
            }

            throw new JsonSerializationException($"Không thể convert token {reader.TokenType} sang ObscuredFloat.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null) writer.WriteValue((float)(ObscuredFloat)value);
        }
    }
}
