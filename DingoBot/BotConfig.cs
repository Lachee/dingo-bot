using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DingoBot
{
    public class BotConfig
    {
        public string Prefix { get; set; } = "\\";


        [JsonIgnore]
        public string Token { get { return File.ReadAllText(TokenFile); } }
        public string TokenFile { get; set; } = "discord.key";

        [JsonIgnore]
        public string R6StatsToken { get { return File.ReadAllText(R6StatsTokenFile);  } }
        public string R6StatsTokenFile { get; set; } = "r6stats.key";


        public string Resources { get; set; } = "Resources/";

        public string WkHtmlToImage { get; set; } = @"D:\wkhtmltox\bin\wkhtmltoimage.exe";
        public string WkHtmlToImageArguments { get; set; } = "--quiet";

        public RedisConfig Redis { get; set; } = new RedisConfig();
        public class RedisConfig
        {
            public string Address = "127.0.0.1";
            public int Database = 0;
            public string Prefix = "dingo";
        }

    }
}
