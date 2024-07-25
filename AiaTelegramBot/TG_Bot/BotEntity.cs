using AiaTelegramBot.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Newtonsoft.Json;
using AiaTelegramBot.TG_Bot.models;
using Telegram.Bot.Exceptions;
using Polly;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using AiaTelegramBot.API;
using System.Net;
using System.Text;
using System.Collections;
using System;
using static System.Collections.Specialized.BitVector32;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Text.Json;
using Telegram.Bot.Types;
using System.Reflection;

namespace AiaTelegramBot.TG_Bot
{
    internal partial class BotEntity
    {
        #region API
        private HttpListener? APIListener;
        string API_URI_FORMATTED_MD_STATUS = "⛔️ В данный момент API бота недоступно";
        protected async void StartAPI()
        {

            if (botClient == null)
            {
                BotLogger.Log("Невозможно запустить API, т.к. бот не запущен", BotLogger.LogLevels.ERROR, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                StopApi();
                return;
            }
            try
            {
                APIListener = new HttpListener();
                BotLogger.Log($"Запуск API на http://127.0.0.1:{RunningConfiguration.ApiPort}/", BotLogger.LogLevels.INFO, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                APIListener.Prefixes.Add($"http://127.0.0.1:{RunningConfiguration.ApiPort}/{BotID}/");
                API_URI_FORMATTED_MD_STATUS = $"✅ API бота доступно по следующему адресу:\n```\nhttp://127.0.0.1:{RunningConfiguration.ApiPort}/{BotID}/\n```";
                APIListener.Start();
                BotLogger.Log($"Успешно запущено API на http://127.0.0.1:{RunningConfiguration.ApiPort}/{BotID}/", BotLogger.LogLevels.SUCCESS, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                while (true)
                {
                    HttpListenerContext context = await APIListener.GetContextAsync();
                    HttpListenerRequest req = context.Request;
                    string requestedActionLine = Extract(req);
                    string ErrorMessage = "{\"Started\":\"false\", \"error\": \"Невозможно выполнить действие: не было передано имени действия\"}";
                    if (!string.IsNullOrEmpty(requestedActionLine))

                    {
                        APIRequestEntity? RE = JsonConvert.DeserializeObject<APIRequestEntity>(requestedActionLine);
                        if (RE != null)
                        {
                            if (!string.IsNullOrEmpty(RE.ActionName))
                            {
                                BotAction? targetAction = BotActions.FirstOrDefault(x => x.Name?.ToLower() == RE.ActionName?.ToLower());
                                if (targetAction == null) ErrorMessage = "{\"Started\":\"false\", \"error\": \"Не удалось найти указанное действие в списке активных действий\"}";
                                else
                                {
                                    Telegram.Bot.Types.Update update = new Telegram.Bot.Types.Update()
                                    {
                                        Message = new()
                                        {
                                            Chat = new()
                                            {
                                                Id = RE.ChatID
                                            },
                                            MessageId = 0,
                                            Text = RE.Args
                                        }
                                    };
                                    await targetAction.RunAction(botClient, update, cancellationToken.Token, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                                    ErrorMessage = "{\"Started\":\"true\", \"error\": \"OK\"}";
                                }
                            }
                        }
                    }

                    using HttpListenerResponse resp = context.Response;
                    resp.Headers.Set("Content-Type", "text/plain");

                    byte[] buffer = Encoding.UTF8.GetBytes(ErrorMessage);
                    resp.ContentLength64 = buffer.Length;
                    using Stream ros = resp.OutputStream;
                    ros.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception tcpListenerException)
            {
                BotLogger.Log($"Ошибка при запуске сервиса API:\n{tcpListenerException.Message}", BotLogger.LogLevels.WARNING, $"{RunningConfiguration}/latest.log");
            }
            finally
            {
                StopApi();
            }
        }
        protected void RestartAPI()
        {
            BotLogger.Log($"Сервис API перезапускается...", BotLogger.LogLevels.INFO, $"{RunningConfiguration}/latest.log");
            StopApi();
            if (RunningConfiguration.IsApiEnabled)
            {
                StartAPI();
            }
        }
        protected void StopApi()
        {
            if (APIListener != null)
            {
                API_URI_FORMATTED_MD_STATUS = "⛔️ В данный момент API бота недоступно";
                if (APIListener.IsListening) APIListener.Stop();
                APIListener.Close();
                APIListener = null;
                BotLogger.Log($"Сервис API остановлен", BotLogger.LogLevels.SUCCESS, $"{RunningConfiguration}/latest.log");
                return;
            }
            BotLogger.Log($"Сервис API не может быть остановлен, т.к. не работает", BotLogger.LogLevels.INFO, $"{RunningConfiguration}/latest.log");
        }
        public static string Extract(HttpListenerRequest message)
        {
            using (var reader = new StreamReader(message.InputStream))
            {
                string jsonString = reader.ReadToEnd();
                return jsonString;
            }
        }
        #endregion

        #region BOT
        public string? BotID { get; protected set; } = string.Empty;
        public string? BotName { get; protected set; } = string.Empty;
        public string? UserDirectory { get; protected set; } = string.Empty;
        protected StatUnit? statContiner = null;
        public readonly static List<BotAction> BotSystemActions = new List<BotAction>()
        {
            #region HELPS implementation
            /* help */ 
            new BotAction()
            {
                Name = "SYSTEM: help",
                Keyword = "/help",
                Description = "Получить список команд, доступных пользователю",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = false,
                LogOutput = false,
                Handler = GetHelp
            },
            /* admin-help */ 
            new BotAction()
            {
                Name = "SYSTEM: admin-help",
                Keyword = "/admin-help",
                Description = "Получить список команд, доступных администратору",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = GetAdminHelp
            },
            #endregion
            #region DEBUG implementation
            /* ping */
            new BotAction()
            {
                Name = "SYSTEM: ping",
                Keyword = "/ping",
                Description = "Проверить работу бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = GetPing
            },
            /* stats */
            new BotAction()
            {
                Name = "SYSTEM: stats",
                Keyword = "/stats",
                Description = "Получить статистику работы бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = GetStats
            },
            /* action-names */
            new BotAction()
            {
                Name = "SYSTEM: action-names",
                Keyword = "/action-names",
                Description = "Получить имена всех действий (используется для работы с API-службой)",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = GetActionNames
            },
            #endregion
            #region API implementation
            /* get-api */
            new BotAction()
            {
                Name = "SYSTEM: get-api",
                Keyword = "/get-api",
                Description = "Проверить работу API-службы",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = GetApi
            },
            /* stop-api */
            new BotAction()
            {
                Name = "SYSTEM: stop-api",
                Keyword = "/stop-api",
                Description = "Остановить API-службу",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = StopApi
            },
            /* restart-api */
            new BotAction()
            {
                Name = "SYSTEM: restart-api",
                Keyword = "/restart-api",
                Description = "Перезапустить API-службу",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = false,
                Handler = RestartApi
            },
            #endregion
            #region UPDATES implementation
            /* update-config */
            new BotAction()
            {
                Name = "SYSTEM: update-config",
                Keyword = "/update-config",
                Description = "Обновить конфигурацию бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = true,
                Handler = GetUpdateConfiguration
            },
            /* update-whitelist */
            new BotAction()
            {
                Name = "SYSTEM: update-whitelist",
                Keyword = "/update-whitelist",
                Description = "Обновить белый список бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = true,
                Handler = GetUpdateWhitelist
            },
            /* update-actions */
            new BotAction()
            {
                Name = "SYSTEM: update-actions",
                Keyword = "/update-actions",
                Description = "Обновить список команд бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = true,
                Handler = GetUpdateActions
            },
            /* update-env */
            new BotAction()
            {
                Name = "SYSTEM: update-env",
                Keyword = "/update-env",
                Description = "Обновить переменные окружения из файла [EnvPath]",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = true,
                Handler = GetUpdateEnv
            },
            #endregion
            #region GET REQUESTS implementation
            /* get-config */
            new BotAction()
            {
                Name = "SYSTEM: get-config",
                Keyword = "/get-config",
                Description = "Получить активную конфигурацию бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = true,
                Handler = GetRunningConfiguration
            },
            /* get-whitelist */
            new BotAction()
            {
                Name = "SYSTEM: get-whitelist",
                Keyword = "/get-whitelist",
                Description = "Получить актуальный список ID пользователей из белого списка бота",
                ActionType = "SYSTEM",
                IsActive = true,
                UseParsing = true,
                IsAdmin = true,
                LogOutput = true,
                Handler = GetRunningWhitelist
            }
            #endregion
        };
        public static List<BotAction> BotActions = BotSystemActions;
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
        #region FUNCTIONS
        // Идентификация пользователя
        protected BotAction? VerifyUserComand(long userID, string requestedKeyword)
        {
            if (string.IsNullOrEmpty(requestedKeyword)) return null;
            if (RunningConfiguration.ParsedUsers.FirstOrDefault(x => (x.UserID == userID) && (x.ActiveComands != null) &&
            (!string.IsNullOrEmpty(x.ActiveComands?.FirstOrDefault(j => j == requestedKeyword)))) != null)
            {
                return BotActions.FirstOrDefault(x => x.Keyword.ToLower() == requestedKeyword.ToLower());
            }
            return null;
        }
        // Парсинг команды пользователя
        protected string? IdentifyCommand(Telegram.Bot.Types.Update update)
        {
            string? comand = update.Message?.Text;
            if (!string.IsNullOrEmpty(comand)) return comand.ToLower();
            return null;
        }
        // Если вместо юзера указан просто ID чата, то это тоже считается пользователем
        protected UserEntity? IdentifyUser(Telegram.Bot.Types.Update update)
        {
            if (update.Message == null) return null;
            return RunningConfiguration.ParsedUsers.FirstOrDefault(x => x.UserID == update.Message.Chat.Id);
        }
        protected async Task HandleUserUpdate(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            string? comandName = IdentifyCommand(update);
            if (string.IsNullOrEmpty(comandName)) return;
            UserEntity? targetUser = IdentifyUser(update);
            if (targetUser == null)
            {
                SendAndLogMessage(client, update, token, "Вы не можете выполнять эту команду, т.к. вас нет в белом списке", BotLogger.LogLevels.WARNING, RunningConfiguration.LogPath);
                return;
            }

            BotAction? targetAction = VerifyUserComand(targetUser.UserID, comandName);
            if (targetAction != null && targetAction.IsActive)
            {
                if (targetAction.ActionType == "SYSTEM")
                {
                    // Обработка вызова системной команды
                    targetAction.Handler?.Invoke(client, update, token, $"{RunningConfiguration.LogPath}", this);
                }
                else
                {
                    targetAction?.RunAction(client, update, token, $"{RunningConfiguration.LogPath}");
                }
            }
        }
        protected async void StoreUsername(Telegram.Bot.Types.Update update)
        {
            if (!RunningConfiguration.StoreNewUsernames) return;
            if (update.Message?.From != null)
            {
                var msg = update.Message.From;
                string usernameString = $"@{msg.Username ?? "[не определено]"} ({msg.Id}): {msg.FirstName ?? "[не определено]"} {msg.LastName ?? "[не определено]"}\n";
                try
                {
                    if (System.IO.File.Exists($"{RunningConfiguration.WorkingDirectory}/usernames"))
                    {
                        string[] lines = System.IO.File.ReadAllLines($"{RunningConfiguration.WorkingDirectory}/usernames");
                        if (lines.FirstOrDefault(x => x == usernameString) != null)
                        {
                            Log($"Боту написал новый пользователь, которого бот раньше не встречал!\n\t{usernameString}", BotLogger.LogLevels.INFO);
                            await System.IO.File.AppendAllTextAsync($"{RunningConfiguration.WorkingDirectory}/usernames", usernameString);
                        }
                        return;
                    }
                    Log($"Боту написал новый пользователь, которого бот раньше не встречал!\n\t{usernameString}", BotLogger.LogLevels.INFO);
                    await System.IO.File.AppendAllTextAsync($"{RunningConfiguration.WorkingDirectory}/usernames", usernameString);
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
                await System.IO.File.AppendAllTextAsync(conversationPath, $"[{update.Message.Date.ToLocalTime()}][{BotName}][@{update.Message?.From?.Username ?? "???"} ({update.Message?.From?.Id.ToString() ?? "???"})]: {update.Message?.Text ?? "[пустое сообщение]"}\n");
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
            if (!RunningConfiguration.UpdateUsersConfiguration(BotActions)) return;
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
                BotName = botInfo.Username ?? "[не определено]";
                BotID = GetBotID(token);
                Log($"Успешно запущен бот {BotName}!", BotLogger.LogLevels.SUCCESS);
                Log($"Идентификатор бота: {BotID}", BotLogger.LogLevels.INFO);
                statContiner = new StatUnit(BotName, BotID, DateTime.Now.ToLocalTime());
                // Запуск API
                RestartAPI();
                //if (RunningConfiguration.IsApiEnabled) 
                //{
                //    //APIEntity = new BotAPIEntity(RunningConfiguration.ApiPort, $"{RunningConfiguration.WorkingDirectory}/latest.log", botClient, cancellationToken);
                //    //APIEntity.Listen();
                //}
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
            if (statContiner != null) statContiner.ReceivedUpdatesCount++;
            StoreUsername(update);
            StoreConversation(update);
            switch (update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Unknown:
                    Log($"[{BotName}][@{update.Message?.From?.Username ?? "???"} ({update.Message?.From?.Id.ToString() ?? "???"})]: {update.Message?.Text ?? "[UNKNOWN_UPDATE_TYPE]"}", BotLogger.LogLevels.MESSAGE);
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    if ((update.Message == null) || (update.Message.Text == null)) return;
                    if (statContiner != null) statContiner.ReceivedMessagesCount++;
                    Log($"[{BotName}][{update.Message?.From?.FirstName} {update.Message?.From?.LastName} " +
                        $"(@{update.Message?.From?.Username ?? "???"} - {update.Message?.From?.Id.ToString() ?? "???"})]: " +
                        $"{update.Message?.Text ?? "[пустое сообщение]"}", BotLogger.LogLevels.MESSAGE);
                    bool userIsAdmin = false;
                    // Проверка, что пользователь есть в белом списке
                    if (RunningConfiguration.Whitelist.Count > 0) if (RunningConfiguration.Whitelist.FirstOrDefault(x => x == $"{update.Message?.From?.Id}") != null) userIsAdmin = true;
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                    if (RunningConfiguration.UseUserConfiguration) await HandleUserUpdate(client, update, token);// await HandleUpdateAsync_UserConfiguration(client, update, token, update.Message.Text.Trim(), userIsAdmin);
                    else await HandleUpdateAsync_vanilla(client, update, token, update.Message.Text.Trim(), userIsAdmin);
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
                    break;
            }
        }
        protected async Task HandleUpdateAsync_vanilla(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string msg, bool userIsAdmin = false)
        {
            if (string.IsNullOrEmpty(msg)) return;

            for (int i = 0; i < BotActions.Count; i++)
            {
                if (msg.ToLower().StartsWith(BotActions[i].Keyword.ToLower()))
                {
                    Log($"[{BotName}]: Распознана команда \"{BotActions[i].Keyword}\"", BotLogger.LogLevels.COMMAND);
                    // Проверка прав пользователя
                    if (BotActions[i].IsAdmin)
                    {
                        if (!userIsAdmin)
                        {
                            SendAndLogMessage(client, update, token, $"Обнаружена попытка запуска команды администратора пользователем, который им не является.\n" +
                                $"`Этот инцидент будет отправлен на рассмотрение активным администраторам`",
                                BotLogger.LogLevels.WARNING,
                                $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            if (statContiner != null) statContiner.SentMessagesCount++;
                            return;
                        }
                    }
                    // Проверка, что команда активна
                    if (!BotActions[i].IsActive)
                    {
                        SendAndLogMessage(client, update, token, $"В данный момент эта команда не активна",
                            BotLogger.LogLevels.WARNING,
                            $"{RunningConfiguration.WorkingDirectory}/latest.log");
                        if (statContiner != null) statContiner.SentMessagesCount++;
                        return;
                    }
                    if (BotActions[i].ActionType == "SYSTEM")
                    {
                        // Обработка вызова системной команды
                        BotActions[i].Handler?.Invoke(client, update, token, $"{RunningConfiguration.WorkingDirectory}/latest.log", this);
                        return;
                    }
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                    // Обработка вызова команды
                    BotActions[i].RunAction(client, update, token, logPath: $"{RunningConfiguration.WorkingDirectory}/latest.log");
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                    if (statContiner != null) statContiner.SentMessagesCount++;
                }
            }
        }
        protected async Task HandleUpdateAsync_UserConfiguration(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string msg, bool userIsAdmin = false)
        {
            if (string.IsNullOrEmpty(msg)) return;

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
                                return;
                            }
                            BotLogger.Log($"Запущена команда администратора ({update.Message.From.Id}/{BotActions[i].Name})",
                                BotLogger.LogLevels.INFO, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            await BotActions[i].RunAction(client, update, token, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                            return;
                        }
                        SendAndLogMessage(client, update, token, $"Обнаружена попытка запуска команды администратора пользователем, который им не является: {update.Message.From.Id} (@{update.Message.From.Username}).\n" +
                            $"`Этот инцидент будет отправлен на рассмотрение активным администраторам`", BotLogger.LogLevels.WARNING, $"{RunningConfiguration.WorkingDirectory}/latest.log");
                        return;
                    }
                    if (!BotActions[i].IsActive)
                    {
                        SendAndLogMessage(client, update, token, "К сожалению, на данный момент эта команда *не активна*",
                            BotLogger.LogLevels.WARNING, $"{RunningConfiguration.WorkingDirectory}/latest.log");
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
                        "/get-config",
                        "/get-whitelist",
                        "/update-actions",
                        "/update-whitelist",
                        "/update-config",
                        "/stats",
                        "/get-api",
                        "/stop-api",
                        "/restart-api",
                        "/action-names",
                        "/ping",
                        "/update-env",
                        "/admin-help"
            })
            {
                if (msg.ToLower().StartsWith(command.ToLower()))
                {

                }
            }
            if (update.Message == null) return;
            statContiner.SentMessagesCount++;
            switch (msg.ToLower())
            {
                //case "/get-actions":
                //case "/help":
                //    GetHelpMessage(client, update, token, userIsAdmin);
                //    return;
                //case "/get-config":
                //    SendAndLogMessage(client, update, token, $"Актуальная конфигурация бота:\n{RunningConfiguration.GetBotConfiguration()}",
                //        BotLogger.LogLevels.WARNING,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/get-whitelist":
                //    SendAndLogMessage(client, update, token, $"{RunningConfiguration.GetBotWhiteList()}",
                //        BotLogger.LogLevels.WARNING,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/update-actions":
                //    if (!UpdateBotActions())
                //    {
                //        SendAndLogMessage(client, update, token, $"Не удалось обновить список действий бота",
                //            BotLogger.LogLevels.WARNING,
                //            $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //        return;
                //    }
                //    SendAndLogMessage(client, update, token, $"Cписок действий бота успешно обновлен.\nАктивных действий: `{BotActions.Count}`.\nИспользуйте команду `/help`, чтобы получить актуальный список действий",
                //        BotLogger.LogLevels.WARNING,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/update-whitelist":
                //    if (!RunningConfiguration.UpdateBotWhiteList())
                //    {
                //        SendAndLogMessage(client, update, token, $"Не удалось обновить белый список бота",
                //            BotLogger.LogLevels.WARNING,
                //            $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //        return;
                //    }
                //    SendAndLogMessage(client, update, token, $"Белый список бота успешно обновлен!\n{RunningConfiguration.GetBotWhiteList()}",
                //        BotLogger.LogLevels.WARNING,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/update-config":
                //    if (!UpdateBotConfiguration())
                //    {
                //        SendAndLogMessage(client, update, token, $"Не удалось обновить конфигурацию бота",
                //            BotLogger.LogLevels.WARNING,
                //            $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //        return;
                //    }
                //    SendAndLogMessage(client, update, token, $"Конфигурация бота успешно обновлена:\n{RunningConfiguration.GetBotConfiguration()}",
                //        BotLogger.LogLevels.WARNING,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/stats":
                //    await client.SendTextMessageAsync(update.Message.Chat.Id, $"{statContiner.GetBotStats()}",
                //        cancellationToken: token,
                //        replyToMessageId: update.Message.MessageId,
                //        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                //    //SendAndLogMessage(client, update, token, $"{statContiner.GetBotStats()}",
                //    //BotLogger.LogLevels.COMMAND,
                //    //$"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/get-api":
                //    SendAndLogMessage(client, update, token, $"{API_URI_FORMATTED_MD_STATUS}",
                //    BotLogger.LogLevels.COMMAND,
                //    $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;
                //case "/stop-api":
                //    SendAndLogMessage(client, update, token, "Инициирую *остановку* сервиса API...",
                //        BotLogger.LogLevels.COMMAND,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    StopApi();
                //    break;
                //case "/restart-api":
                //    SendAndLogMessage(client, update, token, "Инициирую *перезапуск* сервиса API...",
                //        BotLogger.LogLevels.COMMAND,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    // Пока вы пишете код, который контролирует фабрику асинхронных лисенеров, я тупо перезапускаю API-сервис дважды и живу спокойно :)
                //    // Да, это откровенно конченое решение, да еще и работает с задержкой от 500 мс, но оно РАБОТАЕТ
                //    RestartAPI();
                //    await Task.Delay(1000);
                //    RestartAPI();
                //    break;
                //case "/action-names":
                //    string replyMsg = $"Список всех действий, о которых знает бот:\n";
                //    BotActions.ForEach(x => replyMsg += $"✅ {(string.IsNullOrEmpty(x.Name) ? "[пусто]\n" : $"{x.Name}\n")}");
                //    await client.SendTextMessageAsync(update.Message.Chat.Id, replyMsg,
                //        cancellationToken: token,
                //        replyToMessageId: update.Message.MessageId,
                //        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                //    break;
                //case "/ping":
                //    await client.SendTextMessageAsync(update.Message.Chat.Id, "✅ PONG",
                //        cancellationToken: token,
                //        replyToMessageId: update.Message.MessageId,
                //        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                //    break;
                //case "/update-env":
                //    if (!RunningConfiguration.ExportEnvVars())
                //    {
                //        SendAndLogMessage(client, update, token, $"Не удалось обновить список переменных окружения бота",
                //            BotLogger.LogLevels.WARNING,
                //            $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //        return;
                //    }
                //    string envList = string.Empty;
                //    foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                //        envList += $"{de.Key} = {de.Value}";
                //    SendAndLogMessage(client, update, token, $"Список переменных окружения бота успешно обновлен:\n{envList}",
                //        BotLogger.LogLevels.SUCCESS,
                //        $"{RunningConfiguration.WorkingDirectory}/latest.log");
                //    break;

                case "/admin-help":
                    /* 
                     await client.SendTextMessageAsync(update.Message.Chat.Id,
                        "⚙️ /help\n" +
                        "Получить список доступных действий\n" +
                        "⚙️ /get_actions\n" +
                        "То же самое, что и help\n" +
                        "⚙️ /get_config\n" +
                        "Получить конфигурацию бота в нный момент\n" +
                        "⚙️ /get_whitelist\n" +
                        "Получить список идентификаторов администраторов бота (Telegram User ID)\n" +
                        "⚙️ /update_actions\n" +
                        "Обновить список доступных действий\n" +
                        "⚙️ /update_whitelist\n" +
                        "Обновить список идентификаторов администраторов бота (Telegram User ID)\n" +
                        "⚙️ /update_config\n" +
                        "Обновить конфигурацию бота\n" +
                        "⚙️ /ping\n" +
                        "Если бот жив, он ответит: *PONG*\n" +
                        "⚙️ /stats\n" +
                        "Получить статистику по количеству сообщений, обработанных ботом, включая запросы к API\n" +
                        "⚙️ /get_api\n" +
                        "Получить строку подключения к API, если оно активировано и работает\n" +
                        "⚙️ /stop_api\n" +
                        "Остановить API-сервис бота, если тот запущен\n" +
                        "⚙️ /restart_api\n" +
                        "Попытаться перезапустить API-сервис бота\n" +
                        "⚙️ /action_names\n" +
                        "Получить список всех действий по их именам",
                        cancellationToken: token,
                        replyToMessageId: update.Message.MessageId);
                    */
                    await client.SendTextMessageAsync(update.Message.Chat.Id,
                        "⚙️ `/help`\n" +
                        "Получить список доступных действий\n" +
                        "⚙️ `/get-actions`\n" +
                        "То же самое, что и help\n" +
                        "⚙️ `/get-config`\n" +
                        "Получить конфигурацию бота в нный момент\n" +
                        "⚙️ `/get-whitelist`\n" +
                        "Получить список идентификаторов администраторов бота (Telegram User ID)\n" +
                        "⚙️ `/update-actions`\n" +
                        "Обновить список доступных действий\n" +
                        "⚙️ `/update-whitelist`\n" +
                        "Обновить список идентификаторов администраторов бота (Telegram User ID)\n" +
                        "⚙️ `/update-config`\n" +
                        "Обновить конфигурацию бота\n" +
                        "⚙️ `/ping`\n" +
                        "Если бот жив, он ответит: *PONG*\n" +
                        "⚙️ `/stats`\n" +
                        "Получить статистику по количеству сообщений, обработанных ботом, включая запросы к API\n" +
                        "⚙️ `/get-api`\n" +
                        "Получить строку подключения к API, если оно активировано и работает\n" +
                        "⚙️ `/stop-api`\n" +
                        "Остановить API-сервис бота, если тот запущен\n" +
                        "⚙️ `/restart-api`\n" +
                        "Попытаться перезапустить API-сервис бота\n" +
                        "⚙️ `/action-names`\n" +
                        "Получить список всех действий по их именам\n" +
                        "⚙️ `/update-env`\n" +
                        "Обновить переменные окружения из файла **EnvPath**. Переменные доступны во скриптах, всех запускаемых ботом",
                        cancellationToken: token,
                        replyToMessageId: update.Message.MessageId,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    statContiner.SentMessagesCount++;
                    break;
            }
        }
        protected async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log(exception, BotLogger.LogLevels.CRITICAL);
            Log($"Попытка перезапуска бота @{BotName ?? "[неизвестно]"}", BotLogger.LogLevels.INFO);
            await Task.Run(() => RestartBot(Environment.GetCommandLineArgs()[1]));
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
            if (statContiner != null) statContiner.SentMessagesCount++;
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
            Console.WriteLine(RunningConfiguration.ActionsDirectory);
            Log($"Начат процесс обновления списка действий.\n\tЧтение из директории \"{RunningConfiguration.ActionsDirectory}\"", BotLogger.LogLevels.INFO);
            List<string> configLocations = new List<string>();
            List<BotAction> botActions_buffer = new List<BotAction>();
            try
            {
                configLocations = Directory.EnumerateFiles(RunningConfiguration.ActionsDirectory, "*.json", SearchOption.AllDirectories).ToList<string>();
                botActions_buffer = new List<BotAction>();
                foreach (string filePath in configLocations)
                {
                    try
                    {
                        Console.WriteLine($"\n\t{filePath}");
                        string content = System.IO.File.ReadAllText(filePath);
                        JsonSerializerSettings jsonSelectSettings = new JsonSerializerSettings()
                        {
                            EqualityComparer = StringComparer.OrdinalIgnoreCase,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore,
                            Formatting = Formatting.Indented
                        };
                        BotAction? targetAction = JsonConvert.DeserializeObject<BotAction>(content, jsonSelectSettings);
                        //Console.WriteLine($"---------- Содержание файла ----------\n{content}\n--------------------------------------");
                        //Console.WriteLine($"{JsonConvert.SerializeObject(targetAction, Formatting.Indented)}");
                        //continue;
                        if (targetAction == null)
                        {
                            Log($"Не удалось прочитать действие {filePath}:", BotLogger.LogLevels.WARNING);
                            Console.WriteLine($"---------- Содержание файла ----------\n{content}\n--------------------------------------");
                            continue;
                        }
                        if (!targetAction.IsActive)
                        {
                            Log($"Действие {filePath} проигнорировано, т.к. помечено как неактивное", BotLogger.LogLevels.WARNING);
                            continue;
                        }
                        //Console.WriteLine($"\t\t\t{targetAction.ImageGroupPaths.Length}");
                        botActions_buffer.Add(targetAction);
                        Log($"Добавлено новое действие:\n{targetAction.GetActionInfo()} ", BotLogger.LogLevels.SUCCESS);

                        //Console.WriteLine($"{JsonConvert.SerializeObject(content)}");

                        //BotAction? targetAction = System.Text.Json.JsonSerializer.Deserialize<BotAction>(content, options);
                        //if (targetAction == null)
                        //{
                        //    Log($"Не удалось десериализовать действие {filename}:\n", BotLogger.LogLevels.ERROR);
                        //    Log($"---------- Содержание файла ----------\n{content}\n--------------------------------------", BotLogger.LogLevels.INFO);
                        //    continue;
                        //}
                        //Log($"Добавлено новое действие:\n{JsonConvert.SerializeObject(targetAction)}\n", BotLogger.LogLevels.INFO);

                        //if (targetAction.IsActive)
                        //{
                        //    botActions_buffer.Add(targetAction);
                        //    //Log($"Добавлено новое действие:\n{JsonConvert.SerializeObject(targetAction)}\n", BotLogger.LogLevels.INFO);
                        //    Log($"Добавлено новое действие:\n{targetAction.GetActionInfo()}", BotLogger.LogLevels.INFO);
                        //}
                        //else
                        //{
                        //    Log($"Действие:\n{targetAction.Name} проигнорировано, т.к. помечено как неактивное", BotLogger.LogLevels.WARNING);
                        //}
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
            BotActions.AddRange(BotSystemActions);
            BotActions.Sort((a, b) => a.Keyword.CompareTo(b.Keyword));
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
                    if (RunningConfiguration.IsApiEnabled) RestartAPI();
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
        protected async void GetHelpMessage(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, bool adminUser)
        {
            if (update.Message == null) return;
            string HelpMessage = "Доступные вам команды:\n";
            if (RunningConfiguration.ParsedUsers.Count == 0)
            {
                if (update.Message == null) return;
                List<BotAction> actionsBuffer = new List<BotAction>();
                if (RunningConfiguration.HideInactiveActions)
                {
                    actionsBuffer = BotActions.FindAll(x => x.IsActive == true && x.IsAdmin == adminUser); // Выбираем только те действия, которые активны в данный момент
                }
                else
                {
                    actionsBuffer = BotActions.FindAll(x => x.IsAdmin == adminUser); // Выбираем только те действия, которые активны в данный момент
                }
                if (actionsBuffer.Count == 0)
                {
                    await client.SendTextMessageAsync(update.Message.Chat.Id, $"{HelpMessage}Нет доступных команд",
                        cancellationToken: token,
                        replyToMessageId: update.Message.MessageId,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    return;
                }
                actionsBuffer.Sort((a, b) => a.Keyword.CompareTo(b.Keyword.Split(' ')[0]));

                List<BotAction> duplicateActions = new List<BotAction>();
                string[] buffer;

                for (int i = 0; i < actionsBuffer.Count; i++)
                {
                    // Находим все команды, начало которых совпадает с началом других команд из списка
                    buffer = actionsBuffer[i].Keyword.Split(' ');
                    if (buffer.Length > 1)
                    {
                        duplicateActions = actionsBuffer.FindAll(x => x.Keyword.StartsWith(buffer[0]));
                        if (duplicateActions.Count > 0)
                        {
                            actionsBuffer = actionsBuffer.Except(duplicateActions).ToList();
                            // Желательно, чтобы у дублирующих действий было идентичное описание. В противном случае, берем первое попавшееся (даже если это пустая строка)
                            HelpMessage += $"{(duplicateActions[0].IsAdmin ? "⚙️" : "✏️")} `{duplicateActions[0].Keyword.Split(' ')[0]}`\n    ";
                            foreach (BotAction action in duplicateActions)
                            {
                                string options = action.Keyword.Replace(buffer[0], "");
                                //if (string.IsNullOrEmpty(options)) continue; // Желательно, но длина буфера и так будет > 0, а буфер формируется исходя из пробелов
                                options = options.Trim(); //  Т.е. в теории словить доской по ебалу тут нельзя. В теории...
                                                          //for (int j = 1; j < buffer.Length; j++) options += buffer[j];
                                HelpMessage += $"`[{options.Trim()}]` ";
                            }
                            HelpMessage += $"\n📝 {duplicateActions[0].Description ?? "(нет описания действия)"}\n\n";
                            continue;
                        }
                    }
                    HelpMessage += $"{(actionsBuffer[i].IsAdmin ? "⚙️" : "✏️")} `{actionsBuffer[i].Keyword}`\n" +
                    $"📝 {actionsBuffer[i].Description ?? "(нет описания действия)"}\n\n";

                    //duplicateActions = actionsBuffer.FindAll(x => (CoreFunctions.StringCompare(x.Keyword.Split(' ')[0], buffer[0]) > 60.0));
                }
            }
            await client.SendTextMessageAsync(update.Message.Chat.Id, $"{HelpMessage}",
                cancellationToken: token,
                replyToMessageId: update.Message.MessageId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

            //string[] buffer;
            //List<BotAction> ReverseBuffer = new List<BotAction>();
            //Console.WriteLine($"\tBotActions: {BotActions.Count}\tactionsBuffer: {actionsBuffer.Count}");
            //for (int i = 0; i < actionsBuffer.Count; i++)
            //{
            //    string helpEmoji = $"{(actionsBuffer[i].IsAdmin ? "⚙️" : "✏️")}";
            //    if (RunningConfiguration.HideInactiveActions && !BotActions[i].IsActive) continue;
            //    if (string.IsNullOrEmpty(actionsBuffer[i].Keyword)) continue;
            //    buffer = actionsBuffer[i].Keyword.Split(' ');
            //    List<BotAction> botactions = actionsBuffer.FindAll(x => (CoreFunctions.StringCompare(x.Keyword.Split(' ')[0], buffer[0]) > 60.0));
            //    Console.WriteLine($"botactions: {botactions.Count}");
            //    if (botactions.Count == 1)
            //    {
            //        HelpMessage += $"{helpEmoji} `{actionsBuffer[i].Keyword}`\n" +
            //        $"📝 {actionsBuffer[i].Description ?? "(нет описания действия)"}\n\n";
            //    }
            //    //Сомнения
            //    if (botactions.Count > 1)
            //    {
            //        HelpMessage += $"{helpEmoji} `{buffer[0]}`\n    ";
            //        botactions.ForEach(x =>
            //        {
            //            // ToDo: Тут бы рекурсию сделать
            //            buffer = x.Keyword.Split(' ');
            //            if (buffer.Length > 1)
            //            {
            //                HelpMessage += $"`[{(string.IsNullOrEmpty(buffer[1]) ? "пусто" : $"{buffer[1]}")}]`  ";
            //            }
            //        });
            //        HelpMessage += $"\n📝 {actionsBuffer[i].Description ?? "(нет описания действия)"}\n\n";
            //    }
            //    actionsBuffer = actionsBuffer.Except(botactions).ToList();
            //    //ReverseBuffer.AddRange(botactions.FindAll(x => x.IsAdmin == adminUser));
            //}
            //foreach (var item in BotActions.Except(ReverseBuffer))
            //{
            //    HelpMessage += $"{(item.IsAdmin ? "⚙️" : "✏️")} `{item.Keyword}`\n" +
            //    $"📝 {item.Description ?? "(нет описания действия)"}\n\n";
            //}
            //await client.SendTextMessageAsync(update.Message.Chat.Id, $"{HelpMessage}",
            //    cancellationToken: token,
            //    replyToMessageId: update.Message.MessageId,
            //    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
        #endregion
        #endregion
    }
}
