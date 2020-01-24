﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Entities
{
    public class OperatorStatistic
    {
        [JsonProperty("kills")]
        public uint Kills { get; private set; }
        [JsonProperty("deaths")]
        public uint Deaths { get; private set; }
        public double KD { get { return (double) Kills / (double) Deaths; } }

        [JsonProperty("headshots")]
        public uint HeadShots { get; private set; }
        [JsonProperty("wins")]
        public uint Wins { get; private set; }
        [JsonProperty("losses")]
        public uint Losses { get; private set; }
        public uint Matches { get { return Wins + Losses; } }
        public double WinPercent {  get { return (double)Wins / (double)Matches;  } }
        [JsonProperty("total_xp")]
        public ulong TotalExperience { get; private set; }
        [JsonProperty("melee_kills")]
        public uint MeleeKills { get; private set; }
        [JsonProperty("blind_kills")]
        public uint BlindKills { get; private set; }

        [JsonProperty("time_played")]
        public string TimePlayed { get; private set; }
    }

    public class ModeStatistics : OperatorStatistic
    {
        [JsonProperty("kills-match")]
        public double KillsPerMatch { get; private set; }
        [JsonProperty("kills-min")]
        public double KillsPerMinute { get; private set; }
    }
}
