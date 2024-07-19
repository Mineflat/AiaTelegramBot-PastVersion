using AiaTelegramBot.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Schema;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bots.Types;
using static AiaTelegramBot.Logging.BotLogger;
using static System.Net.Mime.MediaTypeNames;
using InputFile = Telegram.Bots.Types.InputFile;

namespace AiaTelegramBot.TG_Bot.models
{
    internal class BotAction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public bool UseParsing { get; set; } = false;
        public bool LogOutput { get; set; } = true;
        public bool IsAdmin { get; set; } = true;
        public string ActionType = string.Empty;
        public string? Location = string.Empty;
        public string? Filename = string.Empty;
        public bool SendFile = false;
        public string? FilePath = null;
        public string[]? ImageGroupPaths { get; set; }
        //public string[]? ImageGroupPaths { get; set; }
        public string? TargetID { get; set; } = string.Empty;
        public string? TargetMessage { get; set; } = string.Empty;
        protected List<string> FileContent = new List<string>();
        //public BotAction(string name, string keyword, bool isActive, 
        //    bool isAdmin, string actionType, string location, string targetID, 
        //    string? targetMessage, string[] ImageGroupPaths)
        //{
        //    // Прости господи меня за такой код...
        //    if (string.IsNullOrEmpty(name)) throw new Exception("Поле name не может быть пустым");
        //    if (string.IsNullOrEmpty(keyword)) throw new Exception("Поле keyword не может быть пустым");
        //    if (string.IsNullOrEmpty(actionType)) throw new Exception("Поле actionType не может быть пустым");
        //    // Господь простил :) 
        //    Name = name;
        //    Keyword = keyword;
        //    IsActive = isActive;
        //    IsAdmin = isAdmin;
        //    ActionType = actionType;
        //    ImageGroupPaths = ImageGroupPaths;
        //    if (string.IsNullOrEmpty(targetID) || string.IsNullOrEmpty(targetMessage))
        //    {
        //        if (!string.IsNullOrEmpty(location))
        //            if (!(System.IO.File.Exists(location) || System.IO.Directory.Exists(location)))
        //            {
        //                throw new Exception($"Файл или директория, указанная в поле location, не существует ({name})");

