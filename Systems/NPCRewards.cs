using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public class NPCRewards : GlobalNPC {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //xp send packet interval
        private const long TICKS_PER_XP_SEND = (long)(TimeSpan.TicksPerSecond * 0.5);

        //orb drop values (min/max is before number-of-players modifier)
        private const double DROP_CHANCE_ORB_MONSTER_MIN = 0.0005;
        private const double DROP_CHANCE_ORB_MONSTER_MAX = 0.05;
        private const double DROP_CHANCE_ORB_MONSTER_MODIFIER = 1.8;
        private const double DROP_CHANCE_ORB_BOSS_MIN = 0.01;
        private const double DROP_CHANCE_ORB_BOSS_MAX = 0.5;
        private const double DROP_CHANCE_ORB_BOSS_MODIFIER = 1.5;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static bool Rebalance = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private double base_xp_value = 0;
        private bool treat_as_boss = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Instance per entity to store base xp, etc.
        /// </summary>
        public override bool InstancePerEntity => true;

        public override void SetDefaults(NPC npc) {
            base.SetDefaults(npc);

            //treat this npc as a boss?
            treat_as_boss = npc.boss;
            switch (npc.type) {
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                    treat_as_boss = true;
                    break;
            }

            //calculate xp value on base stats (uses non-expert, single-player stats)
            base_xp_value = XP.CalculateBaseXPValue(npc);

            //rebalance (normal mode)
            if (!Main.expertMode) {
                RebalanceStats(ref npc);
            }
        }

        public override void ScaleExpertStats(NPC npc, int numPlayers, float bossLifeScale)
        {
            base.ScaleExpertStats(npc, numPlayers, bossLifeScale);
            //rebalance (expert mode)
            RebalanceStats(ref npc);
        }

        public override void NPCLoot(NPC npc) {
            base.NPCLoot(npc);

            if (base_xp_value > 0) {
                //get config
                ConfigServer config = Shortcuts.GetConfigServer;

                //find eligible players
                List<byte> eligible_players = GetEligiblePlayers(npc, treat_as_boss, config.RewardDistance);

                //reward split modifier
                float reward_multiplier = (1.0f + ((eligible_players.Count - 1.0f) * config.RewardModPerPlayer)) / eligible_players.Count;

                //orb loot
                AwardOrbs(base_xp_value, treat_as_boss, npc, eligible_players, reward_multiplier);

                //award xp
                uint xp = (uint)Math.Ceiling(base_xp_value * reward_multiplier * config.XPRate);
                AwardXP(xp, eligible_players);

                //wof defeated
                if (npc.type == NPCID.WallofFlesh) {
                    DefeatWOF(eligible_players);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void RebalanceStats(ref NPC npc)
        {
            if (Rebalance)
            {
                double scale = 2.0;
                if (!Main.hardMode)
                {
                    scale = 1.0 + Math.Min(1.0, base_xp_value / 15.0);
                }
                npc.damage = (int)Math.Ceiling(npc.damage * scale);
                npc.lifeMax = (int)Math.Ceiling(npc.lifeMax * scale);
            }
        }

        /// <summary>
        /// Returns a list of player indices for all players eligible for rewards.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="treat_as_boss"></param>
        /// <returns></returns>
        private static List<byte> GetEligiblePlayers(NPC npc, bool treat_as_boss, float range) {
            List<byte> eligible_players = new List<byte>();
            Player player;
            EACPlayer eacplayer;
            if (Shortcuts.IS_SERVER) {
                for (int player_index = 0; player_index < Main.maxPlayers; player_index++) {
                    player = Main.player[player_index];

                    //must exist
                    if (!player.active) continue;

                    eacplayer = player.GetModPlayer<EACPlayer>();
                    //must not be afk
                    if (!eacplayer.PSheet.Character.AFK) {
                        //must have hit the target or be nearby (unless boss)
                        if (treat_as_boss || (!player.dead && (npc.playerInteraction[player_index] || (player.Distance(npc.position) <= range)))) {
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
        /// <param name="reward_multiplier"></param>
        private static void AwardOrbs(double base_xp, bool treat_as_boss, NPC npc, List<byte> eligible_players, float reward_multiplier) {
            //backup npc interactions
            bool[] prior_interactions = npc.playerInteraction;

            //init orb interactions
            bool[] orb_monster_interactions = new bool[prior_interactions.Length];
            bool[] orb_boss_interactions = new bool[prior_interactions.Length];

            //track any drop
            bool any_orb_monster = false;
            bool any_orb_boss = false;

            //process players
            EACPlayer EACPlayer;
            foreach (byte player_index in eligible_players) {
                EACPlayer = Main.player[player_index].GetModPlayer<EACPlayer>();

                if (Main.rand.NextDouble() <= CalculateOrbChanceMonster(base_xp, EACPlayer.PSheet.Character.Level, reward_multiplier)) {
                    orb_monster_interactions[player_index] = true;
                    any_orb_monster = true;
                }
                if (treat_as_boss && (Main.rand.NextDouble() <= CalculateOrbChanceBoss(base_xp, EACPlayer.PSheet.Character.Level, reward_multiplier))) {
                    orb_boss_interactions[player_index] = true;
                    any_orb_boss = true;
                }
            }

            //monster orb drop
            if (any_orb_monster) {
                npc.playerInteraction = orb_monster_interactions;
                npc.DropItemInstanced(npc.position, npc.Size, ModContent.ItemType<Items.Orb_Monster>(), 1, true);
            }

            //boss orb drop
            if (any_orb_boss) {
                npc.playerInteraction = orb_boss_interactions;
                npc.DropItemInstanced(npc.position, npc.Size, ModContent.ItemType<Items.Orb_Boss>(), 1, true);
            }

            //restore interactions
            npc.playerInteraction = prior_interactions;
        }

        /// <summary>
        /// Returns chance ratio for monster orb
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="character_level"></param>
        /// <param name="reward_multiplier"></param>
        /// <returns></returns>
        private static double CalculateOrbChanceMonster(double base_xp, int character_level, float reward_multiplier) {
            return Utilities.Commons.Clamp(base_xp / Math.Pow(character_level * 4.55, DROP_CHANCE_ORB_MONSTER_MODIFIER) * reward_multiplier, DROP_CHANCE_ORB_MONSTER_MIN, DROP_CHANCE_ORB_MONSTER_MAX);
        }

        /// <summary>
        /// Returns chance ratio for boss orb
        /// </summary>
        /// <param name="base_xp"></param>
        /// <param name="character_level"></param>
        /// <param name="reward_multiplier"></param>
        /// <returns></returns>
        private static double CalculateOrbChanceBoss(double base_xp, int character_level, float reward_multiplier) {
            return Utilities.Commons.Clamp(base_xp / Math.Pow(character_level * 4.55, DROP_CHANCE_ORB_BOSS_MODIFIER) * reward_multiplier, DROP_CHANCE_ORB_BOSS_MIN, DROP_CHANCE_ORB_BOSS_MAX);
        }

        private static void DefeatWOF(List<byte> eligible_players) {
            foreach (byte player_index in eligible_players) {
                if (Shortcuts.IS_SERVER) {
                    Utilities.PacketHandler.WOF.Send(player_index, -1);
                }
                else {
                    Shortcuts.LOCAL_PLAYER.PSheet.Character.DefeatWOF();
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP Delivery System ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static uint[] xp_buffer = new uint[Main.maxPlayers];
        private static DateTime time_send_xp_buffer = DateTime.MinValue;
        private static bool xp_buffer_empty = true;

        /// <summary>
        /// Award player with xp (place in xp_buffer)
        /// </summary>
        /// <param name="xp"></param>
        /// <param name="eligible_players"></param>
        private static void AwardXP(uint xp, List<byte> eligible_players) {
            foreach (byte player_index in eligible_players) {
                xp_buffer[player_index] += xp;
            }
            xp_buffer_empty = false;
        }

        /// <summary>
        /// Award player with xp (place in xp_buffer)
        /// </summary>
        /// <param name="xp"></param>
        /// <param name="player_index"></param>
        public static void AwardXP(uint xp, byte player_index) {
            xp_buffer[player_index] += xp;
            xp_buffer_empty = false;
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
                                if (Shortcuts.IS_SERVER) {
                                    Utilities.PacketHandler.XP.Send(player_index, -1, xp_buffer[player_index]);
                                }
                                else {
                                    XP.Adjustments.LocalAddXP(xp_buffer[player_index]);
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

    }
}
