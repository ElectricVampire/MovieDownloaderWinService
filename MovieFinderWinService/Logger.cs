using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieFinderWinService
{
    /// <summary>
    /// Abstract class for logger implementation
    /// </summary>
    internal abstract class BaseLogger
    {
        /// <summary>
        /// Logger method to log the information
        /// </summary>
        /// <param name="logSource">source name</param>
        /// <param name="logLevel">level of log</param>
        /// <param name="message">logging information</param>
        public abstract void Log(string logSource, LogLevel logLevel, string message);

        /// <summary>
        /// lock for concurrent I/O call protection
        /// </summary>
        protected readonly object syncLock = new object();
    }

    /// <summary>
    /// Logger class for logging information in file
    /// </summary>
    internal class FileLogger : BaseLogger
    {
        /// <summary>
        /// StreamWriter Object
        /// </summary>
        private static StreamWriter sw = null;

        /// <summary>
        /// Constructor which take Path of log File
        /// </summary>
        /// <param name="logFilePath"></param>
        public FileLogger(string logFilePath)
        {
            if (sw == null)
            {
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    sw = new StreamWriter(logFilePath, false);
                    sw.AutoFlush = true;
                }
                else
                {
                    throw new ArgumentException("Path of log File is Required as argument");
                }
            }
        }

        /// <summary>
        /// Logger method to log the information in text File
        /// </summary>
        /// <param name="logSource">source name</param>
        /// <param name="logLevel">level of log</param>
        /// <param name="message">logging information</param>>
        public override void Log(string logSource, LogLevel logLevel, string message)
        {
            lock (syncLock)
            {
                sw.WriteLine(string.Format("{0}  :: {1}  :: {2}  =>  {3}", DateTime.Now, logLevel.ToString(), logSource, message));
                sw.Flush();
            }
        }
    }

    /// <summary>
    /// Class to log information in win events
    /// </summary>
    internal class EventLogger : BaseLogger
    {
        /// <summary>
        /// Logger method to log the information in events
        /// </summary>
        /// <param name="logSource">source name</param>
        /// <param name="logLevel">level of log</param>
        /// <param name="message">logging information</param>
        public override void Log(string logSource, LogLevel logLevel, string message)
        {
            lock (syncLock)
            {
                EventLog eventLog = new EventLog();
                eventLog.Source = logSource;
                EventLogEntryType eventLogLevel = GetEventLogLevel(logLevel);
                eventLog.WriteEntry(message, eventLogLevel);
            }
        }

        /// <summary>
        /// mapping log level with event log level
        /// </summary>
        /// <param name="logLevel">log level of message</param>
        /// <returns>Event log level enum of same msg</returns>
        private EventLogEntryType GetEventLogLevel(LogLevel logLevel)
        {
            EventLogEntryType eventLogLevel = EventLogEntryType.Information;

            if (logLevel.Equals(LogLevel.Warning))
            {
                eventLogLevel = EventLogEntryType.Information;
            }
            else if (logLevel.Equals(LogLevel.Error))
            {
                eventLogLevel = EventLogEntryType.Error;
            }

            return eventLogLevel;
        }
    }

    /// <summary>
    /// Logger class to log information
    /// Note: Must set LogFilePath if LogType is File
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Instance of abstract class
        /// </summary>
        private static BaseLogger logger;

        /// <summary>
        /// Path of log file when LogType is File else property is ignored
        /// </summary>
        private static string logFilePath = string.Empty;

        /// <summary>
        /// Path of log file when LogType is File else property is ignored
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                return logFilePath;
            }
            set
            {
                value = Path.Combine(Path.GetDirectoryName(value), Path.GetFileNameWithoutExtension(value) + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(value));
                if (!Directory.Exists(Path.GetDirectoryName(value)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(value));
                    File.Create(Path.GetFileName(value));
                }
                else
                {
                    if (!File.Exists(value))
                    {
                        File.Create(Path.GetFileName(value));
                    }
                }

                logFilePath = value;
            }
        }

        /// <summary>
        /// Factory instance to get logger instance according to LogType
        /// </summary>
        private static LoggerFactory loggerFactory = new LoggerFactory();

        /// <summary>
        /// Logger method to log the information in File or Events
        /// Note: Must set LogFilePath if LogType is File before calling this method
        /// </summary>
        /// <param name="logSource">source name</param>
        /// <param name="logLevel">level of log</param>
        /// <param name="message">logging information</param>
        /// <param name="logType">Target where logging is required. Default is file</param>
        /// <param name="logAtAllTargets">log at all target values(Both File and Events)</param>
        public static void Log(string logSource, string message, LogLevel logLevel = LogLevel.Information, LogType logType = LogType.File, bool logAtAllTargets = false)
        {
            if (logAtAllTargets)
            {
                logger = loggerFactory.GetInstance(LogType.Event);
                logger.Log(logSource, logLevel, message);

                logger = loggerFactory.GetInstance(LogType.File, logFilePath);
                logger.Log(logSource, logLevel, message);
            }
            else
            {
                logger = loggerFactory.GetInstance(logType, logFilePath);
                logger.Log(logSource, logLevel, message);
            }
        }
    }

    /// <summary>
    /// Factory to create logger object
    /// </summary>
    internal class LoggerFactory
    {
        /// <summary>
        /// Instance of abstract class
        /// </summary>
        private static Dictionary<LogType, BaseLogger> logger = new Dictionary<LogType, BaseLogger>();

        /// <summary>
        /// Return instance of logger according to LogType
        /// </summary>
        /// <param name="logType">Target where logging is required</param>
        /// <param name="logFilePath">Path of log file </param>
        /// <returns>Base Logger instance</returns>
        public BaseLogger GetInstance(LogType logType, string logFilePath = null)
        {
            switch (logType)
            {
                case LogType.File:
                    if (!logger.ContainsKey(logType))
                    {
                        logger[logType] = new FileLogger(logFilePath);
                    }
                    break;
                case LogType.Event:
                    if (!logger.ContainsKey(logType))
                    {
                        logger[LogType.Event] = new EventLogger();
                    }
                    break;
                default:
                    break;
            }

            return logger[logType];
        }
    }

    /// <summary>
    /// Level of logging information
    /// </summary>
    public enum LogLevel
    {
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// Target location where logging is required
    /// </summary>
    public enum LogType
    {
        File,
        Event
    }
}
