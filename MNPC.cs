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

        public override void NPCLoot(NPC npc) {
            base.NPCLoot(npc);

            if (!npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue) {

                //base xp
                double xp = Systems.XP.CalculateXP(npc);

                //base orb drop

                //do
                if (ExperienceAndClasses.IS_SERVER) {
                    //find eligible player
                    List<int> eligible_players = Systems.XP.GetEligiblePlayers(npc);

                    //overall xp increased by 20% per player
                    xp *= (1 + ((eligible_players.Count - 1) * 0.2));
                    xp /= eligible_players.Count;

                    //give
                    foreach (byte player_index in eligible_players) {
                        PacketHandler.SendXP(player_index, xp);
                    }
                }
                else { //singleplayer
                    //directly add
                    ExperienceAndClasses.LOCAL_MPLAYER.LocalAddXP(xp);
                }

            }
        }

    }
}
