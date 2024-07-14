using AiaTelegramBot.Logging;
using AiaTelegramBot.TG_Bot.models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace AiaTelegramBot.API
{
    internal class BotAPIEntity
    {
        public ushort Port { get; protected set; } = 3200;
        //public string ApiKey { get; protected set; } = string.Empty;
        public string? LogPath { get; protected set; } = string.Empty;
        protected ITelegramBotClient? Client;
        private TcpListener? APIListener;
        public CancellationTokenSource CancellationToken { get; protected set; }
        public BotAPIEntity(ushort port, string? logPath, ITelegramBotClient? botClient, CancellationTokenSource cancellationToken)
        {
            Port = port;
            LogPath = logPath;
            Client = botClient;
            CancellationToken = cancellationToken;
        }
        public async void Listen()
        {
            BotLogger.Log($"Запуск API на порту {Port}", BotLogger.LogLevels.INFO, LogPath);
            try
            {
                APIListener = new TcpListener(IPAddress.Any, Port);
                while (true)
                {
                    var tcpClient = await APIListener.AcceptTcpClientAsync();
                    ProcessClientAsync(tcpClient);
                }
            }
            catch (Exception tcpListenerException)
            {
                BotLogger.Log($"Ошибка при запуске сервиса API:\n{tcpListenerException.Message}", BotLogger.LogLevels.ERROR, LogPath);
            }
            finally
            {
                StopApi();
            }
        }
        public void StopApi()
        {
            APIListener?.Stop();
            BotLogger.Log($"Сервис API остановлен", BotLogger.LogLevels.INFO, LogPath);
        }
        protected async void ProcessClientAsync(TcpClient tcpClient)
        {
            BotLogger.Log($"Новое подключение к API, клиент: {tcpClient.Client.RemoteEndPoint}", BotLogger.LogLevels.WARNING, LogPath);
            NetworkStream ns = tcpClient.GetStream();
            byte[] bytes = new byte[tcpClient.ReceiveBufferSize];
            ns.Read(bytes, 0, bytes.Length);
            string receivedString = Encoding.Unicode.GetString(bytes);
            BotAction? targetAction = JsonConvert.DeserializeObject<BotAction>(receivedString);
            if (targetAction == null)
            {
                BotLogger.Log($"Не удалось десериализовать действие, полученное через API!", BotLogger.LogLevels.ERROR);
            }
            else
            {
                if (!targetAction.IsActive)
                {
                    BotLogger.Log($"Не удалось выполнить действие, полученное через API: переданное действие помечено как неактивное:\t\"IsActive\": \"{targetAction.IsActive}\"", BotLogger.LogLevels.ERROR);
                }
                else
                {
                    BotLogger.Log($"Выполнео действие, полученное через API!", BotLogger.LogLevels.SUCCESS);
                    BotLogger.Log($"---------- Полученные данные ----------\n{receivedString}\n---------------------------------------", BotLogger.LogLevels.INFO);
                }
                //await targetAction.RunAction();

                // ITelegramBotClient client,
                // Telegram.Bot.Types.Update update,
                // CancellationToken token
            }
            tcpClient.Close();
        }
    }
}
