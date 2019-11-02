using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.Systems {
    class XP {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY = 0.7;
        public const double SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY = 0.4;

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
            // ([level - 1] * 20) + (10 ^ [1 + (level / 16)])
            for (uint i = 1; i < XP_REQ_CHARACTER.Length; i++) {
                XP_REQ_CHARACTER[i] = (uint)( ((i - 1.0) * 20.0) + Math.Round( Math.Pow(10.0, 1.0 + (i / 16.0)), 0 ) );
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Requirements ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Server Send XP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void ServerAddXP(uint xp) {
            //TODO
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Local Add XP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void LocalAddXP(uint xp) {
            //TODO
        }

    }
}
