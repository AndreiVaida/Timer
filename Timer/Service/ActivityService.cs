using System;
using System.Collections.Generic;
using Timer.model;
using Timer.Model;

namespace Timer.Service;

public interface ActivityService {
    public IObservable<TimeEvent> TimeUpdates { get; }
    public void CreateActivity(string activityName);
    public void StartStep(Step step);
    List<string> GetLatestActivities(int numberOfActivities);
    IDictionary<DateOnly, List<Activity>> GetWeekSummary(DateOnly dayInWeek, bool includeWeekends = false);
}