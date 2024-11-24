using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timer.model;
using Timer.Model;
using Timer.Utils;

namespace Timer.Repository {
    public class ActivityRepository {
        private const string DataFolderPath = "Activities";
        private const string CsvHeader = "Date & Time,Step";
        private const string CsvSeparator = ",";
        private string _filePath;

        public ActivityRepository() {
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

            var formattedDateTime = TimeUtils.FormatDateTime(dateTime);
            var line = $"{formattedDateTime}{CsvSeparator}{step}";
            streamWriter.WriteLine(line);
        }

        public IList<TimeLog> GetTimeLogs() =>
            File.ReadAllLines(_filePath)
                .Where(IsValidTimeEventLine)
                .Select(MapLineToTimeLog)
                .ToList();

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
        private bool IsValidTimeEventLine(string line) => line.Trim().Length > 0 && line.Split(CsvSeparator).Length == 2 && line != CsvHeader;

        private TimeLog MapLineToTimeLog(string line) {
            var parts = line.Split(CsvSeparator);
            var step = (Step)Enum.Parse(typeof(Step), parts[1]);
            var dateTime = TimeUtils.ToDateTime(parts[0]);
            return new TimeLog(step, dateTime);
        }
    }
}
