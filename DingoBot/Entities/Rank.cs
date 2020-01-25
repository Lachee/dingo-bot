using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Entities
{
    public enum Rank : uint
    {
        Unranked        = 0,

        CopperV         = 0001,
        CopperIV        = 1200,
        CopperIII       = 1300,
        CopperII        = 1400,
        CopperI         = 1500,
                
        BronzeV         = 1600,
        BronzeIV        = 1700,
        BronzeIII       = 1900,
        BronzeII        = 1900,
        BronzeI         = 2000,
                
        SilverV         = 2100,
        SilverIV        = 2200,
        SilverIII       = 2300,
        SilverII        = 2400,
        SilverI         = 2500,
        
        GoldIII         = 2600,
        GoldII          = 2800,
        GoldI           = 3000,
        
        PlatinumIII     = 3200,
        PlatinumII      = 3600,
        PlatinumI       = 4000,
        
        Diamond         = 4400,        
        Champion        = 5000,
    }

    public static class RankUtil
    {
        /// <summary>
        /// Gets the colour of the rank
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public static DiscordColor GetColor(this Rank rank)
        {
            if (rank >= Rank.CopperV && rank < Rank.BronzeV)
                return new DiscordColor("ac1e13");

            if (rank >= Rank.BronzeV && rank < Rank.SilverV)
                return new DiscordColor("e0aa63");

            if (rank >= Rank.SilverV && rank < Rank.GoldIII)
                return new DiscordColor("c5c5c5");

            if (rank >= Rank.GoldIII && rank < Rank.PlatinumIII)
                return new DiscordColor("e5ce1b");

            if (rank >= Rank.PlatinumIII && rank < Rank.Diamond)
                return new DiscordColor("2abdc0");

            if (rank >= Rank.Diamond && rank < Rank.Champion)
                return new DiscordColor("a791ec");

            if (rank >= Rank.Champion)
                return new DiscordColor("941355");

            return DiscordColor.Black;
        }

        /// <summary>
        /// Gets the rank from the given MMR
        /// </summary>
        /// <param name="mmr"></param>
        /// <returns></returns>
        public static Rank GetRank(uint mmr)
        {
            Rank previous = Rank.Unranked;
            foreach(var v in Enum.GetValues(typeof(Rank))) {
                if (mmr >= (uint)v)
                    previous = (Rank)v;

                if (mmr < (uint)v)
                    break;
            }

            return previous;
        }

        /// <summary>
        /// Gets the next rank for the given MMR
        /// </summary>
        /// <param name="mmr"></param>
        /// <returns></returns>
        public static Rank GetNextRank(uint mmr)
        {
            var values = Enum.GetValues(typeof(Rank));

            for(int i = 0; i < values.Length; i++) {
                int ni = Math.Min(i + 1, values.Length - 1);

                var cur = (Rank) values.GetValue(i);
                var nex = (Rank) values.GetValue(ni);

                if (mmr >= (uint)cur && mmr < (uint)nex) {
                    return nex;
                }
            }

            return Rank.Champion;
        }

        /// <summary>
        /// Gets the prevoius rank for the MMR
        /// </summary>
        /// <param name="mmr"></param>
        /// <returns></returns>
        public static Rank GetPreviousRank(uint mmr)
        {
            var values = Enum.GetValues(typeof(Rank));

            for (int i = 1; i < values.Length; i++)
            {
                int ni = Math.Min(i + 1, values.Length - 1);

                var cur = (Rank)values.GetValue(i);
                var nex = (Rank)values.GetValue(ni);

                if (mmr >= (uint)cur && mmr < (uint)nex)
                {
                    return (Rank)values.GetValue(i-1);
                }
            }

            return Rank.Champion;
        }
    }
}
