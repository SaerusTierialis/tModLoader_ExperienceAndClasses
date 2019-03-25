using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses.Systems {
    static class XP {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY = 0.7;
        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY = 0.4;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP Rewards ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// XP rewards are tallied server-side in a buffer and then sent out at regular intervals.
        /// 
        /// NOTE: consuming orbs for xp is handled entirely client-side
        /// </summary>
        public static class Rewards {

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            private const long TICKS_PER_XP_SEND = (long)(TimeSpan.TicksPerSecond * 0.5);

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            private static uint[] xp_buffer = new uint[Main.maxPlayers];
            private static DateTime time_send_xp_buffer = DateTime.MinValue;
            private static bool xp_buffer_empty = true;

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            /// <summary>
            /// Send all player rewards at once on interval
            /// </summary>
            public static void ServerProcessXPBuffer() {
                if (!xp_buffer_empty) { //fast check
                    DateTime now = DateTime.Now;
                    if (now.CompareTo(time_send_xp_buffer) >= 0) {
                        //send any rewards
                        for (byte player_index = 0; player_index < Main.maxPlayers; player_index++) {
                            if ((xp_buffer[player_index] > 0) && (Main.player[player_index].active)) {
                                //do reward
                                if (Utilities.Netmode.IS_SERVER) {
                                    Utilities.PacketHandler.XP.Send(player_index, -1, xp_buffer[player_index]);
                                }
                                else {
                                    ExperienceAndClasses.LOCAL_MPLAYER.AddXP(xp_buffer[player_index]);
                                }

                                //set back to 0
                                xp_buffer[player_index] = 0;
                            }
                        }

                        //set time next
                        time_send_xp_buffer = now.AddTicks(TICKS_PER_XP_SEND);

                        //buffer is empty
                        xp_buffer_empty = true;
                    }
                }
            }

            /// <summary>
            /// Track combat xp to be sent to client in a lump sum at next sync interval.
            /// </summary>
            /// <param name="player_index"></param>
            /// <param name="xp"></param>
            public static void ServerTallyCombatXP(byte player_index, double xp) {
                xp_buffer[player_index] += FinalizeXP(player_index, xp);
                xp_buffer_empty = false;
            }

            public static void ServerResetPlayerXPBuffer(int player_index) {
                xp_buffer[player_index] = 0;
            }

            /// <summary>
            /// Apply any bonuses and convert to uint
            /// </summary>
            /// <param name="player_index"></param>
            /// <param name="xp"></param>
            /// <returns></returns>
            private static uint FinalizeXP(byte player_index, double xp) {
                Player player = Main.player[player_index];

                //5% bonus for well fed
                if (player.wellFed) {
                    xp *= 1.05d;
                }

                return (uint)Math.Ceiling(xp);
            }

            /// <summary>
            /// Returns the value of a boss orb for a specific class/level
            /// </summary>
            /// <param name="c"></param>
            /// <param name="level"></param>
            /// <returns></returns>
            public static uint GetBossOrbXP(Class c, byte level) {
                Class pre = c.Prereq;
                uint level_sum = level;
                while (pre != null) {
                    level_sum += pre.Max_Level;
                    pre = pre.Prereq;
                }

                uint xp = 195 + (5 * (uint)Math.Pow(level_sum, 1.76));
                uint xp_min = (uint)(0.005 * Requirements.GetXPReq(c, level));
                if (xp < xp_min)
                    xp = xp_min;

                return xp;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP Requirements ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static class Requirements {

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables treated as const ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            private static uint[] XP_REQ { get; set; }

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Startup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            /// <summary>
            /// call once at load to pre-calc requirements
            /// </summary>
            public static void SetupXPRequirements() {
                //aprox pre-revamp to post-revamp xp requirements
                //new lv50 tier 2 = old level 25
                //new lv100 tier 3 = old level 180

                //tier 1 (predefined)
                //uint[] xp_predef = new uint[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 }; //length+1 must be UI.UI.MAX_LEVEL[1]
                uint[] xp_predef = new uint[] { 0, 10, 15, 20, 30, 40, 50, 60, 80, 100 }; //length+1 must be UI.UI.MAX_LEVEL[1]
                int num_predef = xp_predef.Length - 1;

                int levels = Class.TIER_MAX_LEVELS[1] + Class.TIER_MAX_LEVELS[2] + Class.TIER_MAX_LEVELS[3];

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

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            /// <summary>
            /// Returns the amount of xp needed for class c to reach specified level from one level prior
            /// </summary>
            /// <param name="c"></param>
            /// <param name="level"></param>
            /// <returns></returns>
            public static uint GetXPReq(Class c, byte level) {
                if (level >= c.Max_Level) {
                    return 0;
                }

                Class pre = c.Prereq;
                while (pre != null) {
                    level += pre.Max_Level;
                    pre = pre.Prereq;
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
}
