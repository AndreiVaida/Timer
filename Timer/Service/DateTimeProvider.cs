using System;

namespace Timer.Service {
    public interface DateTimeProvider {
        DateTime GetNow();
    }
}
