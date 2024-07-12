using AiaTelegramBot.Logging;
using AiaTelegramBot.TG_Bot;

namespace AiaTelegramBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 1) 
            {
                if (!string.IsNullOrEmpty(args[0]))
                {
                    BotEntity be = new BotEntity();
                    be.StartBot(args[0]);
                    Console.ReadKey();
                    return;
                }
            }
            BotLogger.Log($"Для запуска бота используйте команду:\n./{System.AppDomain.CurrentDomain.FriendlyName} [токен бота]", BotLogger.LogLevels.ERROR);
        }
    }
}
