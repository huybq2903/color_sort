/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-25
 */

using UnityEngine.Localization.Settings;

namespace Falcon.Modules.UnityLocalization.Runtime
{
    public readonly struct ULocEntry
    {
        private readonly string _table;
        private readonly string _key;

        public ULocEntry(string table, string key)
        {
            _table = table;
            _key = key;
        }

        public override string ToString() =>
            LocalizationSettings.StringDatabase.GetLocalizedString(_table, _key);

        public string Param(params object[] args) =>
            LocalizationSettings.StringDatabase.GetLocalizedString(_table, _key, args);

        public static implicit operator string(ULocEntry e) => e.ToString();
    }
}