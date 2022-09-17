using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer.model;
using Timer.Repository;

namespace Timer.service {
    public class TimeService {
        private readonly TimeRepository _timeRepository;

        public TimeService() {
            _timeRepository = new();
        }

        public void Download(string activityName) {
            var now = DateTime.Now;
            _timeRepository.AddStep(activityName, now, Step.DOWNLOAD);
        }
    }
}
