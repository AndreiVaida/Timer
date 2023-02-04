using System;
using Timer.model;

namespace Timer.Model;

public class TimeLog {
    public readonly Step Step;
    public DateTime DateTime { get; set; }

    public TimeLog(Step step, DateTime dateTime) {
        Step = step;
        DateTime = dateTime;
    }

    public override string? ToString() => $"{GetType().Name}{{Step={Step}, DateTime={DateTime}}}";
}