﻿﻿using System;
 using System.Collections;
 using System.Diagnostics;
﻿using System.Linq;
 using System.Reflection;
 using NLog.StructuredLogging.Json.Helpers;

namespace NLog.StructuredLogging.Json
{
    public static class LoggerExtensions
    {
        public static void ExtendedDebug(this ILogger logger, string message, object logProperties)
        {
            Extended(logger, LogLevel.Debug, message, logProperties, null);
        }

        public static void ExtendedInfo(this ILogger logger, string message, object logProperties)
        {
            Extended(logger, LogLevel.Info, message, logProperties, null);
        }

        public static void ExtendedWarn(this ILogger logger, string message, object logProperties)
        {
            Extended(logger, LogLevel.Warn, message, logProperties, null);
        }

        public static void ExtendedError(this ILogger logger, string message, object logProperties)
        {
            Extended(logger, LogLevel.Error, message, logProperties, null);
        }

        public static void ExtendedException(this ILogger logger, Exception ex, string message, object logProperties = null)
        {
            Extended(logger, LogLevel.Error, message, logProperties, ex);
        }

        public static void Extended(this ILogger logger, LogLevel logLevel, string message, object logProperties, Exception ex = null)
        {
            if (ex == null)
            {
                ExtendedWithException(logger, logLevel, message, logProperties, null, 0, 0, null);
            }
            else if (ex.InnerException == null)
            {
                ExtendedWithException(logger, logLevel, message, logProperties, ex, 1, 1, null);
            }
            else
            {
                var allExceptions = ConvertException.ToList(ex);
                var tag = Guid.NewGuid().ToString();

                for (var index = 0; index < allExceptions.Count; index++)
                {
                    ExtendedWithException(logger, logLevel, message, logProperties,
                        allExceptions[index], index + 1, allExceptions.Count, tag);
                }
            }
        }

        public static INestedContext BeginScope(this ILogger logger, string scopeName, object logProps = null)
        {
            var properties = ObjectDictionaryParser.ConvertObjectToDictionaty(logProps);
            return new NestedContext(logger, scopeName, properties);
        }

        private static void ExtendedWithException(ILogger logger, LogLevel logLevel, string message, object logProperties,
            Exception ex, int exceptionIndex, int exceptionCount, string tag)
        {
            var log = new LogEventInfo(logLevel, logger.Name, message);
            TransferDataObjectToLogEventProperties(log, logProperties);
            TransferContextDataToLogEventProperties(log);
            TransferScopeDataToLogEventProperties(log);

            if (ex != null)
            {
                log.Exception = ex;
                log.Properties.Add("ExceptionIndex", exceptionIndex);
                log.Properties.Add("ExceptionCount", exceptionCount);

                if (!string.IsNullOrEmpty(tag))
                {
                    log.Properties.Add("ExceptionTag", tag);
                }
            }

#if NET452
            var stackTrace = new StackTrace(0);
            log.SetStackTrace(stackTrace, StackHelper.IndexOfFirstCallingMethod(stackTrace.GetFrames()));
            // todo: There is still no netCore subsitiute for this!
#endif

            logger.Log(log);
        }

        private static void TransferDataObjectToLogEventProperties(LogEventInfo log, object logProperties)
        {
            if (logProperties == null)
            {
                return;
            }

            if (logProperties is string)
            {
                return;
            }

            if (IsDictionary(logProperties))
            {
                var dict = (IDictionary)logProperties;

                foreach (var key in dict.Keys)
                {
                    log.Properties.Add(key, dict[key]);
                }
            }
            else
            {
                var convertObjectToDictionaty = ObjectDictionaryParser.ConvertObjectToDictionaty(logProperties);
                foreach (var pair in convertObjectToDictionaty)
                {
                    log.Properties.Add(pair.Key, pair.Value);
                }
            }
        }

        private static void TransferContextDataToLogEventProperties(LogEventInfo log)
        {
            foreach (var contextItemName in MappedDiagnosticsLogicalContext.GetNames())
            {
                var key = contextItemName;
                if (log.Properties.ContainsKey(contextItemName))
                {
                    key = "log_context_" + contextItemName;
                }

                if (!log.Properties.ContainsKey(key))
                {
                    var value = MappedDiagnosticsLogicalContext.Get(contextItemName);
                    log.Properties.Add(key, value);
                }
            }
        }

        private static void TransferScopeDataToLogEventProperties(LogEventInfo log)
        {
            var nestedContexts = NestedDiagnosticsLogicalContext.GetAllObjects();
            var topScope = nestedContexts?.FirstOrDefault() as NestedContext;
            if(topScope == null) return;

            
            foreach (var property in topScope.Properties)
            {
                var key = property.Key;
                if (log.Properties.ContainsKey(key))
                {
                    key = "log_nested_context_" + key;
                }

                if (!log.Properties.ContainsKey(key))
                {
                    log.Properties.Add(key, property.Value);
                }
            }
        }

        private static readonly TypeInfo DictType = typeof(IDictionary).GetTypeInfo();

        private static bool IsDictionary(object logProperties)
        {
            return DictType.IsAssignableFrom(logProperties.GetType().GetTypeInfo());
        }        
    }
}
