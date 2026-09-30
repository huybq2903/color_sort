/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using I2.Loc;

namespace Falcon.Modules.Packs.Core.Runtime
{
    public static class FormatHelper
    {
        /// <summary>
        /// Gán lại nếu muốn đổi hàm
        /// </summary>
        public static Func<(string name, int amount, string data), string> FormatQuantityReward = reward => reward.amount.ToString();
        
        private static readonly string[] kNumToStr = Enumerable.Range(0, 60).Select(i => i.ToString("00")).ToArray();
        /// <summary>
        /// Định dạng một số nguyên với dấu cách làm ký tự phân tách hàng nghìn.
        /// Ví dụ: 1234567 thành "1 234 567".
        /// </summary>
        /// <param name="number">Số nguyên cần định dạng.</param>
        /// <returns>Chuỗi đã định dạng với dấu cách phân tách.</returns>
        public static string FormatWithSpace(this int number)
        {
            return number.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
        }
        
        public static string FormatSeconds(this int seconds)
        {
            if (seconds < 60)
                return $"{seconds}s";
            if (seconds < 3600)
                return $"{seconds / 60}m";
            return $"{seconds / 3600}h";
        }

        public static string FormatSecondsCoolDown(this long seconds)
        {
            var day = seconds / 86400;
            if (day < 1)
            {
                var hour = seconds / 3600;
                var min = (seconds % 3600) / 60;
                var second = (seconds % 3600) % 60;
                return $"{kNumToStr[hour]}:{kNumToStr[min]}:{kNumToStr[second]}";
            }
            else
            {
                var hour = seconds % 86400 / 3600;
                return $"{day}d {hour}h";
            }
        }
        
        public static string Localize(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim();
            var result = Regex.Replace(input, @"\s+", "_").ToLower();
            return LocalizationManager.GetTranslation(result) ?? input;
        }
    }
}