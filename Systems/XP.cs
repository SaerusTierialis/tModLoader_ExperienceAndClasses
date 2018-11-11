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



    }
}
