using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer.model;
using Timer.Model;
using Timer.Repository;

namespace Timer.service {
    public class TimeService {
        private readonly TimeRepository _timeRepository;
        private readonly IDictionary<Step, TimeSpan> _stepsDuration;
        public readonly Subject<TimeEvent> TimeSubject;

        public TimeService() {
            _timeRepository = new();
            _stepsDuration = new Dictionary<Step, TimeSpan>();
            TimeSubject = new();
        }

        public void CreateActivity(string activityName) {
            _timeRepository.CreateActivity(activityName);
        }

        public void Download() {
            var now = DateTime.Now;
            _timeRepository.AddStep(now, Step.DOWNLOAD);
        }

        public void Loading() {
            var now = DateTime.Now;
            _timeRepository.AddStep(now, Step.LOAD);
        }

        public void Editing() {
            var now = DateTime.Now;
            _timeRepository.AddStep(now, Step.EDIT);
        }

        public void FreezeReload() {
            var now = DateTime.Now;
            _timeRepository.AddStep(now, Step.FREEZE_RELOAD);
        }

        public void Pause() {
            var now = DateTime.Now;
            _timeRepository.AddStep(now, Step.PAUSE);
        }

        public void Export() {
            var now = DateTime.Now;
            _timeRepository.AddStep(now, Step.EXPORT);
        }
    }
}
