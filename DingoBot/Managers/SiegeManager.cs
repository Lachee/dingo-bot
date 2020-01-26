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
using System.Threading.Tasks;

namespace DingoBot.Managers
{
    public class SiegeManager : Manager
    {
        private static readonly string API = "https://d.lu.je/siege";

        public HttpClient HttpClient { get; }
        public string RedisPrefix { get; set; } = "r6";

        /// <summary>
        /// Current MMR Season
        /// </summary>
        public uint Season { get; set; } = 7;

        public SiegeManager(Dingo bot, Logger logger = null) : base(bot, logger)
        {
            HttpClient = new HttpClient();
        }

        /// <summary>
        /// Links a user to the account name and returns the profile
        /// </summary>
        /// <param name="user"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public async Task<SiegeProfile> LinkProfileAsync(DiscordUser user, string accountName) {
            await Redis.StoreStringAsync(Namespace.Combine(RedisPrefix, "links"), user.Id.ToString(), accountName);
            return await GetProfileAsync(accountName);
        }

        /// <summary>
        /// Fetches a profile from the DiscordUser. Returns null if there is none linked
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<SiegeProfile> GetProfileAsync(DiscordUser user, bool recache = false) {
            string accountName = await Redis.FetchStringAsync(Namespace.Combine(RedisPrefix, "links"), user.Id.ToString(), null);
            if (accountName == null) return null;

            return await GetProfileAsync(accountName, recache);
        }

        /// <summary>
        /// Fetches a profile and loads stuff
        /// </summary>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public async Task<SiegeProfile> GetProfileAsync(string accountName, bool recache = false) {
            var profile = new SiegeProfile(this, accountName);
            await profile.UpdateAsync(recache);
            return profile;
        }

        /// <summary>
        /// Fetches the string from the API
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        internal async Task<T> InternalGetStringFromAPIAsync<T>(string endpoint) {
            string json = await HttpClient.GetStringAsync(API + endpoint);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Prepares a new renderer
        /// </summary>
        /// <returns></returns>
        internal WkHtmlRenderer InternalGetWkHtmlRenderer() {
            //Generate a new image
            return new WkHtmlRenderer(Bot.Configuration.WkHtmlToImage, Bot.Configuration.WkHtmlToImageArguments)
            {
                EmbedCookies = false,
                Width = 540,
                Height = 450,
                Cropping = new WkHtml.WkHtmlRenderer.Crop()
                {
                    X = 0,
                    Y = 0,
                    Width = 540,
                    Height = 450,
                }
            };
        }

        /// <summary>
        /// Gets the current full path to the HTML
        /// </summary>
        /// <returns></returns>
        internal string InternalGetWkHtmlPage() => Path.GetFullPath(Path.Combine(Bot.Configuration.Resources, "profile/", "slider.html"));
    }
}
