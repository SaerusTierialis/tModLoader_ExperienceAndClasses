using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Attribute {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs (used in MPlayer save/load)
        public enum IDs : byte {
            Power,
            Vitality,
            Mind,
            Spirit,
            Agility,
            Dexterity,

            //insert here

            NUMBER_OF_IDs, //leave this last
            NONE,
        }

        //this may be reordered, UI uses this order
        public static IDs[] ATTRIBUTES_UI_ORDER = new IDs[] { IDs.Power, IDs.Vitality, IDs.Mind, IDs.Spirit, IDs.Agility, IDs.Dexterity };

        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY = 0.8f;
        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY = 0.6f;

        //class attribute growth
        public const byte ATTRIBUTE_GROWTH_LEVELS = 10;

        //allocation points
        public static readonly int[] ALLOCATION_POINTS_PER_LEVEL_TIERS = new int[] { 0, 1, 2, 3 };

        //reset
        public static readonly ModItem RESET_COST_ITEM = ExperienceAndClasses.MOD.GetItem<Items.Orb_Monster>();
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (specific) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Specifc_Name { get; private set; } = "default_name";
        public string Specific_Name_Short { get; private set; } = "default_name_short";
        public string Specific_Description { get; private set; } = "default_description";

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (generic) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;
        public byte ID_num { get; private set; } = (byte)IDs.NONE;
        public string Bonus { get; private set; } = "";

        /// <summary>
        /// set false to disable attribute effects
        /// </summary>
        public bool Active { get; private set; } = true;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Attribute(IDs id) {
            ID = id;
            ID_num = (byte)ID;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected abstract void Effect(MPlayer mplayer, int points);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ApplyEffect(MPlayer mplayer, int points) {
            if (Active) {
                //for local ui, display milestone bonus
                if (mplayer.Is_Local_Player) {
                    Bonus = "\nAllocation Milestone Bonus: " + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated_Milestone[ID_num] + "\n";
                }

                //do specific effect
                Effect(mplayer, points);
            }
        }

        /// <summary>
        /// allocation points needed to add 1 attribute point
        /// </summary>
        /// <param name="new_value"></param>
        /// <returns></returns>
        public static int AllocationPointCost(int current_value) {
            return (int)Math.Ceiling((current_value + 1) / 5d);
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
        /// total allocation points earned by player
        /// </summary>
        /// <param name="mplayer"></param>
        /// <returns></returns>
        public static int LocalAllocationPointTotal() {
            int sum = 0;

            for (byte i = 0; i < ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels.Length; i++) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[i] && Class.LOOKUP[i].Gives_Allocation_Attributes && Class.LOOKUP[i].Tier > 0 && Class.LOOKUP[i].Enabled) {
                    sum += Math.Min(ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[i], Class.LOOKUP[i].Max_Level) * ALLOCATION_POINTS_PER_LEVEL_TIERS[Class.LOOKUP[i].Tier];
                }
            }

            return sum;
        }

        /// <summary>
        /// total allocation points spent by player
        /// </summary>
        /// <param name="mplayer"></param>
        /// <returns></returns>
        public static int LocalAllocationPointSpent() {
            int sum = 0;

            for (byte i = 0; i < ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated.Length; i++) {
                if (LOOKUP[i].Active) {
                    sum += AllocationPointCostTotal(ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[i]);
                }
            }

            return sum;
        }

        public static int LocalCalculateResetCost() {
            int points = ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Spent - RESET_POINTS_FREE;
            if (points > 0)
                return (int)Math.Floor(Math.Pow(points, 0.35));
            else
                return 0;
        }


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Power : Attribute {
            public const float PER_POINT_DAMAGE = 0.005f;
            public const float PER_POINT_FISH = 0.1f;

            public Power() : base(IDs.Power) {
                Specifc_Name = "Power";
                Specific_Name_Short = "PWR";
                Specific_Description = "TODO";
            }
            protected override void Effect(MPlayer mplayer, int points) {
                Bonus += PowerScaling.ApplyPower(mplayer, points);
            }
        }

        public class Vitality : Attribute {
            private const float PER_POINT_LIFE = 0.5f;
            private const float PER_POINT_LIFE_REGEN = 0.2f;
            private const float PER_POINT_DEFENSE = 0.1f;

            public Vitality() : base(IDs.Vitality) {
                Specifc_Name = "Vitality";
                Specific_Name_Short = "VIT";
                Specific_Description = "TODO";
            }
            protected override void Effect(MPlayer mplayer, int points) {
                int bonus;

                //life
                bonus = (int)Math.Floor(PER_POINT_LIFE * points);
                mplayer.player.statLifeMax2 += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + " maximum life (" + PER_POINT_LIFE + " per point)";

                //life regen
                bonus = (int)Math.Floor(PER_POINT_LIFE_REGEN * points);
                mplayer.player.lifeRegen += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + " life regeneration (" + PER_POINT_LIFE_REGEN + " per point)";

                //defense
                bonus = (int)Math.Floor(PER_POINT_DEFENSE * points);
                mplayer.player.statDefense += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + " defense (" + PER_POINT_DEFENSE + " per point)";
            }
        }

        public class Mind : Attribute {
            private const float PER_POINT_MANA = 1f;
            private const float PER_POINT_MANA_REGEN = 0.5f;
            private const float PER_POINT_MANA_DELAY = 0.5f;

            public Mind() : base(IDs.Mind) {
                Specifc_Name = "Mind";
                Specific_Name_Short = "MND";
                Specific_Description = "TODO";
            }
            protected override void Effect(MPlayer mplayer, int points) {
                int bonus;

                //mana
                bonus = (int)Math.Floor(PER_POINT_MANA * points);
                mplayer.player.statManaMax2 += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + " maximum mana (" + PER_POINT_MANA + " per point)";

                //mana regen
                bonus = (int)Math.Floor(PER_POINT_MANA_REGEN * points);
                mplayer.player.manaRegenBonus += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + " mana regeneration (" + PER_POINT_MANA_REGEN + " per point)";

                //mana delay
                bonus = (int)Math.Floor(PER_POINT_MANA_DELAY * points);
                if (mplayer.player.manaRegenDelay > 50) {
                    int new_delay = (int)Math.Max(Math.Round(mplayer.player.manaRegenDelay * (100f / (100f + bonus))), 50);
                    mplayer.player.manaRegenDelayBonus += mplayer.player.manaRegenDelay - new_delay;
                }
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + "% reduced mana delay (" + PER_POINT_MANA_DELAY + " per point)";
            }
        }

        public class Spirit : Attribute {
            private const float PER_POINT_CRIT = 0.125f;
            private const float PER_POINT_MINION_CAP = 0.025f;
            private const float PER_POINT_HOLY_HEAL = 0.01f;

            public Spirit() : base(IDs.Spirit) {
                Specifc_Name = "Spirit";
                Specific_Name_Short = "SPT";
                Specific_Description = "TODO";
            }
            protected override void Effect(MPlayer mplayer, int points) {
                int bonus;

                //crit
                bonus = (int)Math.Floor(PER_POINT_CRIT * points);
                mplayer.player.meleeCrit += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + "% melee critical chance (" + PER_POINT_CRIT + " per point)";
                mplayer.player.rangedCrit += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + "% ranged critical chance (" + PER_POINT_CRIT + " per point)";
                mplayer.player.magicCrit += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + "% magic critical chance (" + PER_POINT_CRIT + " per point)";
                mplayer.player.thrownCrit += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + "% throwing critical chance (" + PER_POINT_CRIT + " per point)";

                //minion cap
                bonus = (int)Math.Floor(PER_POINT_MINION_CAP * points);
                mplayer.player.maxMinions += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus + " maximum minions (" + PER_POINT_MINION_CAP + " per point)";

                //healing (use holy damage scaling)
                float holy_damage_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Holy_Damage, mplayer.Class_Secondary.Power_Scaling.Holy_Damage / 2);
                if (holy_damage_per > 0f) {
                    float bonus_per_point = holy_damage_per * PER_POINT_HOLY_HEAL;
                    float bonus_float = bonus_per_point * points;
                    mplayer.holy_healing += bonus_float;
                    if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% holy healing (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }
        }

        public class Agility : Attribute {
            private const float PER_POINT_MOVEMENT = 0.005f;
            private const float PER_POINT_JUMP = 0.01f;
            private const float PER_POINT_DODGE = 0.0025f;
            private const float PER_POINT_FLY = 0.5f;

            public Agility() : base(IDs.Agility) {
                Specifc_Name = "Agility";
                Specific_Name_Short = "AGI";
                Specific_Description = "TODO";
            }
            protected override void Effect(MPlayer mplayer, int points) {
                float bonus_float;
                int bonus_int;

                //run
                bonus_float = PER_POINT_MOVEMENT * points;
                mplayer.player.maxRunSpeed *= (1f + bonus_float);
                mplayer.player.runAcceleration *= (1f + bonus_float);
                mplayer.player.runSlowdown *= (1f / (1f + bonus_float));
                if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% movement speed (" + Math.Round(PER_POINT_MOVEMENT * 100, 3) + " per point)";

                //jump
                bonus_float = PER_POINT_JUMP * points;
                mplayer.player.jumpSpeedBoost += (bonus_float * 5);
                if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% increased jump (" + Math.Round(PER_POINT_JUMP * 100, 3) + " per point)";

                //dodge
                bonus_float = PER_POINT_DODGE * points;
                mplayer.dodge_chance += bonus_float;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% dodge chance (" + Math.Round(PER_POINT_DODGE * 100, 3) + " per point)";

                //max fly time
                bonus_int = (int)Math.Floor(PER_POINT_FLY * points);
                mplayer.player.wingTimeMax += bonus_int;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + bonus_int + " wing time (" + PER_POINT_FLY + " per point)";
            }
        }

        public class Dexterity : Attribute {
            private const float PER_POINT_USE_SPEED = 0.0025f;
            private const float PER_POINT_ABILITY_DELAY = 0.01f;

            public Dexterity() : base(IDs.Dexterity) {
                Specifc_Name = "Dexterity";
                Specific_Name_Short = "DEX";
                Specific_Description = "TODO";
            }
            protected override void Effect(MPlayer mplayer, int points) {
                float bonus;

                //ability after use delay
                bonus = PER_POINT_ABILITY_DELAY * points;
                mplayer.ability_delay_reduction += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus * 100, 3) + "% reduced ability delay (" + Math.Round(PER_POINT_ABILITY_DELAY * 100, 3) + " per point)";

                //tool use time (if non-combat)
                float fish_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Fish_Power, mplayer.Class_Secondary.Power_Scaling.Fish_Power / 2);
                if (fish_per > 0f) {
                    float bonus_per_point = fish_per * PER_POINT_USE_SPEED;
                    bonus = bonus_per_point * points;
                    mplayer.use_speed_tool += bonus;
                    if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus * 100, 3) + "% tool use speed (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }

                //weapon use time
                bonus = PER_POINT_USE_SPEED * points;
                mplayer.use_speed_weapon += bonus;
                if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bonus * 100, 3) + "% weapon use speed (" + Math.Round(PER_POINT_USE_SPEED * 100, 3) + " per point)";
            }
        }
    }

    public abstract class PowerScaling {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public enum IDs : byte {
            None,
            CloseRangeMelee,
            CloseRangeAll,
            Projectile,
            AllCore,
            Holy_AllCore,
            MinionOnly,
            NonCombat,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        protected const float SCALE_PRIMARY = 1f;
        protected const float SCALE_SECONDARY = 0.7f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static PowerScaling[] LOOKUP { get; private set; }

        static PowerScaling() {
            LOOKUP = new PowerScaling[(ushort)IDs.NUMBER_OF_IDs];
            for (ushort i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<PowerScaling>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (specific) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Primary_Types { get; private set; } = "";
        public string Secondary_Types { get; private set; } = "";

        //core types
        protected float Melee { get; private set; } = 0f;
        protected float Ranged { get; private set; } = 0f;
        protected float Magic { get; private set; } = 0f;
        protected float Throwing { get; private set; } = 0f;
        protected float Minion { get; private set; } = 0f;

        //custom types
        protected float Melee_Close_Range { get; private set; } = 0f;
        protected float NonMelee_Close_Range { get; private set; } = 0f;
        protected float Melee_Projectile { get; private set; } = 0f;
        public float Holy_Damage { get; private set; } = 0f;

        //non-combat
        public float Fish_Power { get; private set; } = 0f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (generic) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.None;
        public byte ID_num { get; private set; } = (byte)IDs.None;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public PowerScaling(IDs id) {
            ID = id;
            ID_num = (byte)ID;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static string ApplyPower(MPlayer mplayer, int points) {
            string bonus = "";

            //calculate scaling values to use...

            //core
            float melee_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Melee, mplayer.Class_Secondary.Power_Scaling.Melee / 2);
            float ranged_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Ranged, mplayer.Class_Secondary.Power_Scaling.Ranged / 2);
            float magic_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Magic, mplayer.Class_Secondary.Power_Scaling.Magic / 2);
            float throwing_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Throwing, mplayer.Class_Secondary.Power_Scaling.Throwing / 2);
            float minion_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Minion, mplayer.Class_Secondary.Power_Scaling.Minion / 2);

            //custom types
            float melee_close_range_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Melee_Close_Range, mplayer.Class_Secondary.Power_Scaling.Melee_Close_Range / 2);
            float nonmelee_close_range_per = Math.Max(mplayer.Class_Primary.Power_Scaling.NonMelee_Close_Range, mplayer.Class_Secondary.Power_Scaling.NonMelee_Close_Range / 2);
            float melee_projectile_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Melee_Projectile, mplayer.Class_Secondary.Power_Scaling.Melee_Projectile / 2);
            float holy_damage_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Holy_Damage, mplayer.Class_Secondary.Power_Scaling.Holy_Damage / 2);

            //non-combat
            float fish_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Fish_Power, mplayer.Class_Secondary.Power_Scaling.Fish_Power / 2);

            //apply bonuses...
            float bonus_per_point;
            float bonus_total;

            //all melee
            if (melee_per > 0f) {
                bonus_per_point = melee_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.player.meleeDamage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all melee damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //projectile melee
            if (melee_projectile_per > 0f) {
                bonus_per_point = melee_projectile_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.melee_projectile_damage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% melee projectile damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //close-range melee
            if (melee_close_range_per > 0f) {
                bonus_per_point = melee_close_range_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.close_range_melee_damage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% close-range melee damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //close_range non-melee
            if (nonmelee_close_range_per > 0f) {
                bonus_per_point = nonmelee_close_range_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.close_range_nonmelee_damage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% close-range non-melee damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //all ranged
            if (ranged_per > 0f) {
                bonus_per_point = ranged_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.player.rangedDamage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all ranged damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //all magic
            if (magic_per > 0f) {
                bonus_per_point = magic_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.player.magicDamage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all magic damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //all throwing
            if (throwing_per > 0f) {
                bonus_per_point = throwing_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.player.thrownDamage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all throwing damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //all minion
            if (minion_per > 0f) {
                bonus_per_point = minion_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.player.minionDamage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all minion damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //holy
            if (holy_damage_per > 0f) {
                bonus_per_point = holy_damage_per * Attribute.Power.PER_POINT_DAMAGE;
                bonus_total = bonus_per_point * points;
                mplayer.holy_damage += bonus_total;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all holy damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }
            }

            //fish
            if (fish_per > 0f) {
                bonus_per_point = fish_per * Attribute.Power.PER_POINT_FISH;
                int bonus_total_int = (int)Math.Floor(bonus_per_point * points);
                mplayer.player.fishingSkill += bonus_total_int;
                if (mplayer.Is_Local_Player) {
                    bonus += "\n+" + bonus_total_int + " fishing power (" + Math.Round(bonus_per_point, 3) + " per point)";
                }
            }

            return bonus;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Power Scaling ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class None : PowerScaling {
            public None() : base(IDs.None) {
                //leave defaults
            }
        }

        public class CloseRangeMelee : PowerScaling {
            public CloseRangeMelee() : base(IDs.CloseRangeMelee) {
                Primary_Types = "Melee (bonus for close-range)";
                Melee = SCALE_PRIMARY;
                Melee_Close_Range = 0.2f;
                Secondary_Types = "Close-Range Non-Melee";
                NonMelee_Close_Range = SCALE_SECONDARY;
            }
        }

        public class CloseRangeAll : PowerScaling {
            public CloseRangeAll() : base(IDs.CloseRangeAll) {
                Primary_Types = "All Close-Range";
                Melee_Close_Range = 0.5f;
                NonMelee_Close_Range = 0.5f;
                Secondary_Types = "Melee, Ranged, Magic, Throwing, Minion";
                Melee = SCALE_SECONDARY;
                Ranged = SCALE_SECONDARY;
                Magic = SCALE_SECONDARY;
                Throwing = SCALE_SECONDARY;
                Minion = SCALE_SECONDARY;
            }
        }

        public class Projectile : PowerScaling {
            public Projectile() : base(IDs.Projectile) {
                Primary_Types = "Ranged, Magic, Throwing, Projectile Melee";
                Ranged = SCALE_PRIMARY;
                Magic = SCALE_PRIMARY;
                Throwing = SCALE_PRIMARY;
                Melee_Projectile = SCALE_PRIMARY;
            }
        }

        public class AllCore : PowerScaling {
            public AllCore() : base(IDs.AllCore) {
                Primary_Types = "Melee, Ranged, Magic, Throwing, Minion";
                Melee = SCALE_PRIMARY;
                Ranged = SCALE_PRIMARY;
                Magic = SCALE_PRIMARY;
                Throwing = SCALE_PRIMARY;
                Minion = SCALE_PRIMARY;
            }
        }

        public class Holy_AllCore : PowerScaling {
            public Holy_AllCore() : base(IDs.Holy_AllCore) {
                Primary_Types = "Holy";
                Holy_Damage = SCALE_PRIMARY;

                Secondary_Types = "Melee, Ranged, Magic, Throwing, Minion";
                Melee = SCALE_SECONDARY;
                Ranged = SCALE_SECONDARY;
                Magic = SCALE_SECONDARY;
                Throwing = SCALE_SECONDARY;
                Minion = SCALE_SECONDARY;
            }
        }

        public class MinionOnly : PowerScaling {
            public MinionOnly() : base(IDs.MinionOnly) {
                Primary_Types = "Minion";
                Minion = SCALE_PRIMARY;
            }
        }

        public class NonCombat : PowerScaling {
            public NonCombat() : base(IDs.NonCombat) {
                Primary_Types = "Fishing Power";
                Fish_Power = SCALE_PRIMARY;
            }
        }

    }
}
