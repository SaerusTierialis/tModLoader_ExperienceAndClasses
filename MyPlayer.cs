using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using Terraria.Localization;
using Terraria.GameInput;
using System.Linq;
using System.Reflection;

namespace ExperienceAndClasses
{
    public class MyPlayer : ModPlayer
    {
        public static double MAX_EXPERIENCE = Methods.Experience.GetExpReqForLevel(ExperienceAndClasses.MAX_LEVEL, true);

        //kill count
        public int kill_count_track_id = 0;
        public int kill_count = 0;
        public bool show_kill_count = false;

        //general
        public bool auth = false;
        public bool traceChar = false;

        public double experience = -1;

        private double offlineXPChunk = 0;
        private DateTime offlineXPTime = DateTime.MinValue;

        public bool allowAFK = true;
        public DateTime afkTime = DateTime.MaxValue;
        public bool afk = false;

        public bool displayExp = true;

        public float UILeft = 400f;
        public float UITop = 100f;
        public bool UIShow = true;
        public bool UIExpBar = true;
        public bool UITrans = false;
        public bool UICDBars = true;
        public bool UIInventory = true;

        public List<Tuple<ModItem, string>> classTokensEquipped;
        public int numberClasses = 1;
        public int effectiveLevel = 0;
        public bool levelCapped = false;

        //abilities
        public bool[] unlocked_abilities_next;
        public bool[] unlocked_abilities_current;
        public Abilities.AbilityMain.ID[] selectedActiveAbilities;
        public Abilities.AbilityMain.RETURN latestAbilityFail = Abilities.AbilityMain.RETURN.FAIL_NOT_IMPLEMENTRD;
        public Boolean showFailMessages = true;
        public float thresholdCDMsg = 3f;
        public bool itemUsePrevented = false;
        public DateTime timeAllowItemUse = DateTime.MinValue;
        public Projectile[] sanctuaries;
        public bool ability_message_overhead = true;
        public DateTime time_last_hit_taken = DateTime.MinValue;
        public DateTime time_last_sanc_effect = DateTime.MinValue;

        //status system
        public bool[] status_active;
        public bool[] status_new;
        public DateTime[] status_end_time;
        public float[] status_magnitude;
        public Projectile[] status_visuals;
        public int[] status_visuals_projectile_ids;

        //debuff immunity
        public static bool[] debuff_immunity_active = new bool[ExperienceAndClasses.NUMBER_OF_DEBUFFS];
        public static bool[] debuff_immunity_update = new bool[ExperienceAndClasses.NUMBER_OF_DEBUFFS];
        public static DateTime[] debuff_immunity_time_start = new DateTime[ExperienceAndClasses.NUMBER_OF_DEBUFFS];
        public static DateTime[] debuff_immunity_time_end = new DateTime[ExperienceAndClasses.NUMBER_OF_DEBUFFS];
        public static Double[] debuff_immunity_duration_seconds = new double[ExperienceAndClasses.NUMBER_OF_DEBUFFS];

        //custom stats
        public float healing_power = 1f;

        //rogue
        public float percentMidas = 0;
        public int dodgeChancePct = 0;

        //assassin
        public double bonusCritPct = 0;
        public double openerBonusPct = 0;
        public int openerTime_msec = 0;
        public DateTime timeLastAttack = DateTime.MinValue;
        public int openerImmuneTime_msec = 0;
        public DateTime openerImmuneEnd = DateTime.MinValue;

        //save and load specific
        private string last_world_name;
        private IList<float> sanctuaries_centers;
        private bool recreated_sancs = false;

        public override void Initialize()
        {
            //initialize instanced arrays here to prevent bug

            //abilities
            unlocked_abilities_next = new bool[(int)Abilities.AbilityMain.ID.NUMBER_OF_IDs];
            unlocked_abilities_current = new bool[(int)Abilities.AbilityMain.ID.NUMBER_OF_IDs];
            selectedActiveAbilities = new Abilities.AbilityMain.ID[ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS];
            sanctuaries = new Projectile[2];

            //status system
            status_active = new bool[(int)ExperienceAndClasses.STATUSES.COUNT];
            status_new = new bool[(int)ExperienceAndClasses.STATUSES.COUNT];
            status_end_time = new DateTime[(int)ExperienceAndClasses.STATUSES.COUNT];
            status_magnitude = new float[(int)ExperienceAndClasses.STATUSES.COUNT];
            status_visuals = new Projectile[(int)ExperienceAndClasses.STATUSES.COUNT];
            status_visuals_projectile_ids = new int[(int)ExperienceAndClasses.STATUSES.COUNT];

            //define status visuals
            status_visuals_projectile_ids[(int)ExperienceAndClasses.STATUSES.SANCTUARY] = mod.ProjectileType<Abilities.AbilityProj.Status_Cleric_Sanctuary>();
            status_visuals_projectile_ids[(int)ExperienceAndClasses.STATUSES.PARAGON] = mod.ProjectileType<Abilities.AbilityProj.Status_Cleric_Paragon>();
            status_visuals_projectile_ids[(int)ExperienceAndClasses.STATUSES.PARAGON_RENEW] = mod.ProjectileType<Abilities.AbilityProj.Status_Cleric_Paragon_Renew>();
        }

