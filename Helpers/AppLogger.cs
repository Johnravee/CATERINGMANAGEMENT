using System;
using System.Diagnostics;
using System.IO;
using System.Linq; // Kailangan para sa Directory.GetFiles().Where(...)
using System.Runtime.CompilerServices;
using System.Windows;

namespace CATERINGMANAGEMENT.Helpers
{
    public enum LogLevel
    {
        Info,
        Error,
        Success
    }

    public static class AppLogger
    {
        private static readonly string logDirectory = "Logs";
        private static readonly string logFilePath = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
        private static readonly object _lock = new();

        // Threshhold (30 days)
        private const int LogRetentionDays = 30;

        // Static constructor to create log directory and clean up old logs
        static AppLogger()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            else
            {
                
                CleanUpOldLogs();
            }
        }
        private static void CleanUpOldLogs()
        {
           
            DateTime thresholdDate = DateTime.Now.AddDays(-LogRetentionDays);

            try
            {
               
                var oldLogFiles = Directory.GetFiles(logDirectory, "*.log")
                    .Where(file =>
                    {
                     
                        DateTime creationDate = File.GetCreationTime(file);
                        return creationDate < thresholdDate;
                    })
                    .ToList();

          
                foreach (var file in oldLogFiles)
                {
                    File.Delete(file);
                    Debug.WriteLine($"[Logger Cleanup] Deleted old log file: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                
                Debug.WriteLine($"[Logger Error] Failed to clean up old logs: {ex.Message}");
            }
        }


        // Log Info
        public static void Info(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Info, message, member, file, line);
        }

        // Log Success
        public static void Success(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Success, message, member, file, line);
        }

        // Log Error with message
        public static void Error(string message, bool showToUser = true,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Error, message, member, file, line);

            if (showToUser)
            {
                MessageBox.Show("Something went wrong. Please try again or contact support.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Log Error with exception
        public static void Error(Exception ex, string? userMessage = null, bool showToUser = true,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            string message = $"{ex.Message}\n{ex.StackTrace}";
            Log(LogLevel.Error, message, member, file, line);

            if (showToUser)
            {
                MessageBox.Show(userMessage ?? "Something went wrong. Please try again or contact support.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Core log method
        private static void Log(LogLevel level, string message, string member, string file, int line)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string fileName = Path.GetFileName(file);
            string logOutput = $"{timestamp} [{level}] {message} (at {fileName}::{member} - Line {line})";

            // Write to Debug and Console
            Debug.WriteLine(logOutput);
            Console.WriteLine(logOutput);

            // Write to file
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(logFilePath, logOutput + Environment.NewLine);
                }
                catch (Exception fileEx)
                {
                    Debug.WriteLine($"[Logger Error] Failed to write log: {fileEx.Message}");
                }
            }
        }
    }
}