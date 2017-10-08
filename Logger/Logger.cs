using System;
using System.Collections.Generic;
using log4net;
using System.Diagnostics;
using System.Threading;

namespace OverWatcher.Common.Logging
{
    public static class Logger
    {
        private static readonly Dictionary<string, ILog> LoggerMap;
        static Logger()
        {
            LoggerMap = new Dictionary<string, ILog>();
        }

        private static ILog GetLogger()
        {
            var stackTrace = new StackTrace();
            var methodBase = stackTrace.GetFrame(2).GetMethod();
            var type = methodBase.ReflectedType;
            var loggerName = type?.Name??"" + Thread.CurrentThread.ManagedThreadId;
            if (LoggerMap.ContainsKey(loggerName))
            {
                return LoggerMap[loggerName];
            }
            ILog logger = LogManager.GetLogger(loggerName);
            LoggerMap.Add(loggerName, logger);
            return logger;
        }

        public static void Info(string formattedString, params object[] param)
        {
            GetLogger().Info(string.Format(formattedString, param));
        }

        public static void Warn(string formattedString, params object[] param)
        {
            GetLogger().Warn(string.Format(formattedString, param));
        }

        public static void Error(string formattedString, params object[] param)
        {
            GetLogger().Error(string.Format(formattedString, param));
        }

        public static void Fatal(string formattedString, params object[] param)
        {
            GetLogger().Fatal(string.Format(formattedString, param));
        }


        public static void Warn(Exception ex, string formattedString, params object[] param)
        {
            GetLogger().Warn(string.Format(formattedString, param), ex);
        }

        public static void Error(Exception ex, string formattedString, params object[] param)
        {
            GetLogger().Error(string.Format(formattedString, param), ex);
        }

        public static void Fatal(Exception ex, string formattedString, params object[] param)
        {
            GetLogger().Fatal(string.Format(formattedString, param), ex);
        }


        public static void Debug(string formattedString, params object[] param)
        {
            GetLogger().Debug(string.Format(formattedString, param));
        }

        public static void Info(string log)
        {                              
            GetLogger().Info(log);
        }                              
                                       
        public static void Warn(Exception ex, string log)
        {                              
            GetLogger().Warn(log, ex);
        }                              
                                       
        public static void Error(Exception ex, string log)
        {
            GetLogger().Error(log, ex);
        }                              
                                       
        public static void Fatal(Exception ex, string log)
        {                              
            GetLogger().Fatal(log, ex);
        }

        public static void Warn(string log)
        {
            GetLogger().Warn(log);
        }

        public static void Error(string log)
        {
            GetLogger().Error(log);
        }

        public static void Fatal(string log)
        {
            GetLogger().Fatal(log);
        }

        public static void Debug(string log)
        {                              
            GetLogger().Debug(log);          
        }
    }
}
