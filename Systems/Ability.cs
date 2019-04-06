using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

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
            FAIL_ON_COOLDOWN,
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

        protected TARGET_POSITION_TYPE specific_target_position_type = TARGET_POSITION_TYPE.NONE;

        protected float specific_mana_cost_flat = 0;
        protected float specific_mana_cost_percent = 0f;
        protected bool specific_mana_apply_reduction = true;

        protected Systems.Resource Specific_Resource = null;
        protected float specific_resource_cost_flat = 0;
        protected float specific_resource_cost_percent = 0f;

        protected float specific_cooldown_seconds = 0f;
        protected bool specific_cooldown_apply_reduction = true;

        protected List<Systems.Status.IDs> specific_antirequisite_statuses = new List<Status.IDs>();

        protected bool specific_can_be_used_while_channelling = false;

        public Systems.Class.IDs Specific_Required_Class_ID { get; protected set; } = Systems.Class.IDs.None;
        public byte Specific_Required_Class_Level { get; protected set; } = 0;

        protected float specific_range_base = 0;
        protected float specific_range_level_multiplier = 0;

        protected float specific_radius_base = 0;
        protected float specific_radius_level_multiplier = 0;

        protected float specific_power_base = 0f;
        protected bool specific_power_base_add_weapon_damage_if_type = false; //if weapon matches type, add its per-hit damage to base power
        protected bool specific_power_base_add_weapon_dpt_if_type = false; //if weapon matches type, add its damage-per-use-time to base power
        protected bool specific_power_apply_bonus_highest = false; //apply highest of type bonuses
        protected bool specific_power_apply_bonus_all = false; //apply all bonuses
        protected float[] specific_power_attribute_multipliers = new float[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
        protected float specific_power_level_multiplier = 0; //final multiplier (compounds)

        protected bool specific_type_melee = false;
        protected bool specific_type_ranged = false;
        protected bool specific_type_throwing = false;
        protected bool specific_type_minion = false;
        protected bool specific_type_magic = false;
        protected bool specific_type_holy = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic (between activations) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;

        public ushort ID_num { get; private set; } = (ushort)IDs.NONE;

        private DateTime Time_Cooldown_End = DateTime.MinValue;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic (within activation) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        protected byte level;
        private float range, radius, power;
        private ushort cost_mana, cost_resource;
        private float cooldown_seconds;
        private Vector2 position_player, position_cursor, position_target;
        private float position_target_distance;
        private bool target_position_valid;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Core Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Ability(IDs id) {
            ID = id;
            ID_num = (ushort)id;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void Activate() {
            //check if activation is allowed, else show message
            USE_RESULT result = TryUse();
            if (result != USE_RESULT.SUCCESS) {
                FailMessage(result);
                return;
            }



        }

        /// <summary>
        /// Checks if ability can be used and returns true if USE_RESULT.SUCCESS
        /// </summary>
        public bool CanUse {
            get {
                return TryUse() == USE_RESULT.SUCCESS;
            }
        }

        /// <summary>
        /// Returns true even if reduced to zero so long as there was a value to begin with 
        /// </summary>
        public bool HasManaCost {
            get {
                return (specific_mana_cost_flat > 0 || specific_mana_cost_percent > 0);
            }
        }

        /// <summary>
        /// Returns true even if reduced to zero so long as there was a value to begin with 
        /// </summary>
        public bool HasResourceCost {
            get {
                return (Specific_Resource != null) && (specific_resource_cost_flat > 0 || specific_resource_cost_percent > 0);
            }
        }

        /// <summary>
        /// Returns true even if reduced to zero so long as there was a value to begin with 
        /// </summary>
        public bool HasCooldown {
            get {
                return (specific_cooldown_seconds > 0);
            }
        }

        /// <summary>
        /// Calculates and returns mana cost. For use outside of Ability.
        /// </summary>
        public ushort CostMana {
            get {
                return CalculateManaCost();
            }
        }

        /// <summary>
        /// Calculates and returns resource cost. For use outside of Ability.
        /// </summary>
        public ushort CostResource {
            get {
                return CalculateResourceCost();
            }
        }

        /// <summary>
        /// Calculates and returns cooldown in seconds. For use outside of Ability.
        /// </summary>
        public float CooldownSeconds {
            get {
                return CalculateCooldown();
            }
        }

        public byte Level {
            get {
                return CalculateLevel();
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Generic Calculations ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        // These calculate and return a value + set the variable for use elsewhere
        // Called by public lookups and during TryUse() - i.e., automatically called during activation

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
            if (Specific_Resource == null) {
                cost_resource = 0;
            }
            else {
                float cost_resource_base = ModifyCostResource(specific_resource_cost_flat + (specific_resource_cost_percent * Specific_Resource.Capacity));
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
            return CalculateCooldown();
        }

        private void CalculatePosition() {
            position_player = Main.LocalPlayer.position;
            position_cursor = Main.MouseWorld;

            switch (specific_target_position_type) {
                case TARGET_POSITION_TYPE.NONE:
                    position_target_distance = 0;
                    target_position_valid = true;
                    break;
                case TARGET_POSITION_TYPE.SELF:
                    position_target = position_player;
                    position_target_distance = 0;
                    target_position_valid = true;
                    break;
                case TARGET_POSITION_TYPE.CURSOR:
                    position_target = position_cursor;
                    position_target_distance = Vector2.Distance(position_player, position_target);
                    target_position_valid = (position_target_distance <= range) && Collision.CanHitLine(position_player, 0, 0, position_target, 0, 0);
                    break;
                case TARGET_POSITION_TYPE.BETWEEN_SELF_AND_CURSOR:
                    //calculate position
                    //TODO

                    position_target_distance = Vector2.Distance(position_player, position_target);
                    target_position_valid = (position_target_distance <= range) && Collision.CanHitLine(position_player, 0, 0, position_target, 0, 0);
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
            power = ModifyPowerBase(specific_power_base);

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
            if (specific_power_level_multiplier > 0) {
                power *= (specific_power_level_multiplier * level);
            }

            //final modification
            power = ModifyPowerFinal(power);

            return power;
        }

        private byte CalculateLevel() {
            //calculate for primary
            Systems.Class c = Systems.Class.LOOKUP[(byte)Specific_Required_Class_ID];
            
            


            //TODO

            return level;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Private Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            private USE_RESULT TryUse() {
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

            //cost resource (calculates for use later in activation)
            if ((Specific_Resource != null) && (Specific_Resource.Amount < CalculateResourceCost())) {
                return USE_RESULT.FAIL_NOT_ENOUGH_RESOURCE;
            }

            //cooldown (calculates for use later in activation)
            CalculateCooldown();
            if (HasCooldown && (Time_Cooldown_End.CompareTo(ExperienceAndClasses.Now) > 0)) {
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
            if (!target_position_valid) {
                return USE_RESULT.FAIL_LINE_OF_SIGHT;
            }

            //targets (calculates for use later in activation)
            CalculateRadius();
            //TODO get targets
            //TODO check min # targets
            //TODO FAIL_NO_TARGET

            //ability-specific fail
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
                            message = "Not Enough " + Specific_Resource.Name;
                            break;

                        case USE_RESULT.FAIL_ON_COOLDOWN:
                            message = "Ability on Coolown";
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
                            message = "Requirements Not Met";
                            break;

                        case USE_RESULT.SUCCESS:
                            Utilities.Commons.Error("FailMessage called for USE_RESULT.SUCCESS for [" + ID + "]: " + result);
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
        /// If false, TryUse returns USE_RESULT.FAIL_SPECIFIC
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Warrior ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Block : Ability {
            public Block() : base(IDs.Block) {

            }
        }

    }
}
