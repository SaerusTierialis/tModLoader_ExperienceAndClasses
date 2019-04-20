using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    class NPCRewards : GlobalNPC {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const long TICKS_PER_XP_SEND = (long)(TimeSpan.TicksPerSecond * 0.5);

        //range for reward eligibility
        private const float RANGE_ELIGIBLE = 2500f;

        //rewards are increased prior to division based on the number of eligable players 
        private const double PER_PLAYER_MODIFIER = 0.2;

        //orb drop values (min/max is before number-of-players modifier)
        private const double DROP_CHANCE_ORB_MONSTER_MIN = 0.0005;
        private const double DROP_CHANCE_ORB_MONSTER_MAX = 0.05;
        private const double DROP_CHANCE_ORB_MONSTER_MODIFIER = 1.8;
        private const double DROP_CHANCE_ORB_BOSS_MIN = 0.01;
        private const double DROP_CHANCE_ORB_BOSS_MAX = 0.5;
        private const double DROP_CHANCE_ORB_BOSS_MODIFIER = 1.5;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Varibles (static) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static uint[] xp_buffer = new uint[Main.maxPlayers];
        private static DateTime time_send_xp_buffer = DateTime.MinValue;
        private static bool xp_buffer_empty = true;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Varibles (instance) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

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
                AwardOrbs(base_xp, treat_as_boss, npc, eligible_players, reward_modifier);

                //award xp
                AwardXP(base_xp, eligible_players, reward_modifier);

                //wof defeated
                if (npc.netID == NPCID.WallofFlesh) {
                    DefeatWOF(eligible_players);
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
        /// Award players with xp
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="eligible_players"></param>
        /// <param name="reward_modifier"></param>
        private static void AwardXP(double base_xp, List<byte> eligible_players, double reward_modifier) {
            double xp = base_xp * reward_modifier;
            foreach (byte player_index in eligible_players) {
                ServerTallyCombatXP(player_index, xp);
            }
        }

        /// <summary>
        /// Base orb rates are based on NPC base xp and individual player progression. The chances are then reducced for the number of players recieving rewards.
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="treat_as_boss"></param>
        /// <param name="npc"></param>
        /// <param name="eligible_players"></param>
        /// <param name="reward_modifier"></param>
        private static void AwardOrbs(double base_xp, bool treat_as_boss, NPC npc, List<byte> eligible_players, double reward_modifier) {
            //backup npc interactions
            bool[] prior_interactions = npc.playerInteraction;

            //init orb interactions
            bool[] orb_monster_interactions = new bool[prior_interactions.Length];
            bool[] orb_boss_interactions = new bool[prior_interactions.Length];

            //track any drop
            bool any_orb_monster = false;
            bool any_orb_boss = false;

            //process players
            MPlayer mplayer;
            foreach (byte player_index in eligible_players) {
                mplayer = Main.player[player_index].GetModPlayer<MPlayer>(ExperienceAndClasses.MOD);

                if (Main.rand.NextDouble() <= CalculateOrbChanceMonster(base_xp, mplayer.Progression, reward_modifier)) {
                    orb_monster_interactions[player_index] = true;
                    any_orb_monster = true;
                }
                if (treat_as_boss && (Main.rand.NextDouble() <= CalculateOrbChanceBoss(base_xp, mplayer.Progression, reward_modifier))) {
                    orb_boss_interactions[player_index] = true;
                    any_orb_boss = true;
                }
            }

            //monster orb drop
            if (any_orb_monster) {
                npc.playerInteraction = orb_monster_interactions;
                npc.DropItemInstanced(npc.position, npc.Size, ExperienceAndClasses.MOD.ItemType<Items.Orb_Monster>(), 1, true);
            }

            //boss orb drop
            if (any_orb_boss) {
                npc.playerInteraction = orb_boss_interactions;
                npc.DropItemInstanced(npc.position, npc.Size, ExperienceAndClasses.MOD.ItemType<Items.Orb_Boss>(), 1, true);
            }

            //restore interactions
            npc.playerInteraction = prior_interactions;
        }

        /// <summary>
        /// Returns chance ratio for monster orb
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="player_progression"></param>
        /// <param name="reward_modifier"></param>
        /// <returns></returns>
        private static double CalculateOrbChanceMonster(double base_xp, int player_progression, double reward_modifier) {
            return Math.Max(Math.Min(base_xp / Math.Pow(player_progression, DROP_CHANCE_ORB_MONSTER_MODIFIER), DROP_CHANCE_ORB_MONSTER_MAX), DROP_CHANCE_ORB_MONSTER_MIN) * reward_modifier;
        }

        /// <summary>
        /// Returns chance ratio for boss orb
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="player_progression"></param>
        /// <param name="reward_modifier"></param>
        /// <returns></returns>
        private static double CalculateOrbChanceBoss(double base_xp, int player_progression, double reward_modifier) {
            return Math.Max(Math.Min(base_xp / Math.Pow(player_progression, DROP_CHANCE_ORB_BOSS_MODIFIER), DROP_CHANCE_ORB_BOSS_MAX), DROP_CHANCE_ORB_BOSS_MIN) * reward_modifier;
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
                xp = (npc.lifeMax / 80d) * (1d + (npc.defDamage / 20d));
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

        private static void DefeatWOF(List<byte> eligible_players) {
            foreach (byte player_index in eligible_players) {
                if (Utilities.Netmode.IS_SERVER) {
                    Utilities.PacketHandler.WOF.Send(player_index, -1);
                }
                else {
                    MPlayer.LocalDefeatWOF();
                }
            }
        }

        /// <summary>
        /// Send all player rewards at once on interval
        /// </summary>
        public static void ServerProcessXPBuffer() {
            if (!xp_buffer_empty) { //fast check
                DateTime now = DateTime.Now;
                if (now.CompareTo(time_send_xp_buffer) >= 0) {
                    //send rewards
                    for (byte player_index = 0; player_index < Main.maxPlayers; player_index++) {
                        if (xp_buffer[player_index] > 0) {
                            //do reward
                            if (Main.player[player_index].active) {
                                if (Utilities.Netmode.IS_SERVER) {
                                    Utilities.PacketHandler.XP.Send(player_index, -1, xp_buffer[player_index]);
                                }
                                else {
                                    Systems.XP.Adjusting.LocalAddXP(xp_buffer[player_index]);
                                }
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
        /// Track combat xp to be sent to client in a lump sum at next sync interval. Used for singleplayer too.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="xp"></param>
        public static void ServerTallyCombatXP(byte player_index, double xp) {
            xp_buffer[player_index] += FinalizeXP(player_index, xp);
            xp_buffer_empty = false;
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
        /// Returns the value of a boss orb for the local player
        /// </summary>
        /// <returns></returns>
        public static double GetBossOrbXP() {
            return Math.Pow(ExperienceAndClasses.LOCAL_MPLAYER.Progression, 1.7);
        }
    }
}
