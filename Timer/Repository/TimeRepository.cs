using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer.model;

namespace Timer.Repository {
    public class TimeRepository {
        private const string DataFolderPath = "Activities";
        private const string CsvSeparator = ",";
        private string _filePath;

        public TimeRepository() {
            CreateDataFolderIfNotExists();
        }

        public void CreateActivity(string activityName) {
            _filePath = $"{DataFolderPath}{Path.DirectorySeparatorChar}{activityName}.csv";

            if (IsEmptyFile(_filePath)) {
                using var streamWriter = new StreamWriter(_filePath);
                AddCsvHeader(streamWriter);
            }            
        }

        private static void CreateDataFolderIfNotExists() {
            if (!Directory.Exists(DataFolderPath)) {
                Directory.CreateDirectory(DataFolderPath);
            }
        }

        public void AddStep(DateTime dateTime, Step step) {
            using var streamWriter = new StreamWriter(_filePath, append: true);

            var formattedDateTime = dateTime.ToString("yyyy.MM.dd HH:mm:ss");
            var line = $"{formattedDateTime}{CsvSeparator}{step}";
            streamWriter.WriteLine(line);
        }

        private static bool IsEmptyFile(string filePath) => !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

        private static void AddCsvHeader(StreamWriter streamWriter) => streamWriter.WriteLine("Date & Time,Step");
    }
}
