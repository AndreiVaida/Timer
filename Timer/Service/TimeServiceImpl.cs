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

namespace Timer.service;

public class TimeServiceImpl : TimeService  {
    private const int IntervalSeconds = 1;
    private readonly TimeRepository _timeRepository;
    private readonly Subject<TimeEvent> _timeSubject = new();
    private IDisposable? _timeUpdatesDisposable;
    private IDictionary<Step, TimeSpan> _stepsDuration;
    private IList<TimeLog>? _timeLogs;
    private readonly HashSet<Step> _activeSteps = new();
    private readonly TimeUtils _timeUtils;
    public IObservable<TimeEvent> TimeUpdates => _timeSubject.AsObservable();

    public TimeServiceImpl(TimeRepository timeRepository, TimeUtils timeUtils) {
        _timeRepository = timeRepository;
        _timeUtils = timeUtils;
    }

    public TimeLog? CreateActivity(string activityName) {
        _timeUpdatesDisposable?.Dispose();
        _activeSteps.Clear();

        _timeRepository.CreateActivity(activityName);
        CalculateLoggedStepsDurationAndTotal();
        UpdateActiveStepsAfterActivityLoaded();
        NotifyLoggedStepsDuration();
        StartTimerLiveUpdate();
        return _timeLogs!.LastOrDefault();
    }

    public void StartStep(Step step) {
        if (_timeLogs == null) return;

        var now = _timeUtils.CurrentDateTime();
        _timeLogs.Add(new TimeLog(step, now));
        _timeRepository.AddStep(now, step);

        UpdateActiveSteps(step);
    }

    public (string?, TimeLog?) LoadLatestActivity() {
        var activityName = _timeRepository.GetLastActivityName();
        if (activityName == null)
            return (null, null);

        CreateActivity(activityName);
        return (activityName, _timeLogs!.LastOrDefault());
    }

    private void CalculateLoggedStepsDurationAndTotal() {
        _timeLogs = _timeRepository.GetTimeLogs();
        InitializeStepsDuration();

        if (_timeLogs.Count < 2) return;

        CalculateLoggedStepsDuration();
        CalculateLoggedTotalStepsDuration();
    }

    private void CalculateLoggedStepsDuration() {
        foreach (var (timeLog, index) in _timeLogs!.Select((value, i) => (value, i))) {
            if (timeLog.Step == Step.PAUSE || timeLog.Step.IsParallelEnd())
                continue;

            var stepDuration = ComputeStepDuration(timeLog, index);
            AddDuration(timeLog.Step, stepDuration);
        }
    }

    private void CalculateLoggedTotalStepsDuration() {
        var sessionStartLogs = new List<TimeLog>();
        foreach (var timeLog in _timeLogs!) {
            if (IsSessionStartLog(timeLog)) {
                var startLog = timeLog.Step.IsParallel()
                    ? new TimeLog(timeLog.Step, timeLog.DateTime)
                    : new TimeLog(Step.INVESTIGATE, timeLog.DateTime); // use INVESTIGATE as placeholder for all sequential steps

                if (timeLog.Step.IsParallel()) {
                    var startLogOfSession = sessionStartLogs.FirstOrDefault();
                    sessionStartLogs.RemoveAll(log => !log.Step.IsParallel());

                    if (startLogOfSession?.Step.IsSequential() ?? false)
                        startLog.DateTime = startLogOfSession.DateTime;
                }

                if (sessionStartLogs.All(log => log.Step != startLog.Step))
                    sessionStartLogs.Add(startLog);
            }

            else if (IsEndOfAllSessions(sessionStartLogs, timeLog)) {
                var sessionStartLog = sessionStartLogs.FirstOrDefault() ?? timeLog;
                var sessionDuration = ComputeStepDuration(sessionStartLog, timeLog);
                AddDuration(Step.TOTAL, sessionDuration);
                sessionStartLogs.Clear();
            }

            else if (IsEndOfAParallelSession(sessionStartLogs, timeLog)) {
                var startStep = timeLog.Step.GetStartStepOfParallelEnd();
                var startLogOfSession = sessionStartLogs.First();
                sessionStartLogs.RemoveAll(log => log.Step == startStep);

                if (startLogOfSession.Step == startStep) {
                    sessionStartLogs.First().DateTime = startLogOfSession.DateTime;
                }
            }
        }

        if (sessionStartLogs.Count > 0) {
            var sessionStartLog = sessionStartLogs.First();
            var nowLog = new TimeLog(sessionStartLog.Step, _timeUtils.CurrentDateTime());
            var sessionDuration = ComputeStepDuration(sessionStartLog, nowLog);
            AddDuration(Step.TOTAL, sessionDuration);
        }
    }

    private static bool IsSessionStartLog(TimeLog timeLog)
        => timeLog.Step.IsParallelStart() || timeLog.Step.IsSequential();

    private static bool IsEndOfAllSessions(List<TimeLog> sessionStartLogs, TimeLog timeLog) {
        if (timeLog.Step == Step.PAUSE || sessionStartLogs.Count == 0)
            return true;

        if (sessionStartLogs.Count >= 2)
            return false;

        var sessionStartStep = sessionStartLogs.First().Step;
        if (sessionStartStep.IsParallelStart())
            return timeLog.Step == sessionStartStep.GetEndStepOfParallelStart();

        // timeLog.Step is Sequential or ParallelStart
        return false;
    }

