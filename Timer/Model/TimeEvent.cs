using System;
using Timer.model;

namespace Timer.Model;

public class TimeEvent(string activityName, Step step, TimeSpan duration, bool isActive) {
    public readonly string ActivityName = activityName;
    public readonly Step Step = step;
    public readonly TimeSpan Duration = duration;
    public readonly bool IsActive = isActive;

    public override string ToString() => $"{GetType().Name}{{Step={Step}, Duration={Duration}, IsActive={IsActive}}}";

    public override bool Equals(object? obj) =>
        obj is TimeEvent @event &&
        Step == @event.Step &&
        Duration.Equals(@event.Duration) &&
        IsActive == @event.IsActive;

    public override int GetHashCode() => HashCode.Combine(Step, Duration, IsActive);
}