        //            }
        //            else
        //            {
        //                Location = location;
        //                Filename = GetFilename(Location);
        //            }
        //        return;
        //    }
        //    TargetMessage = targetMessage;
        //    TargetID = targetID;
        //}
        public static string? GetFilename(string location)
        {
            string[] pathContent;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) pathContent = location.Split('\\');
            else pathContent = location.Split('/');
            if (pathContent.Length > 0)
            {
                return pathContent[pathContent.Length - 1];
            }
            return null;
        }
        public async Task RunAction(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            if (!IsActive)
            {
                BotLogger.Log("Команда находится в списке неиспользуемых, поэтому не выполнена", LogLevels.INFO);
                return;
            }
            if (string.IsNullOrEmpty(ActionType))
            {
                IsActive = false;
                BotLogger.Log($"Не удалось запустить действие \"{Name}\", т.к. оно имеет неправильный тип действия: \"{ActionType}\"",
                    BotLogger.LogLevels.ERROR, logPath);
                BotLogger.Log($"Действие \"{Name}\" помечено как неисполняемое (не может быть вызвано)",
                    BotLogger.LogLevels.INFO, logPath);
                return;
            }
            switch (ActionType.Trim().ToLower())
            {
                case "text":
                    GetFileText(client, update, token, logPath);
                    break;
                case "random_text":
                    GetRandomText(client, update, token, logPath);
                    break;
                case "image_group":
                    SendImageGroup(client, update, token, logPath);
                    break;
                case "random_image":
                    GetRandomImage(client, update, token, logPath);
                    break;
                case "file":
                    GetFile(client, update, token, logPath);
                    break;
                case "script":
                    RunScript(client, update, token, logPath);
                    break;
                case "message":
                    SendMessage(client, update, token, logPath);
                    break;
                default:
                    BotLogger.Log($"Не удалось запустить действие \"{Name}\", т.к. оно имеет неправильный тип действия: \"{ActionType}\"",
                        BotLogger.LogLevels.ERROR, logPath);
                    IsActive = false;
                    BotLogger.Log($"Действие \"{Name}\" помечено как неисполняемое (не может быть вызвано)",
                        BotLogger.LogLevels.INFO, logPath);
                    break;
            }
        }
        protected async void SendImageGroup(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            if (update.Message == null) return;
            if ((ImageGroupPaths == null) || (ImageGroupPaths.Length == 0))
            {
                BotLogger.Log($"Невозможно отправить несколько картинок в чат по команде `{Keyword}` ({Name}): В описании действия не задан массив ImageGroupPaths", LogLevels.ERROR, Location);
                return;
            }

            //await client.SendMediaGroupAsync(update.Message.Chat.Id, phts,  cancellationToken: token);

            //List<Telegram.Bot.Types.InputFile> content = new List<Telegram.Bot.Types.InputFile>();
            //List<InputMediaPhoto> phts = new List<InputMediaPhoto>();
            //for (int i = 0; i < ImageGroupPaths?.Length; i++)
            //{
            //    //imageStreams.Add(System.IO.File.OpenRead($"{ImageGroupPaths[i]}"));
            //    content.Add(Telegram.Bot.Types.InputFile.FromStream(System.IO.File.OpenRead($"{ImageGroupPaths[i]}")));
            //    phts.Add(new InputMediaPhoto(content[i]) { Caption = "--"});
            //}
            //await client.SendMediaGroupAsync(update.Message.Chat.Id, phts,  cancellationToken: token);
            //return;

            List<FileStream> streams = new List<FileStream>();
            List<InputMediaPhoto> phts = new List<InputMediaPhoto>();
            try
            {
                foreach (string p in ImageGroupPaths)
                {
                    InputFile file = new InputFile(p);
                    FileStream s = System.IO.File.OpenRead(p);
                    streams.Add(s);
                    phts.Add(new InputMediaPhoto(Telegram.Bot.Types.InputFile
                    {
                        s,
                        Path.GetFileName(p)
                    }));
                }
                await client.SendMediaGroupAsync(update.Message.Chat.Id, phts);
                //List<Stream> imageStreams = new List<Stream>();
                ////List<Telegram.Bot.Types.InputFile> content = new List<Telegram.Bot.Types.InputFile>();
                ////List<InputMediaPhoto> phts = new List<InputMediaPhoto>();
                ////for (int i = 0; i < ImageGroupPaths?.Length; i++)
                ////{
                ////    //imageStreams.Add(System.IO.File.OpenRead($"{ImageGroupPaths[i]}"));
                ////    content.Add(Telegram.Bot.Types.InputFile.FromStream(System.IO.File.OpenRead($"{ImageGroupPaths[i]}")));
                ////    phts.Add(new InputMediaPhoto(content[i]));
                ////}
                ////await client.SendMediaGroupAsync(update.Message.Chat.Id, phts);
                //await using Stream stream = System.IO.File.OpenRead(randomImagePath);
                //if (update.Message.MessageId == 0)
                //{
                //    await client.SendPhotoAsync(update.Message.Chat.Id,
                //        Telegram.Bot.Types.InputFile.FromStream(stream, $"{GetFilename(randomImagePath)}"),
                //        caption: $"`{GetFilename(randomImagePath)}`",
                //        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                //}
                //else
                //{
                //    await client.SendPhotoAsync(update.Message.Chat.Id,
                //        Telegram.Bot.Types.InputFile.FromStream(stream, $"{GetFilename(randomImagePath)}"),
                //        caption: $"`{GetFilename(randomImagePath)}`",
                //        replyToMessageId: update.Message.MessageId,
                //        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                //}
            }
            catch (Exception IOException)
            {
                BotLogger.Log($"Не удалось получить один или несколько файлов по команде \"{Keyword}\":\n{IOException.Message}", LogLevels.ERROR, logPath);
                SendCustomMessage_API(client, update, token, $"Не удалось выполнить команду `{Keyword}` ({Name}):\n```\n{IOException.Message}\n```");
            }
            finally
            {
                foreach (var s in streams) s.Dispose();
                phts.Clear();
            }

        }

        protected async void SendMessage(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            if (string.IsNullOrEmpty(update.Message?.Text)) return;
            if (long.TryParse(TargetID, out long chat_id))
            {
                string? reply = $"{(string.IsNullOrEmpty(TargetMessage) ? $"{GetArgs(update.Message.Text)}" : $"{TargetMessage}")}";
                if (!string.IsNullOrEmpty(reply))
                {
                    //SendCustomMessage_API(client, update, token, reply);
                    await client.SendTextMessageAsync(chat_id,
                        reply,
                        cancellationToken: token,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    return;
                }
                BotLogger.Log($"Не удалось отправить сообщение пользователю {TargetID} - пустое сообщение и, при этом, не заполнен параметр TargetMessage ", LogLevels.ERROR, logPath);
            }
            BotLogger.Log($"Не удалось отправить сообщение пользователю {TargetID} - не числовой идентификатор", LogLevels.ERROR, logPath);
        }
        protected async void GetFileText(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            if (update.Message == null || string.IsNullOrEmpty(Location)) return;
            try
            {
                string fileText = System.IO.File.ReadAllText(Location, Encoding.UTF8);
                if (string.IsNullOrEmpty(fileText))
                {
                    BotLogger.Log($"Cодержимое файла \"{Location.Replace("\\", "\\\\")}\" является пустой строкой", LogLevels.ERROR, logPath);
                    SendCustomMessage_API(client, update, token, $"Cодержимое файла:\n```\n{Location.Replace("\\", "\\\\")}\n```\n не может быть отправлено, т.к. является пустой строкой");
                }
                else
                {
                    SendCustomMessage_API(client, update, token, $"{fileText}");
                }
            }
            catch (Exception filereadException)
            {
                BotLogger.Log($"Не удалось получить содержимое файла \"{Location.Replace("\\", "\\\\")}\":\n{filereadException.Message}", LogLevels.ERROR, logPath);
            }
        }
        protected async void GetRandomText(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            bool CanGetFileContent = await GetFileContent();
            if (!CanGetFileContent) return;
            Console.WriteLine(FileContent.Count);
            //text = text.Replace("\\n", $"{Environment.NewLine}\n");
            if (FileContent.Count > 0 && update.Message != null)
            {
                string text = FileContent[CoreFunctions.GetRandom(0, FileContent.Count - 1)];
                Console.WriteLine(text);
                SendCustomMessage_API(client, update, token, text);
                //await client.SendTextMessageAsync(update.Message.Chat.Id,
                //    text,
                //    cancellationToken: token,
                //    replyToMessageId: update.Message.MessageId);
            }
        }
        protected async void GetRandomImage(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            try
            {
                if (update.Message == null) return;
                if ((update.Message.Chat.Id == 0))
                {
                    BotLogger.Log("Не удалось выполнить команду, т.к. некуда отправлять результат!", LogLevels.ERROR, logPath);
                    return;
                }
                List<string> images = Directory.EnumerateFiles(Location, "*.*", SearchOption.AllDirectories)
                    .Where(s => new List<string> { "jpg", "gif", "png", "jpeg" }
                    .Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()))
                    .ToList<string>();
                BotLogger.Log($"В директории {Location} и поддиректориях обнаружено картинок: {images.Count} (только форматы: *.jpg, *.jpeg, *.png, *.gif)", LogLevels.INFO, logPath);
                if (images.Count == 0)
                {
                    BotLogger.Log($"В указанной директории нет картинок: \"{Location}\"", LogLevels.ERROR, logPath);
                    SendCustomMessage_API(client, update, token, $"В указанной директории нет картинок:\n```\n{Location}\n```\n");
                    //await client.SendTextMessageAsync(update.Message.Chat.Id,
                    //    $"В указанной директории нет картинок:\n```\n{Location}\n```\n",
                    //    cancellationToken: token,
                    //    replyToMessageId: update.Message.MessageId,
                    //    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
                else
                {
                    string randomImagePath = images[CoreFunctions.GetRandom(0, images.Count - 1)];
                    await using Stream stream = System.IO.File.OpenRead(randomImagePath);
                    if (update.Message.MessageId == 0)
                    {
                        await client.SendPhotoAsync(update.Message.Chat.Id,
                            Telegram.Bot.Types.InputFile.FromStream(stream, $"{GetFilename(randomImagePath)}"),
                            caption: $"`{GetFilename(randomImagePath)}`",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                    else
                    {
                        await client.SendPhotoAsync(update.Message.Chat.Id,
                            Telegram.Bot.Types.InputFile.FromStream(stream, $"{GetFilename(randomImagePath)}"),
                            caption: $"`{GetFilename(randomImagePath)}`",
                            replyToMessageId: update.Message.MessageId,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                    BotLogger.Log($"Отправлена картинка \"{randomImagePath}\" в чат {update.Message.Chat.Id} (пользователь @{update.Message.From?.Username ?? "[не определено]"})",
                        LogLevels.SUCCESS, logPath);
                }
            }
            catch (Exception TCPSendException)
            {
                if (update.Message == null) return;
                BotLogger.Log($"Не удалось получить файл \"{Filename}\":\n{TCPSendException.Message}", LogLevels.ERROR, logPath);
                SendCustomMessage_API(client, update, token, $"Не удалось получить файл \"{Filename}\":\n```\n{TCPSendException.Message}\n```");
                //await client.SendTextMessageAsync(update.Message.Chat.Id,
                //    $"Не удалось получить файл \"{Filename}\":\n```\n{TCPSendException.Message}\n```",
                //    cancellationToken: token,
                //    replyToMessageId: update.Message.MessageId,
                //    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
        }
        protected async void GetFile(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null, string? targetPath = null)
        {
            string path = $"{(string.IsNullOrEmpty(targetPath) ? $"{Location}" : $"{targetPath}")}";
            string name = $"{(string.IsNullOrEmpty(Filename) ? "result.txt" : $"{Filename}")}";
            try
            {
                if (update.Message == null) return;
                if ((update.Message.Chat.Id == 0))
                {
                    BotLogger.Log("Не удалось выполнить команду, т.к. некуда отправлять результат!", LogLevels.ERROR, logPath);
                    return;
                }
                await using Stream stream = System.IO.File.OpenRead(path);
                if (update.Message.MessageId == 0)
                {
                    await client.SendDocumentAsync(update.Message.Chat.Id,
                        document: Telegram.Bot.Types.InputFile.FromStream(stream, $"{name?.Replace(".", "\\.")}"),
                        caption: $"Файл `{(string.IsNullOrEmpty(name) ? "[скрыто]" : $"{Filename}")}`",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
                else
                {
                    await client.SendDocumentAsync(update.Message.Chat.Id,
                        document: Telegram.Bot.Types.InputFile.FromStream(stream, $"{name?.Replace(".", "\\.")}"),
                        caption: $"Файл `{(string.IsNullOrEmpty(name) ? "[скрыто]" : $"{Filename}")}`",
                        replyToMessageId: update.Message.MessageId,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
                BotLogger.Log($"Отправлен файл \"{path}\" в чат {update.Message.Chat.Id} (пользователь {update.Message.From?.Username ?? "[не определено]"})",
                    LogLevels.SUCCESS, logPath);
            }
            catch (Exception TCPSendException)
            {
                if (update.Message == null) return;
                BotLogger.Log($"Не удалось получить файл \"{Filename}\":\n{TCPSendException.Message}", LogLevels.ERROR, logPath);
                SendCustomMessage_API(client, update, token, $"Не удалось получить файл \"{Filename}\":\n```\n{TCPSendException.Message}\n```");
                //await client.SendTextMessageAsync(update.Message.Chat.Id,
                //    $"Не удалось получить файл \"{Filename}\":\n```\n{TCPSendException.Message}\n```",
                //    cancellationToken: token,
                //    replyToMessageId: update.Message.MessageId,
                //    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
        }
        protected async void RunScript(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string? logPath = null)
        {
            if (update.Message == null) return;
            string args = GetArgs(update.Message.Text);
            BotLogger.Log($"Запуск скрипта по команде \"{Keyword}\":\n\t{Location.Replace("\\", "\\\\")} {args}", LogLevels.INFO, logPath);
            SendCustomMessage_API(client, update, token, $"Запуск скрипта по команде `{Keyword}`:\n```\n{Filename ?? "нет имени файла"} {args}\n```");

            string output = string.Empty;
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = Location,
                        Arguments = $"{args}",
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    }
                };
                process.EnableRaisingEvents = false;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        output += $"{args.Data}\n";
                        if (LogOutput) BotLogger.Log($"{args.Data}", LogLevels.SCRIPT, logPath);
                    }
                };
                BotLogger.Log($"Запуск процесса {Location}", LogLevels.INFO, logPath);
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                BotLogger.Log($"Процесс {Location} завершился успешно", LogLevels.SUCCESS, logPath);
                process.CancelOutputRead();
                if (string.IsNullOrEmpty(output))
                {
                    SendCustomMessage_API(client, update, token, $"Команда \"{Keyword}\" завершилась, но не давала текстового вывода в терминал");
                    //await client.SendTextMessageAsync(update.Message.Chat.Id, $"Команда \"{Keyword}\" завершилась, но не давала текстового вывода в терминал",
                    //    cancellationToken: token,
                    //    replyToMessageId: update.Message.MessageId);
                    return;
                }
                if (SendFile)
                {
                    if (!string.IsNullOrEmpty(FilePath))
                    {
                        GetFile(client, update, token, logPath, FilePath);
                    }
                    else
                    {
                        SendCustomMessage_API(client, update, token, $"Невозможно отправить вывод, т.к. в конфигурации действия не указан путь к файлу (или путь является пустой строкой)");
                        //await client.SendTextMessageAsync(update.Message.Chat.Id, $"Невозможно отправить вывод, т.к. в конфигурации действия не указан путь к файлу (или путь является пустой строкой)",
                        //     cancellationToken: token,
                        //     replyToMessageId: update.Message.MessageId, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                }
                else
                {
                    SendCustomMessage_API(client, update, token, (UseParsing ? $"{(string.IsNullOrEmpty(output) ? "[пустой вывод]" : output)}" : $"Результат выполнения скрипта по команде \"{Keyword}\":\n```\n{output}\n```"));
                    //await client.SendTextMessageAsync(update.Message.Chat.Id, (UseParsing ? $"Результат выполнения скрипта:\n{output}" : $"Результат выполнения скрипта по команде \"{Keyword}\":\n```\n{output}\n```"),
                    //cancellationToken: token,
                    //replyToMessageId: update.Message.MessageId, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
            }
            catch (Exception proccessStartException)
            {
                BotLogger.Log($"Произошла ошибка при запуске скрипта {Keyword}:\n{proccessStartException.Message}", LogLevels.ERROR, logPath);
            }
        }
        protected async void SendCustomMessage_API(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, string message)
        {
            if (update.Message == null || string.IsNullOrEmpty(message)) return;
            if (update.Message.Chat.Id != 0)
            {
                if (update.Message.MessageId != 0)
                {
                    if (UseParsing)
                    {
                        await client.SendTextMessageAsync(update.Message.Chat.Id, message,
                        cancellationToken: token,
                        replyToMessageId: update.Message.MessageId,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(update.Message.Chat.Id, message,
                            cancellationToken: token,
                            replyToMessageId: update.Message.MessageId);
                    }

                }
                else
                {
                    if (UseParsing)
                    {
                        await client.SendTextMessageAsync(update.Message.Chat.Id, message,
                            cancellationToken: token,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(update.Message.Chat.Id, message,
                            cancellationToken: token);
                    }
                }
            }
        }
        public async Task<bool> GetFileContent()
        {
            try
            {
                string[] readContent = System.IO.File.ReadAllLines(Location);
                if (readContent.Length > 0)
                {
                    List<string> buffer = new List<string>();
                    string variation = string.Empty;
                    BotLogger.Log($"Успешно прочитано содержимое файла {Location} (строк: {readContent.Length})", BotLogger.LogLevels.INFO);
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < readContent.Length; i++)
                        {
                            if (string.IsNullOrEmpty(readContent[i])) continue;
                            if (readContent[i].Trim() == "##########")
                            {
                                if (string.IsNullOrEmpty(variation)) continue;
                                buffer.Add($"{variation}{Environment.NewLine}");
                                variation = string.Empty;
                                continue;
                            }
                            variation += $"{readContent[i].Trim()}\n";
                        }
                        Console.WriteLine($"ЗАПИСАНО В БУФЕР: {buffer.Count}");
                        FileContent = buffer;
                    });
                    BotLogger.Log($"Успешно добавлено вариаций текста: {buffer.Count}", BotLogger.LogLevels.SUCCESS);
                    return true;
                }
                BotLogger.Log($"Не удалось прочитать содержимое файла {Location}: слишком мало строк", BotLogger.LogLevels.ERROR);
            }
            catch (Exception fileReadException)
            {
                BotLogger.Log($"Не удалось прочитать содержимое файла {Location}:\n{fileReadException.Message}", BotLogger.LogLevels.ERROR);
            }
            return false;
        }
        public void ChangeActionAvailability() => IsActive = !IsActive;
        protected string GetArgs(string? updateMessage)
        {
            if (string.IsNullOrEmpty(updateMessage)) return string.Empty;
            return updateMessage.Replace(Keyword, string.Empty);
        }
        public string GetActionInfo()
        {
            if ((ImageGroupPaths != null) && (ImageGroupPaths.Length > 0))
            {
                string imagePathshPart = string.Empty;
                foreach (var item in ImageGroupPaths)
                {
                    imagePathshPart += $"\n- {item}";
                }
                return $"\tИмя: \"{Name}\"\tСтрока вызова: \"{Keyword}\"\tТип: {ActionType}\n" +
                    $"\tТолько администраторы: {IsAdmin}\t Активное: {IsActive}\n" +
                    $"\tВывод форматируется на стороне Телеграмм: {UseParsing}\tВывод логируется: {LogOutput}\n" +
                    $"\tСвязанный файл: \"{Location}\"\n" +
                    $"\tСвязанные файлы:\n" +
                    $"{imagePathshPart}";
            }
            return $"\tИмя: \"{Name}\"\tСтрока вызова: \"{Keyword}\"\tТип: {ActionType}\n" +
                $"\tТолько администраторы: {IsAdmin}\t Активное: {IsActive}\n" +
                $"\tВывод форматируется на стороне Телеграмм: {UseParsing}\tВывод логируется: {LogOutput}\n" +
                $"\tСвязанный файл: \"{Location}\"\n";

        }
    }
}
