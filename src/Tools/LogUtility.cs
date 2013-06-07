/*
 * Revisions:
 *  # NB - 10/28/2008 3:33:46 PM - Source File Created
 */
using System;
using System.Diagnostics;

namespace fmDotNet.Tools
{
    public static class LogUtility
    {
        public const string LogSource = "fmDotNet";

        /// <summary>
        /// Writes an event to the Event Log.
        /// </summary>
        /// <param name="ex">The exception that was caught.</param>
        /// <param name="type">The type of entry to create.</param>
        public static Boolean WriteEntry(Exception ex, EventLogEntryType type)
        {
            try
            {
                String logName = "Application";
                EventLog log;

                if (EventLog.SourceExists(LogSource) == false)
                {
                    EventLog.CreateEventSource(new EventSourceCreationData(LogSource, logName));
                }

                log = new EventLog(logName, Environment.MachineName, LogSource);

                // append all the data together
                String logEntery = String.Empty;

                logEntery += "Message: " + ex.Message + Environment.NewLine;
                logEntery += "Stack Trace: " + ex.StackTrace + Environment.NewLine;
                logEntery += "Source: " + ex.Source + Environment.NewLine + Environment.NewLine;

                // write to the event log
                log.WriteEntry(logEntery, type);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}