#nullable disable
namespace SpecFlow.VisualStudio.Diagnostics;

public class DeveroomFileLogger : IDeveroomLogger
{
    private static readonly object LockObject = new();

    public DeveroomFileLogger(TraceLevel level = TraceLevel.Verbose)
    {
        LogFilePath = GetLogFile();
        Level = level;
        CheckLogFolder();
        DeleteOldLogFiles();
    }

    public string LogFilePath { get; private set; }
    public TraceLevel Level { get; set; }

    public void Log(TraceLevel messageLevel, string message)
    {
        if (messageLevel <= Level) WriteToLogFile(messageLevel, message);
    }

    private void WriteToLogFile(TraceLevel messageLevel, string message, bool append = true)
    {
        if (LogFilePath != null)
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

    private void CheckLogFolder()
    {
        if (LogFilePath != null)
            try
            {
                LogFilePath = Path.GetFullPath(LogFilePath);
                var logFolder = Path.GetDirectoryName(LogFilePath);
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

    private void DeleteOldLogFiles()
    {
        if (LogFilePath != null)
            try
            {
                var logFolder = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(logFolder))
                    return;

                var logFiles = Directory.GetFiles(logFolder, "specflow-vs-*.log");

                foreach (string logFile in logFiles)
                    lock (LockObject)
                    {
                        FileInfo fi = new FileInfo(logFile);
                        if (fi.LastWriteTime < DateTime.Now.AddDays(-10))
                            fi.Delete();
                    }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Error deleting log files");
            }
    }

    internal static string GetLogFile()
    {
        return Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SpecFlow",
#if DEBUG
            $"specflow-vs-debug-{DateTime.Now:yyyyMMdd}.log");
#else
                $"specflow-vs-{DateTime.Now:yyyyMMdd}.log");
#endif
    }
}
