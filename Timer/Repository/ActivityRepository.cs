using System;
using System.Collections.Generic;
using Timer.model;
using Timer.Model;

namespace Timer.Repository;

public interface ActivityRepository {
    public void CreateActivity(string activityName);
    public void AddStep(DateTime dateTime, Step step);
    public IList<TimeLog> GetTimeLogs(string? activityName = null);
    public string? GetLastActivityName();
    List<string> GetLastActivities(int numberOfActivities);
}