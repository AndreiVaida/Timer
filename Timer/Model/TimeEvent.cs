using System;
using Timer.model;

namespace Timer.Model {
    public class TimeEvent {
        public readonly Step Step;
        public readonly DateTime DateTime;

        public TimeEvent(Step step, DateTime dateTime) {
            Step = step;
            DateTime = dateTime;
        }
    }
}
