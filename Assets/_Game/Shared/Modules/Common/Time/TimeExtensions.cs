// /*
//  * Author: Bui Quang Huy
//  * Email: huybq@falcongames.com
//  * Company: Falcon Games
//  * Date: 2026-01-29
//  */

using System.Linq;

namespace Falcon.Shared.Common.Time
{
    public static class TimeExtension
	{
		private static readonly string[] NumToStr = Enumerable.Range(0, 60).Select(i => i.ToString("00")).ToArray();

		public static string ToTime(this long shit, string dayFormat, string timeFormat)
		{
			var day = shit / 86400;
			var hour = shit % 86400 / 3600;
			var min = shit % 86400 % 3600 / 60;
			var second = shit % 86400 % 3600 % 60;

			var timeString = "";
			if (day > 0) timeString = dayFormat.Replace("DD", day.ToString("00"));
			timeString += timeFormat.Replace("hh", NumToStr[hour]).Replace("mm", NumToStr[min])
				.Replace("ss", NumToStr[second]);
			return timeString;
		}

		public static string ToTime(this long shit, string timeFormat)
		{
			var hour = shit / 3600;
			var min = shit % 3600 / 60;
			var second = shit % 3600 % 60;

			var timeString = "";
			timeString += timeFormat.Replace("hh", hour.ToString("00")).Replace("mm", NumToStr[min])
				.Replace("ss", NumToStr[second]);
			return timeString;
		}

		public static string ToTime(this int shit, string timeFormat)
		{
			var hour = shit / 3600;
			var min = shit % 3600 / 60;
			var second = shit % 3600 % 60;

			var timeString = "";
			timeString += timeFormat.Replace("hh", hour.ToString("00")).Replace("mm", NumToStr[min])
				.Replace("ss", NumToStr[second]);
			return timeString;
		}

		public static string ToTime(this long shit)
		{
			var day = shit / 86400;
			if (day < 1)
			{
				var hour = shit / 3600;
				var min = shit % 3600 / 60;
				var second = shit % 3600 % 60;
				return $"{NumToStr[hour]}:{NumToStr[min]}:{NumToStr[second]}";
			}
			else
			{
				var hour = shit % 86400 / 3600;
				return $"{day}d {hour}h";
			}
		}

		public static string ToTimeMinute(this long shit)
		{
			var min = shit / 60;
			var second = shit % 60;
			return $"{min:00}:{NumToStr[second]}";
		}

		public static string ToTimeMinute(this int shit)
		{
			var min = shit / 60;
			var second = shit % 60;
			return $"{min:00}:{NumToStr[second]}";
		}
	}
}