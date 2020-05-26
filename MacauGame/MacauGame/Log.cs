using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauGame
{
    public static class Log
    {
        public class LogMessage
        {
            public DateTime Date { get; set; }
            public string Location { get; set; }
            public LogSeverity Severity { get; set; } 
            public string Message { get; set; }
            public Exception Exception { get; set; }
            public LogMessage(string location, LogSeverity sev, string message, Exception ex)
            {
                Date = DateTime.Now;
                Location = location;
                Severity = sev;
                Message = message;
                Exception = ex;
            }
            public LogMessage(string location, LogSeverity sev, string message) : this(location, sev, message, null) { }
            public LogMessage(string location, Exception ex, string msg = null) : this(location, LogSeverity.Error, msg, ex) { }
            
            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append($"[{Date:hh:mm:ss.fff} ");
                builder.Append($"{Severity.ToString().ToUpper()}] ");
                builder.Append($"{Location}: ");
                int length = builder.Length;
                string lPadding = new string(' ', length);
                string msg;
                if (Message == null)
                {
                    msg = Exception.ToString();
                }
                else if (Exception == null)
                {
                    msg = Message;
                } else
                {
                    msg = Message + "\r\nException: " + Exception.ToString();
                }
                msg = msg.Replace("\r\n", "\r\n" + lPadding);
                builder.Append(msg);
                return builder.ToString();
            }
        }
        public enum LogSeverity
        {
            Trace,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }
    
        static ConsoleColor getColor(LogSeverity logSeverity)
        {
            switch(logSeverity)
            {
                case LogSeverity.Trace:
                    return ConsoleColor.DarkGray;
                case LogSeverity.Debug:
                    return ConsoleColor.Gray;
                case LogSeverity.Info:
                    return ConsoleColor.White;
                case LogSeverity.Warning:
                    return ConsoleColor.Yellow;
                case LogSeverity.Error:
                    return ConsoleColor.Red;
                case LogSeverity.Fatal:
                    return ConsoleColor.DarkRed;
                default:
                    return ConsoleColor.Magenta;
            }
        }

        static object padlock = new object();

        public static string GetFolderPath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CheAle14-Macau", "logs");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
        public static string GetFileName(DateTime date) => $"{date:yyyy-MM-dd}.txt";
        public static string GetFilePath(DateTime date) => Path.Combine(GetFolderPath(), GetFileName(date));

        public static void LogMsg(LogMessage msg)
        {
            lock(padlock)
            {
                Console.ForegroundColor = getColor(msg.Severity);
                var content = msg.ToString();
                Console.WriteLine(content);

                var path = GetFilePath(msg.Date);
                try
                {
                    File.AppendAllText(path, content + "\r\n");
                } catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"====== FAILED TO LOG TO FILE ======");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine($"====== FAILED TO LOG TO FILE ======");
                }
            }
        }

        static string fileName(string s) => s?.Substring(s.LastIndexOf('\\', '/') + 1) ?? "na";

        public static string FormatStackFrame(StackFrame frame)
        {
            return 
                (frame.GetMethod()?.Name ?? "na") + 
                "::" +
                (fileName(frame.GetFileName())) +
                "#" + frame.GetFileLineNumber();
        }

        public static string FindLocation(int frames = 2)
        { // 0 = this function; 1 == that caller (the ones below); 2 == the one we want
            var stack = new StackTrace(true);
            var frame = stack.GetFrame(frames); 
            return FormatStackFrame(frame);
        }

        public static void Trace(string location, string message)
        {
#if DEBUG
            LogMsg(new LogMessage(location, LogSeverity.Trace, message));
#endif
        }

        public static void Trace(string message)
        {
#if DEBUG
            LogMsg(new LogMessage(FindLocation(), LogSeverity.Trace, message));
#endif
        }
        public static void Debug(string location, string message) => LogMsg(new LogMessage(location, LogSeverity.Debug, message));
        public static void Debug(string message) => LogMsg(new LogMessage(FindLocation(), LogSeverity.Debug, message));
        public static void Info(string location, string message) => LogMsg(new LogMessage(location, LogSeverity.Info, message));
        public static void Info(string message) => LogMsg(new LogMessage(FindLocation(), LogSeverity.Info, message));
        public static void Warn(string location, string message) => LogMsg(new LogMessage(location, LogSeverity.Warning, message));
        public static void Warn(string message) => LogMsg(new LogMessage(FindLocation(), LogSeverity.Warning, message));
        public static void Error(string location, Exception ex) => LogMsg(new LogMessage(location, LogSeverity.Error, null, ex));
        public static void Error(string location, string message) => LogMsg(new LogMessage(location, LogSeverity.Error, message));

    }
}
