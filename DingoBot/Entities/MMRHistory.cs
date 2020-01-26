using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Entities
{
    /// <summary>
    /// Class representing the accounts MMR History for this season.
    /// </summary>
    public class MMRHistory : Base64Serializable
    {
        private const int CACHE_ROUNDING = 50;

        [Redis.Serialize.RedisProperty("sea")]
        public uint Season { get; set; }

        [Redis.Serialize.RedisProperty("min")]
        public uint Minimum { get; set; }

        [Redis.Serialize.RedisProperty("avg")]
        public uint Average { get; set; }

        [Redis.Serialize.RedisProperty("max")]
        public uint Maximum { get; set; }

        [Redis.Serialize.RedisProperty("now")]
        public uint Current { get; set; }

        /// <summary>
        /// Max Rank the user has achieved
        /// </summary>
        [Redis.Serialize.RedisIgnore]
        public Rank MaximumRank => RankUtil.GetRank(Maximum);

        /// <summary>
        /// Min Rank the user has achieved
        /// </summary>
        [Redis.Serialize.RedisIgnore]
        public Rank MinimumRank => RankUtil.GetRank(Minimum);

        /// <summary>
        /// The current rank of the user
        /// </summary>
        [Redis.Serialize.RedisIgnore]
        public Rank Rank => RankUtil.GetRank(Current);

        [Redis.Serialize.RedisIgnore]
        public Rank RankBelowMinimum => RankUtil.GetPreviousRank(Minimum);

        [Redis.Serialize.RedisIgnore]
        public Rank RankAboveMaximum => RankUtil.GetNextRank(Maximum);

        /// <summary>
        /// A hash code used to associate with the render cahce
        /// </summary>
        /// <returns></returns>
        public int GetCacheCode()
        {
            if ((Minimum | Maximum | Average) == 0) return 0;
            return (int)(15961635 ^ Season)
                ^ (int)Math.Floor(Current - ((decimal)Current % CACHE_ROUNDING)) << 7
                ^ (int)Math.Floor(Minimum - ((decimal)Current % Minimum)) << 5
                ^ (int)Math.Floor(Maximum - ((decimal)Current % Maximum)) << 3
                ^ (int)Math.Floor(Average - ((decimal)Current % Average)) << 1;
        }

    }
}
