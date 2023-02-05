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
    private string _filePath;
    private readonly TimeUtils _timeUtils;

    public TimeRepositoryImpl(TimeUtils timeUtils) {
        _timeUtils = timeUtils;
        CreateDataFolderIfNotExists();
    }

    public void CreateActivity(string activityName) {
        _filePath = $"{DataFolderPath}{Path.DirectorySeparatorChar}{activityName}.csv";

        if (IsEmptyFile(_filePath)) {
            using var streamWriter = new StreamWriter(_filePath);
            streamWriter.WriteLine(CsvHeader);
        }            
    }

    private static void CreateDataFolderIfNotExists() {
        if (!Directory.Exists(DataFolderPath)) {
            Directory.CreateDirectory(DataFolderPath);
        }
    }

    public void AddStep(DateTime dateTime, Step step) {
        using var streamWriter = new StreamWriter(_filePath, append: true);

        var formattedDateTime = _timeUtils.FormatDateTime(dateTime);
        var line = $"{formattedDateTime}{CsvSeparator}{step}";
        streamWriter.WriteLine(line);
    }

    public IList<TimeLog> GetTimeLogs() =>
        File.ReadAllLines(_filePath)
            .Where(IsValidTimeEventLine)
            .Select(MapLineToTimeLog)
            .ToList();

    public string? GetLastActivityName() =>
        new DirectoryInfo(DataFolderPath).GetFiles()
            .OrderByDescending(file => file.LastWriteTime)
            .Take(1)
            .Select(file => Path.GetFileNameWithoutExtension(file.Name))
            .FirstOrDefault();

    private static bool IsEmptyFile(string filePath) => !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

    private static bool IsValidTimeEventLine(string line) => line.Trim().Length > 0 && line.Split(CsvSeparator).Length == 2 && line != CsvHeader;

    private TimeLog MapLineToTimeLog(string line) {
        var parts = line.Split(CsvSeparator);
        var step = (Step)Enum.Parse(typeof(Step), parts[1]);
        var dateTime = _timeUtils.ToDateTime(parts[0]);
        return new TimeLog(step, dateTime);
    }
}