        /// <summary>
        /// Returns experience total.
        /// </summary>
        /// <returns></returns>
        public double GetExp()
        {
            return experience;
        }

        /// <summary>
        /// Adds experience. For use in single-player or by server.
        /// </summary>
        /// <param name="xp"></param>
        public void AddExp(double xp, bool force=false)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1)
            {
                if (force)
                {
                    Methods.PacketSender.ClientTellAddExp(mod, xp);
                }
                else
                {
                    return;
                }
            }

            SetExp(GetExp() + xp);
        }

        //take xp from player
        public void SubtractExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            SetExp(GetExp() - xp);
        }

        //set xp of player
        public void SetExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            //in the rare case that the player is not synced with the server, don't do anything
            double priorExp = GetExp();
            if (Main.netMode == 2 && priorExp == -1)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Failed to change the experience value for player #" +player.whoAmI+":"+player.name +" (player not yet synced)"), ExperienceAndClasses.MESSAGE_COLOUR_RED);
                return;
            }

            int priorLevel = Methods.Experience.GetLevel(GetExp());
            experience = Math.Floor(xp);
            LimitExp();
            LevelUp(priorLevel);

            //if server, mark for update
            if (Main.netMode == 2 && !MyWorld.clientNeedsExpUpdate[player.whoAmI])
            {
                MyWorld.clientNeedsExpUpdate[player.whoAmI] = true;
                MyWorld.clientNeedsExpUpdate_who[MyWorld.clientNeedsExpUpdate_counter] = player.whoAmI;
                MyWorld.clientNeedsExpUpdate_counter++;
            }
            //if singleplayer, it's already done so display
            else if (Main.netMode==0)
            {
                ExpMsg(experience - priorExp);
            }
        }

        /// <summary>
        /// Keep experience between zero and max.
        /// </summary>
        public void LimitExp()
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            if (experience < 0) experience = 0;
            if (experience > MAX_EXPERIENCE) experience = MAX_EXPERIENCE;
        }

        /// <summary>
        /// Displays level-up/down messages. For use in single-player or by server.
        /// </summary>
        /// <param name="priorLevel"></param>
        public void LevelUp(int priorLevel)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            int level = Methods.Experience.GetLevel(GetExp());
            if (level != priorLevel)
            {
                //if server, full sync immediately
                if (Main.netMode == 2) Methods.PacketSender.ServerSyncExp(mod, true);

                if (level > priorLevel)
                {
                    if (Main.netMode == 0)
                        Main.NewText("You have reached level " + level + "!");
                    else
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(player.name + " has reached level " + level + "!"), ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
                }
                else if (level < priorLevel)
                {
                    if (Main.netMode == 0)
                        Main.NewText("You has fallen to level " + level + "!");
                    else
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(player.name + " has dropped to level " + level + "!"), ExperienceAndClasses.MESSAGE_COLOUR_RED);
                }
            }
        }

        /// <summary>
        /// Displays the local "You have gained/lost x experience." message. Loss is always displayed.
        /// </summary>
        /// <param name="experienceChange"></param>
        public void ExpMsg(double experienceChange, bool offlineChunk = false)
        {
            if (!Main.LocalPlayer.Equals(player) || Main.netMode==2) return;

            if (((experienceChange > 0) || offlineChunk) && displayExp)
            {
                if (Main.netMode == 1)
                {
                    //Main.NewText("You have earned " + Math.Round(experienceChange) + " experience.", ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
                    CombatText.NewText(player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_GREEN, "+" + Math.Round(experienceChange) + "XP");
                }
                else if (Main.netMode == 0)
                {
                    if (offlineChunk)
                    {
                        CombatText.NewText(player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_GREEN, "+" + Math.Round(offlineXPChunk) + "XP");
                        offlineXPChunk = 0;
                    }
                    else
                    {
                        offlineXPChunk += experienceChange;
                    }
                }
            }
            else if (experienceChange<0)
            {
                //Main.NewText("You have lost " + Math.Round(experienceChange * -1) + " experience.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
                CombatText.NewText(player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_RED, Math.Round(experienceChange) + "XP");
            }
        }

        public int GetLevel()
        {
            return Methods.Experience.GetLevel(GetExp());
        }

        public double GetBossOrbXP()
        {
            double xp = Math.Min(197 + (3 * Math.Pow(GetLevel(), 1.76)), 10000);
            double xp_min = 0.005 * Methods.Experience.GetExpReqForLevel(GetLevel() + 1, false);
            if (xp < xp_min)
                xp = xp_min;

            return Math.Floor(xp * ExperienceAndClasses.worldExpModifier);
        }

        public double GetMonsterOrbXP()
        {
            return Math.Floor(GetBossOrbXP() / 3);
        }

        //save xp
        public override TagCompound Save()
        {
            UILeft = UI.UIExp.GetLeft();
            UITop = UI.UIExp.GetTop();

            IList<float> sanctuaries_centers = new List<float>();
            for (int i = 0; i < sanctuaries.Length; i++)
            {
                if (sanctuaries[i] != null)
                {
                    sanctuaries_centers.Add(i);
                    sanctuaries_centers.Add(sanctuaries[i].Center.X);
                    sanctuaries_centers.Add(sanctuaries[i].Center.Y);
                }
            }

            return new TagCompound {
                {"experience", experience},
                {"display_exp", displayExp},
                {"allow_afk", allowAFK},
                {"UI_left", UILeft},
                {"UI_top", UITop},
                {"UI_show", UIShow},
                {"UI_expbar_show", UIExpBar},
                {"UI_trans", UITrans},
                {"UI_cdbars_show", UICDBars},
                {"UI_inv_show", UIInventory},
                {"traceChar", traceChar},
                {"thresh_cd_message", thresholdCDMsg},
                {"show_kill_count", show_kill_count},
                {"ability_message_overhead", ability_message_overhead},
                {"last_world_name", Main.worldName},
                {"sanctuaries_centers", sanctuaries_centers},
            };
        }

        //load xp
        public override void Load(TagCompound tag)
        {
            //load exp
            experience = Commons.TryGet<double>(tag, "experience", 0);
            if (experience < 0) experience = 0;
            if (experience > MAX_EXPERIENCE) experience = MAX_EXPERIENCE;

            //settings
            displayExp = Commons.TryGet<bool>(tag, "display_exp", true);
            thresholdCDMsg = Commons.TryGet<float>(tag, "thresh_cd_message", 3f);

            allowAFK = Commons.TryGet<bool>(tag, "allow_afk", true);

            //UI
            UILeft = Commons.TryGet<float>(tag, "UI_left", 400f);
            UITop = Commons.TryGet<float>(tag, "UI_top", 100f);
            UIShow = Commons.TryGet<bool>(tag, "UI_show", true);
            UIExpBar = Commons.TryGet<bool>(tag, "UI_expbar_show", true);
            UITrans = Commons.TryGet<bool>(tag, "UI_trans", false);
            UICDBars = Commons.TryGet<bool>(tag, "UI_cdbars_show", true);
            UIInventory = Commons.TryGet<bool>(tag, "UI_inv_show", true);
            show_kill_count = Commons.TryGet<bool>(tag, "show_kill_count", false);
            ability_message_overhead = Commons.TryGet<bool>(tag, "ability_message_overhead", true);

            //trace
            traceChar = Commons.TryGet<bool>(tag, "traceChar", false);

            //sanctuaries
            last_world_name = Commons.TryGet<string>(tag, "last_world_name", "");
            sanctuaries_centers = Commons.TryGetList<float>(tag, "sanctuaries_centers");
        }

        public override void SetupStartInventory(IList<Item> items)
        {
            Item item = new Item();
            item.SetDefaults(mod.ItemType("ClassToken_Novice"));
            item.stack = 1;
            items.Add(item);

            base.SetupStartInventory(items);
        }

        public override void OnEnterWorld(Player player)
        {
            if (Main.LocalPlayer.Equals(player))
            {
                //reset own sanctuaries if left and rejoined
                sanctuaries = new Projectile[2];

                //afk timing
                afkTime = DateTime.Now;

                if (experience < 0) //occurs when a player who does not have the mod joins a server that uses the mod
                {
                    experience = 0;
                    player.PutItemInInventory(mod.ItemType("ClassToken_Novice"));
                }

                UI.UIExp.Init(this);
                UI.UIExp.SetTransparency(UITrans);
                UI.UIExp.SetPosition(UILeft, UITop);

                //set shortcuts
                ExperienceAndClasses.localMyPlayer = this;

                //settings if singleplayer
                if (Main.netMode == 0)
                {
                    Methods.ChatCommands.CommandDisplaySettings(mod);
                }
            }

            base.OnEnterWorld(player);
        }

        public override void PreUpdate()
        {
            //empty current class list
            classTokensEquipped = new List<Tuple<ModItem, string>>();

            //default var bonuses
            bonusCritPct = 0;
            openerBonusPct = 0;
            openerTime_msec = 0;
            openerImmuneTime_msec = 0;
            percentMidas = 0;
            dodgeChancePct = 0;

            base.PreUpdate();
        }

        public override void PostUpdateEquips()
        {
            DateTime now = DateTime.Now;

            //number of classes
            numberClasses = classTokensEquipped.Count;
            if (numberClasses < 1) numberClasses = 1;

            //apply class effects
            foreach (var i in classTokensEquipped)
            {
                Items.Helpers.ClassTokenEffects(mod, player, i.Item1, i.Item2, true, this);
            }

            //calculate effective level for bonuses and abilities
            effectiveLevel = Methods.Experience.GetLevel(GetExp());
            if ((ExperienceAndClasses.worldLevelCap > 0) && (effectiveLevel > ExperienceAndClasses.worldLevelCap))
            {
                effectiveLevel = ExperienceAndClasses.worldLevelCap;
                levelCapped = true;
            }
            else
            {
                levelCapped = false;
            }
            effectiveLevel = (int)Math.Floor((double)effectiveLevel / numberClasses);

            //self only ability stuff
            if ((player.whoAmI == Main.LocalPlayer.whoAmI) && (Main.netMode != 2))
            {
                //set current abilities
                unlocked_abilities_current = unlocked_abilities_next;
                unlocked_abilities_next = new bool[(int)Abilities.AbilityMain.ID.NUMBER_OF_IDs];

                //do any passives
                int number_abilities = unlocked_abilities_current.Length;
                Abilities.AbilityMain.Ability ability;
                for (int i = 0; i < number_abilities; i++)
                {
                    if (unlocked_abilities_current[i])
                    {
                        ability = Abilities.AbilityMain.AbilityLookup[i];
                        if (ability.IsTypePassive())
                        {
                            ability.Use((byte)effectiveLevel, false);
                        }
                    }
                }

                //update debuff immunities
                string message_gain = "";
                string message_lose = "";
                string message_cured = "";
                for (int i = 0; i<ExperienceAndClasses.NUMBER_OF_DEBUFFS; i++)
                {
                    //triggering new immuities
                    if (debuff_immunity_update[i] && now.CompareTo(debuff_immunity_time_start[i])>0)
                    {
                        //start immunity
                        if (debuff_immunity_duration_seconds[i] <= 0)
                        {
                            //message
                            if (Main.LocalPlayer.HasBuff(ExperienceAndClasses.DEBUFFS[i]))
                            {
                                if (message_cured.Length > 0)
                                {
                                    message_cured += ", ";
                                }
                                message_cured += ExperienceAndClasses.DEBUFF_NAMES[i];
                            }

                            //one-time cure
                            player.buffImmune[ExperienceAndClasses.DEBUFFS[i]] = true;
                        }
                        else
                        {
                            //message
                            if (!debuff_immunity_active[i] && !player.buffImmune[ExperienceAndClasses.DEBUFFS[i]])
                            {
                                if (message_gain.Length > 0)
                                {
                                    message_gain += ", ";
                                }
                                message_gain += ExperienceAndClasses.DEBUFF_NAMES[i];
                            }

                            //immunity with duration
                            debuff_immunity_active[i] = true;
                            debuff_immunity_time_end[i] = now.AddSeconds(debuff_immunity_duration_seconds[i]);
                        }
                        debuff_immunity_update[i] = false;
                        debuff_immunity_duration_seconds[i] = 0;
                    }

                    //handling current immunities
                    if (debuff_immunity_active[i])
                    {
                        if (now.CompareTo(debuff_immunity_time_end[i])>0)
                        {
                            //end immunity
                            debuff_immunity_active[i] = false;

                            //message
                            if (!player.buffImmune[ExperienceAndClasses.DEBUFFS[i]])
                            {
                                if (message_lose.Length > 0)
                                {
                                    message_lose += ", ";
                                }
                                message_lose += ExperienceAndClasses.DEBUFF_NAMES[i];
                            }
                        }
                        else
                        {
                            //grant immunity
                            player.buffImmune[ExperienceAndClasses.DEBUFFS[i]] = true;
                        }
                    }
                }
                //messages
                if (message_gain.Length > 0)
                {
                    Main.NewText("Gained Immunity: " + message_gain, ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
                }
                if (message_lose.Length > 0)
                {
                    Main.NewText("Lost Immunity: " + message_lose, ExperienceAndClasses.MESSAGE_COLOUR_RED);
                }
                if (message_cured.Length > 0)
                {
                    Main.NewText("Cured: " + message_cured, ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
                }

                //update healing power
                int count_immunities = 0;
                foreach (int i in ExperienceAndClasses.DEBUFFS)
                {
                    if (player.buffImmune[i])
                        count_immunities++;
                }
                float heal_power_bonus = count_immunities * ExperienceAndClasses.HEAL_POWER_PER_IMMUNITY;
                if (heal_power_bonus > ExperienceAndClasses.MAX_HEAL_POWER_IMMUNITY_BONUS)
                    heal_power_bonus = ExperienceAndClasses.MAX_HEAL_POWER_IMMUNITY_BONUS;
                healing_power = 1f + heal_power_bonus;
            }

            //status system
            bool do_sync = false;
            bool draw_visual = false;
            if (ExperienceAndClasses.sync_local_status && (player.whoAmI == Main.LocalPlayer.whoAmI))
            {
                do_sync = true;
                ExperienceAndClasses.sync_local_status = false;
            }
            for (int i = 0; i < (byte)ExperienceAndClasses.STATUSES.COUNT; i++)
            {
                if (status_active[i])
                {
                    if (now.Subtract(status_end_time[i]).Ticks > 0)
                    {
                        //status ending
                        EndStatus(i);
                    }
                    else
                    {
                        //anything that happens at the start of status
                        if (status_new[i])
                        {
                            status_new[i] = false;
                            //visual, if any
                            if (player.whoAmI == Main.LocalPlayer.whoAmI)
                            {
                                //default to true if any
                                draw_visual = (status_visuals_projectile_ids[i] > 0);

                                //special cases
                                if ((i == (int)ExperienceAndClasses.STATUSES.SANCTUARY) && (status_magnitude[i] <= 0))
                                {
                                    draw_visual = false;
                                }

                                //draw
                                if (draw_visual)
                                {
                                    Projectile.NewProjectile(player.Center, new Vector2(0f), status_visuals_projectile_ids[i], 0, 0, player.whoAmI);
                                }
                            }
                        }

                        //status effect
                        switch ((ExperienceAndClasses.STATUSES)i)
                        {
                            case (ExperienceAndClasses.STATUSES.IMMUNITY):
                                player.immune = true;
                                break;
                            case (ExperienceAndClasses.STATUSES.SANCTUARY):
                                Lighting.AddLight(player.Top, Abilities.AbilityMain.Cleric_Active_Sanctuary.BUFF_LIGHT_COLOUR);
                                break;
                            default:
                                //do nothing
                                break;
                        }

                        //sync
                        if (do_sync)
                        {
                            Projectile.NewProjectile(player.Center, new Vector2(0.1f), mod.ProjectileType<Abilities.AbilityProj.Misc_PlayerStatus>(), player.whoAmI, status_magnitude[i], player.whoAmI, i, (float)status_end_time[i].Subtract(now).TotalSeconds);
                        }
                    }
                }
            }

            base.PostUpdateEquips();
        }

        public override void ModifyDrawInfo(ref PlayerDrawInfo drawInfo)
        {
            base.ModifyDrawInfo(ref drawInfo);
        }

        public override void PostUpdate()
        {
            DateTime now = DateTime.Now;

            //stop preventing item use
            if (itemUsePrevented && (now.CompareTo(timeAllowItemUse) > 0))
            {
                itemUsePrevented = false;
            }

            //single player xp chunk update
            if (Main.netMode == 0)
            {
                if ((offlineXPChunk > 0) && (now.AddMilliseconds(-MyWorld.TIME_BETWEEN_SYNC_EXP_CHANGES_MSEC).CompareTo(offlineXPTime) > 0))
                {
                    ExpMsg(0, true);
                    offlineXPTime = now;
                }
            }

            //self stuff
            if (Main.LocalPlayer.Equals(player))
            {
                //check if afk
                if (!afk && allowAFK && (Main.netMode != 2) && (now.AddSeconds(-ExperienceAndClasses.AFK_TIME_TICKS_SEC).CompareTo(afkTime) > 0))
                {
                    if (Main.netMode == 0) Main.NewText("You are now AFK. You will not recieve death penalties to experience but you cannot gain experience either.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
                    afk = true;
                    Methods.PacketSender.ClientAFK(mod);
                }

                //TEMPORARY: select first 4 actives
                selectedActiveAbilities = new Abilities.AbilityMain.ID[ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS];
                int slot = 0;
                for (int i = 0; i < unlocked_abilities_current.Length; i++)
                {
                    if (unlocked_abilities_current[i] && (Abilities.AbilityMain.AbilityLookup[i].IsTypeActive())) //have ability + is active
                    {
                        selectedActiveAbilities[slot] = (Abilities.AbilityMain.ID)i;
                        if (slot++ >= ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS)
                            break;
                    }
                }

                //check ability cooldowns
                Abilities.AbilityMain.Ability ability;
                for (int i = 0; i < unlocked_abilities_current.Length; i++)
                {
                    if (unlocked_abilities_current[i])
                    {
                        ability = Abilities.AbilityMain.AbilityLookup[i];
                        if (ability.OnCooldown() && (ability.GetCooldownRemainingSeconds() <= 0))
                        {
                            ability.OnCooldown(true, false);
                            if (ability.IsTypeActive() && (ability.GetCooldownSecs((byte)effectiveLevel) >= thresholdCDMsg))
                            {
                                OffCooldownMessage(ability.GetName());
                            }
                        }
                    }
                }

                if (!recreated_sancs)
                {
                    //do once
                    recreated_sancs = true;

                    //recreate sanctuaries
                    if (last_world_name.Equals(Main.worldName))
                    {
                        int counter = 0;
                        int sanc_index;
                        int proj_index;
                        float x, y;
                        while (counter < sanctuaries_centers.Count)
                        {
                            sanc_index = (int)sanctuaries_centers[counter++];
                            x = (int)sanctuaries_centers[counter++];
                            y = (int)sanctuaries_centers[counter++];

                            proj_index = Projectile.NewProjectile(new Vector2(x, y), new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<Abilities.AbilityProj.Cleric_Sanctuary>(), 0, 0, Main.LocalPlayer.whoAmI, sanc_index);
                            sanctuaries[sanc_index] = Main.projectile[proj_index];

                            Main.NewText("Recreated sanctuary #" + (sanc_index + 1) + "!", ExperienceAndClasses.MESSAGE_COLOUR_OFF_COOLDOWN);
                        }
                    }
                }
            }

            base.PostUpdate();
        }

        public override void OnRespawn(Player player)
        {
            time_last_hit_taken = DateTime.MinValue;
            base.OnRespawn(player);
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            int level = Methods.Experience.GetLevel(GetExp());
            if (afk)
            {
                //protected
                Main.NewText("Experience death penalty does not apply while afk.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
            }
            else if (ExperienceAndClasses.worldDeathPenalty <= 0)
            {
                //no penalty
            }
            else if (level < ExperienceAndClasses.LEVEL_START_APPLYING_DEATH_PENALTY)
            {
                //protected
                Main.NewText("Experience death penalty does not apply until level " + ExperienceAndClasses.LEVEL_START_APPLYING_DEATH_PENALTY + ".", ExperienceAndClasses.MESSAGE_COLOUR_RED);
            }
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            else if (!pvp && (Main.netMode == 0 || Main.netMode == 2))
            {
                //double maxLoss = Methods.Experience.GetExpReqForLevel(level + 1, false) * (ExperienceAndClasses.worldDeathPenalty / 100);
                //double expSoFar = Methods.Experience.GetExpTowardsNextLevel(GetExp());

                //double expLoss = maxLoss;
                //if (expSoFar < maxLoss)
                //{
                //    expLoss = expSoFar;
                //}
                //expLoss = Math.Floor(expLoss);

                SubtractExp( Methods.Experience.GetExpTowardsNextLevel(GetExp()) * ExperienceAndClasses.worldDeathPenalty); //notifies client if server

                //if (Main.netMode == 0)
                //{
                //    (mod as ExperienceAndClasses).uiExp.Update();
                //}
            }

            //remove all status
            for (byte i = 0; i < (byte)ExperienceAndClasses.STATUSES.COUNT; i++)
            {
                if (status_active[i])
                {
                    //status ending
                    status_active[i] = false;
                    status_new[i] = false;
                    status_end_time[i] = DateTime.MinValue;
                    status_magnitude[i] = 0;
                    if (player.whoAmI == Main.LocalPlayer.whoAmI)
                    {
                        //local, sync effect ending
                        Projectile.NewProjectile(player.Center, new Vector2(0.1f), ExperienceAndClasses.mod.ProjectileType<Abilities.AbilityProj.Misc_PlayerStatus>(), player.whoAmI, 0, player.whoAmI, i, -1);
                    }
                }
            }

            base.Kill(damage, hitDirection, pvp, damageSource);
        }

        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
            //on-hit midas
            if (Main.rand.Next(100) < (percentMidas * 100)) target.AddBuff(Terraria.ID.BuffID.Midas, 300);

            //Assassin special attack
            DateTime now = DateTime.Now;
            bool ready = timeLastAttack.AddMilliseconds(openerTime_msec).CompareTo(now) <= 0;
            if (openerBonusPct>0 && item.melee && (ready || target.life==target.lifeMax))
            {
                //if ready, add phase
                if (ready)
                {
                    player.AddBuff(mod.BuffType<Buffs.Buff_OpenerPhase>(), 1);
                    openerImmuneEnd = now.AddMilliseconds(openerImmuneTime_msec);
                }

                //bonus opener damage
                damage = (int)Math.Round((double)damage * (1 + openerBonusPct), 0);

                //crit opener?
                if (bonusCritPct > 0) damage = (int)Math.Round((double)damage * (1 + (bonusCritPct*3)), 0);
            }
            else
            {
                //bonus crit damage (Assassin)
                if (item.melee && bonusCritPct > 0) damage = (int)Math.Round((double)damage * (1 + bonusCritPct), 0);
            }

            //record time
            timeLastAttack = now;

            //remove buff
            int buffInd = player.FindBuffIndex(mod.BuffType<Buffs.Buff_OpenerAttack>());
            if (buffInd != -1) player.DelBuff(buffInd);

            //base
            base.ModifyHitNPC(item, target, ref damage, ref knockback, ref crit);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            //on-hit midas
            if (Main.rand.Next(100) < (percentMidas * 100)) target.AddBuff(Terraria.ID.BuffID.Midas, 300);

            //Assassin special attack for yoyo
            DateTime now = DateTime.Now;
            Item item = Main.player[proj.owner].HeldItem;
            bool ready = timeLastAttack.AddMilliseconds(openerTime_msec).CompareTo(now) <= 0;
            if (openerBonusPct > 0 && Items.Helpers.HeldYoyo(player) && (ready || target.life == target.lifeMax))
            {
                //if ready, add phase
                if (ready)
                {
                    player.AddBuff(mod.BuffType<Buffs.Buff_OpenerPhase>(), 2);
                    openerImmuneEnd = now.AddMilliseconds(openerImmuneTime_msec);
                }

                //bonus opener damage (50% YOYO PENALTY)
                damage = (int)Math.Round((double)damage * (1 + (openerBonusPct/2)), 0);

                //crit opener?
                if (bonusCritPct > 0) damage = (int)Math.Round((double)damage * (1 + (bonusCritPct * 3)), 0);
            }

            //record time
            timeLastAttack = now;

            //flex time for point-blank melee weapons that have a projectile
            if (openerBonusPct > 0 && !Items.Helpers.HeldYoyo(player)) timeLastAttack.AddMilliseconds(-50);

            //remove buff
            int buffInd = player.FindBuffIndex(mod.BuffType<Buffs.Buff_OpenerAttack>());
            if (buffInd != -1) player.DelBuff(buffInd);

            //base
            base.ModifyHitNPCWithProj(proj, target, ref damage, ref knockback, ref crit, ref hitDirection);
        }

        public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            int buffInd = player.FindBuffIndex(mod.BuffType<Buffs.Buff_OpenerAttack>());
            if (buffInd != -1)
            {
                if (Main.rand.Next(5) == 0 && drawInfo.shadow == 0f)
                {
                    int dust = Dust.NewDust(drawInfo.position - new Vector2(2f, 2f), player.width + 4, player.height + 4, mod.DustType("Dust_OpenerAttack"), player.velocity.X * 0.4f, player.velocity.Y * 0.4f, 100, default(Color), 3f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 1.8f;
                    Main.dust[dust].velocity.Y -= 0.5f;
                    Main.playerDrawDust.Add(dust);
                }
            }
            base.DrawEffects(drawInfo, ref r, ref g, ref b, ref a, ref fullBright);
        }

        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            //% dodge chance (shadow dodge and ninja dodge both use 80 ticks so I used that here)
            if (Main.LocalPlayer.Equals(player))
            {
                if (dodgeChancePct > 0 && Main.rand.Next(100) < dodgeChancePct)
                {
                    player.immune = true;
                    player.immuneTime = 80;
                    player.NinjaDodge();
                    NetMessage.SendData(62, -1, -1, null, player.whoAmI, 2f, 0f, 0f, 0, 0, 0); //might not work anymore
                    return false;
                }
            }

            return base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);
        }
        
        public override void OnHitAnything(float x, float y, Entity victim)
        {
            //always local only
            
            //afk
            afkTime = DateTime.Now;
            if (afk)
            {
                if (Main.netMode == 0) Main.NewText("You are no longer AFK.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
                afk = false;
                Methods.PacketSender.ClientUnAFK(mod);
            }

            base.OnHitAnything(x, y, victim);
        }

        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
        {
            //combat time
            time_last_hit_taken = DateTime.Now;

            base.Hurt(pvp, quiet, damage, hitDirection, crit);

            //sanctuary buff heal
            if (!player.dead && status_active[(int)ExperienceAndClasses.STATUSES.SANCTUARY] && status_magnitude[(int)ExperienceAndClasses.STATUSES.SANCTUARY] > 0)
            {
                //heal
                Projectile.NewProjectile(player.Center, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<Abilities.AbilityProj.Misc_HealHurt>(), (int)status_magnitude[(int)ExperienceAndClasses.STATUSES.SANCTUARY], 0, Main.LocalPlayer.whoAmI, 1, Main.LocalPlayer.whoAmI);
                //remove heal from status
                Projectile.NewProjectile(player.Center, new Vector2(0.1f), ExperienceAndClasses.mod.ProjectileType<Abilities.AbilityProj.Misc_PlayerStatus>(), player.whoAmI, 0, player.whoAmI, (float)ExperienceAndClasses.STATUSES.SANCTUARY, (float)status_end_time[(int)ExperienceAndClasses.STATUSES.SANCTUARY].Subtract(DateTime.Now).TotalSeconds);
                //failsafe
                status_magnitude[(int)ExperienceAndClasses.STATUSES.SANCTUARY] = 0;
                //remove visual
                if (status_visuals[(int)ExperienceAndClasses.STATUSES.SANCTUARY] != null)
                {
                    status_visuals[(int)ExperienceAndClasses.STATUSES.SANCTUARY].Kill();
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet) //CLIENT-SIDE ONLY
        {
            //prevent weapon use after 
            if (triggersSet.MouseLeft && itemUsePrevented && ((player.HeldItem.damage > 0) || player.HeldItem.useStyle==1 || player.HeldItem.useStyle == 3 || player.HeldItem.useStyle == 5)) player.controlUseItem = false;

            //track afk
            if (Main.netMode != 2 && (triggersSet.MouseLeft || triggersSet.MouseMiddle || triggersSet.MouseRight || triggersSet.Jump || triggersSet.Up || triggersSet.Down || triggersSet.Left || triggersSet.Right))
            {
                afkTime = DateTime.Now;
                if (afk)
                {
                    if (Main.netMode == 0) Main.NewText("You are no longer AFK.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
                    afk = false;
                    Methods.PacketSender.ClientUnAFK(mod);
                }
            }

            //reset which errors can display when an ability key is pressed
            for (int i=0; i<ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS; i++)
            {
                if (ExperienceAndClasses.HOTKEY_ABILITY[i].JustPressed)
                {
                    latestAbilityFail = Abilities.AbilityMain.RETURN.UNUSED;
                    showFailMessages = true;
                    break;
                }
            }

            //which ability
            int slot = -1;
            for (int i=0; i<ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS; i++)
            {
                if (ExperienceAndClasses.HOTKEY_ABILITY[i].Current)
                {
                    slot = i;
                    break;
                }
            }

            //do ability, if any
            if (slot >= 0)
            {
                Abilities.AbilityMain.ID id = selectedActiveAbilities[slot];
                if (id != Abilities.AbilityMain.ID.UNDEFINED)
                {
                    Abilities.AbilityMain.RETURN ret = Abilities.AbilityMain.AbilityLookup[(int)id].Use((byte)effectiveLevel, ExperienceAndClasses.HOTKEY_ALTERNATE_EFFECT.Current);

                    if (ret == Abilities.AbilityMain.RETURN.SUCCESS)
                        showFailMessages = false;

                    if (showFailMessages && (ret != latestAbilityFail))
                    {
                        latestAbilityFail = ret;
                        SendReturnMessage(id, latestAbilityFail);
                    }
                }
            }
            else if (ExperienceAndClasses.HOTKEY_ALTERNATE_EFFECT.JustPressed)
            {
                Abilities.AbilityMain.Cleric_Active_Sanctuary.TryWarp();
            }
        }

        public static void SendReturnMessage(Abilities.AbilityMain.ID id, Abilities.AbilityMain.RETURN ret)
        {
            string message = null;
            switch (ret)
            {
                case (Abilities.AbilityMain.RETURN.FAIL_COOLDOWN):
                    message = "Not Ready!";
                    break;
                case (Abilities.AbilityMain.RETURN.FAIL_LINE_OF_SIGHT):
                    message = "Line of Sight!";
                    break;
                case (Abilities.AbilityMain.RETURN.FAIL_MANA):
                    message = "Not Enough Mana!";
                    break;
                case (Abilities.AbilityMain.RETURN.FAIL_NOT_IMPLEMENTRD):
                    message = "Not Yet Implemented!";
                    break;
                case (Abilities.AbilityMain.RETURN.FAIL_STATUS):
                    message = "Cannot Be Used Right Now!";
                    break;
                case (Abilities.AbilityMain.RETURN.FAIL_REQUIREMENTS):
                    message = "Requirements Not Met!";
                    break;
                default:
                    //no message
                    break;
            }
            if (message != null)
            {
                if (ExperienceAndClasses.localMyPlayer.ability_message_overhead)
                {
                    CombatText.NewText(Main.LocalPlayer.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_RED, message);
                }
                else
                {
                    Main.NewText(Abilities.AbilityMain.AbilityLookup[(int)id].GetName() + ": " + message, ExperienceAndClasses.MESSAGE_COLOUR_RED);
                }
            }
        }

        public void PreventItemUse(int milliseconds)
        {
            itemUsePrevented = true;
            DateTime time = DateTime.Now.AddMilliseconds(milliseconds);
            if(time.CompareTo(timeAllowItemUse) > 0)
                timeAllowItemUse = time;
        }

        public void OffCooldownMessage(string abilityName)
        {
            CombatText.NewText(player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_OFF_COOLDOWN, abilityName + " Ready!");
        }

        public static void GrantDebuffImunity(int index, DateTime time_start, double duration_seconds)
        {
            //trigger update
            MyPlayer.debuff_immunity_update[index] = true;

            //set start time unless there is already a sooner, upcoming start
            if (DateTime.Now.CompareTo(MyPlayer.debuff_immunity_time_start[index]) > 0 || time_start.CompareTo(MyPlayer.debuff_immunity_time_start[index]) < 0)
            {
                MyPlayer.debuff_immunity_time_start[index] = time_start;
            }

            //set duration unless there is already a longer duration triggered
            if (!(debuff_immunity_duration_seconds[index] > duration_seconds))
            {
                debuff_immunity_duration_seconds[index] = duration_seconds;
            }
        }

        public bool HasImmunityItem()
        {
            foreach (Item i in player.armor)
            {
                if (i.Name.Length > 0 && (i.Name.Equals("Cross Necklace") || i.Name.Equals("Star Veil")))
                {
                    return true;
                }
            }
            return false;
        }

        public void EndStatus(int index)
        {
            //status ending
            status_active[index] = false;
            status_end_time[index] = DateTime.MinValue;
            status_magnitude[index] = 0;
            if (player.whoAmI == Main.LocalPlayer.whoAmI)
            {
                //local, sync effect ending
                Projectile.NewProjectile(player.Center, new Vector2(0.1f), mod.ProjectileType<Abilities.AbilityProj.Misc_PlayerStatus>(), player.whoAmI, 0, player.whoAmI, index, -1);
            }
        }

    }
}
