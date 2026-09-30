// I2LocalizationService.cs

using System;
using System.Collections.Generic;

namespace Falcon.Modules.LocalizationService.Runtime
{
    public sealed class I2LocalizationService : ILocalizationService
    {
        public event Action<string> OnLanguageChanged;

        public string CurrentLanguage
        {
            get
            {
                return I2.Loc.LocalizationManager.CurrentLanguage;
            }
            set
            {
                if (!string.Equals(I2.Loc.LocalizationManager.CurrentLanguage, value, StringComparison.Ordinal))
                {
                    I2.Loc.LocalizationManager.CurrentLanguage = value;
                    OnLanguageChanged?.Invoke(value);
                }
            }
        }

        public IReadOnlyList<string> GetAllLanguages()
        {
            return I2.Loc.LocalizationManager.GetAllLanguages();
        }

        public bool HasTerm(string term)
        {
            if (string.IsNullOrEmpty(term)) return false;
            return I2.Loc.LocalizationManager.TryGetTranslation(term, out var res);
        }

        public string GetTranslation(string term, string overrideLanguage = null, string fallback = null, params object[] args)
        {
            if (string.IsNullOrEmpty(term))
                return fallback ?? string.Empty;

            string raw = null;

            // Phù hợp thói quen I2: (term, overrideLanguage)
            raw = I2.Loc.LocalizationManager.GetTranslation(term, overrideLanguage: overrideLanguage);

            var text = string.IsNullOrEmpty(raw) ? (fallback ?? term) : raw;

            if (args != null && args.Length > 0)
            {
                try { text = string.Format(text, args); }
                catch (FormatException) { /* giữ nguyên nếu format không khớp */ }
            }
            return text;
        }

        public bool TryGetTranslation(string term, out string result, string overrideLanguage = null, string fallback = null, params object[] args)
        {
            result = GetTranslation(term, overrideLanguage, fallback, args);
            return !string.IsNullOrEmpty(result);
        }
    }
}
