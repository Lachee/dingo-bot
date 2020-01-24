using DingoBot.Entities;
using DingoBot.Redis;
using DingoBot.Redis.Serialize;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DingoBot.Extensions
{
    public class GuildSettings
    {
        /// <summary>
        /// The default prefix for a guild
        /// </summary>
        public static string DefaultPrefix { get; set; } = "?";

        //Cache of all the prefixes
        private static Dictionary<ulong, string> _prefixCache = new Dictionary<ulong, string>();

        /// <summary>
        /// Gets the prefix for a guild
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static async Task<string> GetPrefixAsync(DiscordGuild guild)
        {
            if (_prefixCache.TryGetValue(guild.Id, out var prefix))
                return prefix;

            var settings = await GetGuildSettingsAsync(guild);
            return settings.Prefix;
        }

        /// <summary>
        /// Gets the guild settings
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static async Task<GuildSettings> GetGuildSettingsAsync(DiscordGuild guild)
        {
            var settings = await Dingo.Instance.Redis.FetchObjectAsync<GuildSettings>(Namespace.Combine(guild, "settings"));
            if (settings == null)
            {
                settings = new GuildSettings(guild, DefaultPrefix);
                await settings.SaveAsync();
            }

            settings.Guild = guild;
            return settings;
        }



        /// <summary>
        /// The prefix of the guild
        /// </summary>
        [RedisProperty]
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                _prefixCache[GuildId] = value;
            }
        }
        private string _prefix;

        /// <summary>
        /// The guild the settings belong too. Maybe null.
        /// </summary>
        [RedisIgnore]
        public DiscordGuild Guild
        {
            get => _guild;
            set
            {
                _guild = value;
                GuildId = value.Id;
            }
        }
        private DiscordGuild _guild;

        /// <summary>
        /// The ID of the guild the settings belongs too.
        /// </summary>
        [RedisProperty]
        public ulong GuildId { get; private set; }

        [RedisProperty]
        public ulong BlackBaconId { get; set; } = 0;


        [RedisProperty]
        public bool PermissionAwardRoles { get; set; } = false;

        public GuildSettings() { }
        public GuildSettings(DiscordGuild guild, string prefix)
        {
            Guild = guild;
            Prefix = prefix;
        }

        /// <summary>
        /// Gets the black bacon role
        /// </summary>
        /// <returns></returns>
        public DiscordRole GetBlackBaconRole() => Guild?.GetRole(BlackBaconId);

        /// <summary>
        /// Gets the roles for the ranks
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<Rank, ulong>> GetRankRolesAsync() {
            var rr = await Dingo.Instance.Redis.FetchHashMapAsync(Namespace.Combine(GuildId, "rankroles"));
            var d = new Dictionary<Rank, ulong>(rr.Count);

            foreach(var kp in rr)
            {
                uint rank = uint.Parse(kp.Key);
                ulong role = ulong.Parse(kp.Value);
                d.Add((Rank)rank, role);
            }

            return d;
        }

        public async Task SaveRankRolesAsync(Dictionary<Rank, ulong> roles)
        {
            var rr = new Dictionary<string, string>(roles.Count);
            foreach(var kp in roles)
            {
                rr.Add(((uint)kp.Key).ToString(), kp.Value.ToString());
            }

            await Dingo.Instance.Redis.StoreHashMapAsync(Namespace.Combine(GuildId, "rankroles"), rr);
        }

        /// <summary>
        /// Saves the settings to the Redis
        /// </summary>
        /// <returns></returns>
        public Task SaveAsync()
        {
            return Dingo.Instance.Redis.StoreObjectAsync(Namespace.Combine(GuildId, "settings"), this);
        }
    }
}
