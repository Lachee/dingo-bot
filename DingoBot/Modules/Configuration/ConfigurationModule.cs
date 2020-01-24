using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DingoBot.Redis;
using DingoBot.Extensions;
using DingoBot.Logging;
using System.IO;
using DSharpPlus.Entities;
using DingoBot.CommandNext;
using DingoBot.Entities;
using System.Linq;

namespace DingoBot.Modules.Configuration
{
    [Group("config")]
    [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
    public partial class ConfigurationModule : BaseCommandModule
    {
        public Dingo Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }

        public ConfigurationModule(Dingo bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-CONFIG", bot.Logger);
        }

        [Command("prefix")]
        [Description("Sets the prefix of the bot for the guild.")]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException("prefix", "Prefix cannot be null or empty.");

            //Fetch the settings, update its prefix then save again
            var settings = await GuildSettings.GetGuildSettingsAsync(ctx.Guild);
            settings.Prefix = prefix;
            await settings.SaveAsync();

            //Respond that we did that.
            await ctx.ReplyReactionAsync(true);
        }

        [Command("blackbacon"), Aliases("bb")]
        [Description("Sets the black bacon")]
        public async Task SetBlackBacon(CommandContext ctx, DiscordRole role)
        {
            if (role == null)
                throw new ArgumentNullException("role", "Role cannot be null!");

            //Fetch the settings, update its prefix then save again
            var settings = await GuildSettings.GetGuildSettingsAsync(ctx.Guild);
            settings.BlackBaconId = role.Id;
            await settings.SaveAsync();

            //Respond that we did that.
            await ctx.ReplyReactionAsync(true);
        }

        [Command("createrankroles")]
        [Description("Creates a collection of roles for the ranks and stores them")]
        public async Task CreateRankRoles(CommandContext ctx)
        {
            ctx.ReplyWorkingAsync();

            var roleRanks = new Dictionary<Rank, ulong>();
            roleRanks.Add(Rank.BronzeV, await CreateRankRole(ctx.Channel.Guild, Rank.BronzeV, "R6: Bronze"));
            roleRanks.Add(Rank.SilverV, await CreateRankRole(ctx.Channel.Guild, Rank.SilverV, "R6: Silver"));
            roleRanks.Add(Rank.GoldIII, await CreateRankRole(ctx.Channel.Guild, Rank.GoldIII, "R6: Gold"));
            roleRanks.Add(Rank.PlatinumIII, await CreateRankRole(ctx.Channel.Guild, Rank.PlatinumIII, "R6: Plat"));
            roleRanks.Add(Rank.Diamond, await CreateRankRole(ctx.Channel.Guild, Rank.Diamond, "R6: Diamond"));

            var settings = await ctx.Guild.GetSettingsAsync();
            await settings.SaveRankRolesAsync(roleRanks);

            await settings.SaveAsync();
            await ctx.ReplyReactionAsync(true);
        }

        private async Task<ulong> CreateRankRole(DiscordGuild guild, Rank rank, string title) {
            var role = await guild.CreateRoleAsync(title, permissions: DSharpPlus.Permissions.None, color: rank.GetColor());
            return role.Id;
        }
    }
}
