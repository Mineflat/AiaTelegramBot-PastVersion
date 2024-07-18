using AiaTelegramBot.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AiaTelegramBot.TG_Bot.models
{
    internal class BotConfigurationUnit
    {
        // Рабочая директория бота, в которой будут храниться его временные файлы: 
        // логи, история переписки, список имен пользователей (если не запрещено)
        public string? WorkingDirectory { get; set; } = "";
        // Путь до файла со списком числовых идентификаторов пользователей Телеграмм, 
        // которые являются администраторами бота. 
        // Если не задан или задан пустой файл, никто из пользователей не может использовать
        // команды, помеченные как "IsAdmin": "true"
        public string? WhitelistLocation { get; set; } = "";
        // Содержит список из идентификаторов пользователей, которые являются администраторами бота
        // Список идентификаторов читается из файла по пути из WhitelistLocation
        public List<string> Whitelist { get; set; } = new List<string>();
        // Указывает, что бот может сохранять переписку на диск.
        // Если разрешено (путь заполнен), то бот сохранит переписку в директории [WorkingDirectory]/Conversation/[ID_чата]*
        public bool StoreConversationStory { get; set; } = false;
        // Указывает, что бот может сохранять имена пользователей, которые ему пишут, на диск.
        // Если разрешено, то бот сохранит идентификаторы пользователей в файл [WorkingDirectory]/[имя_бота]_uuids.txt
        public bool StoreNewUsernames { get; set; } = false;
        // Указывает, что бот может сохранять файл истории, на диск.
        // Если разрешено, то бот будет писать историю сообщений (лог) в файл [WorkingDirectory]/[имя_бота].log
        public bool StoreLogs { get; set; } = false;
        // Указывает на директорию со списком действий.
        // Каждый отдельный файл в директории яволяется действием, описанным в формате JSON. 
        // Например:
        // {
        //   "name": "имя_действия",     // Псевдоним действия
        //   "keyword": "имя",           // Указывает команду бота, по которой вызывается это действие
        //   "is_active": "true/false",  // Указывает, может ли быть вызван этот метод
        //   "is_admin": "true/false",   // Указывает, что метод вызывает только администратор из белого списка
        //   "type": "random_str",       // Указывает, какой тип команды используется: random_str, file, image, script 
        //                                   text:  Ожидает, что в JSON-файле - только массив элементов c полем "text"
        //                                       Метод выбирает рандомный элемент и отправляет содержимое поля "text"
        //                                   file:  Отправит указанный файл в чат 
        //                                   random_image: Отправит указанную картинку в чат
        //                                   script Запустит указанный скрипт, дождется его окончания и отправит его вывод 
        //                                       (std_out) в чат
        //   "Location": "файл"          // Указывает, с каким конкретно файлом производить действие типа "Type"
        // }
        public string? ActionsDirectory { get; set; } = "";
        public bool HideInactiveActions { get; set; } = false;

        public ushort ApiPort { get; protected set; } = 3200;
        public bool IsApiEnabled { get; protected set; } = false;
        //public string ApiKey { get; protected set; } = string.Empty;
        public string? LogPath { get; protected set; } = string.Empty;
        public string? EnvPath { get; protected set; } = string.Empty;

        #region По командам 
        // Список команд администратора:
        // - actions - Посмотреть список всех дейсчтвий, которые пропарсил бот
        // - update - Обновить список дейсчтвий, которые лежат в директории ActionsDirectory
        // - info - Получить информацию о настройках бота 
        // - status - Получить информацию о кол-ве обработанных ботом сообщений, их авторов и пользователях из белого списка 
        //   формат: [username]:[msgs_count]
        // - stop - немедленная остановка бота
        // - restart - выполняет остановку и запуск бота, при этом вызывается команда update
        //   Обратите внимание, что для перезапуска необходим повторный ввод токена бота
        #endregion
        public BotConfigurationUnit(string? workingDirectory = "",
            string? whitelistLocation = "",
            bool storeConversationStory = false,
            bool storeNewUsernames = false,
            bool storeLogs = false,
            string? actionsDirectory = "", ushort apiPort = 3200, bool isApiEnabled = false)
        {
            WorkingDirectory = workingDirectory;
            WhitelistLocation = whitelistLocation;
            StoreConversationStory = storeConversationStory;
            StoreNewUsernames = storeNewUsernames;
            StoreLogs = storeLogs;
            ActionsDirectory = actionsDirectory;
            ApiPort = apiPort;
            IsApiEnabled = isApiEnabled;
            UpdateBotWhiteList();
            ExportEnvVars();
        }

        public bool ExportEnvVars()
        {
            try
            {
                if (File.Exists(EnvPath))
                {
                    BotLogger.Log($"Экспорт переменных окруженя из файла {EnvPath}...", BotLogger.LogLevels.INFO, LogPath);
                    List<string> lines = File.ReadLines(EnvPath).ToList();
                    string[] buffer;
                    if (lines.Count > 0)
                    {
                        for (int i = 0; i < lines.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(lines[i])) continue;
                            buffer = lines[i].Split('=');
                            if ((buffer.Length == 2) && (!string.IsNullOrEmpty(buffer[0])) && (!string.IsNullOrEmpty(buffer[1])))
                            {
                                try
                                {
                                    Environment.SetEnvironmentVariable(buffer[0], buffer[1]);
                                }
                                catch (Exception setEnvError)
                                {
                                    BotLogger.Log($"Ошибка экспорта переменной окружения: {setEnvError.Message} пуст", BotLogger.LogLevels.WARNING, LogPath);
                                }
                            }
                        }
                        return true;
                    }
                    BotLogger.Log($"Ошибка экспорта переменных окруженя - файл {EnvPath} пуст", BotLogger.LogLevels.WARNING, LogPath);
                }
                BotLogger.Log($"Ошибка экспорта переменных окруженя - файл {EnvPath} не указан или не существует", BotLogger.LogLevels.WARNING, LogPath);
            }
            catch (Exception e)
            {
                BotLogger.Log($"Ошибка экспорта переменных окруженя из файла {EnvPath}: {e.Message}", BotLogger.LogLevels.ERROR, LogPath);
            }
            return false;
        }
        public bool UpdateBotWhiteList()
        {
            if (!File.Exists(WhitelistLocation))
            {
                BotLogger.Log($"Не задан файл с пользователями из белого списка. Команды, помеченные как \"IsAdmin\": \"true\" не будут обрабатываться",
                    BotLogger.LogLevels.WARNING,
                    $"{WorkingDirectory}/latest.log");
                return false;
            }
            try
            {
                BotLogger.Log($"Начата попытка обновления белого списка из файла: \"{WhitelistLocation}\"",
                    BotLogger.LogLevels.INFO,
                    $"{WorkingDirectory}/latest.log");
                List<string> buffer = File.ReadAllLines(WhitelistLocation).ToList();
                if (buffer.Count > 0)
                {
                    BotLogger.Log($"Белый список обновлен из файла \"{WhitelistLocation}\": {Whitelist?.Count ?? 0} => {buffer.Count}",
                        BotLogger.LogLevels.SUCCESS,
                        $"{WorkingDirectory}/latest.log");
                    Whitelist = new List<string>();
                    buffer.ForEach(UUID =>
                    {
                        if (!string.IsNullOrEmpty(UUID))
                        {
                            if (long.TryParse(UUID, out _))
                            {
                                BotLogger.Log($"Добавлен пользователь с идентификатором {UUID}",
                                    BotLogger.LogLevels.INFO,
                                    $"{WorkingDirectory}/latest.log");
                                Whitelist.Add(UUID);
                            }
                            else
                            {
                                BotLogger.Log($"Пользователь с идентификатором \"{UUID}\" не может быть добавлен (нечисловой идентификатор)",
                                    BotLogger.LogLevels.ERROR,
                                    $"{WorkingDirectory}/latest.log");
                            }
                        }
                    });
                    return true;
                }
                BotLogger.Log($"Попытка обновления белого списка из файла \"{WhitelistLocation}\" провалилась, т.к. файл не содержит записей",
                    BotLogger.LogLevels.ERROR,
                    $"{WorkingDirectory}/latest.log");
            }
            catch (Exception fileReadException)
            {
                BotLogger.Log($"Не удалось прочитать список идентификаторов пользователей:\n{fileReadException.Message}",
                    BotLogger.LogLevels.ERROR, $"{WorkingDirectory}/latest.log");
            }
            return false;
        }
        public string GetBotWhiteList()
        {
            if (Whitelist.Count > 0)
            {
                string whitelist_buffer = "Белый список бота:\n";
                Whitelist.ForEach(x => whitelist_buffer += $"`{x}`\n");
                return whitelist_buffer;
            }
            return "Белый список пуст. Команды администратора не выполняются";
        }
        public string GetBotConfiguration()
        {
            return $"⚙️ *Рабочая директория бота:*\n```\n{WorkingDirectory}\n```\n" +
                $"⚙️ *Путь к белому списку:*\n```\n{WhitelistLocation ?? "[не задан] (команды администратора невозможно вызвать)"}\n```\n" +
                $"⚙️ *Сохранять историю переписки:* `{StoreConversationStory}`\n" +
                $"⚙️ *Сохранять идентификаторы новых пользователей:* `{StoreNewUsernames}`\n" +
                $"⚙️ *Запись лога в файл:* `{StoreLogs}`\n" +
                $"⚙️ *Директория действий бота:*\n```\n{ActionsDirectory}\n```\n" +
                $"⚙️ *API разрешено:* `{IsApiEnabled}`\n" +
                $"⚙️ *Порт API:* `{ApiPort}`";
        }
    }
}
