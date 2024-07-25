using AiaTelegramBot.Logging;
using AiaTelegramBot.TG_Bot;
using AiaTelegramBot.TG_Bot.models;
using Newtonsoft.Json;
using Npgsql;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Telegram.Bot.Types;
using Telegram.Bots.Requests;

namespace AiaTelegramBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string path = "E:\\Pet-projects\\C#\\AiaTelegramBot\\AiaTelegramBot\\bin\\Release\\net6.0\\publish\\linux-x64\\actions\\shit\\123.json";
            //string content = System.IO.File.ReadAllText(path, Encoding.UTF8);
            //Console.WriteLine($"CONTENT:\n---------------\n{content}\n---------------");
            //BotAction? deserialized = JsonConvert.DeserializeObject<BotAction>(content);
            //string serialized = JsonConvert.SerializeObject(deserialized, Formatting.Indented);
            //Console.WriteLine($"SERIALIZED:\n---------------\n{serialized}\n---------------");
            //return;
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().Location);


            if (args.Length == 1)
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
