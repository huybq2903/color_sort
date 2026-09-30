// ILocalizationService.cs
using System;
using System.Collections.Generic;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace Falcon.Modules.LocalizationService.Runtime
{
    /// <summary>
    /// Giao diện trung lập nhưng tên hàm “giống I2”.
    /// </summary>
    public interface ILocalizationService
    {
        // Map: I2.Loc.LocalizationManager.CurrentLanguage
        string CurrentLanguage { get; set; }

        // Map: I2.Loc.LocalizationManager.GetAllLanguages(out list)
        IReadOnlyList<string> GetAllLanguages();

        // Map: I2.Loc.LocalizationManager.HasTerm(term)
        bool HasTerm(string term);

        // Gần với: I2.Loc.LocalizationManager.GetTranslation(term, overrideLanguage)
        // Thêm fallback (tùy chọn) để dễ dùng thực tế.
        string GetTranslation(string term, string overrideLanguage = null, string fallback = null, params object[] args);

        bool TryGetTranslation(string term, out string result, string overrideLanguage = null, string fallback = null, params object[] args);

        event Action<string> OnLanguageChanged;
    }
}
