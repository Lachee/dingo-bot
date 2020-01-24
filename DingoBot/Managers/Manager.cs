using DSharpPlus;
using DingoBot.Logging;
using DingoBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Managers
{
    public class Manager
    {
        public Logger Logger { get; }
        public Dingo Bot { get; }

        public DiscordClient Discord => Bot.Discord;
        public IRedisClient Redis => Bot.Redis;

        public Manager(Dingo bot, Logger logger = null)
        {
            Bot = bot;
            Logger = logger ?? new Logger(GetType().Name);
        }
    }
}
