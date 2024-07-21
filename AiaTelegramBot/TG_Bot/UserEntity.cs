using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiaTelegramBot.TG_Bot
{
    internal class UserEntity
    {
        public long UserID {  get; set; }
        public string[]? ActiveComands { get; set; }
    }
}
