using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Attribute {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs (used in EACPlayer save/load)
        public enum IDs : byte {
            Power,
            Vitality,
            Mind,
            Spirit,
            Agility,
            Dexterity,

            //insert here

            NUMBER_OF_IDs, //leave this second last
            NONE, //leave this last
        }

        //set by modconfig
        public static float ATTRIBUTE_BONUS_MULTIPLIER = 1f;

        //this may be reordered, UI uses this order
        public static IDs[] ATTRIBUTES_UI_ORDER = new IDs[] { IDs.Power, IDs.Vitality, IDs.Mind, IDs.Spirit, IDs.Agility, IDs.Dexterity }; //TODO - unused

        //attribute bonus from active class
        public const float LEVELS_PER_ATTRIBUTE_POINT_PER_STAR = 10f;
        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY = 0.8f;
        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY = 0.6f;

        //allocation points
        public const double ALLOCATION_POINTS_PER_INCREASED_COST = 5d;
        public const float ALLOCATION_POINTS_PER_CHARACTER_LEVEL = 5f;
        public static readonly float[] ALLOCATION_POINTS_PER_LEVEL_TIERS = new float[] { 0f, 0.2f, 0.3f, 0.5f };

        //zero point calculation
        public const float PENALTY_RATIO = 0.7f;

        //reset
        public static ModItem RESET_COST_ITEM;
        public const int RESET_POINTS_FREE = 99;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Attribute[] LOOKUP { get; private set; }

        static Attribute() {
            LOOKUP = new Attribute[(ushort)IDs.NUMBER_OF_IDs];
            for (ushort i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Attribute>(Enum.GetName(typeof(IDs), i));
            }
        }

        public readonly static byte Count = (byte)IDs.NUMBER_OF_IDs;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;
        public byte ID_num { get; private set; } = (byte)IDs.NONE;
        private readonly string INTERNAL_NAME;
        public string Effect_Text { get; private set; } = "";

        /// <summary>
        /// set false to disable attribute effects
        /// </summary>
        public bool Active { get; private set; } = true;

        public string Name { get { return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_" + INTERNAL_NAME + "_Name"); } }
        public string Shortform { get { return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_" + INTERNAL_NAME + "_Shortform"); } }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Attribute(IDs id) {
            ID = id;
            ID_num = (byte)ID;
            INTERNAL_NAME = Enum.GetName(typeof(IDs), id);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected abstract void Effect(EACPlayer eacplayer, int points, bool do_effects = true);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ApplyEffect(EACPlayer eacplayer, int points, bool do_effects = true) {
            if (Active) {
                //clear effect text
                if (eacplayer.Fields.Is_Local) Effect_Text = "";
                //do specific effect
                Effect(eacplayer, points, do_effects);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Calculate the zero point for attributes based on allocations
        /// </summary>
        /// <param name="psheet"></param>
        /// <returns></returns>
        public static int CalculateZeroPoint(PSheet psheet) {
            if ((psheet.Classes.Primary.Class.Tier < 1) && (psheet.Attributes.Points_Spent == 0))
                return 0;
            else {
                int zero_point = 0;
                float amount = psheet.Attributes.Points_Total / 6.0f;
                while (AllocationPointCostTotal(zero_point+1) <= amount) {
                    zero_point++;
                }
                zero_point = (int)Math.Floor(zero_point * PENALTY_RATIO);

                return zero_point;
            }
        }

        /// <summary>
        /// allocation points needed to add 1 attribute point
        /// </summary>
        /// <param name="new_value"></param>
        /// <returns></returns>
        public static int AllocationPointCost(int current_value) {
            return (int)Math.Ceiling((current_value + 1) / ALLOCATION_POINTS_PER_INCREASED_COST);
        }

        /// <summary>
        /// total allocation points needed for 1-to-x attribute points
        /// </summary>
        /// <param name="current_value"></param>
        /// <returns></returns>
        public static int AllocationPointCostTotal(int current_value) {
            //must be updated if AllocationPointCost is changed
            int number_complete_5s = (int)Math.Floor((current_value - 1) / 5d);
            int number_partial = current_value - (number_complete_5s * 5);
            return ((5 + (number_complete_5s * 5)) * number_complete_5s / 2) + ((1 + number_complete_5s) * number_partial);
        }

        /// <summary>
        /// total allocation points available to player
        /// </summary>
        /// <param name="psheet"></param>
        /// <returns></returns>
        public static int LocalAllocationPointTotal(PSheet psheet) {
            float sum = 0;

            //points from character
            sum += psheet.Character.Level * ALLOCATION_POINTS_PER_CHARACTER_LEVEL;

            //get class levels
            int[] class_level_per_tier = psheet.Classes.GetTierTotalLevels(true);

            //points from classes
            for (byte i = 0; i < PlayerClass.MAX_TIER; i++) {
                sum += class_level_per_tier[i] * ALLOCATION_POINTS_PER_LEVEL_TIERS[i];
            }

            return (int)Math.Floor(sum);
        }

        public static void LocalTryReset() {
            //item cost
            int cost = LocalCalculateResetCost();
            int type = RESET_COST_ITEM.item.type;
            int held = Shortcuts.LOCAL_PLAYER.player.CountItem(type);

            EACPlayer eacplayer = Shortcuts.LOCAL_PLAYER;

            //do reset
            if (eacplayer.PSheet.Attributes.Points_Spent <= 0)
                Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_Reset_NoPoints"), UI.Constants.COLOUR_MESSAGE_ERROR);
            if (eacplayer.PSheet.Character.In_Combat)
                Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_Reset_InCombat"), UI.Constants.COLOUR_MESSAGE_ERROR);
            else if (held >= cost) {
                //consume
                for (int i = 0; i < cost; i++)
                    eacplayer.player.ConsumeItem(type);

                //reset
                eacplayer.PSheet.Attributes.Reset();

                //message
                Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_Reset_Success"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
            }
            else
                Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_Reset_Fail"), UI.Constants.COLOUR_MESSAGE_ERROR);
        }

        /// <summary>
        /// number of RESET_COST_ITEM for reset cost
        /// </summary>
        /// <returns></returns>
        public static int LocalCalculateResetCost() {
            int points = Shortcuts.LOCAL_PLAYER.PSheet.Attributes.Points_Spent - RESET_POINTS_FREE;
            if (points > 0)
                return (int)Math.Floor(Math.Pow(points, 0.35));
            else
                return 0;
        }

        public static int GetClassBonus(PSheet psheet, byte id) {
            float value_primary = psheet.Classes.Primary.Class.Attribute_Growth[id] * psheet.Classes.Primary.Level_Effective;
            PlayerClass p = psheet.Classes.Primary.Class.Prereq;
            while (p != null) {
                if (p.Gives_Allocation_Attributes) {
                    value_primary += p.Attribute_Growth[id] * p.Max_Level;
                }
                p = p.Prereq;
            }
            value_primary /= LEVELS_PER_ATTRIBUTE_POINT_PER_STAR;

            float value_secondary = psheet.Classes.Secondary.Class.Attribute_Growth[id] * psheet.Classes.Secondary.Level_Effective;
            p = psheet.Classes.Secondary.Class.Prereq;
            while (p != null) {
                if (p.Gives_Allocation_Attributes) {
                    value_secondary += p.Attribute_Growth[id] * p.Max_Level;
                }
                p = p.Prereq;
            }
            value_secondary /= LEVELS_PER_ATTRIBUTE_POINT_PER_STAR;

            if (psheet.Classes.Primary.Valid_Class && psheet.Classes.Secondary.Valid_Class) {
                return (int)Math.Floor((value_primary * SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY) + (value_secondary * SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY));
            }
            else if (psheet.Classes.Primary.Valid_Class) {
                return (int)Math.Floor(value_primary);
            }
            else {
                return 0;
            }
        }

        public static string BonusValueString(float value, string type, bool percent = false, float per_point = 0, string type_suffix = "", bool show_plus = true) {
            string str = "\n";

            if (value >= 0 && show_plus) {
                str += "+";
            }

            if (percent) {
                str += Math.Round(value * 100, 3) + "%";
            }
            else {
                str += Math.Round(value, 3);
            }

            str += " " + Language.GetTextValue("Mods.ExperienceAndClasses.Common." + type) + type_suffix;

            if (per_point > 0) {
                str += " (";

                if (percent) {
                    str += Math.Round(per_point * 100, 3) + "%";
                }
                else {
                    str += per_point;
                }

                str += " per point)";
            }

            return str;
        }

        private static int RoundIntBonus(float value) {
            if (value > 0) {
                return (int)Math.Floor(value);
            }
            else {
                return (int)Math.Ceiling(value);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Power : Attribute {
            public static float PER_POINT_DAMAGE { get { return 0.0025f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            public static float PER_POINT_MINING { get { return 0.005f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            public static float PER_POINT_FISH { get { return 0.1f * ATTRIBUTE_BONUS_MULTIPLIER; } }

            public Power() : base(IDs.Power) {}
            protected override void Effect(EACPlayer eacplayer, int points, bool do_effects = true) {
                //damage
                string str = eacplayer.PSheet.Attributes.Power_Scaling.ApplyPoints(eacplayer, points * PER_POINT_DAMAGE, PER_POINT_DAMAGE, do_effects);
                if (eacplayer.Fields.Is_Local) Effect_Text += str;
            }
        }

        public class Vitality : Attribute {
            private static float PER_POINT_LIFE { get { return 0.5f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_LIFE_REGEN { get { return 0.2f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_DEFENSE { get { return 0.1f * ATTRIBUTE_BONUS_MULTIPLIER; } }

            public Vitality() : base(IDs.Vitality) {}
            protected override void Effect(EACPlayer eacplayer, int points, bool do_effects = true) {
                int bonus;

                //life
                bonus = RoundIntBonus(PER_POINT_LIFE * points);
                if (do_effects) eacplayer.player.statLifeMax2 += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Defensive_Life", false, PER_POINT_LIFE);

                //life regen
                bonus = (int)Math.Max(0, PER_POINT_LIFE_REGEN * points);
                if (do_effects) eacplayer.player.lifeRegen += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Defensive_LifeRegen", false, PER_POINT_LIFE_REGEN);

                //defense
                bonus = RoundIntBonus(PER_POINT_DEFENSE * points);
                if (do_effects) eacplayer.player.statDefense += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Defensive_Defense", false, PER_POINT_DEFENSE);
            }
        }

        public class Mind : Attribute {
            private static float PER_POINT_MANA { get { return 0.5f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_MANA_REGEN { get { return 0.5f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_MANA_DELAY { get { return 0.005f * ATTRIBUTE_BONUS_MULTIPLIER; } }

            public Mind() : base(IDs.Mind) {}
            protected override void Effect(EACPlayer eacplayer, int points, bool do_effects = true) {
                int bonus;

                //mana
                bonus = RoundIntBonus(PER_POINT_MANA * points);
                if (do_effects) eacplayer.player.statManaMax2 += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Mana_Mana", false, PER_POINT_MANA);

                //mana regen
                bonus = (int)Math.Max(0, PER_POINT_MANA_REGEN * points);
                if (do_effects) eacplayer.player.manaRegenBonus += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Mana_ManaRegen", false, PER_POINT_MANA_REGEN);

                //mana delay
                float bonus_float = PER_POINT_MANA_DELAY * points;
                if (do_effects) eacplayer.PSheet.Stats.Mana_Regen_Delay_Reduction += bonus_float;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_float, "Stat_Mana_ManaRegenDelay", true, PER_POINT_MANA_DELAY);
            }
        }

        public class Spirit : Attribute {
            private static float PER_POINT_CRIT { get { return 0.0015f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_MINION_CAP { get { return 0.04f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_HOLY_HEAL { get { return 0.01f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            
            public Spirit() : base(IDs.Spirit) {}
            protected override void Effect(EACPlayer eacplayer, int points, bool do_effects = true) {
                int bonus;
                float bonus_float;

                //crit
                bonus_float = PER_POINT_CRIT * points;
                if (do_effects) eacplayer.PSheet.Stats.Crit_All += bonus_float;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_float, "Stat_Crit_All", true, PER_POINT_CRIT);

                //minion cap
                bonus = RoundIntBonus(PER_POINT_MINION_CAP * points);
                if (do_effects) eacplayer.player.maxMinions += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Misc_MinionCap", false, PER_POINT_MINION_CAP);

                //healing
                bonus_float = PER_POINT_HOLY_HEAL * points;
                if (do_effects) eacplayer.PSheet.Stats.Healing_Mult += bonus_float;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_float, "Stat_Abilities_Healing", true, PER_POINT_HOLY_HEAL);
            }
        }

        public class Agility : Attribute {
            private static float PER_POINT_MOVEMENT { get { return 0.005f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_JUMP { get { return 0.01f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_DODGE { get { return 0.0025f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_FLY { get { return 0.5f * ATTRIBUTE_BONUS_MULTIPLIER; } }

            public Agility() : base(IDs.Agility) {}
            protected override void Effect(EACPlayer eacplayer, int points, bool do_effects = true) {
                float bonus_float;
                int bonus_int;

                //run
                bonus_float = (float)Utilities.Commons.Clamp(PER_POINT_MOVEMENT * points, -0.4f, 0.75f);
                if (do_effects) eacplayer.player.maxRunSpeed *= (1f + bonus_float);
                if (do_effects) eacplayer.player.runAcceleration *= (1f + bonus_float);
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_float, "Stat_Mobility_Movement", true, PER_POINT_MOVEMENT);

                //jump
                bonus_float = (float)Utilities.Commons.Clamp(PER_POINT_JUMP * points, -0.8f, 1.5f);
                if (do_effects) eacplayer.player.jumpSpeedBoost += (bonus_float * 5);
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_float, "Stat_Mobility_Jump", true, PER_POINT_JUMP);
                
                //dodge
                bonus_float = PER_POINT_DODGE * points;
                if (do_effects) eacplayer.PSheet.Stats.Dodge += bonus_float;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_float, "Stat_Defensive_Dodge", true, PER_POINT_DODGE);
                
                //max fly time
                bonus_int = RoundIntBonus(PER_POINT_FLY * points);
                if (do_effects) eacplayer.player.wingTimeMax += bonus_int;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus_int, "Stat_Mobility_WingTime", false, PER_POINT_FLY);
            }
        }

        public class Dexterity : Attribute {
            private static float PER_POINT_USE_SPEED { get { return 0.0025f * ATTRIBUTE_BONUS_MULTIPLIER; } }
            private static float PER_POINT_ABILITY_DELAY { get { return 0.01f * ATTRIBUTE_BONUS_MULTIPLIER; } }

            public Dexterity() : base(IDs.Dexterity) {}
            protected override void Effect(EACPlayer eacplayer, int points, bool do_effects = true) {
                float bonus;

                //ability after use delay
                bonus = PER_POINT_ABILITY_DELAY * points;
                if (do_effects) eacplayer.PSheet.Stats.Ability_Delay_Reduction += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_Abilities_Cooldown", true, PER_POINT_ABILITY_DELAY);

                //weapon use time
                bonus = PER_POINT_USE_SPEED * points;
                if (do_effects) eacplayer.PSheet.Stats.Item_Speed_Weapon += bonus;
                if (eacplayer.Fields.Is_Local) Effect_Text += BonusValueString(bonus, "Stat_ItemSpeed_Weapon", true, PER_POINT_USE_SPEED);
            }
        }
    }
}