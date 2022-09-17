using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Timer.model;
using Timer.Model;
using Timer.Repository;
using Timer.Service;
using Timer.Utils;

namespace Timer.service {
    public class TimeService {
        private readonly TimeRepository _timeRepository;
        private readonly Subject<TimeEvent> _timeSubject = new();
        private IDictionary<Step, TimeSpan> _stepsDuration;
        private IList<TimeLog> _timeLogs;
        public IObservable<TimeEvent> TimeUpdates => _timeSubject.AsObservable();

        public TimeService() {
            _timeRepository = new();
        }

        public void CreateActivity(string activityName) {
            _timeRepository.CreateActivity(activityName);
            CalculateLoggedStepsDuration();
            NotifyLoggedStepsDuration();
            StartTimer();
        }

        public void StartStep(Step step) {
            if (_timeLogs == null) return;

            var now = TimeUtils.CurrentDateTime();
            UpdateLastStepDuration();
            _timeLogs.Add(new TimeLog(step, now));
            _timeRepository.AddStep(now, step);
        }

        private void CalculateLoggedStepsDuration() {
            _timeLogs = _timeRepository.GetTimeLogs();
            InitializeStepsDuration();

            if (_timeLogs.Count == 0) return;

            for (var i = 1; i < _timeLogs.Count; i++) {
                var timeLog = _timeLogs[i - 1];
                var nextTimeLog = _timeLogs[i];
                if (timeLog.Step == Step.PAUSE) continue;

                var stepDuration = nextTimeLog.DateTime.Subtract(timeLog.DateTime);
                AddDuration(timeLog.Step, stepDuration);
                AddDuration(Step.TOTAL, stepDuration);
            }
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

        private void NotifyLoggedStepsDuration() {
            foreach (var (step, duration) in _stepsDuration) {
                _timeSubject.OnNext(new TimeEvent(step, duration));
            }
        }

        private void NotifyStepDuration(Step step, TimeSpan duration) {
            _timeSubject.OnNext(new TimeEvent(step, duration));
            _timeSubject.OnNext(new TimeEvent(Step.TOTAL, duration));
        }

        private void StartTimer() =>
            Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => CalculateAndNotifyStepsDuration());

        private void CalculateAndNotifyStepsDuration() {
            var currentTimeLog = _timeLogs.LastOrDefault();
            if (currentTimeLog == null || currentTimeLog.Step == Step.PAUSE) return;

            var stepDuration = CalculateCurrentStepDuration(currentTimeLog);
            var totalDuration = CalculateTotalDuration(currentTimeLog.Step, stepDuration);
            NotifyStepDuration(currentTimeLog.Step, stepDuration);
            NotifyStepDuration(Step.TOTAL, totalDuration);
        }

        private TimeSpan CalculateCurrentStepDuration(TimeLog timeLog) {
            var durationSinceStepStarted = TimeUtils.CurrentDateTime().Subtract(timeLog.DateTime);
            return durationSinceStepStarted.Add(_stepsDuration[timeLog.Step]);
        }

        private TimeSpan CalculateTotalDuration(Step stepToRecalculate, TimeSpan recalculatedDuration) {
            var duration = _stepsDuration
                .Where(stepDuration => stepDuration.Key != Step.TOTAL && stepDuration.Key != stepToRecalculate)
                .Select(stepDuration => stepDuration.Value)
                .Aggregate(new TimeSpan(), (totalDuration, duration) => totalDuration.Add(duration));

            return duration.Add(recalculatedDuration);
        }

        private void UpdateLastStepDuration() {
            var currentTimeLog = _timeLogs.LastOrDefault();
            if (currentTimeLog == null || currentTimeLog.Step == Step.PAUSE) return;

            var stepDuration = CalculateCurrentStepDuration(currentTimeLog);
            _stepsDuration[currentTimeLog.Step] = stepDuration;
        }
    }
}
