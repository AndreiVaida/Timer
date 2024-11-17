using System;
using System.Globalization;

namespace Timer.Utils;

public class TimeUtilsImpl : TimeUtils {
    protected const string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";
    public string FormatDateTime(DateTime dateTime) => dateTime.ToString(DateTimeFormat);
    public DateTime ToDateTime(string stringDateTime) => DateTime.ParseExact(stringDateTime, DateTimeFormat, CultureInfo.InvariantCulture);
    public DateTime CurrentDateTime() => ToDateTime(FormatDateTime(DateTime.Now));
    public DateOnly GetFirstDayOfWeek(DateOnly date)
    {
        // Sunday=0, Monday=1 ...
        var daysToSubtract = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;
        return date.AddDays(-daysToSubtract);
    }
}