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

        //eater of world multipliers (I don't see a good way to grant all exp/drops from final piece so instead divide based on typical case)
        private const double EATER_HEAD_MULT = 1.801792115f;
        private const double EATER_BODY_MULT = 1.109713024f;
        private const double EATER_TAIL_MULT = 0.647725809f;

        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY = 0.7;
        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY = 0.4;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables treated as const ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static uint[] XP_REQ { get; set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static uint[] TRACK_PLAYER_XP = new uint[Main.maxPlayers];

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void AddXP(byte player_index, double xp) {
            if (!ExperienceAndClasses.IS_CLIENT) {
                TRACK_PLAYER_XP[player_index] += ModifyXP(player_index, xp);
            }
        }

        public static double CalculateXP(NPC npc) {
            //xp is double until it is added to mplayer as uint

            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            //calculate
            double xp = 0;
            if (npc.defDefense >= 1000)
                xp = (npc.lifeMax / 100d) * (1d + (npc.defDamage / 25d));
            else
                xp = (npc.lifeMax / 100d) * (1d + (npc.defDefense / 10d)) * (1d + (npc.defDamage / 25d));

            //modify if exception
            switch (npc.netID) {
                case NPCID.EaterofWorldsHead:
                    xp *= EATER_HEAD_MULT;
                    break;

                case NPCID.EaterofWorldsBody:
                    xp *= EATER_BODY_MULT;
                    break;

                case NPCID.EaterofWorldsTail:
                    xp *= EATER_TAIL_MULT;
                    break;
            }

            return xp;
        }

        public static uint ModifyXP(byte player_index, double xp) {
            Player player = Main.player[player_index];

            //5% bonus for well fed
            if (player.wellFed) {
                xp *= 1.05d;
            }

            return (uint)Math.Ceiling(xp);
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
            uint[] xp_predef = new uint[] { 0, 10, 15, 20, 30, 40, 50, 60, 80, 100 }; //length+1 must be UI.UI.MAX_LEVEL[1]
            int num_predef = xp_predef.Length - 1;

            int levels = Class.MAX_LEVEL[1] + Class.MAX_LEVEL[2] + Class.MAX_LEVEL[3];

            XP_REQ = new uint[1 + levels];

            double adjust;
            for (int i = 1; i <= levels; i++) {
                if (i <= num_predef) {
                    XP_REQ[i] = xp_predef[i];
                }
                else {
                    adjust = Math.Max(1.09 - ((i - 1 - num_predef) / 10000), 1.08);
                    XP_REQ[i] = (uint)Math.Round(XP_REQ[i - 1] * adjust, 0);
                }
            }
        }

        public static uint GetXPReq(Class c, byte level) {
            if (level >= Class.MAX_LEVEL[c.Tier]) {
                return 0;
            }

            byte id = c.ID_Prereq;
            while (id != (byte)Class.CLASS_IDS.New) {
                level += Class.MAX_LEVEL[Class.CLASS_LOOKUP[id].Tier];
                id = Class.CLASS_LOOKUP[id].ID_Prereq;
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
