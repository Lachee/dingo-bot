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
using NReco.ImageGenerator;

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

            //Store the string
            try
            {
                //Fetch the profile and store it
                var profile = await Profile.LoadAsync(accountName);
                await Redis.StoreStringAsync(Namespace.Combine("link", ctx.Member), accountName);

                //Print out the profile image
                var image = await profile.RenderProfileImageAsync();
                using (MemoryStream stream = new MemoryStream(image))
                    await ctx.RespondWithFileAsync(accountName + ".png", stream, "The following profile has been associated with you: ");

                //Now try and figure out the correct role
                var settings = await ctx.Guild.GetSettingsAsync();
                var rankedRoleIds = await settings.GetRankRolesAsync();

                //Prepare a list of roles and remove all the ranked roles
                var rolelist = ctx.Member.Roles.ToList();                
                foreach(var rrkp in rankedRoleIds)
                    rolelist.RemoveAll(r => r.Id == rrkp.Value);

                //Find the closest rank
                Rank previous = Rank.Unranked;
                foreach (var v in rankedRoleIds.Keys)
                {
                    if (profile.MMR >= (uint)v) previous = v;
                    if (profile.MMR < (uint)v) break;
                }

                //Add it to our list
                if (previous != Rank.Unranked)
                {
                    var r = ctx.Guild.GetRole(rankedRoleIds[previous]);
                    rolelist.Add(r);
                }

                //Apply it
                await ctx.Member.ReplaceRolesAsync(rolelist);

            }
            catch (Exception e)
            {
                Logger.LogError(e);
                await ctx.ReplyReactionAsync(false);
            }
        }

        [Command("profile"), Aliases("p")]
        [Description("Gets the siege profile for the user")]
        public async Task Fetch(CommandContext ctx, DiscordMember member = null)
        {
            if (member == null)
                member = ctx.Member;

            //Fetcht he account name
            await ctx.ReplyWorkingAsync();
            string accountName = await Redis.FetchStringAsync(Namespace.Combine("link", member));
            if (accountName == null)
            {
                await ctx.ReplyReactionAsync(false);
                return;
            }

            //Fetch the profile
            var profile = await Profile.LoadAsync(accountName);
            var image = await profile.RenderProfileImageAsync();
            using (MemoryStream stream = new MemoryStream(image)) 
                await ctx.RespondWithFileAsync(accountName + ".png", stream);

            //Modify the embed and send it
            /*
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder(profile.CreateEmbed());
            builder.WithFooter($"{member.DisplayName}'s Profile", EmbedExtensions.GetAvatarURL(member));
            await ctx.ReplyAsync(embed: builder.Build());
            */
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
            
            //Fetch the profile
            var profile = await Profile.LoadAsync(accountName);
            var image = await profile.RenderProfileImageAsync();

            using (MemoryStream stream = new MemoryStream(image))
                await ctx.RespondWithFileAsync(accountName + ".png", stream);
        }

    }
}
