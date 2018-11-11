using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses.Systems {
    class XP {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //range for xp, orbs, etc (unless boss or interaction)
        public const float RANGE_ELIGIBLE = 2500f;

        //eater of world multipliers (I don't see a good way to grant all exp/drops from final piece so instead divide based on typical case)
        public const double EATER_HEAD_MULT = 1.801792115f;
        public const double EATER_BODY_MULT = 1.109713024f;
        public const double EATER_TAIL_MULT = 0.647725809f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static ulong[] XP_REQ { get; private set; }
        public static ulong[] XP_REQ_TOTAL { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ General ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static List<int> GetEligiblePlayers(NPC npc) {
            List<int> eligible_players = new List<int>();
            Player player;
            MPlayer mplayer;
            if (ExperienceAndClasses.IS_SERVER) {
                bool treat_as_boss = TreatAsBoss(npc);
                for (int player_index = 0; player_index < 255; player_index++) {
                    player = Main.player[player_index];

                    //must exist
                    if (!player.active) continue;

                    mplayer = player.GetModPlayer<MPlayer>(ExperienceAndClasses.MOD);
                    //must not be afk
                    if (!mplayer.AFK) {
                        //must have hit the target or be nearby (unless boss)
                        if (treat_as_boss || npc.playerInteraction[player_index] || player.Distance(npc.position) <= RANGE_ELIGIBLE) {
                            eligible_players.Add(player.whoAmI);
                        }
                    }
                }
            }
            else {
                //always eligible in singleplayer
                eligible_players.Add(Main.LocalPlayer.whoAmI);
            }
            return eligible_players;
        }

        public static bool TreatAsBoss(NPC npc) {
            bool treat_as_boss = false;

            switch (npc.netID) {
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                    treat_as_boss = true;
                    break;
            }

            return treat_as_boss;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static double CalculateXP(NPC npc) {
            //xp is double until it is added to mplayer as ulong

            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            //calculate
            double experience = 0;
            if (npc.defDefense >= 1000)
                experience = (npc.lifeMax / 100) * (1 + (npc.defDamage / 25));
            else
                experience = (npc.lifeMax / 100) * (1 + (npc.defDefense / 10)) * (1 + (npc.defDamage / 25));

            //modify if exception
            switch (npc.netID) {
                case NPCID.EaterofWorldsHead:
                    experience *= EATER_HEAD_MULT;
                    break;

                case NPCID.EaterofWorldsBody:
                    experience *= EATER_BODY_MULT;
                    break;

                case NPCID.EaterofWorldsTail:
                    experience *= EATER_TAIL_MULT;
                    break;
            }

            return experience;
        }

        /// <summary>
        /// call once at load
        /// </summary>
        public static void CalcXPRequirements() {
            //tier 1 (predefined)
            ulong[] xp_predef = new ulong[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 }; //length+1 must be Shared.MAX_LEVEL[1]
            int num_predef = xp_predef.Length - 1;

            int levels = Shared.MAX_LEVEL[1] + Shared.MAX_LEVEL[2] + Shared.MAX_LEVEL[3];

            XP_REQ = new ulong[1 + levels];
            XP_REQ_TOTAL = new ulong[1 + levels];

            double adjust;
            for (int i = 2; i <= levels; i++) {
                if (i <= num_predef) {
                    XP_REQ[i] = xp_predef[i];
                }
                else {
                    adjust = Math.Max(1.35 - ((i - 1 - num_predef) / 100), 1.04);
                    XP_REQ[i] = (ulong)Math.Round(XP_REQ[i - 1] * adjust, 0);
                }
                XP_REQ_TOTAL[i] = XP_REQ_TOTAL[i - 1] + XP_REQ[i];
            }
        }

        public static byte GetLevel(byte tier, ulong xp_total) {
            int level = 1;
            while (XP_REQ_TOTAL[level] <= xp_total) {
                level++;
            }

            switch(tier) {
                case 2:
                    level -= (Shared.MAX_LEVEL[1] - 1);
                    break;
                case 3:
                    level -= (Shared.MAX_LEVEL[1] + Shared.MAX_LEVEL[2] - 1);
                    break;
            }

            return (byte)level;
        }

        public static ulong GetXPNextLevel(byte tier, byte level) {
            int ind = level;

            switch (tier) {
                case 2:
                    ind += (Shared.MAX_LEVEL[1] - 1);
                    break;
                case 3:
                    ind += (Shared.MAX_LEVEL[1] + Shared.MAX_LEVEL[2] - 1);
                    break;
            }

            return (XP_REQ_TOTAL[ind + 1] - XP_REQ_TOTAL[ind]);
        }

        public static ulong GetXPTowardsNextLevel(byte tier, ulong xp_total) {
            int ind = GetLevel(tier, xp_total);

            switch (tier) {
                case 2:
                    ind += (Shared.MAX_LEVEL[1] - 1);
                    break;
                case 3:
                    ind += (Shared.MAX_LEVEL[1] + Shared.MAX_LEVEL[2] - 1);
                    break;
            }

            return (XP_REQ_TOTAL[ind + 1] - xp_total);
        }

    }
}
