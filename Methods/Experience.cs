using System;
using Terraria;

namespace ExperienceAndClasses.Methods
{
    static class Experience
    {
        /// <summary>
        /// Calculate experience requirements, call only once when mod starts
        /// </summary>
        public static void CalcExpReqs()
        {
            double adjust = 0;
            double total = 0;
            for (int lvl = 0; lvl <= ExperienceAndClasses.MAX_LEVEL; lvl++)
            {
                if (lvl < ExperienceAndClasses.EARLY_EXP_REQ.Length)
                {
                    ExperienceAndClasses.EXP_REQ[lvl] = ExperienceAndClasses.EARLY_EXP_REQ[lvl];
                }
                else
                {
                    adjust = ((double)lvl - (ExperienceAndClasses.EARLY_EXP_REQ.Length)) / 100;
                    if (adjust > 0.32) adjust = 0.32;
                    ExperienceAndClasses.EXP_REQ[lvl] = Math.Round(ExperienceAndClasses.EXP_REQ[lvl - 1] * (1.35 - adjust), 0);
                }
                total += ExperienceAndClasses.EXP_REQ[lvl];
                ExperienceAndClasses.EXP_REQ_TOTAL[lvl] = total;
            }
        }

        /// <summary>
        /// return the amount of exp required for given level (optionally returns the total exp required instead)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public static double GetExpReqForLevel(int level, bool total)
        {
            if (level > ExperienceAndClasses.MAX_LEVEL) level = ExperienceAndClasses.MAX_LEVEL;
            if (!total)
                return ExperienceAndClasses.EXP_REQ[level];
            else
                return ExperienceAndClasses.EXP_REQ_TOTAL[level];
        }

        /// <summary>
        /// get current level given experience
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public static int GetLevel(double experience)
        {
            int level = 0;
            while (experience >= GetExpReqForLevel(level + 1, true) && level < ExperienceAndClasses.MAX_LEVEL) level++;
            return level;
        }

        /// <summary>
        /// get exp needed to reach next level
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public static double GetExpUntilNextLevel(double experience)
        {
            return GetExpReqForLevel(GetLevel(experience) + 1, true) - experience;
        }


        /// <summary>
        /// get exp needed to reach next level
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public static double GetExpTowardsNextLevel(double experience)
        {
            return experience - GetExpReqForLevel(GetLevel(experience), true);
        }

        /// <summary>
        /// Get player's class(es)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetClass(Player player)
        {
            string job = "";
            Item[] equip = player.armor;
            for (int i = 0; i < equip.Length; i++)
            {
                if (equip[i].name.Contains("Class Token"))
                {
                    if (job.Length > 0) job += " & ";
                    job += equip[i].name.Substring(equip[i].name.IndexOf(":") + 2);
                }
            }
            if (job.Length == 0) job = "No Class";
            return job;
        }

        /// <summary>
        /// Get max tier of player's class(es)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetTier(Player player)
        {
            string str;

            Item[] equip = player.armor;
            int tier = -1;
            for (int i = 0; i < equip.Length; i++)
            {
                if (equip[i].name.Contains("Class Token"))
                {
                    str = equip[i].name.Substring(equip[i].name.IndexOf("Tier") + 5);
                    str = str.Substring(0, str.Length - 1);
                    switch (str)
                    {
                        case "I":
                            if (tier < 1) tier = 1;
                            break;
                        case "II":
                            if (tier < 2) tier = 2;
                            break;
                        case "III":
                            if (tier < 3) tier = 3;
                            break;
                        default:
                            break;
                    }
                }
            }
            return tier;
        }
    }
}
