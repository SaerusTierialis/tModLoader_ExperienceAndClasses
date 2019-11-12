using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses.Systems {
    public static class XP {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY = 0.7;
        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY = 0.3;

        public const byte MAX_LEVEL = 255;

        private static readonly uint[] XP_REQ_CLASS = new uint[1 + MAX_LEVEL];
        private static readonly uint[] XP_REQ_CHARACTER = new uint[1 + MAX_LEVEL];

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Setup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static XP() {
            //aprox pre-revamp to post-revamp xp requirements
            //new lv50 tier 2 = old level 25
            //new lv100 tier 3 = old level 180

            //tier 1 (predefined)
            uint[] xp_predef = new uint[] { 0, 10, 15, 20, 30, 40, 50, 60, 80, 100 }; //length+1 must be UI.UI.MAX_LEVEL[1]
            byte num_predef = (byte)(xp_predef.Length - 1);

            double adjust;
            for (uint i = 1; i < XP_REQ_CLASS.Length; i++) {
                if (i <= num_predef) {
                    XP_REQ_CLASS[i] = xp_predef[i];
                }
                else {
                    adjust = Math.Max(1.09 - ((i - 1.0 - num_predef) / 10000), 1.08);
                    XP_REQ_CLASS[i] = (uint)Math.Round(XP_REQ_CLASS[i - 1] * adjust, 0);
                }
            }

            //character xp requirement
            // ([level - 1] * 20) + (10 ^ [1 + (level / 13.75)])
            //above level 90, just 5% increase per level
            //maxing a single tier 1, 2, and 3 takes approx the same xp as reaching character level 90
            //10x that xp reaches level 117, 20x that xp reaches 130
            for (uint i = 1; i < XP_REQ_CHARACTER.Length; i++) {
                if (i >= 90) {
                    XP_REQ_CHARACTER[i] = (uint)(XP_REQ_CHARACTER[i - 1] * 1.05);
                }
                else {
                    XP_REQ_CHARACTER[i] = (uint)(((i - 1.0) * 20.0) + Math.Round(Math.Pow(10.0, 1.0 + (i / 13.75)), 0));
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Requirements ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static class Requirements {
            public static uint GetXPReqClass(PlayerClass c, byte level) {
                if (level >= c.Max_Level) {
                    return 0;
                }

                PlayerClass pre = c.Prereq;
                while (pre != null) {
                    level += pre.Max_Level;
                    pre = pre.Prereq;
                }

                if (level >= XP_REQ_CLASS.Length) {
                    return 0; //max level
                }
                else {
                    return XP_REQ_CLASS[level];
                }
            }

            public static uint GetXPReqCharacter(byte level) {
                if (level >= MAX_LEVEL) {
                    return 0;
                }

                return XP_REQ_CHARACTER[level];
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP Adjustments ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        
        public static class Adjustments {
            public static void LocalAddXP(uint xp, bool is_combat = true, bool allow_multipliers = true) {
                //convert to double temporarily
                double xpd = xp;

                //apply any player-specific bonuses
                if (allow_multipliers) {
                    if (Main.LocalPlayer.wellFed) {
                        xpd *= 1.05;
                    }
                    //TODO - any other bonuses
                }

                //round up
                xp = (uint)Math.Ceiling(xpd);

                //sheet
                PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;

                //display?
                if (Shortcuts.GetConfigClient.UIMisc_XPOverhead)
                    CombatText.NewText(Main.LocalPlayer.getRect(), UI.Constants.COLOUR_XP_BRIGHT, "+" + xp + " XP");

                //add to player
                psheet.Character.LocalAddXP(xp);

                //add to classes
                if (psheet.Classes.Can_Gain_XP) { //primary and/or secondary can gain xp
                    if (psheet.Classes.Has_Subclass) {
                        if (!psheet.Classes.Primary.Can_Gain_XP) {
                            //secondary only (maxed primary)
                            psheet.Classes.Secondary.AddXP(xp, is_combat, allow_multipliers);
                        }
                        else if (!psheet.Classes.Secondary.Can_Gain_XP) {
                            //primary only (maxed secondary)
                            psheet.Classes.Primary.AddXP(xp, is_combat, allow_multipliers);
                        }
                        else {
                            //both
                            psheet.Classes.Primary.AddXP((uint)Math.Ceiling(xp * SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY), is_combat, allow_multipliers);
                            psheet.Classes.Secondary.AddXP((uint)Math.Ceiling(xp * SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY), is_combat, allow_multipliers);
                        }
                    }
                    else {
                        //primary only (no secondary)
                        psheet.Classes.Primary.AddXP(xp, is_combat, allow_multipliers);
                    }
                }
            }

            public static void LocalDeathPenalty(uint xp) {
                //TODO
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ NPC Value ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Calculates base XP for an npc.
        /// XP is a double until it is added to player as uint
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static double CalculateBaseXPValue(NPC npc) {
            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            //calculate
            double xp = 0;
            if (npc.defense >= 1000)
                xp = (npc.lifeMax / 80d) * (1d + (npc.damage / 20d));
            else
                xp = (npc.lifeMax / 100d) * (1d + (npc.defense / 10d)) * (1d + (npc.damage / 25d));

            //adjustment to keep xp approx where it was pre-revamp
            xp *= 2.0;

            //special cases
            switch (npc.type) {
                case NPCID.EaterofWorldsHead:
                    xp *= 1.801792115f;
                    break;

                case NPCID.EaterofWorldsBody:
                    xp *= 1.109713024f;
                    break;

                case NPCID.EaterofWorldsTail:
                    xp *= 0.647725809f;
                    break;
            }

            return xp;
        }

    }
}
