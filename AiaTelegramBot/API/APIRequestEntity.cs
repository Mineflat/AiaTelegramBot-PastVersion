using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiaTelegramBot.API
{
    internal class APIRequestEntity
    {
        public string ActionName {  get; set; } = string.Empty;
        public long ChatID { get; set; } = 0;

        public string? Args {  get; set; } = null;

        public APIRequestEntity(string actionName, long chatID, string? args)
        {
            ActionName = actionName;
            ChatID = chatID;
            Args = args;
        }
    }
}
