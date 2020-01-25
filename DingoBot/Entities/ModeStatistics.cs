using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Entities
{

    public class ModeStatistics : KillStatistics
    {
        [JsonProperty("kills-match")]
        public double KillsPerMatch { get; private set; }

        [JsonProperty("kills-min")]
        public double KillsPerMinute { get; private set; }
    }
}
