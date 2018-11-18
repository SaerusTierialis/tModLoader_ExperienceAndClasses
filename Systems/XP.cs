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
        private const float RANGE_ELIGIBLE = 2500f;

        //eater of world multipliers (I don't see a good way to grant all exp/drops from final piece so instead divide based on typical case)
        private const double EATER_HEAD_MULT = 1.801792115f;
        private const double EATER_BODY_MULT = 1.109713024f;
        private const double EATER_TAIL_MULT = 0.647725809f;

        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY = 0.7;
        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY = 0.4;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables treated as const ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static uint[] XP_REQ { get; set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/



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
            //xp is double until it is added to mplayer as uint

            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            //calculate
            double experience = 0;
            if (npc.defDefense >= 1000)
                experience = (npc.lifeMax / 100d) * (1d + (npc.defDamage / 25d));
            else
                experience = (npc.lifeMax / 100d) * (1d + (npc.defDefense / 10d)) * (1d + (npc.defDamage / 25d));

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
            //aprox pre-revamp to post-revamp xp requirements
            //new lv50 tier 2 = old level 25
            //new lv100 tier 3 = old level 180

            //tier 1 (predefined)
            //uint[] xp_predef = new uint[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 }; //length+1 must be UI.UI.MAX_LEVEL[1]
            uint[] xp_predef = new uint[] { 0, 0, 10, 15, 20, 30, 40, 50, 60, 80, 100 }; //length+1 must be UI.UI.MAX_LEVEL[1]
            int num_predef = xp_predef.Length - 1;

            int levels = Class.MAX_LEVEL[1] + Class.MAX_LEVEL[2] + Class.MAX_LEVEL[3];

            XP_REQ = new uint[1 + levels];

            double adjust;
            for (int i = 2; i <= levels; i++) {
                if (i <= num_predef) {
                    XP_REQ[i] = xp_predef[i];
                }
                else {
                    adjust = Math.Max(1.09 - ((i - 1 - num_predef) / 10000), 1.08);
                    XP_REQ[i] = (uint)Math.Round(XP_REQ[i - 1] * adjust, 0);
                }
            }
        }

        public static uint GetXPReq(byte tier, byte level) {
            if (level >= Class.MAX_LEVEL[tier]) {
                return 0;
            }

            while (tier > 1) {
                level += Class.MAX_LEVEL[--tier];
            }

            if (level >= XP_REQ.Length) {
                return 0; //max level
            }
            else {
                return XP_REQ[level];
            }
        }

    }
}
