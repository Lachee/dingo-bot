using DSharpPlus.Entities;
using DingoBot.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Extensions
{
    public static class RedisExtensions
    {
        public static Namespace ToNamespace(this DiscordGuild guild) => new Namespace(guild.Id);
    }
}
