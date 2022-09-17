using System;
using Timer.model;

namespace Timer.Model {
    public class TimeEvent {
        public readonly Step Step;
        public readonly TimeSpan Duration;

        public TimeEvent(Step step, TimeSpan duration) {
            Step = step;
            Duration = duration;
        }
    }
}
