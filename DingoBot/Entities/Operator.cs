using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Entities
{
    public class Operator : KillStatistics
    {
        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("img")]
        public string ImageURL { get; private set; }

        [JsonProperty("team")]
        public string Team { get; private set; }

        [JsonProperty("total_xp")]
        public long TotalXP { get; private set; }

        [JsonProperty("operator_stat")]
        public uint OperatorStat { get; private set; }

        /// <summary>
        /// Is the operator a recruit?
        /// </summary>
        public bool IsRecruit => ImageURL.EndsWith("recruit.png");


        /// <summary>
        /// Decodes the operator from Base64
        /// </summary>
        /// <param name="b64"></param>
        /// <returns></returns>
        public static Operator FromBase64(string b64)
        {
            var serializedProfile = Convert.FromBase64String(b64);
            return JsonConvert.DeserializeObject<Operator>(Encoding.UTF8.GetString(serializedProfile));
        }

    }
}
