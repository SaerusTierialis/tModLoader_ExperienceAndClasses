using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;

namespace ExperienceAndClasses.Abilities
{
    public class AbilityMain
    {
        /* ~~~~~~~~~~~~ Cosntants ~~~~~~~~~~~~ */
        protected const int ACTIVE_PREVENT_ATTACK_MILLISECONDS = 400;


        /* ~~~~~~~~~~~~ Values ~~~~~~~~~~~~ */
        public enum RETURN : byte
        {
            UNUSED,
            SUCCESS,
            FAIL_NOT_IMPLEMENTRD,
            FAIL_EXTERNAL_CALL,
            FAIL_MANA,
            FAIL_COOLDOWN,
            FAIL_REQUIREMENTS,
            FAIL_STATUS,
            FAIL_LINE_OF_SIGHT,
        }

        public enum ABILITY_TYPE : byte
        {
            NOT_IMPLEMENTED,
            PASSIVE,
            ACTIVE,
            UPGRADE,
            ALTERNATE,
        }

        public enum ID : ushort //order does not matter except where specified
        {
            UNDEFINED, //leave this first

            Cleric_Passive_Cleanse,
            Cleric_Active_Heal,
            Cleric_Upgrade_Heal_Smite,
            Cleric_Active_Sanctuary,
            Cleric_Alternate_Heal_Barrier,
            Cleric_Upgrade_Sanctuary_Blessing,

            Saint_Active_DivineIntervention,
            Saint_Upgrade_Heal_Cure,
            Saint_Upgrade_Sanctuary_Link,
            Saint_Upgrade_Heal_Purify,
            Saint_Active_Paragon,

            //when adding here, make that that a class of the same name is added below

            NUMBER_OF_IDs, //leave this last
        }

        public enum CLASS_TYPE : byte
        {
            UNUSED,
            SUPPORT,

            NUMBER_OF_CLASS_TYPES
        }

        /* ~~~~~~~~~~~~ List of Abilities (+ initialize class type colours) ~~~~~~~~~~~~ */
        //contains the one and only instance of each ability
        //not actually a "list" but an ID-indexed array
        //auto-populates from ID
        //will CRASH on mod startup if there is no corresponding class for an ID
        //using this list is a pretty cumbersome design choice but it allowed for various efficiencies

        public static Ability[] AbilityLookup = new Ability[(int)ID.NUMBER_OF_IDs];
        public static readonly Color[] COLOUR_CLASS_TYPE = new Color[(int)CLASS_TYPE.NUMBER_OF_CLASS_TYPES];
        static AbilityMain()
        {
            //class type colours
            COLOUR_CLASS_TYPE[(int)CLASS_TYPE.SUPPORT] = new Color(255, 255, 100); //new Color(239, 239, 239);

            //fill list of abilities
            string[] IDs = Enum.GetNames(typeof(ID));
            for (byte i = 0; i < AbilityLookup.Length; i++)
            {
                AbilityLookup[i] = (Ability)(Assembly.GetExecutingAssembly().CreateInstance(typeof(AbilityMain).FullName + "+" + IDs[i]));
            }
        }

        /* ~~~~~~~~~~~~ Abilities (includes all active, upgrade, passive, proc, etc) ~~~~~~~~~~~~ */
        //singleton implementation

        public class Cleric_Passive_Cleanse : Ability
        {
            private const double seconds_delay = 10;
            private const double seconds_duration = 120;

            public Cleric_Passive_Cleanse()
            {
                ability_type = ABILITY_TYPE.PASSIVE;
                name = "Cleanse";
                description = "";
                cooldown_seconds = 1;
                ignore_status_requirements = true;
            }

            protected override RETURN UseEffects(byte level = 1, bool alternate = false)
            {
                Player self = Main.LocalPlayer;
                DateTime now = DateTime.Now;
                int index;
                for (int i = 0; i < ExperienceAndClasses.NUMBER_OF_DEBUFFS; i++)
                {
                    index = ExperienceAndClasses.DEBUFFS[i];
                    if (self.HasBuff(index))
                    {
                        MyPlayer.GrantDebuffImunity(i, now.AddSeconds(seconds_delay), seconds_duration);
                    }
                }

                return RETURN.SUCCESS;
            }
        }

