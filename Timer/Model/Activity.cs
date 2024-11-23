using System;

namespace Timer.Model;

public class Activity(string name) {
    public string Name => name;
    public bool IsActive { get; set; }
    public TimeSpan Duration { get; set; }
}