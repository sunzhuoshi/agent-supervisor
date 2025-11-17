using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AgentSupervisor
{
    /// <summary>
    /// Handles crash dump generation when the application encounters an unhandled exception
    /// </summary>
    public static class CrashDumpHandler
    {
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();

        // MiniDumpWriteDump Windows API
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            SafeHandle hFile,
            uint dumpType,
            IntPtr expParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        // MiniDump types
        private enum MiniDumpType : uint
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        /// <summary>
        /// Initialize the crash dump handler to capture unhandled exceptions
        /// </summary>
        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    return;
                }

                // Handle unhandled exceptions in the main application thread
                Application.ThreadException += OnThreadException;
                
                // Handle unhandled exceptions in background threads
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                _isInitialized = true;
                Logger.LogInfo("Crash dump handler initialized");
            }
        }

        /// <summary>
        /// Handles exceptions from the main UI thread
        /// </summary>
        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                Logger.LogError("Unhandled thread exception", e.Exception);
                WriteCrashDump(e.Exception);
                
                // Show error dialog
                MessageBox.Show(
                    $"An unexpected error occurred and the application must close.\n\n" +
                    $"A crash dump file has been saved to:\n{Constants.CrashDumpFolder}\n\n" +
                    $"Error: {e.Exception.Message}",
                    "Application Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in crash dump handler", ex);
            }
        }

        /// <summary>
        /// Handles unhandled exceptions from background threads
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                Logger.LogError("Unhandled domain exception", exception);
                WriteCrashDump(exception);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in crash dump handler", ex);
            }
        }

        /// <summary>
        /// Writes a crash dump file with exception information
        /// </summary>
        private static void WriteCrashDump(Exception? exception)
        {
            try
            {
                // Ensure crash dump directory exists
                if (!Directory.Exists(Constants.CrashDumpFolder))
                {
                    Directory.CreateDirectory(Constants.CrashDumpFolder);
                }

                // Generate dump file name with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var dumpFileName = Path.Combine(Constants.CrashDumpFolder, $"AgentSupervisor_Crash_{timestamp}.dmp");
                
                // Write the minidump
                using (var fileStream = new FileStream(dumpFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var process = Process.GetCurrentProcess();
                    var processId = (uint)process.Id;
                    var processHandle = process.Handle;

                    // Use MiniDumpWithFullMemory for detailed dumps
                    var dumpType = (uint)(
                        MiniDumpType.MiniDumpWithDataSegs |
                        MiniDumpType.MiniDumpWithHandleData |
                        MiniDumpType.MiniDumpWithUnloadedModules |
                        MiniDumpType.MiniDumpWithProcessThreadData |
                        MiniDumpType.MiniDumpWithFullMemoryInfo |
                        MiniDumpType.MiniDumpWithThreadInfo);

                    bool success = MiniDumpWriteDump(
                        processHandle,
                        processId,
                        fileStream.SafeFileHandle,
                        dumpType,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    if (success)
                    {
                        Logger.LogInfo($"Crash dump written successfully: {dumpFileName}");
                    }
                    else
                    {
                        var error = Marshal.GetLastWin32Error();
                        Logger.LogError($"Failed to write crash dump. Win32 Error: {error}");
                    }
                }

                // Write exception details to a text file
                if (exception != null)
                {
                    var exceptionFileName = Path.Combine(Constants.CrashDumpFolder, $"AgentSupervisor_Exception_{timestamp}.txt");
                    WriteExceptionDetails(exceptionFileName, exception);
                }

                // Clean up old crash dumps
                CleanupOldCrashDumps();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to write crash dump", ex);
            }
        }

        /// <summary>
        /// Writes detailed exception information to a text file
        /// </summary>
        private static void WriteExceptionDetails(string fileName, Exception exception)
        {
            try
            {
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine("=== Agent Supervisor Crash Report ===");
                    writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    writer.WriteLine($"Version: {typeof(Program).Assembly.GetName().Version}");
                    writer.WriteLine($"OS: {Environment.OSVersion}");
                    writer.WriteLine($"CLR Version: {Environment.Version}");
                    writer.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
                    writer.WriteLine();
                    writer.WriteLine("=== Exception Details ===");
                    writer.WriteLine($"Type: {exception.GetType().FullName}");
                    writer.WriteLine($"Message: {exception.Message}");
                    writer.WriteLine();
                    writer.WriteLine("=== Stack Trace ===");
                    writer.WriteLine(exception.StackTrace);
                    
                    // Include inner exceptions
                    var innerException = exception.InnerException;
                    var level = 1;
                    while (innerException != null)
                    {
                        writer.WriteLine();
                        writer.WriteLine($"=== Inner Exception {level} ===");
                        writer.WriteLine($"Type: {innerException.GetType().FullName}");
                        writer.WriteLine($"Message: {innerException.Message}");
                        writer.WriteLine();
                        writer.WriteLine("Stack Trace:");
                        writer.WriteLine(innerException.StackTrace);
                        
                        innerException = innerException.InnerException;
                        level++;
                    }
                }
                
                Logger.LogInfo($"Exception details written to: {fileName}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to write exception details", ex);
            }
        }

        /// <summary>
        /// Cleans up old crash dump files, keeping only the most recent ones
        /// </summary>
        private static void CleanupOldCrashDumps()
        {
            try
            {
                if (!Directory.Exists(Constants.CrashDumpFolder))
                {
                    return;
                }

                // Get all crash dump files
                var dumpFiles = Directory.GetFiles(Constants.CrashDumpFolder, "*.dmp")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Skip(Constants.MaxCrashDumpCount)
                    .ToArray();

                var exceptionFiles = Directory.GetFiles(Constants.CrashDumpFolder, "*.txt")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Skip(Constants.MaxCrashDumpCount)
                    .ToArray();

                // Delete old dump files
                foreach (var file in dumpFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Logger.LogInfo($"Deleted old crash dump: {file}");
                    }
                    catch
                    {
                        // Silent fail
                    }
                }

                // Delete old exception files
                foreach (var file in exceptionFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Logger.LogInfo($"Deleted old exception file: {file}");
                    }
                    catch
                    {
                        // Silent fail
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to cleanup old crash dumps", ex);
            }
        }
    }
}
