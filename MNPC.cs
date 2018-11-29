using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    class MNPC : GlobalNPC {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //range for xp, orbs, etc (unless boss or interaction)
        private const float RANGE_ELIGIBLE = 2500f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void NPCLoot(NPC npc) {
            base.NPCLoot(npc);

            if (!npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue) {

                //base xp
                double xp = Systems.XP.CalculateXP(npc);

                //base orb drop

                //divvy up xp...

                //find eligible player
                List<int> eligible_players = GetEligiblePlayers(npc);

                //overall xp increased by 20% per player then divided
                xp *= (1 + ((eligible_players.Count - 1) * 0.2));
                xp /= eligible_players.Count;

                //give
                foreach (byte player_index in eligible_players) {
                    Systems.XP.AddXP(player_index, xp);
                }

            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

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
                        if (treat_as_boss || (!player.dead && (npc.playerInteraction[player_index] || (player.Distance(npc.position) <= RANGE_ELIGIBLE)))) {
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
            bool treat_as_boss = npc.boss;

            switch (npc.netID) {
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                    treat_as_boss = true;
                    break;
            }

            return treat_as_boss;
        }

    }
}
