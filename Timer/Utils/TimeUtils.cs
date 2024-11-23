using System;
using System.Globalization;
using Timer.Service;

namespace Timer.Utils;

public static class TimeUtils {
    private const string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";
    public static string FormatDateTime(DateTime dateTime) => dateTime.ToString(DateTimeFormat);
    public static DateTime ToDateTime(string stringDateTime) => DateTime.ParseExact(stringDateTime, DateTimeFormat, CultureInfo.InvariantCulture);
    public static DateTime CurrentDateTime(DateTimeProvider timeProvider) => ToDateTime(FormatDateTime(timeProvider.GetNow()));
    public static DateOnly GetFirstDayOfWeek(DateOnly date)
    {
        // Sunday=0, Monday=1 ...
        var daysToSubtract = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;
        return date.AddDays(-daysToSubtract);
    }
}