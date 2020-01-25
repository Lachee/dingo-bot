using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using DingoBot.WkHtml;
using DingoBot.Redis;
using System.IO;

namespace DingoBot.Entities
{
    public class ProfileData : Base64Serializable
    {

        public class Links
        {
            [JsonProperty("url")]
            public string URL { get; private set; }

            [JsonProperty("avatar")]
            public string Avatar { get; private set; }

            [JsonProperty("rank")]
            public string RankSVG { get; private set; }
        }

        public class Meta
        {
            [JsonProperty("level")]
            public uint Level { get; private set; }

            [JsonProperty("best_mmr")]
            public uint BestMMR { get; private set; }

            [JsonProperty("avg_seasonal_mmr")]
            public uint AverageMMR { get; private set; }

            [JsonProperty("mmr")]
            public uint MMR { get; private set; }

            [JsonProperty("rank")]
            public string Rank { get; private set; }

            [JsonProperty("time_played")]
            public string TimePlayed { get; private set; }
        }

        /// <summary>
        /// The current instance of the bot.
        /// TODO: Make this not a singleton.
        /// </summary>
        [JsonIgnore]
        public Dingo Bot => Dingo.Instance;

        public string URL => Link.URL;
        public string Avatar => Link.Avatar;
        public string RankSVG => Link.RankSVG;
        public uint Level => MetaData.Level;
        public uint BestMMR => MetaData.BestMMR;
        public uint AverageMMR => MetaData.AverageMMR;
        public uint MMR => MetaData.MMR;
        public string RankText => MetaData.Rank;
        public string TimePlayed => MetaData.TimePlayed;

        public string Description => TopOperators.FirstOrDefault().Key;

        public Rank Rank => RankUtil.GetRank(MMR);

        /// <summary>
        /// The profile name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>
        /// Links to the profiles avatar and stat page
        /// </summary>
        [JsonProperty("link")]
        public Links Link { get; private set; }

        /// <summary>
        /// Details about the profile such as its level
        /// </summary>
        [JsonProperty("meta")]
        public Meta MetaData { get; private set; }


        /// <summary>
        /// Top operators the user plays and a link to their icons
        /// </summary>
        [JsonProperty("top_operators")]
        public IReadOnlyDictionary<string, string> TopOperators { get; private set; }

        /// <summary>
        /// Total overall statistics
        /// </summary>
        [JsonProperty("general")]
        public ModeStatistics GeneralStatistics { get; private set; }

        /// <summary>
        /// Casual statistics
        /// </summary>
        [JsonProperty("casual")]
        public ModeStatistics CasualStatistics { get; private set; }

        /// <summary>
        /// Ranked statistics
        /// </summary>
        [JsonProperty("ranked")]
        public ModeStatistics RankedStatistics { get; private set; }

        /// <summary>
        /// Creates a DiscordEmbed to represent the profile
        /// </summary>
        /// <returns></returns>
        public DiscordEmbed CreateEmbed()
        {
            var main = TopOperators.FirstOrDefault();

            StringBuilder desc = new StringBuilder();
            desc.AppendLine($"**Level {Level}**");
            desc.AppendLine($"**{main.Key} Main**");

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.WithAuthor(this.Name, this.URL, this.Avatar).WithDescription(desc.ToString());

            if (Rank != Rank.Unranked) {
                builder.WithColor(Rank.GetColor())
                    .AddField("MMR", MMR.ToString(), true).AddField("Best", BestMMR.ToString(), true).AddField("Rank", RankText, true);
            }
                
            builder.AddField("=== Casual Time", CasualStatistics.TimePlayed)
                .AddField("Kills", CasualStatistics.Kills.ToString(), true).AddField("Deaths", CasualStatistics.Deaths.ToString(), true).AddField("KD", CasualStatistics.KD.ToString("F2"), true)
                .AddField("Wins", CasualStatistics.Wins.ToString(), true).AddField("Loss", CasualStatistics.Losses.ToString(), true).AddField("Win%", CasualStatistics.WinPercent.ToString("F2"), true)
                .WithThumbnailUrl(main.Value);

            if (Rank != Rank.Unranked) {
                builder.AddField("=== Ranked Time", RankedStatistics.TimePlayed)
                .AddField("Kills", RankedStatistics.Kills.ToString(), true).AddField("Deaths", RankedStatistics.Deaths.ToString(), true).AddField("KD", RankedStatistics.KD.ToString("F2"), true)
                .AddField("Wins", RankedStatistics.Wins.ToString(), true).AddField("Loss", RankedStatistics.Losses.ToString(), true).AddField("Win%", RankedStatistics.WinPercent.ToString("F2"), true);
            }

            return builder.Build();
        }
        
        /// <summary>
        /// Decodes the profile from Base64
        /// </summary>
        /// <param name="b64"></param>
        /// <returns></returns>
        public static ProfileData FromBase64(string b64) {
            var serializedProfile = Convert.FromBase64String(b64);
            return JsonConvert.DeserializeObject<ProfileData>(Encoding.UTF8.GetString(serializedProfile));
        }

        /// <summary>
        /// Gets a code representing the cache the image would be stored under.
        /// </summary>
        /// <returns></returns>
        public int GetCacheCode() {
            return 45415
                ^ (TimePlayed.GetHashCode() << 7)
                ^ (Avatar.GetHashCode() << 7)
                ^ (Name.GetHashCode() << 7)
                ^ (Rank.GetHashCode() << 7);
        }
    }


}
