using System;
using Timer.model;

namespace Timer.Service {
    public class TimeLog {
        public readonly Step Step;
        public readonly DateTime DateTime;

        public TimeLog(Step step, DateTime dateTime) {
            Step = step;
            DateTime = dateTime;
        }
    }
}
