/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-23
 */

using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace Falcon.Modules.UnityLocalization.Runtime
{
    [ExecuteAlways]
    [RequireComponent(typeof(LocalizeStringEvent))]
    [RequireComponent(typeof(TMP_Text))]
    public class Localize_PrefixSuffixComponent : MonoBehaviour
    {
        [SerializeField] private string prefix = "";
        [SerializeField] private string suffix = "";

        private LocalizeStringEvent _stringEvent;
        private TMP_Text _text;

        private void Awake()
        {
            _stringEvent = GetComponent<LocalizeStringEvent>();
            _text = GetComponent<TMP_Text>();
            _stringEvent.OnUpdateString.RemoveAllListeners();
            _stringEvent.OnUpdateString.AddListener(OnUpdateString);
            _text.SetText($"{prefix}{_stringEvent.StringReference.GetLocalizedString()}{suffix}");
        }

        private void OnUpdateString(string content)
        {
            _text.SetText($"{prefix}{content}{suffix}");
        }
        
#if UNITY_EDITOR
        // Gọi khi thay đổi giá trị trong Inspector
        private void Update()
        {
            if (Application.isPlaying) return;
            
            if (!_text) _text = GetComponent<TMP_Text>();
            if (!_stringEvent) _stringEvent = GetComponent<LocalizeStringEvent>();

            if (_text && _stringEvent && _stringEvent.StringReference != null)
            {
                var currentLocale = LocalizationSettings.SelectedLocale;
                var defaultLocale = LocalizationSettings.ProjectLocale; // Locale mặc định của project
                _stringEvent.StringReference.LocaleOverride = !currentLocale ? defaultLocale : currentLocale;
                var preview = _stringEvent.StringReference.GetLocalizedString();
                _text.SetText($"{prefix}{preview}{suffix}");
            }
        }
#endif
    }
}