using DingoBot.Entities;
using DingoBot.Logging;
using DingoBot.Redis;
using DingoBot.WkHtml;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DingoBot.Managers
{
    public class LastManager : Manager
    {
        /// <summary>Regex for detecting URLs</summary>
        private static Regex UrlRegex = new Regex(@"(?:(?:https?|ftp|file):\/\/|www\.|ftp\.)(?:\([-A-Z0-9+&@#\/%=~_|$?!:,.]*\)|[-A-Z0-9+&@#\/%=~_|$?!:,.])*(?:\([-A-Z0-9+&@#\/%=~_|$?!:,.]*\)|[A-Z0-9+&@#\/%=~_|$])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private Dictionary<ulong, Last> _lasts = new Dictionary<ulong, Last>();

        public class Last
        {
            public ulong MessageId = 0;
            public string URL = null;

            public Task CheckAsync(DiscordMessage message) {

                //Set some obvious
                MessageId = message.Id;

                //Detect URL
                var matches = UrlRegex.Match(message.Content);
                if (matches.Success) URL = matches.Value;

                return Task.CompletedTask;
            }
        }

        public LastManager(Dingo bot, Logger logger = null) : base(bot, logger)
        {
            bot.Discord.MessageCreated += (e) => GetLast(e.Channel).CheckAsync(e.Message);
        }

        public Last GetLast(DiscordChannel channel) {
            if (!_lasts.ContainsKey(channel.Id))
                _lasts.Add(channel.Id, new Last());
            return _lasts[channel.Id];
        }

    }
}
