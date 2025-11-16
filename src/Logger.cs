using System;
using System.IO;
using System.Diagnostics;

namespace AgentSupervisor
{
    public class Logger
    {
        private static readonly object _lockObject = new object();

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
                    var timestamp = DateTime.Now.ToString(Constants.LogTimestampFormat);
                    var logEntry = $"[{timestamp}] [{level}] {message}";
                    
                    // Write to debug output
                    Debug.WriteLine(logEntry);
                    
                    // Write to file
                    // Rotate log if it's too large
                    if (File.Exists(Constants.LogFileName))
                    {
                        var fileInfo = new FileInfo(Constants.LogFileName);
                        if (fileInfo.Length > Constants.MaxLogSizeBytes)
                        {
                            var backupName = $"{Constants.LogFileName}.{DateTime.Now:yyyyMMddHHmmss}{Constants.LogBackupExtension}";
                            File.Move(Constants.LogFileName, backupName);
                            
                            // Keep only the last 5 backup files
                            CleanupOldBackups();
                        }
                    }

                    File.AppendAllText(Constants.LogFileName, logEntry + "\n");
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
                var directory = Path.GetDirectoryName(Constants.LogFileName) ?? ".";
                var backupFiles = Directory.GetFiles(directory, $"{Path.GetFileName(Constants.LogFileName)}.*{Constants.LogBackupExtension}")
                    .OrderByDescending(f => f)
                    .Skip(Constants.MaxLogBackupCount)
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
