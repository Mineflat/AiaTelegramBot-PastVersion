using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiaTelegramBot.Logging
{
    internal class BotLogger
    {
        public enum LogLevels
        {
            CRITICAL, ERROR, WARNING, INFO, MESSAGE, SUCCESS, COMMAND, SCRIPT
        }
        public static void Log(string message, LogLevels logLevel, string? location = null)
        {
            string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fffffff");
            switch (logLevel)
            {
                case LogLevels.CRITICAL:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    break;
                case LogLevels.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevels.WARNING:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogLevels.INFO:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevels.MESSAGE:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogLevels.SUCCESS:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogLevels.COMMAND:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    break;
                case LogLevels.SCRIPT:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }
            Console.Write($"[{logLevel}]");
            Console.ResetColor();
            Console.WriteLine($"[{timestamp}]: {message}");
            if (string.IsNullOrEmpty(location)) return;
            try
            {
                message = message.Trim();
                //message = message.Replace(",", ",\n\t").Replace("{", "{\n\t").Replace("}", "\n}").Replace("\\n", "\n").Replace("\\t", "\t").Trim();
                File.AppendAllText(location, $"[{logLevel}][{timestamp}]: {message}\n");
            }
            catch { }
        }
    }
}
