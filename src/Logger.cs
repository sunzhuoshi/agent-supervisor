using System;
using System.IO;

namespace GitHubCopilotAgentBot
{
    public class Logger
    {
        private static readonly object _lockObject = new object();
        private const string LogFileName = "GitHubCopilotAgentBot.log";
        private const int MaxLogSizeBytes = 10 * 1024 * 1024; // 10 MB

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
            Log("ERROR", fullMessage);
        }

        public static void LogWarning(string message)
        {
            Log("WARN", message);
        }

        private static void Log(string level, string message)
        {
            lock (_lockObject)
            {
                try
                {
                    // Rotate log if it's too large
                    if (File.Exists(LogFileName))
                    {
                        var fileInfo = new FileInfo(LogFileName);
                        if (fileInfo.Length > MaxLogSizeBytes)
                        {
                            var backupName = $"{LogFileName}.{DateTime.Now:yyyyMMddHHmmss}.bak";
                            File.Move(LogFileName, backupName);
                            
                            // Keep only the last 5 backup files
                            CleanupOldBackups();
                        }
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] {message}\n";
                    File.AppendAllText(LogFileName, logEntry);
                }
                catch
                {
                    // Silent fail - logging should not crash the application
                }
            }
        }

        private static void CleanupOldBackups()
        {
            try
            {
                var directory = Path.GetDirectoryName(LogFileName) ?? ".";
                var backupFiles = Directory.GetFiles(directory, $"{Path.GetFileName(LogFileName)}.*.bak")
                    .OrderByDescending(f => f)
                    .Skip(5)
                    .ToArray();

                foreach (var file in backupFiles)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Silent fail
            }
        }
    }
}
