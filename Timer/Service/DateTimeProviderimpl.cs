using System;

namespace Timer.Service {
    internal class DateTimeProviderImpl : DateTimeProvider {
        public DateTime GetNow() => DateTime.Now;
    }
}
