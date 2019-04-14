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

        public const double EXTRA_XP_POOL_MULTIPLIER = 0.5;

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
                        adjust = Math.Max(1.09 - ((i - 1.0 - num_predef) / 10000), 1.08);
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

        public static class Adjusting {
            /// <summary>
            /// Can local player gain xp at all
            /// </summary>
            /// <returns></returns>
            public static bool LocalCanGainXP {
                get {
                    return (LocalCanGainXPPrimary || LocalCanGainXPSecondary);
                }
            }

            /// <summary>
            /// Can local player's primary class gain xp
            /// </summary>
            /// <returns></returns>
            public static bool LocalCanGainXPPrimary {
                get {
                    return (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier > 0) && (ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num] < ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Max_Level);
                }
            }

            /// <summary>
            /// Can local player's secondary class gain xp
            /// </summary>
            /// <returns></returns>
            public static bool LocalCanGainXPSecondary {
                get {
                    return ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary && (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier > 0) && (ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num] < ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Max_Level);
                }
            }

            /// <summary>
            /// Add XP to local character (to primary/secondary/extra)
            /// </summary>
            /// <param name="xp"></param>
            public static void LocalAddXP(uint xp, bool can_show_xp_message = true) {
                if (can_show_xp_message && ExperienceAndClasses.LOCAL_MPLAYER.show_xp) {
                    CombatText.NewText(Main.LocalPlayer.getRect(), UI.Constants.COLOUR_XP_BRIGHT, "+" + xp + " XP");
                }

                if (LocalCanGainXP) {
                    bool add_primary = LocalCanGainXPPrimary;
                    bool add_secondary = LocalCanGainXPSecondary;

                    if (add_primary && add_secondary) {
                        LocalAddXPToClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num, (uint)Math.Ceiling(xp * Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY));
                        LocalAddXPToClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num, (uint)Math.Ceiling(xp * Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY));
                    }
                    else if (add_primary) {
                        LocalAddXPToClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num, xp);
                    }
                    else if (add_secondary) {
                        LocalAddXPToClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num, xp);
                    }
                    else {
                        //shouldn't be reachable unless something is changed later
                        ExperienceAndClasses.LOCAL_MPLAYER.extra_xp = Math.Max(ExperienceAndClasses.LOCAL_MPLAYER.extra_xp, ExperienceAndClasses.LOCAL_MPLAYER.extra_xp + xp); //prevent overflow
                        return;
                    }

                    if (LocalCheckDoLevelup()) {
                        //levelup!
                        Systems.Status.LevelUp.CreateNew(ExperienceAndClasses.LOCAL_MPLAYER.thing);
                    }
                }
                else {
                    ExperienceAndClasses.LOCAL_MPLAYER.extra_xp = Math.Max(ExperienceAndClasses.LOCAL_MPLAYER.extra_xp, ExperienceAndClasses.LOCAL_MPLAYER.extra_xp + xp); //prevent overflow
                }
            }

            /// <summary>
            /// Add class XP (no updates)
            /// </summary>
            /// <param name="class_id"></param>
            /// <param name="amount"></param>
            public static void LocalAddXPToClass(byte class_id, uint amount) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[class_id] = Math.Max(ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[class_id] + amount, ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[class_id]);
            }

            /// <summary>
            /// Subtract class XP (no updates)
            /// </summary>
            /// <param name="class_id"></param>
            /// <param name="amount"></param>
            public static void LocalSubtractXPFromClass(byte class_id, uint amount) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[class_id] = Math.Min(ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[class_id] - amount, ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[class_id]);
            }

            /// <summary>
            /// Look for any levelup, update as needed
            /// </summary>
            private static bool LocalCheckDoLevelup() {
                //store prior levels to detect level-up
                byte effective_primary = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective;
                byte effective_secondary = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective;

                //level up
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.LocalCheckDoLevelup();
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.LocalCheckDoLevelup();

                //adjust effective levels
                MPlayer.LocalCalculateEffectiveLevels();

                //update class info if needed
                if ((effective_primary != ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective) || (effective_secondary != ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective)) {
                    MPlayer.LocalUpdateAll();
                    return true;
                }
                else {
                    //otherwise just update xp bars
                    UI.UIHUD.Instance.UpdateAll();
                    return false;
                }
            }


        }
    }
}
