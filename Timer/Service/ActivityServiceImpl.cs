using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Timer.model;
using Timer.Model;
using Timer.Repository;
using Timer.Utils;

namespace Timer.Service;

public class ActivityServiceImpl(ActivityRepository timeRepository, DateTimeProvider timeProvider) : ActivityService  {
    private const int IntervalSeconds = 1;
    private readonly ReplaySubject<TimeEvent> _timeSubject = new(10);
    private IDisposable? _timeUpdatesDisposable;
    private IDictionary<Step, TimeSpan> _stepsDuration;
    private IList<TimeLog>? _timeLogs;
    private readonly HashSet<Step> _activeSteps = new();
    private string? _activeActivityName;
    public IObservable<TimeEvent> TimeUpdates => _timeSubject.AsObservable();

    public void CreateActivity(string activityName) {
        _timeUpdatesDisposable?.Dispose();
        _activeSteps.Clear();

        timeRepository.CreateActivity(activityName);
        _activeActivityName = activityName;
        CalculateLoggedStepsDurationAndTotal();
        UpdateActiveStepsAfterActivityLoaded();
        NotifyLoggedStepsDuration();
        StartTimerLiveUpdate();
    }

    public void StartStep(Step step) {
        if (_timeLogs == null) return;

        var now = TimeUtils.CurrentDateTime(timeProvider);
        _timeLogs.Add(new TimeLog(step, now));
        timeRepository.AddStep(now, step);

        UpdateActiveSteps(step);
    }

    public List<string> GetLatestActivities(int numberOfActivities) => timeRepository.GetLastActivities(numberOfActivities);

    public IDictionary<DateOnly, List<Activity>> GetWeekSummary(DateOnly dayInWeek, bool includeWeekends = false)
    {
        var firstDayOfWeek = TimeUtils.GetFirstDayOfWeek(dayInWeek);
        var lastDayOfWeek = firstDayOfWeek.AddDays(includeWeekends ? 6 : 4);
        var allTimeLogs = timeRepository.GetLastActivities(10)
            .Select(activityName => Tuple.Create(activityName, timeRepository.GetTimeLogs(activityName)))
            .ToList();

        var weekSummary = new Dictionary<DateOnly, List<Activity>>();
        for (var day = firstDayOfWeek; day <= lastDayOfWeek; day = day.AddDays(1))
        {
            var activitiesOfDay = GetActivitiesOfDay(day, allTimeLogs);
            weekSummary.Add(day, activitiesOfDay);
        }

        return weekSummary;
    }

    public void OpenActivityFile(string? activityName = null) => timeRepository.OpenActivityFile(activityName);

    private List<Activity> GetActivitiesOfDay(DateOnly day, IList<Tuple<string, IList<TimeLog>>> allActivities)
    {
        var activitiesOfDay = allActivities.Where(timeLogsOfDay => HasWork(day, timeLogsOfDay.Item2)).ToList();
        return activitiesOfDay.Select(timeLogsOfActivity =>
        {
            var name = timeLogsOfActivity.Item1;
            var allTimeLogs = timeLogsOfActivity.Item2;
            var timeLogsOfDay = allTimeLogs.Where(timeLog => HasWork(day, timeLog)).ToList();
            var workedTime = CalculateLoggedTotalStepsDuration(timeLogsOfDay);
            return new Activity(name) { Duration = workedTime};
        }).ToList();
    }

    private static bool HasWork(DateOnly day, IList<TimeLog> timeLogs) => timeLogs.Any(timeLog => HasWork(day, timeLog));
    private static bool HasWork(DateOnly day, TimeLog timeLog) => DateOnly.FromDateTime(timeLog.DateTime).Equals(day);

    private void CalculateLoggedStepsDurationAndTotal() {
        _timeLogs = timeRepository.GetTimeLogs();
        InitializeStepsDuration();

        if (_timeLogs.Count < 2) return;

        CalculateLoggedStepsDuration();
        var totalDuration = CalculateLoggedTotalStepsDuration(_timeLogs);
        AddDuration(Step.TOTAL, totalDuration);
    }

    private void CalculateLoggedStepsDuration() {
        foreach (var (timeLog, index) in _timeLogs!.Select((value, i) => (value, i))) {
            if (timeLog.Step == Step.PAUSE || timeLog.Step.IsParallelEnd())
                continue;

            var stepDuration = ComputeStepDuration(timeLog, index);
            AddDuration(timeLog.Step, stepDuration);
        }
    }

    private TimeSpan CalculateLoggedTotalStepsDuration(IList<TimeLog>? timeLogs = null) {
        var logs = timeLogs ?? _timeLogs!;
        var sessionStartLogs = new List<TimeLog>();
        var totalDuration = TimeSpan.Zero;
        foreach (var timeLog in logs) {
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
                totalDuration = totalDuration.Add(sessionDuration);
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
            var nowLog = new TimeLog(sessionStartLog.Step, TimeUtils.CurrentDateTime(timeProvider));
            var sessionDuration = ComputeStepDuration(sessionStartLog, nowLog);
            totalDuration = totalDuration.Add(sessionDuration);
        }

        return totalDuration;
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
        var endOfCurrentLog = GetEndOfCurrentLog(timeLog, indexOfTimeLog) ?? new TimeLog(timeLog.Step, TimeUtils.CurrentDateTime(timeProvider));
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
            _timeSubject.OnNext(new TimeEvent(_activeActivityName!, step, duration, isActive));
        }
    }

    private void NotifyStepDuration(Step step, TimeSpan duration) {
        _timeSubject.OnNext(new TimeEvent(_activeActivityName!, step, duration, _activeSteps.Contains(step)));
        _timeSubject.OnNext(new TimeEvent(_activeActivityName!, Step.TOTAL, duration, _activeSteps.Any()));
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