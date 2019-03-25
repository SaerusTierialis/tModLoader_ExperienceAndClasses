using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    class NPCRewards : GlobalNPC {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //any non-afk player within this range is automatically eligable for rewards
        private const float RANGE_ELIGIBLE = 2500f;

        //rewards are increased prior to division based on the number of eligable players 
        private const double PER_PLAYER_MODIFIER = 0.2;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Varibles ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private bool initialized = false;
        private bool treat_as_boss = false;
        private double base_xp = 0;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Instance per entity to store pre-calculated xp, etc.
        /// </summary>
        public override bool InstancePerEntity { get { return true; } }

        /// <summary>
        /// Pre-calculate xp
        /// (this method appears to be called after all modifications to npc stats, but it's called repeatedly so initialize only once)
        /// </summary>
        /// <param name="npc"></param>
        public override void ResetEffects(NPC npc) {
            base.ResetEffects(npc);
            if (!initialized) {
                //don't repeat init
                initialized = true;

                //treat this npc as a boss?
                treat_as_boss = npc.boss;
                switch (npc.netID) {
                    case NPCID.EaterofWorldsHead:
                    case NPCID.EaterofWorldsBody:
                    case NPCID.EaterofWorldsTail:
                        treat_as_boss = true;
                        break;
                }

                //calculate xp
                base_xp = CalculateBaseXP(npc);
            }
        }

        /// <summary>
        /// Triggers rewards on npc death during loot stage
        /// </summary>
        /// <param name="npc"></param>
        public override void NPCLoot(NPC npc) {
            base.NPCLoot(npc);
            if (!npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue) {
                //find eligible players
                List<byte> eligible_players = GetEligiblePlayers(npc, treat_as_boss);

                //overall rewards are increased by PER_PLAYER_MODIFIER for each player beyond the first, then divided by number of players
                double reward_modifier = (1 + ((eligible_players.Count - 1) * PER_PLAYER_MODIFIER)) / eligible_players.Count;

                //orb loot
                OrbDrop(base_xp, treat_as_boss, npc, eligible_players, reward_modifier);

                //award xp
                double xp_per_player = base_xp * reward_modifier;
                foreach (byte player_index in eligible_players) {
                    Systems.XP.AddXP(player_index, xp_per_player);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Returns a list of player indices for all players eligible for rewards.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="treat_as_boss"></param>
        /// <returns></returns>
        private static List<byte> GetEligiblePlayers(NPC npc, bool treat_as_boss) {
            List<byte> eligible_players = new List<byte>();
            Player player;
            MPlayer mplayer;
            if (Utilities.Netmode.IS_SERVER) {
                for (int player_index = 0; player_index < 255; player_index++) {
                    player = Main.player[player_index];

                    //must exist
                    if (!player.active) continue;

                    mplayer = player.GetModPlayer<MPlayer>(ExperienceAndClasses.MOD);
                    //must not be afk
                    if (!mplayer.AFK) {
                        //must have hit the target or be nearby (unless boss)
                        if (treat_as_boss || (!player.dead && (npc.playerInteraction[player_index] || (player.Distance(npc.position) <= RANGE_ELIGIBLE)))) {
                            eligible_players.Add((byte)player.whoAmI);
                        }
                    }
                }
            }
            else {
                //always eligible in singleplayer
                eligible_players.Add((byte)Main.LocalPlayer.whoAmI);
            }
            return eligible_players;
        }

        /// <summary>
        /// Base orb rates are based on NPC base xp and individual player progression. The chances are then reducced for the number of players recieving rewards.
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="treat_as_boss"></param>
        /// <param name="npc"></param>
        /// <param name="eligible_players"></param>
        /// <param name="reward_modifier"></param>
        private static void OrbDrop(double base_xp, bool treat_as_boss, NPC npc, List<byte> eligible_players, double reward_modifier) {
            //TODO
        }

        /// <summary>
        /// Calculates base XP for an npc.
        /// XP is a double until it is added to player as uint
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        private static double CalculateBaseXP(NPC npc) {
            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            //calculate
            double xp = 0;
            if (npc.defDefense >= 1000)
                xp = (npc.lifeMax / 100d) * (1d + (npc.defDamage / 25d));
            else
                xp = (npc.lifeMax / 100d) * (1d + (npc.defDefense / 10d)) * (1d + (npc.defDamage / 25d));

            //special cases
            switch (npc.netID) {
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
