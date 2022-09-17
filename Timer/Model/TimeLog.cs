using System;
using Timer.model;

namespace Timer.Model {
    public class TimeLog {
        public readonly Step Step;
        public readonly DateTime DateTime;

        public TimeLog(Step step, DateTime dateTime) {
            Step = step;
            DateTime = dateTime;
        }
    }
}
