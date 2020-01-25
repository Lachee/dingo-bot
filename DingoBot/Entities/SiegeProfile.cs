using DingoBot.Managers;
using DingoBot.Redis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DingoBot.Entities
{
    public class SiegeProfile : Base64Serializable
    {
        private const bool DEBUG_COOKIES = true;
        private const bool NO_CACHE_RENDER = true;

        public delegate void MMRHistoryEvent(SiegeProfile profile, MMRHistory previous, MMRHistory current);

        /// <summary>
        /// Called while updating and a new rank has been reached.
        /// </summary>
        public event MMRHistoryEvent OnSeasonHigh;

        /// <summary>
        /// Called while updating and the account rank has changed
        /// </summary>
        public event MMRHistoryEvent OnRankChange;

        /// <summary>
        /// The profile's manager
        /// </summary>
        [JsonIgnore]
        public SiegeManager Manager { get; }

        /// <summary>
        /// The name of the profile
        /// </summary>
        public string Name { get; }

        private string ApiName => Name.ToLowerInvariant();

        /// <summary>
        /// How long before the previously cached profile data expires
        /// </summary>
        public TimeSpan ProfileDataTLL { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How long before the previously cached operators expire
        /// </summary>
        public TimeSpan OperatorTTL { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// How long before the previously rendered profile expires
        /// </summary>
        public TimeSpan ProfileRenderTLL{ get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// The profile data
        /// </summary>
        public ProfileData Profile { get; private set; }

        /// <summary>
        /// The MMR history
        /// </summary>
        public MMRHistory MMRHistory { get; private set; }

        public IReadOnlyDictionary<string, Operator> Operators { get; private set; }

        public SiegeProfile(SiegeManager manager, string accountName)
        {
            Name = accountName;
            Manager = manager;
        }

        /// <summary>
        /// Loads the initial data of the profile
        /// </summary>
        /// <returns></returns>
        public async Task UpdateAsync(bool recache = false)
        {
            //Load the profile data
            Profile = await UpdateProfileAsync(recache);

            //Load ops
            Operators = await UpdateOperatorsAsync(recache);

            //Load the MMR
            MMRHistory = await UpdateMMRHistoryAsync();
        }


        #region Updates

        /// <summary>Fetches a profile</summary>
        public async Task<ProfileData> UpdateProfileAsync(bool recache = false)
        {
            var cacheName = Namespace.Combine(Manager.RedisPrefix, "profiles", ApiName, "b64");

            if (!recache)
            {
                //Fetch the base64 encoded profile. If we find it, then store it.
                var p64 = await Manager.Redis.FetchStringAsync(cacheName);
                if (!string.IsNullOrWhiteSpace(p64))
                    return Profile = ProfileData.FromBase64(p64);
            }

            //Fetch the client
            Profile = await Manager.InternalGetStringFromAPIAsync<ProfileData>($"/profile.php?username={ApiName}");
            await Manager.Redis.StoreStringAsync(cacheName, Profile.ToBase64());
            await Manager.Redis.SetExpiryAsync(cacheName, ProfileDataTLL);
            return Profile;
        }

        /// <summary>
        /// Updates the operators
        /// </summary>
        /// <param name="recache"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, Operator>> UpdateOperatorsAsync(bool recache = false) {

            var cacheName = Namespace.Combine(Manager.RedisPrefix, "profiles", ApiName, "operators");
            if (!recache)
            {
                //Fetch the cached base64 hashset
                var d64 = await Manager.Redis.FetchHashMapAsync(cacheName);
                if (d64.Count != 0)
                {
                    //Decode the base64 to operators
                    var dop = new Dictionary<string, Operator>(d64.Count);
                    foreach (var kp in d64) dop.Add(kp.Key, Operator.FromBase64(kp.Value));

                    //Return
                    return dop;
                }
            }

            //Fetch the operators
            var operators = await Manager.InternalGetStringFromAPIAsync<Dictionary<string, Operator>>($"/operators.php?username={ApiName}");

            //Convert ot base64 and save
            var op64 = new Dictionary<string, string>(operators.Count);
            foreach (var kp in operators)
            {
                if (kp.Value != null)
                    op64.Add(kp.Key, kp.Value.ToBase64());
            }

            await Manager.Redis.StoreHashMapAsync(cacheName, op64);
            await Manager.Redis.SetExpiryAsync(cacheName, OperatorTTL);

            //Return the ops
            return operators;
        }

        /// <summary>
        /// Updates the MMR History.
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<MMRHistory> UpdateMMRHistoryAsync()
        {
            //We need to first update the profile
            if (Profile == null) {
                throw new InvalidOperationException("Cannot get MMR if there is no profile");
            }

            //Fetch the history
            var cacheName = Namespace.Combine(Manager.RedisPrefix, "profiles", ApiName, "history");
            var prevHistory = await Manager.Redis.FetchObjectAsync<MMRHistory>(cacheName);
            if (prevHistory == null || prevHistory.Season != Manager.Season)
            {
                if (Profile != null)
                {
                    //Profile exists, so lets setup the default
                    prevHistory = new MMRHistory()
                    {
                        Season  = Manager.Season,
                        Minimum = Profile.MetaData.MMR,
                        Current = Profile.MetaData.MMR,
                        Maximum = Profile.MetaData.BestMMR,
                        Average = Profile.MetaData.AverageMMR,
                    };
                }
                else
                {
                    //Profile doesn't exist, so early abort.
                    return null;
                }
            }


            //if we are updating, then lets do so
            if (Profile != null) {

                //Create a new history table
                var newHistory = new MMRHistory()
                {
                    Season = Manager.Season,
                    Average = Profile.MetaData.AverageMMR,
                    Current = Profile.MetaData.MMR,
                    Minimum = prevHistory.Minimum > Profile.MetaData.MMR ? Profile.MetaData.MMR : prevHistory.Minimum,
                    Maximum = prevHistory.Maximum < Profile.MetaData.BestMMR ? Profile.MetaData.BestMMR : prevHistory.Maximum
                };

                //Check for rank changes
                if (newHistory.MaximumRank > prevHistory.MaximumRank) {
                    OnSeasonHigh?.Invoke(this, prevHistory, newHistory);
                }

                if (newHistory.Rank != prevHistory.Rank) {
                    OnRankChange?.Invoke(this, prevHistory, newHistory);
                }

                //Store the new history and assign it
                prevHistory = newHistory;
                await Manager.Redis.StoreObjectAsync(cacheName, prevHistory);
            }

            //Return the history
            return MMRHistory = prevHistory;
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Gets a rendering of the profile banner.
        /// </summary>
        /// <param name="recache"></param>
        /// <returns></returns>
        public async Task<byte[]> GetProfileRenderAsync(bool recache = false) {
            if (Profile == null)
                throw new InvalidOperationException("Cannot render a profile if there is no profile");

            if (MMRHistory == null)
                throw new InvalidOperationException("Cannot render a profile if there is no MMR data");

            //Prepare a code which we will use for the cache
            var cacheCode = Profile.GetCacheCode() ^ MMRHistory.GetCacheCode();
            var cacheName = Namespace.Combine(Manager.RedisPrefix, "profiles", ApiName, "render", cacheCode);

            //Fetch the cache
            if (!recache && !NO_CACHE_RENDER)
            {
                var cacheResult = await Manager.Redis.FetchStringAsync(cacheName);
                if (cacheResult != null) return Convert.FromBase64String(cacheResult);
            }

            //Regenerate and recache
            var renderResult = await RenderProfileAsync();
            await Manager.Redis.StoreStringAsync(cacheName, Convert.ToBase64String(renderResult));
            await Manager.Redis.SetExpiryAsync(cacheName, ProfileRenderTLL);
            return renderResult;
        }


        /// <summary>
        /// Renders a profile
        /// </summary>
        /// <returns></returns>
        private Task<byte[]> RenderProfileAsync()
        {
            var renderer = Manager.InternalGetWkHtmlRenderer();
            renderer.SetCookie("profile", this.ToBase64());

            if (DEBUG_COOKIES) {
                System.IO.File.WriteAllText("DEFAULT_PROFILE.js", string.Format("const DEFAULT_PROFILE=decodeURIComponent(\"{0}\");", renderer.GetCookie("profile")));
            }

            return renderer.RenderBytesAsync(Manager.InternalGetWkHtmlPage());
        }
        #endregion
    }
}
