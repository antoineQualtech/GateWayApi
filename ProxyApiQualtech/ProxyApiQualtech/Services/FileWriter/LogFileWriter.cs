using System;
using System.Diagnostics;
using System.IO;

namespace ProxyApiQualtech.Services.FileWriter
{
    public class LogFileWriter : IFileWriter
    {
        public LogFileWriter()
        {
            if (!EventLog.SourceExists("ApiGatewayCustomLogs"))
            {
                EventLog.CreateEventSource("ApiGatewayCustomLogs", "Application");
            }
            EventLog.WriteEntry("ApiGatewayCustomLogs", "Emplacement des logs " + Path.Combine(Directory.GetCurrentDirectory(), "LogFiles"), EventLogEntryType.Information);
        }

        public void WriteLogFile(string logMessage)
        {
            try
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
            }catch(Exception ex)
            {
                if (!EventLog.SourceExists("ApiGatewayCustomLogs"))
                {
                    EventLog.CreateEventSource("ApiGatewayCustomLogs", "Application");
                }
                EventLog.WriteEntry("ApiGatewayCustomLogs", ex.Message, EventLogEntryType.Error);
            }
        }
    }
}