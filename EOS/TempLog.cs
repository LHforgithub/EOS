using System;
using System.Diagnostics;
using System.Text;

namespace EOS
{
    /// <summary>临时的记录器。</summary>
    public sealed class TempLog : Singleton<TempLog>
    {
        private static readonly object @lock = new();
        /// <summary>一个<see cref="StringBuilder"/>记录器，用于记录<see cref="EOS"/>部分抛出的异常</summary>
        public StringBuilder Logger { get; set; } = new StringBuilder();
        private StringBuilder OnceLogger { get; set; } = new StringBuilder();
        private bool isInit = false;
        /// <summary>记录器是否已经初始化</summary>
        public static bool IsInit => Instance.isInit;
        /// <summary>已包含多少条记录</summary>
        public static uint LogTimes { get; private set; } = 0;
        /// <summary>最后一条记录</summary>
        private string LastLine { get; set; } = string.Empty;
        /// <summary>初始化记录器</summary>
        public static void Init()
        {
            Instance.isInit = true;
            Instance.Logger = new StringBuilder();
            Instance.OnceLogger = new StringBuilder();
        }
        /// <summary>向<see cref="Logger"/>中添加新一行字符串</summary>
        public static void Log(string str)
        {
            lock (@lock)
            {
                if (Instance.Logger is null)
                {
                    Init();
                }
                var time = DateTime.Now;
                var stackTrace = new StackTrace().ToString();
                Instance.LastLine = $"[EOS Temp Log {LogTimes}][{time.ToShortDateString()} {time.ToLongTimeString()}] {str} \n" +
                    $"StackTrace :\n{stackTrace}";
                Instance.Logger.AppendLine(Instance.LastLine);
                Instance.OnceLogger.AppendLine(Instance.LastLine);
                LogTimes++;
                return;
            }
        }
        /// <summary>获取<see cref="Logger"/>中未被获取过的字符串。</summary>
        public static string GetOnce()
        {
            var str = Instance.OnceLogger.ToString();
            Instance.OnceLogger.Clear();
            return str;
        }
        /// <summary>获取<see cref="Logger"/>中最后被添加的字符串。</summary>
        public static string GetLast()
        {
            return Instance.LastLine;
        }
    }
}
