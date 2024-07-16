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
            BotName = string.IsNullOrEmpty(botName) ? "(не определено)": botName;
            BotID = string.IsNullOrEmpty(botID) ? "(не определено)" : botID; 
            StartTime = startTime;
        }
        public string GetBotStats()
        {
            string statsMessage = $"`Статистика`\n" +
                    $"✖️ *Имя бота:* {BotName}\n" + // 42
                    $"✖️ *Идентификатор бота:*\n```\n{BotID}\n```\n" +
                    $"✖️ *Время запуска:* {StartTime.ToLocalTime()}\n" +
                    $"✖️ *Обновлений получено:* {ReceivedUpdatesCount}\n" +
                    $"✖️ *Сообщений получено:* {ReceivedMessagesCount}\n" +
                    $"✖️ *Сообщений отправлено:* {SentMessagesCount}\n" +
                    $"✖️ *Соотношение сообщений (отправлено/получено):* 0";
            if (ReceivedMessagesCount != 0)
            {
                statsMessage = $"`Статистика`\n" +
                $"✖️ *Имя бота:* {BotName}\n" +
                $"✖️ *Идентификатор бота:*\n```\n{BotID}\n```\n" +
                $"✖️ *Время запуска:* {StartTime.ToLocalTime()}\n" +
                $"✖️ *Обновлений получено:* {ReceivedUpdatesCount}\n" +
                $"✖️ *Сообщений получено:* {ReceivedMessagesCount}\n" +
                $"✖️ *Сообщений отправлено:* {SentMessagesCount}\n" +
                $"✖️ *Соотношение сообщений (отправлено/получено):* {SentMessagesCount / ReceivedMessagesCount}";
            }
            return statsMessage.Replace("_", "\\_");
        }
    }
}
