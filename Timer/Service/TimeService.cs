using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer.model;
using Timer.Model;
using Timer.Repository;
using Timer.Utils;

namespace Timer.service {
    public class TimeService {
        private readonly TimeRepository _timeRepository;
        private readonly Subject<TimeEvent> _timeSubject = new();
        private IDictionary<Step, TimeSpan> _stepsDuration;
        public IObservable<TimeEvent> TimeUpdates => _timeSubject.AsObservable();

        public TimeService() {
            _timeRepository = new();
        }

        public void CreateActivity(string activityName) {
            _timeRepository.CreateActivity(activityName);
            CalculateLoggedStepsDuration();
            NotifyStepsDuration();
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

        private void CalculateLoggedStepsDuration() {
            var timeLogs = _timeRepository.GetTimeLogs();
            InitializeStepsDuration();

            if (timeLogs.Count == 0) return;

            for (var i = 1; i < timeLogs.Count; i++) {
                var timeLog = timeLogs[i - 1];
                var nextTimeLog = timeLogs[i];
                if (timeLog.Step == Step.PAUSE) continue;

                var stepDuration = nextTimeLog.DateTime.Subtract(timeLog.DateTime);
                AddDuration(timeLog.Step, stepDuration);
                AddDuration(Step.TOTAL, stepDuration);
            }

            var lastTimeLog = timeLogs[timeLogs.Count - 1];
            if (lastTimeLog.Step == Step.PAUSE) return;

            var now = TimeUtils.ToDateTime(TimeUtils.FormatDateTime(DateTime.Now));
            var lastStepDuration = now.Subtract(lastTimeLog.DateTime);
            AddDuration(lastTimeLog.Step, lastStepDuration);
            AddDuration(Step.TOTAL, lastStepDuration);
        }

        private void InitializeStepsDuration() {
            _stepsDuration = new Dictionary<Step, TimeSpan> {
                [Step.DOWNLOAD] = new TimeSpan(),
                [Step.LOAD] = new TimeSpan(),
                [Step.EDIT] = new TimeSpan(),
                [Step.FREEZE_RELOAD] = new TimeSpan(),
                [Step.EXPORT] = new TimeSpan(),
                [Step.TOTAL] = new TimeSpan()
            };
        }

        private void AddDuration(Step step, TimeSpan duration) => _stepsDuration[step] = _stepsDuration[step].Add(duration);

        private void NotifyStepsDuration() {
            foreach (var (step, duration) in _stepsDuration) {
                _timeSubject.OnNext(new TimeEvent(step, duration));
            }
        }
    }
}
