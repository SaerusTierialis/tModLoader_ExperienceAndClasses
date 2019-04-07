﻿using ExperienceAndClasses.Utilities.Containers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Ability {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum IDs : ushort {
            Block,


            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        private enum USE_RESULT : byte {
            SUCCESS,
            FAIL_CLASS_LEVEL,
            FAIL_NOT_ENOUGH_MANA,
            FAIL_NOT_ENOUGH_RESOURCE,
            FAIL_MISSING_RESOURCE,
            FAIL_ON_COOLDOWN,
            FAIL_RANGE,
            FAIL_LINE_OF_SIGHT,
            FAIL_NO_TARGET,
            FAIL_SILENCED,
            FAIL_IMMOBILIZED,
            FAIL_ANTIREQUISITE_STATUS,
            FAIL_DEAD,
            FAIL_CHANNELLING,
            FAIL_SPECIFIC,
        }

        protected enum TARGET_POSITION_TYPE : byte {
            NONE,                       //no position
            SELF,                       //position of self
            CURSOR,                     //position of cursor
            BETWEEN_SELF_AND_CURSOR,    //position between self and cursor (based on range)
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const double time_between_repeat_fail_message_seconds = 0.5;
        private static DateTime time_allow_repeat_fail_message = DateTime.MinValue;
        private static USE_RESULT type_last_Fail_message = USE_RESULT.SUCCESS;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Ability[] LOOKUP { get; private set; }

        static Ability() {
            LOOKUP = new Ability[(ushort)IDs.NUMBER_OF_IDs];
            for (ushort i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Ability>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Status-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Specific_Name { get; protected set; } = "default_name";
        protected string specific_description = "default_description";

        /// <summary>
        /// default is TARGET_POSITION_TYPE.NONE
        /// </summary>
        protected TARGET_POSITION_TYPE specific_target_position_type = TARGET_POSITION_TYPE.NONE;

        protected float specific_mana_cost_flat = 0;
        protected float specific_mana_cost_percent = 0f;
        protected bool specific_mana_apply_reduction = true;

        protected Systems.Resource.IDs specific_resource = Systems.Resource.IDs.NONE;
        protected float specific_resource_cost_flat = 0;
        protected float specific_resource_cost_percent = 0f;

        protected float specific_cooldown_seconds = 0f;
        protected bool specific_cooldown_apply_reduction = true;

        protected List<Systems.Status.IDs> specific_antirequisite_statuses = new List<Status.IDs>();

        /// <summary>
        /// adds description of channelling to the UI text | default is false
        /// </summary>
        protected bool specific_is_channelling = false;
        protected bool specific_can_be_used_while_channelling = false;

        public Systems.Class.IDs Specific_Required_Class_ID { get; protected set; } = Systems.Class.IDs.None;
        public byte Specific_Required_Class_Level { get; protected set; } = 0;

        protected float specific_range_base = 0;
        protected float specific_range_level_multiplier = 0;

        protected float specific_radius_base = 0;
        protected float specific_radius_level_multiplier = 0;

        protected float specific_power_base = 0f;
        /// <summary>
        /// if weapon matches type, add its per-hit damage to base power | default is false
        /// </summary>
        protected bool specific_power_base_add_weapon_damage_if_type = false;
        /// <summary>
        /// if weapon matches type, add its damage-per-use-time to base power | default is false
        /// </summary>
        protected bool specific_power_base_add_weapon_dpt_if_type = false;
        /// <summary>
        /// apply highest of type bonuses | default is false
        /// </summary>
        protected bool specific_power_apply_bonus_highest = false;
        /// <summary>
        /// apply all type bonuses | default is false
        /// </summary>
        protected bool specific_power_apply_bonus_all = false;
        /// <summary>
        /// 0 is no bonus, 0.01 is a 1%/point bonus, etc. | default is 0s
        /// </summary>
        protected float[] specific_power_attribute_multipliers = new float[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
        /// <summary>
        /// final multiplier (compounds other bonuses) | default is 0
        /// </summary>
        protected float specific_power_level_multiplier = 0;

        protected bool specific_type_melee = false;
        protected bool specific_type_ranged = false;
        protected bool specific_type_throwing = false;
        protected bool specific_type_minion = false;
        protected bool specific_type_magic = false;
        protected bool specific_type_holy = false;  //note: there are no holy weapons, but MPlayer has a holy_power stat

        /// <summary>
        /// Max number of targets (not including self if specific_targets_self_always).
        /// This max is SEPARATE for friendly and hostile target.
        /// When selection is limited by this max, the closest targets to the position_target are used.
        /// Set 0 if self is the only target to save time.
        /// | default is 0
        /// </summary>
        protected ushort specific_targets_max = 0;
        /// <summary>
        /// self can be included as a friednly target but it is not guarenteed and does count towards max | default is true
        /// </summary>
        protected bool specific_targets_self = true;
        /// <summary>
        /// ALWAYS include self as a friendly target, does not count towards max | default is true
        /// </summary>
        protected bool specific_targets_self_always = true;
        /// <summary>
        /// targets can include players | default is true
        /// </summary>
        protected bool specific_targets_player = true;
        /// <summary>
        /// targets can include NPCs | default is true
        /// </summary>
        protected bool specific_targets_npc = true;
        /// <summary>
        /// targets can be friendly | default is true
        /// </summary>
        protected bool specific_targets_friedly = true;
        /// <summary>
        /// targets can be hostile | default is true
        /// </summary>
        protected bool specific_targets_hostile = true;
        /// <summary>
        /// target position must have sight of target | default is true
        /// </summary>
        protected bool specific_targets_require_line_of_sight_position = true;
        /// <summary>
        /// player must have sight of target | default is false
        /// </summary>
        protected bool specific_targets_require_line_of_sight_player = false;
        /// <summary>
        /// required unless Systems.Status.IDs.NONE | default is Systems.Status.IDs.NONE
        /// </summary>
        protected Systems.Status.IDs specific_targets_require_status = Systems.Status.IDs.NONE;
        /// <summary>
        /// antirequisit unless Systems.Status.IDs.NONE | default is Systems.Status.IDs.NONE
        /// </summary>
        protected Systems.Status.IDs specific_targets_antirequisite_status = Systems.Status.IDs.NONE;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic (between activations) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;

        public ushort ID_num { get; private set; } = (ushort)IDs.NONE;

        private DateTime Time_Cooldown_End = DateTime.MinValue;

        public ModHotKey hotkey = null;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic (within activation) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //these are set by each PreActivate (and some also when creating UI text)
        protected byte level;
        private float range, radius, power;
        private ushort cost_mana, cost_resource;
        private float cooldown_seconds;
        private Vector2 position_player, position_cursor, position_target;
        private float position_target_distance;
        private bool target_position_line_of_sight;
        protected string custom_use_fail_message;
        protected List<Utilities.Containers.Thing> targets_friendly;
        protected List<Utilities.Containers.Thing> targets_hostile;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Core Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Ability(IDs id) {
            ID = id;
            ID_num = (ushort)id;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void Activate() {
            //check if activation is allowed, otherwise show message
            //pre-calculates:
            //costs
            //positions
            //targets
            //cooldown
            USE_RESULT result = PreActivate();
            if (result != USE_RESULT.SUCCESS) {
                FailMessage(result);
                return;
            }

            //calculate cooldown
            CalculateCooldown();

            //calculate power
            CalculatePower();

            //set cooldown
            if (cooldown_seconds > 0) {
                Time_Cooldown_End = ExperienceAndClasses.Now.AddSeconds(cooldown_seconds);
            }

            //take mana
            if (cost_mana > 0) {
                Main.LocalPlayer.statMana = Math.Max(0, Main.LocalPlayer.statMana - cost_mana);
            }

            //take resource
            if (cost_resource > 0) {
                Systems.Resource.LOOKUP[(byte)specific_resource].Amount = (ushort)Math.Max(0, Systems.Resource.LOOKUP[(byte)specific_resource].Amount - cost_resource);
            }

            //do main effect
            DoEffectMain();

            //do friendly target effect
            foreach (Utilities.Containers.Thing target in targets_friendly) {
                DoEffectTargetFriendly(target);
            }

            //do hostile target effect
            foreach (Utilities.Containers.Thing target in targets_hostile) {
                DoEffectTargetHostile(target);
            }
        }

        public string GetUIText() {
            return "TODO";
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Generic Calculations ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        // These calculate and return a value + set the variable for use elsewhere
        // Called by public lookups and during PreActivate() - i.e., automatically called during activation

        private ushort CalculateManaCost() {
            //mana cost
            float cost_mana_base = ModifyCostManaBase(specific_mana_cost_flat + (specific_mana_cost_percent * ExperienceAndClasses.LOCAL_MPLAYER.player.statManaMax2));
            if (specific_mana_apply_reduction) {
                cost_mana = (ushort)Math.Max(0, ModifyCostManaFinal(cost_mana_base * ExperienceAndClasses.LOCAL_MPLAYER.player.manaCost));
            }
            else {
                cost_mana = (ushort)Math.Max(0, ModifyCostManaFinal(cost_mana_base));
            }
            return cost_mana;
        }

        private ushort CalculateResourceCost() {
            //resource cost
            if (specific_resource == Systems.Resource.IDs.NONE) {
                cost_resource = 0;
            }
            else {
                float cost_resource_base = specific_resource_cost_flat + (specific_resource_cost_percent * Systems.Resource.LOOKUP[(byte)specific_resource].Capacity);
                cost_resource = (ushort)Math.Max(0, ModifyCostResource(cost_resource_base));
            }
            return cost_resource;
        }

        private float CalculateCooldown() {
            //cooldown
            cooldown_seconds = specific_cooldown_seconds;
            if (specific_cooldown_apply_reduction) {
                //100% delay reduction = 50% of base cooldown
                cooldown_seconds /= ExperienceAndClasses.LOCAL_MPLAYER.ability_delay_reduction;
            }
            cooldown_seconds = ModifyCooldown(cooldown_seconds);
            return cooldown_seconds;
        }

        private void CalculatePosition() {
            position_player = Main.LocalPlayer.position;
            position_cursor = Main.MouseWorld;

            switch (specific_target_position_type) {
                case TARGET_POSITION_TYPE.NONE:
                    position_target_distance = 0;
                    target_position_line_of_sight = true;
                    break;
                case TARGET_POSITION_TYPE.SELF:
                    position_target = position_player;
                    position_target_distance = 0;
                    target_position_line_of_sight = true;
                    break;
                case TARGET_POSITION_TYPE.CURSOR:
                    position_target = position_cursor;
                    position_target_distance = Vector2.Distance(position_player, position_target);
                    target_position_line_of_sight = Collision.CanHit(position_player, 0, 0, position_target, 0, 0);
                    break;
                case TARGET_POSITION_TYPE.BETWEEN_SELF_AND_CURSOR:
                    //first check if cursor is within range
                    float distance_cursor = Vector2.Distance(position_player, position_cursor);
                    if (distance_cursor > range) {
                        //cursor is too far - try at full range
                        position_target = Vector2.Lerp(position_player, position_cursor, range / distance_cursor);
                        position_target_distance = Vector2.Distance(position_player, position_target) - 1f;
                        target_position_line_of_sight = Collision.CanHit(position_player, 0, 0, position_target, 0, 0);
                    }
                    else {
                        //cursor was within range so just check sight
                        position_target = position_cursor;
                        position_target_distance = distance_cursor;
                        target_position_line_of_sight = Collision.CanHit(position_player, 0, 0, position_target, 0, 0);
                    }

                    if (!target_position_line_of_sight) {
                        //can't see target, need to move closer
                        Vector2 position_reference = position_target;
                        for (float percent_dist=0.9f; percent_dist >= 0; percent_dist -= 0.1f) {
                            position_target = Vector2.Lerp(position_player, position_reference, percent_dist);
                            position_target_distance = Vector2.Distance(position_player, position_target) - 1f;
                            target_position_line_of_sight = Collision.CanHit(position_player, 0, 0, position_target, 0, 0);
                            if (target_position_line_of_sight) {
                                break;
                            }
                        }
                    }
                    break;
                default:
                    Utilities.Commons.Error("Unsupported TARGET_POSITION_TYPE in [" + ID + "]: " + specific_target_position_type);
                    return;
            }
        }

        private float CalculateRange() {
            range = ModifyRange(specific_range_base + (level * specific_range_level_multiplier));
            return range;
        }

        private float CalculateRadius() {
            radius = ModifyRadius(specific_radius_base + (level * specific_radius_level_multiplier));
            return radius;
        }

        private float CalculatePower() {
            //base: initial
            power = specific_power_base;

            //base: add weapon damae?
            if (specific_power_base_add_weapon_damage_if_type) {
                Item item = Main.LocalPlayer.HeldItem;
                if ((specific_type_melee && item.melee) ||
                    (specific_type_ranged && item.ranged) ||
                    (specific_type_throwing && item.thrown) ||
                    (specific_type_minion && (item.summon || item.sentry)) ||
                    (specific_type_magic && item.magic)) {
                    
                    power += item.damage;
                }
            }

            //base: add weapon dpt?
            if (specific_power_base_add_weapon_dpt_if_type) {
                Item item = Main.LocalPlayer.HeldItem;
                if ((specific_type_melee && item.melee) ||
                    (specific_type_ranged && item.ranged) ||
                    (specific_type_throwing && item.thrown) ||
                    (specific_type_minion && (item.summon || item.sentry)) ||
                    (specific_type_magic && item.magic)) {

                    power += (float)item.damage / item.useTime;

                }
            }

            //base: modifications
            power = ModifyPowerBase(power);

            //multiplier: initialize
            float multiplier = 1f;

            //multiplier: attributes
            for (byte i = 0; i<(byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                if (Systems.Attribute.LOOKUP[i].Active) { //must be an active attribute
                    multiplier += specific_power_attribute_multipliers[i] * ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Final[i];
                }
            }

            //multiplier: highest of type?
            if (specific_power_apply_bonus_highest) {
                float value, highest = 0f;

                if (specific_type_melee) {
                    value = Main.LocalPlayer.meleeDamage - 1f;
                    if (value > highest) {
                        highest = value;
                    }
                }

                if (specific_type_ranged) {
                    value = Main.LocalPlayer.rangedDamage - 1f;
                    if (value > highest) {
                        highest = value;
                    }
                }

                if (specific_type_throwing) {
                    value = Main.LocalPlayer.thrownDamage - 1f;
                    if (value > highest) {
                        highest = value;
                    }
                }

                if (specific_type_minion) {
                    value = Main.LocalPlayer.minionDamage - 1f;
                    if (value > highest) {
                        highest = value;
                    }
                }

                if (specific_type_magic) {
                    value = Main.LocalPlayer.magicDamage - 1f;
                    if (value > highest) {
                        highest = value;
                    }
                }

                if (specific_type_holy) {
                    value = ExperienceAndClasses.LOCAL_MPLAYER.holy_power - 1f;
                    if (value > highest) {
                        highest = value;
                    }
                }

                //add highest to multiplier
                if (highest != 1f) {
                    //subtract out the base 100%
                    multiplier += highest;
                }
            }

            //mutilplier: all bonuses
            if (specific_power_apply_bonus_all) {
                if (specific_type_melee) {
                    multiplier += (Main.LocalPlayer.meleeDamage - 1f);
                }
                if (specific_type_ranged) {
                    multiplier += (Main.LocalPlayer.rangedDamage - 1f);
                }
                if (specific_type_throwing) {
                    multiplier += (Main.LocalPlayer.thrownDamage - 1f);
                }
                if (specific_type_minion) {
                    multiplier += (Main.LocalPlayer.minionDamage - 1f);
                }
                if (specific_type_magic) {
                    multiplier += (Main.LocalPlayer.magicDamage - 1f);
                }
                if (specific_type_holy) {
                    multiplier += (ExperienceAndClasses.LOCAL_MPLAYER.holy_power - 1f);
                }
            }

            //multiplier: apply
            power *= multiplier;

            //level multiplier (compounds other multiplier)
            if (specific_power_level_multiplier > 0f) {
                power *= (1f + (specific_power_level_multiplier * level));
            }

            //final modification
            power = ModifyPowerFinal(power);

            return power;
        }

        private byte CalculateLevel() {
            //calculate for primary
            byte level_primary = 0;
            byte levels = 0;
            Systems.Class c = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary;
            if (c.ID == Specific_Required_Class_ID) {
                level_primary = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective;
            }
            else {
                levels = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective;
                c = c.Prereq;
                while (c != null) {
                    levels += c.Max_Level;
                    if (c.ID == Specific_Required_Class_ID) {
                        level_primary = levels;
                        break;
                    }
                    c = c.Prereq;
                }
            }

            //calculate for secondary
            byte level_secondary = 0;
            levels = 0;
            c = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary;
            if (c.ID == Specific_Required_Class_ID) {
                level_secondary = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective;
            }
            else {
                levels = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective;
                c = c.Prereq;
                while (c != null) {
                    levels += c.Max_Level;
                    if (c.ID == Specific_Required_Class_ID) {
                        level_secondary = levels;
                        break;
                    }
                    c = c.Prereq;
                }
            }

            //use highest level (could still be 0 if not correct class)
            level = Math.Max(level_primary, level_secondary);

            //subtract out levels required
            level = (byte)(level - Specific_Required_Class_Level + 1);

            return level;
        }

        private void CalculateTargets() {
            targets_friendly = new List<Utilities.Containers.Thing>();
            targets_hostile = new List<Utilities.Containers.Thing>();

            Utilities.Containers.Thing self = ExperienceAndClasses.LOCAL_MPLAYER.thing;

            if (specific_targets_max > 0) {
                //create list of all valid friendly and hostile targets
                bool can_be_targeted, is_friendly;
                float distance;

                SortedDictionary<float, Utilities.Containers.Thing> friendly = new SortedDictionary<float, Utilities.Containers.Thing>();
                SortedDictionary<float, Utilities.Containers.Thing> hostile = new SortedDictionary<float, Utilities.Containers.Thing>();

                foreach (Utilities.Containers.Thing thing in Utilities.Containers.Thing.Things.Values) {
                    //don't target dead things or inactive
                    if (thing.Dead || !thing.Active) {
                        continue;
                    }

                    //check if friendly
                    is_friendly = self.IsFriendlyTo(thing);

                    //get distance to position_target
                    distance = thing.DistanceTo(position_target);

                    //standard requirements
                    if ((!specific_targets_player && thing.Is_Player) ||                                            //can't be player?
                        (!specific_targets_npc && thing.Is_Npc) ||                                                  //can't be npc?
                        (!specific_targets_friedly && is_friendly) ||                                               //can't be friendly?
                        (!specific_targets_hostile && !is_friendly) ||                                              //can't be hostile?
                        (distance > radius) ||                                             //too far?
                        (specific_targets_require_line_of_sight_player && !thing.HasSightOf(position_player)) ||    //needs sight of player?
                        (specific_targets_require_line_of_sight_position && !thing.HasSightOf(position_target)) ||  //needs sight of position?
                        (specific_targets_self_always && (thing.Index == self.Index)) ||                            //self_always and this is self (added at end instead)
                        (!thing.HasStatus(specific_targets_require_status)) ||                                      //doesn't have required status?
                        (thing.HasStatus(specific_targets_antirequisite_status))                                    //has antirequisite status?
                        ) {
                        can_be_targeted = false;
                    }
                    else {
                        can_be_targeted = true;
                    }

                    //ability-specific modification
                    can_be_targeted = ModifyCanBeTarget(thing, can_be_targeted);

                    //add as an option
                    if (is_friendly) {
                        friendly.Add(distance, thing);
                    }
                    else {
                        hostile.Add(distance, thing);
                    }
                }

                //copy to lists and reduce to closest targets (specific_targets_max)
                targets_friendly = friendly.Values.ToList();
                if (targets_friendly.Count > specific_targets_max) {
                    targets_friendly.RemoveRange(specific_targets_max, targets_friendly.Count - specific_targets_max + 1);
                }
                targets_hostile = hostile.Values.ToList();
                if (targets_hostile.Count > specific_targets_max) {
                    targets_hostile.RemoveRange(specific_targets_max, targets_hostile.Count - specific_targets_max + 1);
                }
            }

            //always add self? (ignores max)
            if (specific_targets_self_always) {
                targets_friendly.Add(self);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Private Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private USE_RESULT PreActivate() {
            //level (calculates for use later in activation)
            if (CalculateLevel() < 1) {
                return USE_RESULT.FAIL_CLASS_LEVEL;
            }

            //dead
            if (Main.LocalPlayer.dead) {
                return USE_RESULT.FAIL_DEAD;
            }

            //silence
            if (Main.LocalPlayer.HasBuff(BuffID.Silenced)) {
                return USE_RESULT.FAIL_SILENCED;
            }

            //immobile
            if (Main.LocalPlayer.HasBuff(BuffID.Stoned) || Main.LocalPlayer.HasBuff(BuffID.Frozen)) {
                return USE_RESULT.FAIL_IMMOBILIZED;
            }

            //channelling
            if (!specific_can_be_used_while_channelling && ExperienceAndClasses.LOCAL_MPLAYER.channelling) {
                return USE_RESULT.FAIL_CHANNELLING;
            }

            //cost mana (calculates for use later in activation)
            if (Main.LocalPlayer.statMana < CalculateManaCost()) {
                return USE_RESULT.FAIL_NOT_ENOUGH_MANA;
            }

            if (CalculateResourceCost() > 0) {
                //doesn't have the resource
                if (!ExperienceAndClasses.LOCAL_MPLAYER.Resources.Contains(specific_resource)) {
                    return USE_RESULT.FAIL_MISSING_RESOURCE;
                }

                //cost resource (calculates for use later in activation)
                if (Systems.Resource.LOOKUP[(byte)specific_resource].Amount < cost_resource) {
                    return USE_RESULT.FAIL_NOT_ENOUGH_RESOURCE;
                }
            }

            //cooldown
            if (Time_Cooldown_End.CompareTo(ExperienceAndClasses.Now) > 0) {
                return USE_RESULT.FAIL_ON_COOLDOWN;
            }

            //antireq statuses
            foreach (Systems.Status.IDs id in specific_antirequisite_statuses) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.thing.HasStatus(id)) {
                    return USE_RESULT.FAIL_ANTIREQUISITE_STATUS;
                }
            }

            //target position (calculates for use later in activation)
            CalculateRange();
            CalculatePosition();
            if (position_target_distance > range) {
                return USE_RESULT.FAIL_RANGE;
            }
            else if (!target_position_line_of_sight) {
                return USE_RESULT.FAIL_LINE_OF_SIGHT;
            }

            //targets (calculates for use later in activation)
            CalculateRadius();
            CalculateTargets();
            if (targets_friendly.Count == 0 && targets_hostile.Count == 0) {
                return USE_RESULT.FAIL_NO_TARGET;
            }

            //ability-specific fail
            custom_use_fail_message = "Requirements Not Met";
            if (!MeetsSpecificUseRequirements()) {
                return USE_RESULT.FAIL_SPECIFIC;
            }

            //can use!
            return USE_RESULT.SUCCESS;
        }

        private void FailMessage(USE_RESULT result) {
            if (ExperienceAndClasses.LOCAL_MPLAYER.show_ability_fail_messages) {

                if ((result != type_last_Fail_message) || (ExperienceAndClasses.Now.CompareTo(time_allow_repeat_fail_message) > 0)) {

                    type_last_Fail_message = result;
                    time_allow_repeat_fail_message = ExperienceAndClasses.Now.AddSeconds(time_between_repeat_fail_message_seconds);

                    string message;

                    switch (result) {
                        case USE_RESULT.FAIL_CLASS_LEVEL:
                            message = "Class Requirements Not Met";
                            break;

                        case USE_RESULT.FAIL_NOT_ENOUGH_MANA:
                            message = "Not Enough Mana";
                            break;

                        case USE_RESULT.FAIL_NOT_ENOUGH_RESOURCE:
                            message = "Not Enough " + Systems.Resource.LOOKUP[(byte)specific_resource].Name;
                            break;

                        case USE_RESULT.FAIL_ON_COOLDOWN:
                            message = "Ability on Coolown";
                            break;

                        case USE_RESULT.FAIL_RANGE:
                            message = "Out of Range";
                            break;

                        case USE_RESULT.FAIL_LINE_OF_SIGHT:
                            message = "Line of Sight";
                            break;

                        case USE_RESULT.FAIL_NO_TARGET:
                            message = "No Target";
                            break;

                        case USE_RESULT.FAIL_IMMOBILIZED:
                            message = "Immobilized";
                            break;

                        case USE_RESULT.FAIL_SILENCED:
                            message = "Silenced";
                            break;

                        case USE_RESULT.FAIL_ANTIREQUISITE_STATUS:
                            message = "Antirequisite Status";
                            break;

                        case USE_RESULT.FAIL_DEAD:
                            message = "Dead";
                            break;

                        case USE_RESULT.FAIL_CHANNELLING:
                            message = "Channelling";
                            break;

                        case USE_RESULT.FAIL_SPECIFIC:
                            message = custom_use_fail_message;
                            break;

                        case USE_RESULT.SUCCESS:
                            Utilities.Commons.Error("FailMessage called for USE_RESULT.SUCCESS for [" + ID + "]: " + result);
                            return;

                        case USE_RESULT.FAIL_MISSING_RESOURCE:
                            Utilities.Commons.Error("Missing resource for [" + ID + "]: " + specific_resource);
                            return;

                        default:
                            Utilities.Commons.Error("Unsupported USE_RESULT for [" + ID + "]: " + result);
                            return;
                    }

                    CombatText.NewText(Main.LocalPlayer.getRect(), UI.Constants.COLOUR_MESSAGE_ABILITY_FAIL, message);

                }

            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Private Override Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// If false, PreActivate returns USE_RESULT.FAIL_SPECIFIC.
        /// Set custom_use_fail_message to show a non-default message.
        /// </summary>
        /// <returns></returns>
        protected virtual bool MeetsSpecificUseRequirements() { return true; }

        protected virtual float ModifyCostManaBase(float cost) { return cost; }
        protected virtual float ModifyCostManaFinal(float cost) { return cost; }
        protected virtual float ModifyCostResource(float cost) { return cost; }
        protected virtual float ModifyRange(float range) { return range;  }
        protected virtual float ModifyRadius(float radius) { return radius; }
        protected virtual float ModifyPowerBase(float power) { return power; }
        protected virtual float ModifyPowerFinal(float power) { return power; }
        protected virtual float ModifyCooldown(float cooldown_seconds) { return cooldown_seconds; }
        protected virtual bool ModifyCanBeTarget(Utilities.Containers.Thing target, bool can_be_targeted) { return can_be_targeted; }

        protected virtual void DoEffectMain() {}
        protected virtual void DoEffectTargetFriendly(Utilities.Containers.Thing target) { }
        protected virtual void DoEffectTargetHostile(Utilities.Containers.Thing target) { }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Warrior ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Block : Ability {
            public Block() : base(IDs.Block) {
                Specific_Name = "Block";
                Specific_Required_Class_ID = Systems.Class.IDs.Warrior;
                Specific_Required_Class_Level = 1;
                specific_is_channelling = true;
                
            }
            protected override void DoEffectTargetFriendly(Thing target) {
                Systems.Status.Block.CreateNew(target);
            }
        }

    }
}
