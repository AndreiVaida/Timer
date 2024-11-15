using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timer.model;
using Timer.Model;
using Timer.Utils;

namespace Timer.Repository;

public class TimeRepositoryImpl : TimeRepository {
    private const string DataFolderPath = "Activities";
    private const string CsvHeader = "Date & Time,Step";
    private const string CsvSeparator = ",";
    private string _activeActivityFilePath;
    private readonly TimeUtils _timeUtils;

    public TimeRepositoryImpl(TimeUtils timeUtils) {
        _timeUtils = timeUtils;
        CreateDataFolderIfNotExists();
    }

    public void CreateActivity(string activityName) {
        _activeActivityFilePath = GetFilePath(activityName);

        if (IsEmptyFile(_activeActivityFilePath)) {
            using var streamWriter = new StreamWriter(_activeActivityFilePath);
            streamWriter.WriteLine(CsvHeader);
        }            
    }

    private static string GetFilePath(string activityName) => $"{DataFolderPath}{Path.DirectorySeparatorChar}{activityName}.csv";

    private static void CreateDataFolderIfNotExists() {
        if (!Directory.Exists(DataFolderPath)) {
            Directory.CreateDirectory(DataFolderPath);
        }
    }

    public void AddStep(DateTime dateTime, Step step) {
        using var streamWriter = new StreamWriter(_activeActivityFilePath, append: true);

        var formattedDateTime = _timeUtils.FormatDateTime(dateTime);
        var line = $"{formattedDateTime}{CsvSeparator}{step}";
        streamWriter.WriteLine(line);
    }

    public IList<TimeLog> GetTimeLogs(string? activityName = null)
    {
        var filePath = activityName != null ? GetFilePath(activityName) : _activeActivityFilePath;
        return File.ReadAllLines(filePath)
            .Where(IsValidTimeEventLine)
            .Select(MapLineToTimeLog)
            .ToList();
    }

    public string? GetLastActivityName() => GetLastActivities(1).FirstOrDefault();

    public List<string> GetLastActivities(int numberOfActivities) =>
        new DirectoryInfo(DataFolderPath).GetFiles()
            .Where(IsTimerFile)
            .OrderByDescending(file => file.LastWriteTime)
            .Take(numberOfActivities)
            .Select(file => Path.GetFileNameWithoutExtension(file.Name))
            .ToList();

    private static bool IsTimerFile(FileInfo file) => file.Extension == ".csv";

    private static bool IsEmptyFile(string filePath) => !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

    private static bool IsValidTimeEventLine(string line) => line.Trim().Length > 0 && line.Split(CsvSeparator).Length == 2 && line != CsvHeader;

    private TimeLog MapLineToTimeLog(string line) {
        var parts = line.Split(CsvSeparator);
        var step = (Step)Enum.Parse(typeof(Step), parts[1]);
        var dateTime = _timeUtils.ToDateTime(parts[0]);
        return new TimeLog(step, dateTime);
    }
}