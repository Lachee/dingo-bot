using DingoBot.CommandNext;
using DingoBot.Extensions;
using DingoBot.Logging;
using DingoBot.Managers;
using DingoBot.Redis;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DingoBot
{
    public class Dingo
    {
        public static Dingo Instance { get; private set; }

        public BotConfig Configuration { get; }

        public Logger Logger { get; }

        public IRedisClient Redis { get; }

        public DiscordClient Discord { get; }
        public CommandsNextExtension CommandsNext { get; }
        public ReplyManager ReplyManager { get; }
        public SiegeManager SiegeManager { get; }
        public LastManager LastManager { get; }

        public Dingo(BotConfig config)
        {
            Instance = this;
            Configuration = config;
            Logger = new Logger("BOT");

            Logger.Log("Creating Stack Exchange Client");
            Redis = new StackExchangeClient(config.Redis.Address, config.Redis.Database, Logger.CreateChild("REDIS"));
            Namespace.SetRoot(config.Redis.Prefix);

            Logger.Log("Creating new Bot");
            Discord = new DiscordClient(new DiscordConfiguration() { Token = config.Token });

            Logger.Log("Creating Instances");
            ReplyManager = new ReplyManager(this, Logger.CreateChild("REPLY"));
            SiegeManager = new SiegeManager(this, Logger.CreateChild("SIEGE"));
            LastManager = new LastManager(this, Logger.CreateChild("LAST"));

            Logger.Log("Creating Command Next");
            var deps = new ServiceCollection()
                .AddSingleton(this)
                .BuildServiceProvider(true);

            CommandsNext = Discord.UseCommandsNext(new CommandsNextConfiguration() { PrefixResolver = ResolvePrefix, Services = deps });
            CommandsNext.RegisterConverter(new QueryConverter());
            CommandsNext.RegisterConverter(new CommandQueryArgumentConverter());
            CommandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
            CommandsNext.CommandErrored += HandleCommandErrorAsync;

            this.Discord.ClientErrored += async (error) => await LogException(error.Exception);
        }

        internal async Task InitAsync()
        {
            Logger.Log("Initializing Redis");
            await Redis.InitAsync();

            Logger.Log("Initializing Discord");
            await Discord.ConnectAsync();

            Logger.Log("Ready for Commands...");
        }

        internal async Task DeinitAsync()
        {
            await Discord.DisconnectAsync();
            Redis.Dispose();
            Discord.Dispose();
        }


        /// <summary>
        /// Resolves the prefix of the message and returns a index to trim from.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<int> ResolvePrefix(DiscordMessage message)
        {
            try
            {
                if (message.Author == null || message.Author.IsBot) return -1;

                //Get the prefix. If we fail to find the prefix then we will get it from the cache
                //if (!_guildPrefixes.TryGetValue(message.Channel.GuildId, out prefix))
                //{
                //    Logger.Log("Prefix Cache Miss. Fetching new prefix for guild " + message.Channel.GuildId);
                //    prefix = await Redis.FetchStringAsync(Namespace.Combine(message.Channel.GuildId, "prefix"), Configuration.Prefix);
                //    await UpdatePrefix(message.Channel.Guild, prefix);
                //}

                //Get the position of the prefix
                string prefix = await GuildSettings.GetPrefixAsync(message.Channel.Guild);
                var pos = message.GetStringPrefixLength(prefix);

                /*
                if (pos >= 0)
                {
                    //Make sure we are allowed to execute in this channel
                    // We want to be able to execute in this channel unless specifically denied.
                    var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);
                    var state = await member.HasPermissionAsync($"koala.execute.{message.ChannelId}", bypassAdmin: true, allowUnset: true);
                    if (!state) return -1;
                }
                */

                //Return the index of the prefix
                return pos;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e);
                return -1;
            }
        }

        /// <summary>
        /// Handles Failed Executions
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task HandleCommandErrorAsync(CommandErrorEventArgs e)
        {
            //Log the exception
            Logger.LogError(e.Exception);

            //Check if we have permission
            if (e.Exception is ChecksFailedException cfe)
            {
                var first = cfe.FailedChecks.FirstOrDefault();
                    //Generic bad permissions
                    await e.Context.ReplyExceptionAsync($"You failed the check {first.GetType().Name} and cannot execute the function.");

                    //Save the execution to the database
                    //await (new CommandLog(e.Context, failure: $"Failed {first.GetType().Name} check.")).SaveAsync(DbContext);
                    return;
            }
            
            //The bot itself is unable to do it.
            if (e.Exception is DSharpPlus.Exceptions.UnauthorizedException)
            {
                var trace = e.Exception.StackTrace.Split(" in ", 2)[0].Trim().Substring(3);
                await e.Context.ReplyAsync($"I do not have permission to do that, sorry.\n`{trace}`");

                //Save the execution to the database
                //await (new CommandLog(e.Context, failure: $"Unauthorized")).SaveAsync(DbContext);
                return;
            }

            //We dont know the command, so just skip
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
            {
                //Save the execution to the database
                //await (new CommandLog(e.Context, failure: $"Command Not Found")).SaveAsync(DbContext);
                return;
            }

            //If all else fails, then we will log it
            await e.Context.ReplyExceptionAsync(e.Exception, false);

            //Save the execution to the database
            //await (new CommandLog(e.Context, failure: $"Exception: {e.Exception.Message}")).SaveAsync(DbContext);
            return;
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task LogException(Exception exception, DiscordMessage context = null)
        {
            Logger.LogError(exception);
            return Task.CompletedTask;

            /*
            var hook = await Discord.GetWebhookAsync(Configuration.);
            await hook.ExecuteAsync("An error has occured on " + Discord.CurrentApplication.Name + ". ", embeds: new DiscordEmbed[] {
                exception.ToEmbed(),
                new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Orange,
                    Title = "Details",
                    Timestamp = DateTime.UtcNow
                }
                .AddField("Guild", context?.Channel.GuildId.ToString())
                .AddField("Channel", context?.Channel.Id.ToString())
                .AddField("Message", context?.Id.ToString())
            }, files: null);
            */
        }

    }
}
