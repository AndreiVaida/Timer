using System;
using System.Globalization;

namespace Timer.Utils {
    public static class TimeUtils {
        private const string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";

        public static string FormatDateTime(DateTime dateTime) => dateTime.ToString(DateTimeFormat);
        public static DateTime ToDateTime(string stringDateTime) => DateTime.ParseExact(stringDateTime, DateTimeFormat, CultureInfo.InvariantCulture);
    }
}
