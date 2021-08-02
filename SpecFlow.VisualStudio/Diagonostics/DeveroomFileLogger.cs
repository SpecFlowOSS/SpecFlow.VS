using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SpecFlow.VisualStudio.Diagonostics
{
    public class DeveroomFileLogger : IDeveroomLogger
    {
        private static readonly object LockObject = new object();
        public string LogFilePath { get; private set; }
        public TraceLevel Level { get; set; }

        public DeveroomFileLogger(string logFilePath = null, TraceLevel level = TraceLevel.Verbose)
        {
            LogFilePath = logFilePath ?? GetLogFile();
            Level = level;
            CheckLogFolder();
        }

        public void Log(TraceLevel messageLevel, string message)
        {
            if (messageLevel <= Level)
            {
                WriteToLogFile(messageLevel, message);
            }
        }

        private void WriteToLogFile(TraceLevel messageLevel, string message, bool append = true)
        {
            if (LogFilePath != null)
            {
                try
                {
                    lock (LockObject)
                    {
                        var content = $"{DateTime.Now:s}, {messageLevel}, {message}" + Environment.NewLine;
                        if (append)
                            File.AppendAllText(LogFilePath, content, Encoding.UTF8);
                        else
                            File.WriteAllText(LogFilePath, content, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex, "Error writing to the log file");
                }
            }
        }

        private void CheckLogFolder()
        {
            if (LogFilePath != null)
            {
                try
                {
                    LogFilePath = Path.GetFullPath(LogFilePath);
                    var logFolder =  Path.GetDirectoryName(LogFilePath);
                    if (logFolder == null)
                    {
                        LogFilePath = null;
                        return;
                    }

                    lock (LockObject)
                    {
                        if (!Directory.Exists(logFolder))
                            Directory.CreateDirectory(logFolder);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex, $"Error creating log directory for {LogFilePath}");
                    LogFilePath = null;
                }
            }
        }

        internal static string GetLogFile()
        {
            return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Deveroom",
                $"deveroom-vs-{DateTime.Now:yyyyMMdd}.log");
        }

    }
}