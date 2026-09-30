// LocalizationManagerCompat.cs
using System;
using System.Collections.Generic;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace Falcon.Modules.LocalizationService.Runtime
{
    /// <summary>
    /// Facade tĩnh na ná I2.Loc.LocalizationManager để gọi nhanh ở mọi nơi.
    /// </summary>
    public static class FalconLocManager
    {
        private static ILocalizationService _impl;

        public static void Initialize(ILocalizationService impl = null)
        {
            _impl = impl ?? new I2LocalizationService();
        }

        private static ILocalizationService Impl
        {
            get { if (_impl == null) Initialize(); return _impl; }
        }

        // === API “giống I2” ===
        public static string CurrentLanguage
        {
            get => Impl.CurrentLanguage;
            set => Impl.CurrentLanguage = value;
        }

        public static IReadOnlyList<string> GetAllLanguages() => Impl.GetAllLanguages();

        public static bool HasTerm(string term) => Impl.HasTerm(term);

        public static string GetTranslation(string term, string overrideLanguage = null, string fallback = null, params object[] args)
            => Impl.GetTranslation(term, overrideLanguage, fallback, args);

        public static bool TryGetTranslation(string term, out string result, string overrideLanguage = null, string fallback = null, params object[] args)
            => Impl.TryGetTranslation(term, out result, overrideLanguage, fallback, args);

        public static event Action<string> OnLanguageChanged
        {
            add => Impl.OnLanguageChanged += value;
            remove => Impl.OnLanguageChanged -= value;
        }
    }
}
