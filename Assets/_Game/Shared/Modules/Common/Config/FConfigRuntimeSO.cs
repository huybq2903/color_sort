using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Falcon.Shared.Common
{
    [CreateAssetMenu(fileName = "ConfigRuntimeSO", menuName = "Scriptable Objects/ConfigRuntimeSO")]
    public class FConfigRuntimeSO : ScriptableObject
    {
        // Danh sách config, Unity serialize thuần (không cần Odin).
        [SerializeField] private List<FConfigEntry> entries = new();

        private Dictionary<string, FConfigValue> _lookup;

        public List<FConfigEntry> Entries => entries;

        private Dictionary<string, FConfigValue> Lookup
        {
            get
            {
                if (_lookup != null) return _lookup;

                _lookup = new Dictionary<string, FConfigValue>(entries.Count);
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.key)) continue;
                    _lookup[entry.key] = entry.value;
                }

                return _lookup;
            }
        }

        // Inspector sửa entries thì lookup phải dựng lại.
        private void OnValidate() => _lookup = null;

        public int GetInt(string key, int defaultValue = 0)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsInt(defaultValue) : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsFloat(defaultValue) : defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsBool(defaultValue) : defaultValue;
        }

        public string GetString(string key, string defaultValue = null)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsString(defaultValue) : defaultValue;
        }

        public AnimationCurve GetCurve(string key, AnimationCurve defaultValue = null)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsCurve(defaultValue) : defaultValue;
        }

        public Ease GetEase(string key, Ease defaultValue = Ease.Linear)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsEase(defaultValue) : defaultValue;
        }

        public Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsVector2(defaultValue) : defaultValue;
        }

        public Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsVector3(defaultValue) : defaultValue;
        }

        public Color GetColor(string key, Color defaultValue = default)
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsColor(defaultValue) : defaultValue;
        }

        public T GetObject<T>(string key, T defaultValue = null) where T : Object
        {
            return Lookup.TryGetValue(key, out var value) ? value.AsObject(defaultValue) : defaultValue;
        }
    }
}
