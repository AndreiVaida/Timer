using System;
using System.Globalization;

namespace Timer.Utils {
    public static class TimeUtils {
        private const string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";

        public static string FormatDateTime(DateTime dateTime) => dateTime.ToString(DateTimeFormat);
        public static DateTime ToDateTime(string stringDateTime) => DateTime.ParseExact(stringDateTime, DateTimeFormat, CultureInfo.InvariantCulture);
        public static DateTime CurrentDateTime() => ToDateTime(FormatDateTime(DateTime.Now));
        
        public static string GetTimeInHours(TimeSpan duration) {
            var hours = duration.ToHours().ToString("0.#");
            return hours + " " + GetHoursWord(hours); ;
        }
        private static decimal ToHours(this TimeSpan duration) => duration.Hours + (duration.Minutes + (duration.Seconds >= 30 ? 1 : 0)) / 60m;
        private static string GetHoursWord(string hours) => hours == "1" ? "oră" : "ore";
    }
}
