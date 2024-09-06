using System;
using System.IO;

namespace ProxyApiQualtech.Services.FileWriter
{
    public class LogFileWriter : IFileWriter
    {
        public LogFileWriter()
        {
        }

        public void WriteLogFile(string logMessage)
        {
            // Determine the root directory and log files directory
            string rootDirectory = Directory.GetCurrentDirectory();
            string logFilesDirectory = Path.Combine(rootDirectory, "LogFiles");

            // Ensure the LogFiles directory exists
            if (!Directory.Exists(logFilesDirectory))
            {
                Directory.CreateDirectory(logFilesDirectory);
            }

            // Create a log file with today's date as the filename
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFilePath = Path.Combine(logFilesDirectory, $"{currentDate}.txt");

            // Write the log message to the file
            using (var logFile = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write)))
            {
                logFile.WriteLine(logMessage);
            }
        }
    }
}