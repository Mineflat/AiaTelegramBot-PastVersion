using AiaTelegramBot.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace AiaTelegramBot.TG_Bot
{
    internal partial class BotEntity
    {
        /// Перед тем, как хуесосить меня за решение передать объет класса в функцию, 
        /// прочитайте следующую документацию к языку C#:
        /// https://learn.microsoft.com/ru-ru/dotnet/csharp/fundamentals/types/
        /// Объект класса является ССЫЛОЧНЫМ типом. 
        /// Это означает, что вся херня из класса BotEntity (BotEntity.cs),
        /// все его поля, методы и структуры НЕ будут скопированы и не будут занимать в 2 раза больше места в ОЗУ,
        /// нежели это было бы при копировании объекта (а именно эта мысль возникнет в голове толкового разработчика).
        /// В метод передается ссылка на этот объект и, следовательно, все работает так же, как и в более ранних версиях проекта.
        /// 
        /// Если есть предложения по оптимизации - велком на гитхаб проекта:
        /// https://github.com/Mineflat/AiaTelegramBot-MainVersion
        /// Или в мою телегу:
        /// https://t.me/ElijahKamsky


        #region HELPS
        #region HELPS
        /// <summary>
        /// Отправить список команд, доступных пользователю
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>

        protected static async Task GetHelp(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.GetHelpMessage(client, update, token, false);
        }

        /// <summary>
        /// Отправить писок команд, доступных пользователю, если тот-администратор
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetAdminHelp(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.GetHelpMessage(client, update, token, true);
        }

        #endregion

        #region API

        /// <summary>
        /// Получить и отправить пользователю статус API-сервиса
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetApi(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.SendAndLogMessage(client, update, token, $"{bot.API_URI_FORMATTED_MD_STATUS}",
                BotLogger.LogLevels.COMMAND,
                $"{logPath}");
        }

        /// <summary>
        /// Остановить работу API-сервиса 
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task StopApi(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.SendAndLogMessage(client, update, token, "Инициирую *остановку* сервиса API...",
                BotLogger.LogLevels.COMMAND,
                $"{logPath}");
            bot.StopApi();
        }

        /// <summary>
        /// Перезапустить API-сервис
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task RestartApi(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.SendAndLogMessage(client, update, token, "Инициирую *перезапуск* сервиса API...",
                BotLogger.LogLevels.COMMAND,
                $"{logPath}");
            // Пока вы пишете код, который контролирует фабрику асинхронных лисенеров, я тупо перезапускаю API-сервис дважды и живу спокойно :)
            // Да, это откровенно конченое решение, да еще и работает с задержкой от 500 мс, но оно РАБОТАЕТ
            bot.RestartAPI();
            await Task.Delay(1000);
            bot.RestartAPI();
        }

        #endregion

        #region DEBUG
        /// <summary>
        /// ПОтправить статистику работы бота
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetStats(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if ((update.Message == null) || (bot.statContiner == null)) return;
            await client.SendTextMessageAsync(update.Message.Chat.Id, $"{bot.statContiner?.GetBotStats()}",
                cancellationToken: token,
                replyToMessageId: update.Message.MessageId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }

        /// <summary>
        /// Отправить подтверждение активности бота
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetPing(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if (update.Message == null) return;
            await client.SendTextMessageAsync(update.Message.Chat.Id, "✅ PONG",
                cancellationToken: token,
                replyToMessageId: update.Message.MessageId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }

        /// <summary>
        /// Отправить список имен из списка действий бота
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetActionNames(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if (update.Message == null) return;
            string replyMsg = $"Список всех действий, о которых знает бот:\n";
            BotActions.ForEach(x => replyMsg += $"✅ {(string.IsNullOrEmpty(x.Name) ? "[пусто]\n" : $"{x.Name}\n")}");
            await client.SendTextMessageAsync(update.Message.Chat.Id, replyMsg,
                cancellationToken: token,
                replyToMessageId: update.Message.MessageId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }

        #endregion

        #region UPDATES

        /// <summary>
        /// Обновить список идентификаторов пользователей из белого списка
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetUpdateWhitelist(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if (!bot.RunningConfiguration.UpdateBotWhiteList())
            {
                bot.SendAndLogMessage(client, update, token, 
                    $"Не удалось обновить белый список бота",
                    BotLogger.LogLevels.WARNING,
                    $"{logPath}");
                return;
            }
            bot.SendAndLogMessage(client, update, token, 
                $"Белый список бота успешно обновлен!\n{bot.RunningConfiguration.GetBotWhiteList()}",
                BotLogger.LogLevels.WARNING,
                $"{logPath}");
        }

        /// <summary>
        /// Обновить конфигурацию бота
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetUpdateConfiguration(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if (!bot.UpdateBotConfiguration())
            {
                bot.SendAndLogMessage(client, update, token, $"Не удалось обновить конфигурацию бота",
                    BotLogger.LogLevels.WARNING,
                    $"{logPath}");
                return;
            }
            bot.SendAndLogMessage(client, update, token, $"Конфигурация бота успешно обновлена:\n{bot.RunningConfiguration.GetBotConfiguration()}",
                BotLogger.LogLevels.WARNING,
                $"{logPath}");
        }

        /// <summary>
        /// Обновить список действий бота
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetUpdateActions(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if (!bot.UpdateBotActions())
            {
                bot.SendAndLogMessage(client, update, token, $"Не удалось обновить список действий бота",
                    BotLogger.LogLevels.WARNING,
                    $"{logPath}");
                return;
            }
            bot.SendAndLogMessage(client, update, token, $"Cписок действий бота успешно обновлен.\nАктивных действий: `{BotActions.Count}`.\nИспользуйте команду `/help`, чтобы получить актуальный список действий",
                BotLogger.LogLevels.WARNING,
                $"{logPath}");
        }

        /// <summary>
        /// Обновить список переменных окружения
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetUpdateEnv(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            if (!bot.RunningConfiguration.ExportEnvVars())
            {
                bot.SendAndLogMessage(client, update, token, $"Не удалось обновить список переменных окружения бота",
                    BotLogger.LogLevels.WARNING,
                    $"{logPath}");
                return;
            }
            string envList = string.Empty;
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                envList += $"{de.Key} = {de.Value}";
            bot.SendAndLogMessage(client, update, token, $"Список переменных окружения бота успешно обновлен:\n{envList}",
                BotLogger.LogLevels.SUCCESS,
                $"{logPath}");
        }

        #endregion

        #region GET REQUESTS

        /// <summary>
        /// Отправить актуальную конфигурацию бота
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetRunningConfiguration(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.SendAndLogMessage(client, update, token, $"Актуальная конфигурация бота:\n{bot.RunningConfiguration.GetBotConfiguration()}", BotLogger.LogLevels.WARNING, $"{logPath}");
        }

        /// <summary>
        /// Отправить список идентификаторов пользователей из белого списка
        /// </summary>
        /// <param name="client">Клиент Телеграмм, от имени которого работает бот</param>
        /// <param name="update">Обновление, которое бот получил от сервера Телеграмм</param>
        /// <param name="token">Токен, используемый для отмены запроса в Polling-режиме</param>
        /// <param name="logPath">Путь к файлу логов, куда будет записан выхлоп функции</param>
        /// <param name="bot">Объект бота. Необходим для вызова нестатической функции внутри класса</param>
        protected static async Task GetRunningWhitelist(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath, BotEntity bot)
        {
            bot.SendAndLogMessage(client, update, token, $"{bot.RunningConfiguration.GetBotWhiteList()}", BotLogger.LogLevels.WARNING, $"{logPath}");
        }

        #endregion
        #endregion
    }
}
