﻿using System;
using System.Collections.Generic;
using Timer.model;
using Timer.Model;

namespace Timer.Service;

public interface TimeService {
    public IObservable<TimeEvent> TimeUpdates { get; }
    public TimeLog? CreateActivity(string activityName);
    public void StartStep(Step step);
    public (string?, TimeLog?) LoadLatestActivity();
    List<string> GetLatestActivities(int numberOfActivities);
}