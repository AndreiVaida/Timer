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

        public TimeRepository() {
            CreateDataFolderIfNotExists();
        }

        private static void CreateDataFolderIfNotExists() {
            if (!Directory.Exists(DataFolderPath)) {
                Directory.CreateDirectory(DataFolderPath);
            }
        }

        public void AddStep(string activityName, DateTime dateTime, Step step) {
            var filePath = $"{DataFolderPath}{Path.DirectorySeparatorChar}{activityName}.csv";
            using var streamWriter = new StreamWriter(filePath, append: true);

            AddCsvHeaderIfFileIsNew(filePath, streamWriter);

            var formattedDateTime = dateTime.ToString("yyyy.MM.dd HH:mm:ss");
            var line = $"{formattedDateTime}{CsvSeparator}{step}";
            streamWriter.WriteLine(line);
        }

        private static void AddCsvHeaderIfFileIsNew(string filePath, StreamWriter streamWriter) {
            if (new FileInfo(filePath).Length == 0) {
                streamWriter.WriteLine("Date & Time,Step");
            }
        }
    }
}