        public class Cleric_Active_Heal : Ability
        {
            public static float range = 600;
            private static float knockback = 5;
            private static float secondary_targets_multiplier = 0.5f;
            public static float undead_bonus_multiplier = 2f;

            public Cleric_Active_Heal()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Heal";
                name_short = "Heal";
                description = "";
                cost_mana_percent = 0.35f;
                cooldown_seconds = 3;
                class_type = CLASS_TYPE.SUPPORT;
                requires_sight_cursor = true;

                upgrades = new ID[] { ID.Cleric_Upgrade_Heal_Smite , ID.Saint_Upgrade_Heal_Cure , ID.Saint_Upgrade_Heal_Purify };

                alternative = ID.Cleric_Alternate_Heal_Barrier;
                cost_mana_alternative_multiplier = Cleric_Alternate_Heal_Barrier.mana_multiplier;
            }
            protected override RETURN UseEffects(byte level = 1, bool alternate = false)
            {
                //update values
                UpdateHealingValues(level);

                //location
                location = Main.MouseWorld;

                if (!alternate || !ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)alternative]) //main effect (heal)
                {
                    //visual (dust)
                    Projectile.NewProjectile(location, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<DustMakerProj>(), 0, 0, Main.LocalPlayer.whoAmI, (float)DustMakerProj.MODE.heal);

                    //update upgrades
                    upgrade_smite = ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)ID.Cleric_Upgrade_Heal_Smite];

                    //look for players/npcs
                    Tuple<List<Tuple<bool, int, bool>>, int, int, bool, bool> target_info = FindTargets(ExperienceAndClasses.localMyPlayer.player, location, range, true, true, true);
                    nearest_friendly_index = target_info.Item2;
                    nearest_hostile_index = target_info.Item3;
                    nearest_friendly_is_player = target_info.Item4;
                    nearest_hostile_is_player = target_info.Item5;

                    //do action
                    target_info.Item1.ForEach(HealAction);
                }
                else //alternative effect (barrier)
                {
                    Projectile.NewProjectile(location, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<AbilityProj.Cleric_Barrier>(), (int)(value_damage * Cleric_Alternate_Heal_Barrier.damage_multiplier), Cleric_Alternate_Heal_Barrier.knockback, Main.LocalPlayer.whoAmI);
                }

                return RETURN.SUCCESS;
            }

            public static void UpdateHealingValues(byte level = 0)
            {
                //local player
                MyPlayer self = ExperienceAndClasses.localMyPlayer;

                //if not told which level to use, check
                if (level == 0)
                {
                    level = (byte)self.effectiveLevel;
                }

                //calculate heal others (20+((level/10)^1.7))
                value_heal_other = (20 + Math.Pow(level / 10, 1.7)) * self.healing_power;

                //calculate heal self (15+((level/10)^1.4))
                value_heal_self = (15 + Math.Pow(level / 10, 1.4)) * self.healing_power;

                //calculate heal damage (15+((level/10)^2))
                value_damage = (15 + Math.Pow(level / 10, 2)) * self.healing_power;
            }

            private static int nearest_friendly_index;
            private static int nearest_hostile_index;
            private static bool nearest_friendly_is_player;
            private static bool nearest_hostile_is_player;
            private static Vector2 location;
            public static double value_heal_self;
            public static double value_heal_other;
            public static double value_damage;
            private static bool upgrade_smite;
            private static void HealAction(Tuple<bool, int, bool> target)
            {
                //parse input
                bool is_player = target.Item1;
                int index = target.Item2;
                bool is_hostile = target.Item3;

                //ai[0] = 1 if player, 0 if bpc
                float player_val = 1;
                if (!is_player)
                    player_val = 0;

                //immunities
                bool has_cure = ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)ID.Saint_Upgrade_Heal_Cure];
                bool has_purify = ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)ID.Saint_Upgrade_Heal_Purify];
                List<int> immunities = new List<int>();
                if (has_cure || has_purify)
                {
                    for (int i = 0; i < ExperienceAndClasses.NUMBER_OF_DEBUFFS; i++)
                    {
                        if (Main.LocalPlayer.buffImmune[ExperienceAndClasses.DEBUFFS[i]])
                        {
                            immunities.Add(i);
                        }
                    }
                }

                //get value of heal/damage
                double value;
                NPC npc;
                if (is_hostile)
                {
                    //require smite else return
                    if (!upgrade_smite)
                    {
                        return;
                    }

                    //damage
                    value = -1 * value_damage;

                    //adjust
                    if ((index != nearest_hostile_index) || (is_player && !nearest_hostile_is_player))
                    {
                        value *= secondary_targets_multiplier;
                    }

                    //bonus damage to undead
                    if (!is_player)
                    {
                        npc = Main.npc[index];
                        if (IsUndead(npc))
                        {
                            value *= undead_bonus_multiplier;
                        }
                    }
                }
                else
                {
                    //heal
                    if (is_player && index == Main.LocalPlayer.whoAmI)
                    {
                        value = value_heal_self;
                    }
                    else
                    {
                        value = value_heal_other;

                        //cure and purify
                        if (is_player && Main.player[index].active && !Main.player[index].dead)
                        {
                            if (has_purify)
                            {
                                Methods.PacketSender.ClientSendDebuffImmunity(index, immunities, Saint_Upgrade_Heal_Purify.immunity_duration_seconds);
                            }
                            else if (has_cure)
                            {
                                Methods.PacketSender.ClientSendDebuffImmunity(index, immunities, Saint_Upgrade_Heal_Cure.immunity_duration_seconds);
                            }
                        }
                    }

                    //adjust
                    if ((index != nearest_friendly_index) || (is_player && !nearest_friendly_is_player))
                    {
                        value *= secondary_targets_multiplier;
                    }
                }

                //round down to int (implicit)
                int value_final = (int)value;
                
                //create projecile to handle (easy way to sync for multiplayer)
                Projectile.NewProjectile(location, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<AbilityProj.Misc_HealHurt>(), value_final, knockback, Main.LocalPlayer.whoAmI, player_val, index);
            }
        }

        public class Cleric_Upgrade_Heal_Smite : Ability
        {
            public Cleric_Upgrade_Heal_Smite()
            {
                ability_type = ABILITY_TYPE.UPGRADE;
                name = "Heal - Smite";
                description = "";
            }
        }

        public class Cleric_Active_Sanctuary : Ability
        {
            private const double req_time_out_combat_seconds = 10;
            public const double pulse_seconds = 1;
            public const float light_effect = 1f;
            public const double buff_duration_seconds = 120;

            public Cleric_Active_Sanctuary()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Sanctuary";
                name_short = "Sanc";
                description = "";
                cost_mana_percent = 0.90f;
                cooldown_seconds = 120;
                class_type = CLASS_TYPE.SUPPORT;
                upgrades = new ID[] { ID.Cleric_Upgrade_Sanctuary_Blessing , ID.Saint_Upgrade_Sanctuary_Link };
            }

            protected override RETURN UseEffects(byte level = 1, bool alternate = false)
            {
                //which sanctuary to place
                int sanc_index = 0;
                if (alternate && ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)ID.Saint_Upgrade_Sanctuary_Link])
                {
                    sanc_index = 1;
                }

                //create
                Projectile.NewProjectile(Main.LocalPlayer.Center, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<AbilityProj.Cleric_Sanctuary>(), 0, 0, Main.LocalPlayer.whoAmI, sanc_index);

                //success
                return RETURN.SUCCESS;
            }

            protected override RETURN UseChecks(byte level = 1, bool alternate = false)
            {
                //allow use on cooldown if NOT replacing
                int sanc_index = 0;
                if (alternate && ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)ID.Saint_Upgrade_Sanctuary_Link])
                {
                    sanc_index = 1;
                }
                if (ExperienceAndClasses.localMyPlayer.sanctuaries[sanc_index] == null)
                {
                    cooldown_active = false;
                }
                return base.UseChecks(level, alternate);
            }

            public static void Pulse(Projectile projectile)
            {
                //get heal value
                Cleric_Active_Heal.UpdateHealingValues((byte)ExperienceAndClasses.localMyPlayer.effectiveLevel);
                int heal = (int)Cleric_Active_Heal.value_heal_other;

                //heal valid players
                Tuple<List<Tuple<bool, int, bool>>, int, int, bool, bool> target_info = FindTargets(Main.LocalPlayer, projectile.Center, projectile.width / 2, false, true, true, true, false);
                List<Tuple<bool, int, bool>> targets = target_info.Item1;
                DateTime now = DateTime.Now;
                MyPlayer myPlayer;
                int heal_buff;
                foreach (Tuple<bool, int, bool> t in targets)
                {
                    if (t.Item1)
                    {
                        //player
                        myPlayer = Main.player[t.Item2].GetModPlayer<MyPlayer>(ExperienceAndClasses.mod);
                        if ((DateTime.Now.Subtract(myPlayer.time_last_combat).TotalSeconds >= req_time_out_combat_seconds) && 
                            (DateTime.Now.Subtract(myPlayer.time_last_sanc_effect).TotalSeconds >= pulse_seconds))
                        {
                            //heal
                            myPlayer.time_last_sanc_effect = now;
                            Projectile.NewProjectile(projectile.Center, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<AbilityProj.Misc_HealHurt>(), heal, 0, Main.LocalPlayer.whoAmI, 1, t.Item2);

                            //buff
                            if ((myPlayer.sanctuary_buff != null) && myPlayer.sanctuary_buff.heal > heal)
                            {
                                heal_buff = myPlayer.sanctuary_buff.heal;
                            }
                            else if (ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)ID.Cleric_Upgrade_Sanctuary_Blessing])
                            {
                                heal_buff = heal;
                            }
                            else
                            {
                                heal_buff = 0;
                            }
                            Projectile.NewProjectile(myPlayer.player.Center, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<AbilityProj.Cleric_SanctuaryBuff>(), 0, 0, myPlayer.player.whoAmI, heal_buff, 1);
                        }
                    }
                    else 
                    {
                        //npc
                        Projectile.NewProjectile(projectile.Center, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<AbilityProj.Misc_HealHurt>(), heal, 0, Main.LocalPlayer.whoAmI, 0, t.Item2);
                    }
                }
            }

            public static void TryWarp()
            {
                MyPlayer myPlayer;
                int sanc2;
                DateTime now = DateTime.Now;
                for (int player = 0; player < Main.maxPlayers; player++)
                {
                    if (Main.player[player].active)
                    {
                        myPlayer = Main.player[player].GetModPlayer<MyPlayer>(ExperienceAndClasses.mod);
                        for (int sanc = 0; sanc <= 1; sanc++)
                        {
                            if ((myPlayer.sanctuaries[sanc] != null) && myPlayer.sanctuaries[sanc].active)
                            {
                                if (myPlayer.sanctuaries[sanc].Hitbox.Intersects(Main.LocalPlayer.Hitbox))
                                {
                                    sanc2 = 1 - sanc;
                                    if ((myPlayer.sanctuaries[sanc2] != null) && myPlayer.sanctuaries[sanc2].active)
                                    {
                                        if (now.Subtract(ExperienceAndClasses.localMyPlayer.time_last_combat).TotalSeconds >= req_time_out_combat_seconds)
                                        {
                                            //teleport to sanc2
                                            Main.LocalPlayer.UnityTeleport(new Vector2(myPlayer.sanctuaries[sanc2].Center.X - Main.LocalPlayer.width / 2,
                                            myPlayer.sanctuaries[sanc2].Center.Y - Main.LocalPlayer.height / 2));
                                        }
                                        else
                                        {
                                            Main.NewText("Cannot warp during combat!", ExperienceAndClasses.MESSAGE_COLOUR_RED);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public class Cleric_Alternate_Heal_Barrier : Ability
        {
            public static float knockback = 10;
            public static float damage_multiplier = 1f;
            public static float mana_multiplier = 2f;

            public Cleric_Alternate_Heal_Barrier()
            {
                ability_type = ABILITY_TYPE.ALTERNATE;
                name = "Heal - Barrier";
                description = "";
            }
        }

        public class Cleric_Upgrade_Sanctuary_Blessing : Ability
        {
            public Cleric_Upgrade_Sanctuary_Blessing()
            {
                ability_type = ABILITY_TYPE.UPGRADE;
                name = "Sanctuary - Blessing";
                description = "";
            }
        }

        public class Saint_Active_DivineIntervention : Ability
        {
            public Saint_Active_DivineIntervention()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Divine Intervention";
                name_short = "DI";
                description = "";
                cost_mana_percent = 0.50f;
                cooldown_seconds = 20;
                class_type = CLASS_TYPE.SUPPORT;
            }
        }

        public class Saint_Upgrade_Heal_Cure : Ability
        {
            public static double immunity_duration_seconds = 0;
            public Saint_Upgrade_Heal_Cure()
            {
                ability_type = ABILITY_TYPE.UPGRADE;
                name = "Heal - Cure";
                description = "";
            }
        }

        public class Saint_Upgrade_Sanctuary_Link : Ability
        {
            public Saint_Upgrade_Sanctuary_Link()
            {
                ability_type = ABILITY_TYPE.UPGRADE;
                name = "Sanctuary - Link";
                description = "";
            }
        }

        public class Saint_Upgrade_Heal_Purify : Ability
        {
            public static double immunity_duration_seconds = 120;
            public Saint_Upgrade_Heal_Purify()
            {
                ability_type = ABILITY_TYPE.UPGRADE;
                name = "Heal - Purify";
                description = "";
            }
        }

        public class Saint_Active_Paragon : Ability
        {
            public Saint_Active_Paragon()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Paragon";
                name_short = "Para";
                description = "";
                cost_mana_percent = 0.50f;
                cooldown_seconds = 300;
                class_type = CLASS_TYPE.SUPPORT;
            }
        }

        public class UNDEFINED : Ability { }

        /* ~~~~~~~~~~~~ Ability Abstract ~~~~~~~~~~~~ */
        //singleton implementation
        //nothing should be static in here

        public abstract class Ability
        {
            //type of ability
            protected ABILITY_TYPE ability_type = ABILITY_TYPE.NOT_IMPLEMENTED;

            //toggle on for constant passives for efficiency
            protected bool skip_checks_and_costs = false;

            //descriptives
            protected string name = "undefined";
            protected string name_short = "undefined";
            protected string description = "undefined";

            //upgrades and alternative for actives
            protected ID[] upgrades = new ID[0];
            protected ID alternative = new ID();

            //coodlown tracking
            protected bool cooldown_active = false;
            protected DateTime cooldown_time_end = DateTime.MinValue;

            //costs
            protected int cost_mana_base = 0;
            protected float cost_mana_percent = 0f;
            protected float cost_mana_reduction_cap = 0.8f;
            protected float cost_mana_alternative_multiplier = 1f;
            protected double cooldown_seconds = 0;
            protected bool requires_sight_cursor = false;
            protected bool ignore_status_requirements = false;
            protected double override_cooldown = -1;

            //on-use effects
            protected CLASS_TYPE class_type = CLASS_TYPE.UNUSED;
            protected bool active_prevents_attack = true;

            //encapsulate whatever needs external access (better formats wouldn't compile in tModLoader - exit code 1)
            public string GetName()
            {
                return name;
            }
            public string GetNameShort()
            {
                return name_short;
            }
            public bool OnCooldown(bool changeValue = false, bool newValue = false)
            {
                if (changeValue)
                    cooldown_active = newValue;
                return cooldown_active;
            }

            public RETURN Use(byte level = 1, bool alternate = false)
            {
                //not to be used by server, all abilities are client-side
                if (Main.netMode == 2) return RETURN.FAIL_EXTERNAL_CALL;

                //store outcome
                RETURN return_value;

                //pre-checks
                if (!skip_checks_and_costs)
                {
                    return_value = UseChecks(level, alternate);
                    if (return_value != RETURN.SUCCESS) return return_value;
                }

                //do effect (override UseEffects)
                return_value = UseEffects(level, alternate);
                if (return_value != RETURN.SUCCESS) return return_value;

                //active on-use effects
                if (IsTypeActive())
                {
                    CastDust();
                    if (active_prevents_attack)
                        ExperienceAndClasses.localMyPlayer.PreventItemUse(ACTIVE_PREVENT_ATTACK_MILLISECONDS);
                }

                //take costs
                if (!skip_checks_and_costs)
                {
                    return_value = UseCosts(level, alternate);
                }
                override_cooldown = -1;

                //return final result
                return return_value;
            }

            protected virtual RETURN UseChecks(byte level = 1, bool alternate = false)
            {
                //check for invalid statuses
                if (!ignore_status_requirements && (Main.LocalPlayer.frozen || Main.LocalPlayer.silence)) return RETURN.FAIL_STATUS;
                if (Main.LocalPlayer.dead) return RETURN.FAIL_STATUS;

                //check mana cost
                if (Main.LocalPlayer.statMana < GetManaCost(level, alternate)) return RETURN.FAIL_MANA;

                //check cooldown
                if (cooldown_active) return RETURN.FAIL_COOLDOWN;

                //line of sight
                if (requires_sight_cursor)
                {
                    Vector2 target = Main.MouseWorld;
                    if (!Collision.CanHit(Main.LocalPlayer.position, 0, 0, target, 0, 0))
                    {
                        return RETURN.FAIL_LINE_OF_SIGHT;
                    }
                }

                return RETURN.SUCCESS;
            }

            protected virtual RETURN UseEffects(byte level = 1, bool alternate = false)
            {
                return RETURN.FAIL_NOT_IMPLEMENTRD;
            }

            protected virtual RETURN UseCosts(byte level = 1, bool alternate = false)
            {
                Main.LocalPlayer.statMana -= GetManaCost(level, alternate);
                if (Main.LocalPlayer.statMana < 0)
                {
                    Main.LocalPlayer.statMana = 0;
                }
                double cd = GetCooldownSecs(level);
                if (override_cooldown > 0)
                {
                    cooldown_active = true;
                    cooldown_time_end = DateTime.Now.AddSeconds(override_cooldown);
                }
                else if (cd > 0)
                {
                    cooldown_active = true;
                    cooldown_time_end = DateTime.Now.AddSeconds(cd);
                }
                return RETURN.SUCCESS;
            }

            public int GetManaCost(byte level = 1, bool alternate = false)
            {
                int manaCost = (int)((cost_mana_base + (cost_mana_percent * Main.LocalPlayer.statManaMax2)) * Main.LocalPlayer.manaCost); //apply cost_mana_reduction_cap

                if (alternate)
                {
                    manaCost = (int)(manaCost * cost_mana_alternative_multiplier);
                }

                if (manaCost < 0) manaCost = 0;
                if (manaCost > Main.LocalPlayer.statManaMax2) manaCost = Main.LocalPlayer.statManaMax2;

                return manaCost;
            }

            public virtual double GetCooldownSecs(byte level = 1)
            {
                return cooldown_seconds;
            }

            public float GetCooldownRemainingSeconds()
            {
                return (float)(cooldown_time_end.Subtract(DateTime.Now).TotalMilliseconds) / 1000;
            }

            protected void CastDust()
            {
                //create dust from projectile for easy multiplayer sync
                Projectile.NewProjectile(Main.LocalPlayer.position.X, Main.LocalPlayer.position.Y, 0, 0, ExperienceAndClasses.mod.ProjectileType<DustMakerProj>(), 0, 0, Main.LocalPlayer.whoAmI, (float)DustMakerProj.MODE.ability_cast, (float)class_type);
            }

            public virtual string CooldownUI(byte level, out float percent)
            {
                //calculate cd time remaining
                double timeRemain = GetCooldownRemainingSeconds();
                if (timeRemain < 0)
                    timeRemain = 0;

                //if time, set string
                string cooldownText = null;
                if (timeRemain > 0)
                    cooldownText = Math.Round(timeRemain, 1).ToString();

                //also calculate percentage complete
                double cd = GetCooldownSecs(level);
                percent = (float)((cd - timeRemain) / cd);

                return cooldownText;
            }

            public bool IsTypeActive()
            {
                return ability_type == ABILITY_TYPE.ACTIVE;
            }

            public bool IsTypePassive()
            {
                return ability_type == ABILITY_TYPE.PASSIVE;
            }

        }

        /// <summary>
        /// Return Tuple:
        /// List = see below
        /// int1 = nearest_friendly_index
        /// int2 = nearest_hostile_index
        /// bool1 = nearest_friendly_is_player
        /// bool2 = nearest_hostile_is_player
        /// 
        /// List Tuples:
        /// bool1 = is_player
        /// int = index
        /// bool2 = is_hostile
        /// </summary>
        /// <param name="source"></param>
        /// <param name="location"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private static Tuple<List<Tuple<bool, int, bool>>, int, int, bool, bool> FindTargets(Player source, Vector2 location, float radius, bool require_line_of_sight = true, bool include_players = true, bool include_npc = true, bool include_friendly = true, bool include_hostile = true)
        {
            List<Tuple<bool, int, bool>> targets = new List<Tuple<bool, int, bool>>();
            int nearest_friendly_index = -1;
            int nearest_hostile_index = -1;
            float nearest_friendly_distance = radius;
            float nearest_hostile_distance = radius;
            bool nearest_friendly_is_player = false;
            bool nearest_hostile_is_player = false;

            Player player;
            NPC npc;
            float distance;

            //search players
            if (include_players)
            {
                for (int player_index = 0; player_index <= Main.maxPlayers; player_index++)
                {
                    player = Main.player[player_index];
                    if (player.active && !player.dead)
                    {
                        distance = player.Distance(location);
                        if ((distance <= radius) && Collision.CanHit(location, 0, 0, player.Center, 0, 0))
                        {
                            if (source.hostile && player.hostile && ((source.team == 0) || (source.team != player.team)) && (source.whoAmI != player.whoAmI)) //both in pvp, self doesn't have a team or is on a different team
                            {
                                if (include_hostile)
                                {
                                    //hostile
                                    targets.Add(new Tuple<bool, int, bool>(true, player_index, true));
                                    if (distance <= nearest_hostile_distance)
                                    {
                                        nearest_hostile_distance = distance;
                                        nearest_hostile_index = player.whoAmI;
                                        nearest_hostile_is_player = true;
                                    }
                                }
                            }
                            else if (include_friendly)
                            {
                                //friendly
                                targets.Add(new Tuple<bool, int, bool>(true, player_index, false));
                                if (distance <= nearest_friendly_distance)
                                {
                                    nearest_friendly_distance = distance;
                                    nearest_friendly_index = player.whoAmI;
                                    nearest_friendly_is_player = true;
                                }
                            }
                        }
                    }
                }
            }

            //search npcs
            if (include_npc)
            {
                int num_npc_total = Main.npc.Length;
                for (int npc_index = 0; npc_index < num_npc_total; npc_index++)
                {
                    npc = Main.npc[npc_index];
                    if (npc.active)
                    {
                        distance = npc.Distance(location);
                        if ((distance <= radius) && Collision.CanHit(location, 0, 0, npc.Center, 0, 0))
                        {
                            if (!npc.friendly)
                            {
                                if (include_hostile)
                                {
                                    //hostile
                                    targets.Add(new Tuple<bool, int, bool>(false, npc_index, true));
                                    if (distance <= nearest_hostile_distance)
                                    {
                                        nearest_hostile_distance = distance;
                                        nearest_hostile_index = npc.whoAmI;
                                        nearest_hostile_is_player = false;
                                    }
                                }
                            }
                            else if (include_friendly)
                            {
                                //friendly
                                targets.Add(new Tuple<bool, int, bool>(false, npc_index, false));
                                if (distance <= nearest_friendly_distance)
                                {
                                    nearest_friendly_distance = distance;
                                    nearest_friendly_index = npc.whoAmI;
                                    nearest_friendly_is_player = false;
                                }
                            }
                        }
                    }
                }
            }

            //return described above
            return new Tuple<List<Tuple<bool, int, bool>>, int, int, bool, bool>(targets, nearest_friendly_index, nearest_hostile_index, nearest_friendly_is_player, nearest_hostile_is_player);
        }

        public static SortedList npc_undead = new SortedList(300);
        public static bool IsUndead(NPC npc)
        {
            int id = npc.netID;
            if (!npc_undead.ContainsKey(id))
            {
                bool result = false;

                string npc_name = npc.TypeName.ToLower();
                foreach (string type in ExperienceAndClasses.UNDEAD_NAMES)
                {
                    if (npc_name.Contains(type))
                    {
                        result = true;
                        break;
                    }
                }

                npc_undead.Add(id, result);
            }
            return (bool)npc_undead.GetByIndex(npc_undead.IndexOfKey(id));
        }
    }
}
