using AiaTelegramBot.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Newtonsoft.Json;
using AiaTelegramBot.TG_Bot.models;
using Telegram.Bot.Exceptions;
using Polly;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace AiaTelegramBot.TG_Bot
{
    internal class BotEntity
    {
        public string? BotID { get; protected set; } = string.Empty;
        public string? BotName { get; protected set; } = string.Empty;
        protected StatUnit? statContiner = null;
        public static List<BotAction> BotActions = new List<BotAction>();
        public static List<string> WhiteList = new List<string>();

        protected BotConfigurationUnit RunningConfiguration = new BotConfigurationUnit()
        {
            StoreConversationStory = false,
            StoreNewUsernames = false,
            StoreLogs = false
        };

        protected delegate bool FileLogger(string message, string location, BotLogger.LogLevels logLevel);
        protected delegate void ScreenLogger(string message, BotLogger.LogLevels logLevel);
        protected ITelegramBotClient? botClient;
        protected CancellationTokenSource cancellationToken = new CancellationTokenSource();
        protected ReceiverOptions Botoptions = new ReceiverOptions() { AllowedUpdates = { }, ThrowPendingUpdates = true };

        protected List<string> Usernames = new List<string>();
        protected async void StoreUsername(Telegram.Bot.Types.Update update)
        {
            if (!RunningConfiguration.StoreNewUsernames) return;
            if (update.Message?.From != null)
            {
                var msg = update.Message.From;
                string usernameString = $"@{msg.Username ?? "[не определено]"} ({msg.Id}): {msg.FirstName ?? "[не определено]"} {msg.LastName ?? "[не определено]"}\n";
                try
                {
                    if (File.Exists($"{RunningConfiguration.WorkingDirectory}/usernames"))
                    {
                        string[] lines = File.ReadAllLines($"{RunningConfiguration.WorkingDirectory}/usernames");
                        if (lines.FirstOrDefault(x => x == usernameString) != null)
                        {
                            Log($"Боту написал новый пользователь, которого бот раньше не встречал!\n\t{usernameString}", BotLogger.LogLevels.INFO);
                            await File.AppendAllTextAsync($"{RunningConfiguration.WorkingDirectory}/usernames", usernameString);
                        }
                        return;
                    }
                    Log($"Боту написал новый пользователь, которого бот раньше не встречал!\n\t{usernameString}", BotLogger.LogLevels.INFO);
                    await File.AppendAllTextAsync($"{RunningConfiguration.WorkingDirectory}/usernames", usernameString);
                }
                catch (Exception fileWriteException)
                {
                    Log($"Боту написал новый пользователь, которого бот раньше не встречал!\n\t{usernameString}\n" +
                        $"Однако, его не удалось созранить в файл \"{RunningConfiguration.WorkingDirectory}/usernames\" по следующей причине:\n" +
                        $"{fileWriteException.Message}", BotLogger.LogLevels.ERROR);
                }
            }
        }
        protected async void StoreConversation(Telegram.Bot.Types.Update update)
        {
            if (!RunningConfiguration.StoreConversationStory) return;
            try
            {
                if (update.Message == null) return;
                if (!Directory.Exists($"{RunningConfiguration.WorkingDirectory}/CHATS/")) Directory.CreateDirectory($"{RunningConfiguration.WorkingDirectory}/CHATS/");
                string conversationPath = $"{RunningConfiguration.WorkingDirectory}/CHATS/{update.Message.Chat.Title?.Replace(" ", "_").Replace("/", "") ?? update.Message.Chat.Id.ToString()}";
                await File.AppendAllTextAsync(conversationPath, $"[{update.Message.Date.ToLocalTime()}][{BotName}][@{update.Message?.From?.Username ?? "???"} ({update.Message?.From?.Id.ToString() ?? "???"})]: {update.Message?.Text ?? "[пустое сообщение]"}\n");
            }
            catch (Exception fileCretingException)
            {
                Log($"Не удалось сохранить сообщение от пользователя по следующей причине:\n{fileCretingException.Message}", BotLogger.LogLevels.ERROR);
            }
        }
        protected string? GetBotID(string token)
        {
            return CoreFunctions.GetHashString(token);
        }
        public async void StartBot(string token)
        {
            Log("Предварительная настройка Телеграмм-бота", BotLogger.LogLevels.INFO);
            if (!UpdateBotConfiguration()) return;
            if (!UpdateBotActions()) return;
            cancellationToken = new CancellationTokenSource();
            botClient = new TelegramBotClient(token);
            botClient.StartReceiving(
               HandleUpdateAsync,
               HandleErrorAsync,
               Botoptions,
               cancellationToken.Token
               );
            Log("Верификация конфигурации успешна", BotLogger.LogLevels.SUCCESS);
            Log("Производится проверка бота...", BotLogger.LogLevels.INFO);
            try
            {
                Telegram.Bot.Types.User botInfo = await botClient.GetMeAsync(cancellationToken: cancellationToken.Token);
                statContiner = new StatUnit(BotName, BotID, DateTime.Now.ToLocalTime());
                BotName = botInfo.Username ?? "[не определено]";
                BotID = GetBotID(token);
                Log($"Успешно запущен бот {BotName}!", BotLogger.LogLevels.SUCCESS);
                Log($"Идентификатор бота: {BotID}", BotLogger.LogLevels.INFO);
            }
            catch (ApiRequestException botStartException)
            {
                StopBot();
                Log($"Не удалось запустить телеграмм-бота с указанным токеном:\n{botStartException.Message}", BotLogger.LogLevels.CRITICAL);
            }
        }
        public void StopBot()
        {
            cancellationToken.Cancel();
            botClient = null;
        }
        public void RestartBot(string token)
        {
            StopBot();
            StartBot(token);
        }
        protected async Task HandleUpdateAsync(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
            statContiner.ReceivedUpdatesCount++;
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
            StoreUsername(update);
            StoreConversation(update);
            switch (update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Unknown:
                    Log($"[{BotName}][@{update.Message?.From?.Username ?? "???"} ({update.Message?.From?.Id.ToString() ?? "???"})]: {update.Message?.Text ?? "[UNKNOWN_UPDATE_TYPE]"}", BotLogger.LogLevels.MESSAGE);
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    if (update.Message?.Text == null) return;
                    statContiner.ReceivedMessagesCount++;
                    Log($"[{BotName}][{update.Message?.From?.FirstName} {update.Message?.From?.LastName} (@{update.Message?.From?.Username ?? "???"} - {update.Message?.From?.Id.ToString() ?? "???"})]: {update.Message?.Text ?? "[пустое сообщение]"}", BotLogger.LogLevels.MESSAGE);
                    string? msg = update.Message?.Text?.Trim();
                    if (string.IsNullOrEmpty(msg)) return;
                    bool userIsAdmin = false;
                    if (RunningConfiguration.Whitelist.Count > 0)
                    {
                        //if (RunningConfiguration.Whitelist.First(x => x == $"{update.Message?.From?.Id}") != null)
                        if (RunningConfiguration.Whitelist.FirstOrDefault(x => x == $"{update.Message?.From?.Id}") != null)
                        {
                            userIsAdmin = true;
                        }
                    }
                    for (int i = 0; i < BotActions.Count; i++)
                    {
                        if (msg.ToLower().StartsWith(BotActions[i].Keyword.ToLower()))
                        {
                            Log($"[{BotName}]: Распознана команда \"{BotActions[i].Keyword}\"", BotLogger.LogLevels.COMMAND);
                            if (BotActions[i].IsAdmin)
                            {
                                if (update.Message?.From == null) return;
                                if (userIsAdmin)
                                {
                                    if (!BotActions[i].IsActive)
                                    {
                                        SendAndLogMessage(client, update, token, "К сожалению, на данный момент эта команда *не активна*",
                                            BotLogger.LogLevels.WARNING, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                        statContiner.SentMessagesCount++;
                                        return;
                                    }
                                    BotLogger.Log($"Запущена команда администратора ({update.Message.From.Id}/{BotActions[i].Name})",
                                        BotLogger.LogLevels.INFO, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                    await BotActions[i].RunAction(client, update, token, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                    statContiner.SentMessagesCount++;
                                    return;
                                }
                                SendAndLogMessage(client, update, token, $"Обнаружена попытка запуска команды администратора пользователем, который им не является: {update.Message.From.Id} (@{update.Message.From.Username}).\n" +
                                    $"`Этот инцидент будет отправлен на рассмотрение активным администраторам`", BotLogger.LogLevels.WARNING, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                statContiner.SentMessagesCount++;
                                return;
                            }
                            if (!BotActions[i].IsActive)
                            {
                                SendAndLogMessage(client, update, token, "К сожалению, на данный момент эта команда *не активна*",
                                    BotLogger.LogLevels.WARNING, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                statContiner.SentMessagesCount++;
                                return;
                            }
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                            BotActions[i].RunAction(client, update, token, logPath: $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            statContiner.SentMessagesCount++;
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                            //await BotActions[i].RunAction(client, update, token, logPath: $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            return;
                        }
                    }
                    //if (update.Message == null) return;

                    foreach (var command in new string[]
                    {
                        "/get config",
                        "/get whitelist",
                        "/update actions",
                        "/update whitelist",
                        "/update config",
                        "/stats"
                    })
                    {
                        if (msg.ToLower().StartsWith(command.ToLower()))
                        {
                            if (!userIsAdmin)
                            {
                                SendAndLogMessage(client, update, token, $"Обнаружена попытка запуска команды администратора пользователем, который им не является.\n" +
                                    $"`Этот инцидент будет отправлен на рассмотрение активным администраторам`",
                                    BotLogger.LogLevels.WARNING,
                                    $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                return;
                            }
                        }
                    }
                    switch (msg.ToLower())
                    {
                        case "/get actions":
                        case "/help":
                            statContiner.SentMessagesCount++;
                            GetHelpMessage(client, update, token, userIsAdmin);
                            return;
                        case "/get config":
                            SendAndLogMessage(client, update, token, $"Актуальная конфигурация бота:\n{RunningConfiguration.GetBotConfiguration()}",
                                BotLogger.LogLevels.WARNING,
                                $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            break;
                        case "/get whitelist":
                            SendAndLogMessage(client, update, token, $"{RunningConfiguration.GetBotWhiteList()}",
                                BotLogger.LogLevels.WARNING,
                                $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            break;
                        case "/update actions":
                            if (!UpdateBotActions())
                            {
                                SendAndLogMessage(client, update, token, $"Не удалось обновить список действий бота",
                                    BotLogger.LogLevels.WARNING,
                                    $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                return;
                            }
                            SendAndLogMessage(client, update, token, $"Cписок действий бота успешно обновлен. активных действий: `{BotActions.Count}`.\nИспользуйте команду `/help`, чтобы получить актуальный список действий",
                                BotLogger.LogLevels.WARNING,
                                $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            break;
                        case "/update whitelist":
                            if (!RunningConfiguration.UpdateBotWhiteList())
                            {
                                SendAndLogMessage(client, update, token, $"Не удалось обновить белый список бота",
                                    BotLogger.LogLevels.WARNING,
                                    $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                return;
                            }
                            SendAndLogMessage(client, update, token, $"Белый список бота успешно обновлен!\n{RunningConfiguration.GetBotWhiteList()}",
                                BotLogger.LogLevels.WARNING,
                                $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            break;
                        case "/update config":
                            if (!UpdateBotConfiguration())
                            {
                                SendAndLogMessage(client, update, token, $"Не удалось обновить конфигурацию бота",
                                    BotLogger.LogLevels.WARNING,
                                    $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                return;
                            }
                            SendAndLogMessage(client, update, token, $"Конфигурация бота успешно обновлена:\n{RunningConfiguration.GetBotConfiguration()}",
                                BotLogger.LogLevels.WARNING,
                                $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            break;
                        case "/stats":
                            SendAndLogMessage(client, update, token, $"{statContiner.GetBotStats()}",
                            BotLogger.LogLevels.COMMAND,
                            $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            break;
                    }
                    break;
            }
        }
        protected async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log(exception, BotLogger.LogLevels.CRITICAL);
            Log($"Попытка перезапуска бота @{BotName ?? "[неизвестно]"}", BotLogger.LogLevels.INFO);
            //await Task.Run(() => RestartBot(Environment.GetCommandLineArgs()[1]));
            if (botClient != null) Log($"Успешный перезапуск бота @{BotName}", BotLogger.LogLevels.SUCCESS);
            else Log($"Не удалось перезапустить бота @{BotName}. Инициировано завершение процесса", BotLogger.LogLevels.CRITICAL);
        }
        protected async void SendAndLogMessage(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string runningMessage, BotLogger.LogLevels logLevel, string? logPath)
        {
            if (statContiner != null) statContiner.SentMessagesCount++;

            if (update.Message == null || string.IsNullOrEmpty(runningMessage)) return;
            BotLogger.Log($"{runningMessage}",
                logLevel, logPath);
            await client.SendTextMessageAsync(update.Message.Chat.Id, $"{runningMessage}",
                cancellationToken: token,
                replyToMessageId: update.Message.MessageId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
        protected void Log(object? message, BotLogger.LogLevels logLevel, bool firstStartup = false)
        {
            //ScreenLoggerHandler.Invoke($"{message ?? "???"}", logLevel);
            //if (!RunningConfiguration.StoreLogs) return;
            //bool? successFileLog = FileLoggerHandler?.Invoke($"{JsonConvert.SerializeObject(message) ?? "???"}",
            //    $"{RunningConfiguration.WorkingDirectory}/latest.log",
            //    logLevel);
            if (RunningConfiguration.StoreLogs && !string.IsNullOrEmpty(RunningConfiguration.WorkingDirectory))
            {
                BotLogger.Log($"{message}", logLevel, $"{RunningConfiguration.WorkingDirectory}/latest.log");
            }
            else
            {
                BotLogger.Log($"{message}", logLevel);
            }
        }
        public bool UpdateBotActions()
        {
            if (RunningConfiguration.ActionsDirectory == null)
            {
                Log($"Не удалось получить список действий, т.к. не задан путь к директории с ними", BotLogger.LogLevels.ERROR);
                return false;
            }
            Log($"Начат процесс обновления списка действий.\n\tЧтение из директории \"{RunningConfiguration.ActionsDirectory}\"", BotLogger.LogLevels.INFO);
            List<string> configLocations = Directory.EnumerateFiles(RunningConfiguration.ActionsDirectory, "*.json", SearchOption.AllDirectories).ToList<string>();
            List<BotAction> botActions_buffer = new List<BotAction>();
            try
            {
                foreach (string filename in configLocations)
                {
                    string content = System.IO.File.ReadAllText(filename);
                    try
                    {
                        BotAction? targetAction = JsonConvert.DeserializeObject<BotAction>(content);
                        if (targetAction == null)
                        {
                            Log($"Не удалось десериализовать действие {filename}:\n", BotLogger.LogLevels.ERROR);
                            Log($"---------- Содержание файла ----------\n{content}\n--------------------------------------", BotLogger.LogLevels.INFO);
                            continue;
                        }
                        if (targetAction.IsActive)
                        {
                            botActions_buffer.Add(targetAction);
                            Log($"Добавлено новое действие:\n{targetAction.GetActionInfo()}", BotLogger.LogLevels.INFO);
                        }
                        else
                        {
                            Log($"Действие:\n{targetAction.Name} проигнорировано, т.к. помечено как неактивное", BotLogger.LogLevels.WARNING);
                        }
                    }
                    catch (Exception entityCreatingException)
                    {
                        Log($"Не удалось получить список действий:\n{entityCreatingException.Message} ", BotLogger.LogLevels.ERROR);
                    }
                }
            }
            catch (Exception fileReadException)
            {
                Log($"Не удалось получить список действий:\n{fileReadException.Message} ", BotLogger.LogLevels.ERROR);
                return false;
            }
            Log($"Успешно изменен список действий: {BotActions.Count} => {botActions_buffer.Count}", BotLogger.LogLevels.SUCCESS);
            BotActions = botActions_buffer.OrderBy(x => x.Description).ToList();
            return true;
        }
        protected bool UpdateBotConfiguration()
        {
            //DoTo: Сделать не фиксированный путь к файлу, а передавать его в параметре
            Log("Производится попытка обновления конфигурации бота из файла ./config.json ...", BotLogger.LogLevels.INFO);
            try
            {
                if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}config.json"))
                {
                    string? lines = System.IO.File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}config.json");
                    if (string.IsNullOrEmpty(lines))
                    {
                        Log($"Файл {AppDomain.CurrentDomain.BaseDirectory}config.json является пустым файлом", BotLogger.LogLevels.ERROR);
                        CreateTemplateConfig();
                        return false;
                    }
                    BotConfigurationUnit? targetUnit = JsonConvert.DeserializeObject<BotConfigurationUnit>(lines);
                    if (targetUnit == null)
                    {
                        Log($"Не удалось прочитать конфигурацию из файла \"{AppDomain.CurrentDomain.BaseDirectory}config.json\". \n\tПроверьте правильность структуры файла и повторите попытку", BotLogger.LogLevels.CRITICAL);
                        return false;
                    }
                    RunningConfiguration = targetUnit;
                    Log($"Успешно применена следующая конфигурация бота:\n{RunningConfiguration.GetBotConfiguration()}", BotLogger.LogLevels.SUCCESS);
                    return true;
                }
                CreateTemplateConfig();
            }
            catch (Exception fileWriteException)
            {
                Log($"Произошла ошибка при создании шаблонного конфигурационного файла:\n\t{fileWriteException.Message}", BotLogger.LogLevels.CRITICAL);
            }
            return false;
        }
        protected void CreateTemplateConfig()
        {
            string bufferConfig = JsonConvert.SerializeObject(new BotConfigurationUnit());
            bufferConfig = bufferConfig.Replace(",", ",\n\t");
            bufferConfig = bufferConfig.Replace("{", "{\n\t");
            bufferConfig = bufferConfig.Replace("}", "\n}");
            bufferConfig = bufferConfig.Replace(":", ": ");
            System.IO.File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}config.json", bufferConfig);
            Log($"Создан шаблонный конфигурационный файл ./config.json со следующим содержанием:\n{bufferConfig}", BotLogger.LogLevels.SUCCESS);
            Log($"Отредактируйте файл и перезапустите бота командой:" +
                $"{System.AppDomain.CurrentDomain.BaseDirectory}{System.AppDomain.CurrentDomain.FriendlyName} [токен бота]", BotLogger.LogLevels.INFO);
        }
        protected async void GetHelpMessage(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, bool includeAdmin)
        {
            if (update.Message == null) return;
            string HelpMessage = "Доступные вам команды:\n";
            if (BotActions.FirstOrDefault(x => x.IsAdmin == includeAdmin) == null)
            {
                await client.SendTextMessageAsync(update.Message.Chat.Id, $"{HelpMessage}Нет доступных команд",
                    cancellationToken: token,
                    replyToMessageId: update.Message.MessageId,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                return;
            }
            if (RunningConfiguration.HideInactiveActions)
            {
                BotActions.ForEach(action =>
                {
                    if ((action.IsAdmin == includeAdmin) && action.IsActive)
                    {
                        HelpMessage +=
                            $"✏️ `{action.Keyword}`\n" +
                            $"    {action.Description ?? "(нет описания действия)"}\n\n";
                    }
                });
            }
            else
            {
                BotActions.ForEach(action =>
                {
                    if (action.IsAdmin == includeAdmin)
                    {
                        HelpMessage +=
                            $"{(action.IsActive ? $"✅" : "⛔️")} `{action.Keyword}`\n" +
                            $"    {action.Description ?? "(нет описания действия)"}\n\n";
                    }
                });
            }
            await client.SendTextMessageAsync(update.Message.Chat.Id, $"{HelpMessage}",
                                cancellationToken: token,
                                replyToMessageId: update.Message.MessageId,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
    }
}
