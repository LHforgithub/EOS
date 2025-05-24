using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    public sealed class LogTool
    {
        public LogTool()
        {
        }
        public static LogTool Instance { get => Nested.instance; }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly LogTool instance = new();
        }
        public static bool IsInitialized { get => Nested.instance != null; }

        private List<string> LogMessages { get; } = [];
        private List<string> GettedLogs { get; } = [];

        public void Log(string log)
        {
            LogMessages.Add(log);
        }
        public void Log(Exception exception)
        {
            LogMessages.Add(exception.ToString());
        }
        public void Log(int log)
        {
            LogMessages.Add(log.ToString());
        }
        public string GetLastLog()
        {
            if (LogMessages.Count > 0)
            {
                return LogMessages[~1];
            }
            return string.Empty;
        }
        public string GetAllLogs()
        {
            return string.Join(Environment.NewLine, LogMessages);
        }
        public string GetLog(int index)
        {
            if (index >= 0 && index < LogMessages.Count)
            {
                return LogMessages[index];
            }
            return string.Empty;
        }
        public void ClearLogs()
        {
            LogMessages.Clear();
        }
    }
}