    private static bool IsEndOfAParallelSession(List<TimeLog> sessionStartLogs, TimeLog timeLog) {
        if (timeLog.Step.IsParallelEnd()) {
            var startStep = timeLog.Step.GetStartStepOfParallelEnd();
            return sessionStartLogs.Any(log => log.Step == startStep);
        }

        // timeLog.Step is Sequential or ParallelStart
        return false;
    }

    private static TimeSpan ComputeStepDuration(TimeLog startLog, TimeLog endLog) => endLog.DateTime.Subtract(startLog.DateTime);

    private TimeSpan ComputeStepDuration(TimeLog timeLog, int indexOfTimeLog) {
        var endOfCurrentLog = GetEndOfCurrentLog(timeLog, indexOfTimeLog) ?? new TimeLog(timeLog.Step, _timeUtils.CurrentDateTime());
        return ComputeStepDuration(timeLog, endOfCurrentLog);
    }

    private TimeLog? GetEndOfCurrentLog(TimeLog timeLog, int indexOfTimeLog) {
        if (timeLog.Step.IsParallelStart()) {
            var stopStep = timeLog.Step.GetEndStepOfParallelStart();
            return GetFirstLogMatching(indexOfTimeLog, step => step == stopStep || step == Step.PAUSE);
        }

        return GetFirstLogMatching(indexOfTimeLog, step => !step.IsParallelEnd());
    }

    private TimeLog? GetFirstLogMatching(int indexOfTimeLog, Predicate<Step> predicate) =>
        _timeLogs!.Skip(indexOfTimeLog + 1)
            .FirstOrDefault(log => predicate.Invoke(log.Step));

    private void InitializeStepsDuration() {
        _stepsDuration = new Dictionary<Step, TimeSpan> {
            [Step.MEETING] = new(),
            [Step.OTHER] = new(),
            [Step.INVESTIGATE] = new(),
            [Step.IMPLEMENT] = new(),
            [Step.WAIT_FOR_REVIEW__START] = new(),
            [Step.RESOLVE_COMMENTS] = new(),
            [Step.DO_REVIEW] = new(),
            [Step.LOADING__START] = new(),
            [Step.TOTAL] = new()
        };
    }

    private void AddDuration(Step step, TimeSpan duration) => _stepsDuration[step] = _stepsDuration[step].Add(duration);

    private void NotifyLoggedStepsDuration() {
        foreach (var (step, duration) in _stepsDuration) {
            var isActive = step == Step.TOTAL ? _activeSteps.Any() : _activeSteps.Contains(step);
            _timeSubject.OnNext(new TimeEvent(step, duration, isActive));
        }
    }

    private void NotifyStepDuration(Step step, TimeSpan duration) {
        _timeSubject.OnNext(new TimeEvent(step, duration, _activeSteps.Contains(step)));
        _timeSubject.OnNext(new TimeEvent(Step.TOTAL, duration, _activeSteps.Any()));
    }

    private void StartTimerLiveUpdate() {
        _timeUpdatesDisposable = Observable
            .Interval(TimeSpan.FromSeconds(IntervalSeconds))
            .Subscribe(_ => IncrementAndNotifyActiveStepsDuration());
    }

    private void IncrementAndNotifyActiveStepsDuration() {
        if (_activeSteps.Count == 0) return;

        foreach (var step in _activeSteps) {
            _stepsDuration[step] += TimeSpan.FromSeconds(IntervalSeconds);
            NotifyStepDuration(step, _stepsDuration[step]);
        }

        _stepsDuration[Step.TOTAL] += TimeSpan.FromSeconds(IntervalSeconds);
        NotifyStepDuration(Step.TOTAL, _stepsDuration[Step.TOTAL]);
    }

    private void UpdateActiveStepsAfterActivityLoaded() {
        var activeSteps = new HashSet<Step>();

        foreach (var timeLog in _timeLogs!.Reverse()) {
            if (timeLog.Step == Step.PAUSE) {
                break;
            }

            if (timeLog.Step.IsSequential() && !activeSteps.Any(session => session.IsSequential() || session.IsParallelStart())) {
                activeSteps.Add(timeLog.Step);
            }
            else if (timeLog.Step.IsParallelEnd() && activeSteps.All(session => session != timeLog.Step.GetStartStepOfParallelEnd())) {
                activeSteps.Add(timeLog.Step);
            }
            else if (timeLog.Step.IsParallelStart() && activeSteps.All(session => session != timeLog.Step.GetEndStepOfParallelStart())) {
                activeSteps.Add(timeLog.Step);
            }

            if (activeSteps.Any(session => session.IsSequential()) && ContainsAllParallelSteps(activeSteps)) {
                break;
            }
        }

        foreach (var activeStep in activeSteps.Where(step => step.IsSequential() || step.IsParallelStart()))
            _activeSteps.Add(activeStep);
    }

    private static bool ContainsAllParallelSteps(ISet<Step> steps) =>
        Enum.GetValues(typeof(Step)).OfType<Step>().Where(step => step.IsParallelStart())
            .All(startStep => steps.Contains(startStep) || steps.Contains(startStep.GetEndStepOfParallelStart()));

    private void UpdateActiveSteps(Step newStep) {
        if (newStep.IsSequential()) {
            _activeSteps.RemoveWhere(step => !step.IsParallel());
            _activeSteps.Add(newStep);
        }
        else if (newStep.IsParallelStart()) {
            _activeSteps.Add(newStep);
            _activeSteps.RemoveWhere(step => !step.IsParallel());
        }
        else if (newStep.IsParallelEnd()) {
            _activeSteps.Remove(newStep.GetStartStepOfParallelEnd());
        }
        else if (newStep == Step.PAUSE) {
            _activeSteps.Clear();
        }
    }
}