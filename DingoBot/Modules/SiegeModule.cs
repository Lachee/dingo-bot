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
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using DingoBot.CommandNext;
using System.Net.Http;
using DingoBot.Entities;

namespace DingoBot.Modules
{
    public class SiegeModule : BaseCommandModule
    {
        private readonly Regex DiceRegex = new Regex(@"(?'count'\d{1,2})[dD](?'sides'\d{1,3})", RegexOptions.Compiled);

        public Dingo Bot { get; }
        public IRedisClient Redis => Bot.Redis;
        public Logger Logger { get; }


        public SiegeModule(Dingo bot)
        {
            this.Bot = bot;
            this.Logger = new Logger("CMD-FUN", bot.Logger);
        }

        [Command("link"), Aliases("l")]
        [Description("Links a siege account with your discord")]
        public async Task Link(CommandContext ctx, [RemainingText] string accountName)
        {
            await ctx.ReplyWorkingAsync();

            var profile = await Bot.SiegeManager.LinkProfileAsync(ctx.Member, accountName);

            //Render the profile
            var render = await profile.GetProfileRenderAsync();
            await ctx.ReplyWithFileAsync($"{profile.Name}.png", render, "**Profile Linked**");
        }

        [Command("profile"), Aliases("p")]
        [Description("Gets the siege profile for the user")]
        public async Task Fetch(CommandContext ctx, DiscordMember member = null)
        {
            await ctx.ReplyWorkingAsync();
            if (member == null)  member = ctx.Member;

            //Fetch the profile
            var profile = await Bot.SiegeManager.GetProfileAsync(member);
            if (profile == null)
            {
                //Null profile, probably meant the other one
                await FetchAccount(ctx, member.Username);
                return;
            }

            //Render the profile
            var render = await profile.GetProfileRenderAsync();
            await ctx.ReplyWithFileAsync($"{profile.Name}.png", render);
        }

        [Command("profile")]
        public async Task FetchAccount(CommandContext ctx, [RemainingText] string accountName)
        {
            //Fetcht he account name
            await ctx.ReplyWorkingAsync();

            //Make sure account name is valid
            if (string.IsNullOrEmpty(accountName))
            {
                await ctx.ReplyReactionAsync(false);
                return;
            }

            //Fetch the account
            var profile = await Bot.SiegeManager.GetProfileAsync(accountName);

            //Render the profile
            var render = await profile.GetProfileRenderAsync();
            await ctx.ReplyWithFileAsync($"{profile.Name}.png", render);
        }

        [Command("check"), Aliases("c")]
        [Description("Checks for a newer version of the profile, forcing a recache.")]
        public async Task RecacheAccount(CommandContext ctx, [RemainingText] string accountName)
        {

            //Fetcht he account name
            await ctx.ReplyWorkingAsync();

            //Make sure account name is valid
            if (string.IsNullOrEmpty(accountName))
            {
                await ctx.ReplyReactionAsync(false);
                return;
            }

            //Fetch the account and force it to update
            var profile = await Bot.SiegeManager.GetProfileAsync(accountName, true);

            //Render the profile
            var render = await profile.GetProfileRenderAsync();
            await ctx.ReplyWithFileAsync($"{profile.Name}.png", render);
        }

        [Command("clear")]
        [RequireOwner]
        [Description("Clears previously cached images")]
        public async Task ClearRender(CommandContext ctx, [RemainingText] string accountName) {

            await ctx.ReplyWorkingAsync();

            //Make sure account name is valid
            if (string.IsNullOrEmpty(accountName))
            {
                await ctx.ReplyReactionAsync(false);
                return;
            }

            //Get all the keys. Note this is custom because the Profile doesnt actually contain this information
            var cacheKey = Namespace.Combine(Bot.SiegeManager.RedisPrefix, "profiles", accountName.ToLowerInvariant(), "render");
            var searchKey = cacheKey + "*";

            var redis = Redis as StackExchangeClient;
            var server = redis.GetServersEnumable().First();
            var keys = server.Keys(pattern: searchKey);

            //Prepare a transaction
            var t = Redis.CreateTransaction();

            //Delete all the keys
            foreach (var k in keys) {
               _ = t.RemoveAsync(k);
            }

            //Execute Transactoin
            await t.ExecuteAsync();
            await ctx.ReplyReactionAsync(true);
        }
    }
}
