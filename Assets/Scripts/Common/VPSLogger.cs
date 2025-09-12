using System;
using System.IO;
using UnityEngine;

namespace WASVPS
{
    public enum LogLevel
    {
        NONE = 0,
        ERROR = 1,
        DEBUG = 2,
        VERBOSE = 3
    }

    /// <summary>
    /// Custom logger with verbose levels
    /// </summary>
    public static class VPSLogger
    {
        public static bool WriteLogsInFile { get; set; } = false;

#if VPS_DEBUG
        private static LogLevel currentLogLevel = LogLevel.DEBUG;
#elif VPS_VERBOSE
        private static LogLevel currentLogLevel = LogLevel.VERBOSE;
#else
        private static LogLevel currentLogLevel = LogLevel.NONE;
#endif

        private static readonly string LogFilePath = Path.Combine(Application.persistentDataPath, "Log.txt");
        private static StreamWriter _logsStreamWriter;

        public static void Log(LogLevel level, object message)
        {
            if (message == null) return;

            // ERROR level always logs regardless of current level
            if (level == LogLevel.ERROR)
            {
                Debug.LogError(message);
            }
            // Only log if message level is at or below current threshold
            else if (level <= currentLogLevel)
            {
                Debug.Log(message);
            }

            AddToLogFile(message.ToString());
        }

        public static void LogFormat(LogLevel level, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format) || args == null) return;

            if (level == LogLevel.ERROR)
            {
                Debug.LogErrorFormat(format, args);
            }
            else if (level <= currentLogLevel)
            {
                Debug.LogFormat(format, args);
            }

            AddToLogFile(string.Format(format, args));
        }

        public static void SetLogLevel(LogLevel newLevel)
        {
            currentLogLevel = newLevel;
        }

        public static LogLevel GetLogLevel()
        {
            return currentLogLevel;
        }

        private static void AddToLogFile(string logString)
        {
            if (!WriteLogsInFile || string.IsNullOrEmpty(logString))
                return;

            try
            {
                string finalString = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {logString}";
                
                // Lazy initialization of file writer
                if (_logsStreamWriter == null)
                {
                    InitializeLogFileWriter();
                }
                
                _logsStreamWriter.WriteLine(finalString);
                _logsStreamWriter.Flush(); // Ensure data is written immediately
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }
        }
        private static void InitializeLogFileWriter()
        {
            FileStream fs;
            if (!File.Exists(LogFilePath))
            {
                fs = File.Create(LogFilePath);
                // without it log file may not be displayed in android file transfer
#if UNITY_ANDROID && !UNITY_EDITOR
                RefreshAndroidFile(LogFilePath);
#endif
            }
            else
            {
                fs = File.Open(LogFilePath, FileMode.Append);
            }
            _logsStreamWriter = new StreamWriter(fs);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Notifies Android MediaScanner about new file to make it visible in file managers
        /// </summary>
        private static void RefreshAndroidFile(string path) 
        {
            try
            {
                using (AndroidJavaClass jcUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject joActivity = jcUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject joContext = joActivity.Call<AndroidJavaObject>("getApplicationContext"))
                using (AndroidJavaClass jcMediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection"))
                    jcMediaScannerConnection.CallStatic("scanFile", joContext, new string[] { path }, null, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to refresh Android file: {ex.Message}");
            }
        }
#endif
    }
}