using System;
using Timer.model;

namespace Timer.Model;

public class TimeLog(Step step, DateTime dateTime) {
    public readonly Step Step = step;
    public DateTime DateTime { get; set; } = dateTime;

    public override string? ToString() => $"{GetType().Name}{{Step={Step}, DateTime={DateTime}}}";
}