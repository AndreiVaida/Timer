using System;
using Timer.model;

namespace Timer.Model;

public class TimeEvent {
    public readonly Step Step;
    public readonly TimeSpan Duration;
    public readonly bool IsActive;

    public TimeEvent(Step step, TimeSpan duration, bool isActive) {
        Step = step;
        Duration = duration;
        IsActive = isActive;
    }

    public override string ToString() => $"{GetType().Name}{{Step={Step}, Duration={Duration}, IsActive={IsActive}}}";

    public override bool Equals(object? obj) =>
        obj is TimeEvent @event &&
        Step == @event.Step &&
        Duration.Equals(@event.Duration) &&
        IsActive == @event.IsActive;

    public override int GetHashCode() => HashCode.Combine(Step, Duration, IsActive);
}