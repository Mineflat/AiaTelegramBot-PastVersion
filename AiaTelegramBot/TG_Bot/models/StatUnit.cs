using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiaTelegramBot.TG_Bot.models
{
    internal class StatUnit
    {
        public string? BotName { get; protected set; }
        public string? BotID { get; protected set; }
        public DateTime StartTime { get; protected set; } = DateTime.Now;
        public double ReceivedUpdatesCount { get; set; } = 0;
        public double ReceivedMessagesCount { get; set; } = 0;
        public double SentMessagesCount { get; set; } = 0;

        public StatUnit(string? botName, string? botID, DateTime startTime)
        {
            BotName = botName;
            BotID = botID;
            StartTime = startTime;
        }

        public string GetBotStats()
        {
            Console.WriteLine(1);
            if (ReceivedMessagesCount == 0)
            {
                return $"`Статистика`" +
                    $"    - *Имя бота:* {BotName}\n" +
                    $"    - *Идентификатор бота:* {BotID}\n" +
                    $"    - *Время запуска:* {StartTime.ToLocalTime()}\n" +
                    $"    - *Обновлений получено:* {ReceivedUpdatesCount}\n" +
                    $"    - *Сообщений получено:* {ReceivedMessagesCount}\n" +
                    $"    - *Сообщений отправлено:* {SentMessagesCount}\n" +
                    $"    - *Соотношение сообщений (отправлено/получено):* 0";
            }
            Console.WriteLine(SentMessagesCount / ReceivedMessagesCount);
            return $"`Статистика`\n" +
                $"    - *Имя бота:* {BotName}\n" +
                $"    - *Идентификатор бота:* {BotID}\n" +
                $"    - *Время запуска:* {StartTime.ToLocalTime()}\n" +
                $"    - *Обновлений получено:* {ReceivedUpdatesCount}\n" +
                $"    - *Сообщений получено:* {ReceivedMessagesCount}\n" +
                $"    - *Сообщений отправлено:* {SentMessagesCount}\n" +
                $"    - *Соотношение сообщений (отправлено/получено):* {SentMessagesCount / ReceivedMessagesCount}";
        }
    }
}
