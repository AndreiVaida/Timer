using System;
using System.Collections.Generic;

namespace Timer.Utils;

public interface TimeUtils {
    public string FormatDateTime(DateTime dateTime);
    public DateTime ToDateTime(string stringDateTime);
    public DateTime CurrentDateTime();
    public DateOnly GetFirstDayOfWeek(DateOnly date);
}