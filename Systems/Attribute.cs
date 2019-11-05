using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //this may be reordered, UI uses this order
        public static IDs[] ATTRIBUTES_UI_ORDER = new IDs[] { IDs.Power, IDs.Vitality, IDs.Mind, IDs.Spirit, IDs.Agility, IDs.Dexterity }; //TODO - unused

        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY = 0.7f;
        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY = 0.3f;

        //allocation points
        public const double ALLOCATION_POINTS_PER_INCREASED_COST = 5d;
        public const float ALLOCATION_POINTS_PER_CHARACTER_LEVEL = 5f;
        public static readonly float[] ALLOCATION_POINTS_PER_LEVEL_TIERS = new float[] { 0f, 0.2f, 0.3f, 0.5f };

        //reset
        public static readonly ModItem RESET_COST_ITEM = ModContent.GetInstance<Items.Orb_Monster>();
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
        protected abstract void Effect(EACPlayer eacplayer, int points);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ApplyEffect(EACPlayer eacplayer, int points) {
            if (Active) {
                //do specific effect
                Effect(eacplayer, points);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Calculate the zero point for attributes based on average allocated
        /// </summary>
        /// <param name="average"></param>
        /// <returns></returns>
        public static int CalculateZeroPoint(float average) {
            return (int)Math.Floor(average * 0.7);
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

        /// <summary>
        /// number of RESET_COST_ITEM for reset cost
        /// </summary>
        /// <returns></returns>
        public static int LocalCalculateResetCost() {
            int points = Shortcuts.LOCAL_PLAYER.PSheet.Attributes.Points_Spent - RESET_POINTS_FREE;
            if (points > 0)
                return (int)Math.Floor(Math.Pow(points, 0.5));
            else
                return 0;
        }

        public static int GetClassBonus(PSheet psheet, byte id) {
            float value_primary = psheet.Classes.Primary.Class.Attribute_Growth[id] * psheet.Classes.Primary.Level_Effective;
            float value_secondary = psheet.Classes.Secondary.Class.Attribute_Growth[id] * psheet.Classes.Secondary.Level_Effective;

            if (psheet.Classes.Primary.Valid_Class && psheet.Classes.Secondary.Valid_Class) {
                return (int)Math.Floor((value_primary * SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY) + (value_secondary * SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY));
            }
            else if (psheet.Classes.Primary.Valid_Class){
                return (int)Math.Floor(value_primary);
            }
            else {
                return 0;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Power : Attribute {
            public const float PER_POINT_DAMAGE = 0.0025f;
            public const float PER_POINT_FISH = 0.1f;

            public Power() : base(IDs.Power) {
                Specifc_Name = "Power";
                Specific_Name_Short = "PWR";
                Specific_Description = "TODO";
            }
            protected override void Effect(EACPlayer eacplayer, int points) {
                Bonus += PowerScaling.ApplyPower(eacplayer, points);
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
            protected override void Effect(EACPlayer eacplayer, int points) {
                int bonus;

                //life
                bonus = (int)Math.Floor(PER_POINT_LIFE * points);
                eacplayer.player.statLifeMax2 += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + " maximum life (" + PER_POINT_LIFE + " per point)";

                //life regen
                bonus = (int)Math.Floor(PER_POINT_LIFE_REGEN * points);
                eacplayer.player.lifeRegen += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + " life regeneration (" + PER_POINT_LIFE_REGEN + " per point)";

                //defense
                bonus = (int)Math.Floor(PER_POINT_DEFENSE * points);
                eacplayer.player.statDefense += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + " defense (" + PER_POINT_DEFENSE + " per point)";
            }
        }

        public class Mind : Attribute {
            private const float PER_POINT_MANA = 0.5f;
            private const float PER_POINT_MANA_REGEN = 0.5f;
            private const float PER_POINT_MANA_DELAY = 0.5f;

            public Mind() : base(IDs.Mind) {
                Specifc_Name = "Mind";
                Specific_Name_Short = "MND";
                Specific_Description = "TODO";
            }
            protected override void Effect(EACPlayer eacplayer, int points) {
                int bonus;

                //mana
                bonus = (int)Math.Floor(PER_POINT_MANA * points);
                eacplayer.player.statManaMax2 += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + " maximum mana (" + PER_POINT_MANA + " per point)";

                //mana regen
                bonus = (int)Math.Floor(PER_POINT_MANA_REGEN * points);
                eacplayer.player.manaRegenBonus += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + " mana regeneration (" + PER_POINT_MANA_REGEN + " per point)";

                //mana delay
                bonus = (int)Math.Floor(PER_POINT_MANA_DELAY * points);
                if (eacplayer.player.manaRegenDelay > 50) {
                    int new_delay = (int)Math.Max(Math.Round(eacplayer.player.manaRegenDelay * (100f / (100f + bonus))), 50);
                    eacplayer.player.manaRegenDelayBonus += eacplayer.player.manaRegenDelay - new_delay;
                }
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + "% reduced mana delay (" + PER_POINT_MANA_DELAY + " per point)";
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
            protected override void Effect(EACPlayer eacplayer, int points) {
                int bonus;

                //crit
                bonus = (int)Math.Floor(PER_POINT_CRIT * points);
                eacplayer.player.meleeCrit += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + "% melee critical chance (" + PER_POINT_CRIT + " per point)";
                eacplayer.player.rangedCrit += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + "% ranged critical chance (" + PER_POINT_CRIT + " per point)";
                eacplayer.player.magicCrit += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + "% magic critical chance (" + PER_POINT_CRIT + " per point)";
                eacplayer.player.thrownCrit += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + "% throwing critical chance (" + PER_POINT_CRIT + " per point)";

                //minion cap
                bonus = (int)Math.Floor(PER_POINT_MINION_CAP * points);
                eacplayer.player.maxMinions += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus + " maximum minions (" + PER_POINT_MINION_CAP + " per point)";

                //healing (use holy damage scaling)
                float holy_damage_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Damage_Holy, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Damage_Holy / 2);
                float bonus_per_point = holy_damage_per * PER_POINT_HOLY_HEAL;
                float bonus_float = bonus_per_point * points;
                eacplayer.PSheet.Stats.Healing_Mult += bonus_float;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% healing (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
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
            protected override void Effect(EACPlayer eacplayer, int points) {
                float bonus_float;
                int bonus_int;

                //run
                bonus_float = PER_POINT_MOVEMENT * points;
                eacplayer.player.maxRunSpeed *= (1f + bonus_float);
                eacplayer.player.runAcceleration *= (1f + bonus_float);
                eacplayer.player.runSlowdown *= (1f / (1f + bonus_float));
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% movement speed (" + Math.Round(PER_POINT_MOVEMENT * 100, 3) + " per point)";

                //jump
                bonus_float = PER_POINT_JUMP * points;
                eacplayer.player.jumpSpeedBoost += (bonus_float * 5);
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% increased jump (" + Math.Round(PER_POINT_JUMP * 100, 3) + " per point)";

                //dodge
                bonus_float = PER_POINT_DODGE * points;
                eacplayer.PSheet.Stats.Dodge += bonus_float;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus_float * 100, 3) + "% dodge chance (" + Math.Round(PER_POINT_DODGE * 100, 3) + " per point)";

                //max fly time
                bonus_int = (int)Math.Floor(PER_POINT_FLY * points);
                eacplayer.player.wingTimeMax += bonus_int;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + bonus_int + " wing time (" + PER_POINT_FLY + " per point)";
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
            protected override void Effect(EACPlayer eacplayer, int points) {
                float bonus;

                //ability after use delay
                bonus = PER_POINT_ABILITY_DELAY * points;
                eacplayer.PSheet.Stats.Ability_Delay_Reduction += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus * 100, 3) + "% reduced ability delay (" + Math.Round(PER_POINT_ABILITY_DELAY * 100, 3) + " per point)";

                //tool use time (if non-combat)
                float fish_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Fish_Power, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Fish_Power / 2);
                if (fish_per > 0f) {
                    float bonus_per_point = fish_per * PER_POINT_USE_SPEED;
                    bonus = bonus_per_point * points;
                    eacplayer.PSheet.Stats.SpeedAdjust_Tool += bonus;
                    if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus * 100, 3) + "% tool use speed (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                }

                //weapon use time
                bonus = PER_POINT_USE_SPEED * points;
                eacplayer.PSheet.Stats.SpeedAdjust_Weapon += bonus;
                if (eacplayer.Fields.Is_Local) Bonus += "\n+" + Math.Round(bonus * 100, 3) + "% weapon use speed (" + Math.Round(PER_POINT_USE_SPEED * 100, 3) + " per point)";
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Power Scaling ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public abstract class PowerScaling {
            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
            public enum IDs : byte {
                None,
                CloseRange,
                Projectile,
                ProjectileAndMinion,
                AllCore,
                Holy_AllCore,
                MinionOnly,
                NonCombat,
                Rogue,

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

            //labels
            public string Primary_Types { get; private set; } = "";
            public string Secondary_Types { get; private set; } = "";

            //core types
            protected float Minion { get; private set; } = 0f;

            //custom types
            protected float Damage_All_Non_Minion { get; private set; } = 0f;
            protected float Damage_Close_Range { get; private set; } = 0f;
            protected float Damage_Non_Minion_Projectile { get; private set; } = 0f;
            public float Damage_Holy { get; private set; } = 0f;

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

            public static string ApplyPower(EACPlayer eacplayer, int points) {
                string bonus = "";
                bool tooltip_damage_not_shown = false;

                //calculate scaling values to use...

                //core
                float non_minion_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Damage_All_Non_Minion, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Damage_All_Non_Minion / 2);
                float minion_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Minion, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Minion / 2);

                //custom types
                float close_range_damage_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Damage_Close_Range, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Damage_Close_Range / 2);
                float non_minion_projectile_damage_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Damage_Non_Minion_Projectile, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Damage_Non_Minion_Projectile / 2);
                float holy_damage_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Damage_Holy, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Damage_Holy / 2);

                //non-combat
                float fish_per = Math.Max(eacplayer.PSheet.Classes.Primary.Class.Power_Scaling.Fish_Power, eacplayer.PSheet.Classes.Secondary.Class.Power_Scaling.Fish_Power / 2);

                //non_minion_per does not stack with projectile_damage_per and close_range_damage_per
                if ((non_minion_per > 0f) && (non_minion_projectile_damage_per > 0)) {
                    if (non_minion_per > non_minion_projectile_damage_per) {
                        non_minion_projectile_damage_per = 0f;
                    }
                    else {
                        non_minion_projectile_damage_per -= non_minion_per;
                    }
                }
                if ((non_minion_per > 0f) && (close_range_damage_per > 0)) {
                    if (non_minion_per > close_range_damage_per) {
                        close_range_damage_per = 0f;
                    }
                    else {
                        close_range_damage_per -= non_minion_per;
                    }
                }
                if ((close_range_damage_per > 0f) && (non_minion_projectile_damage_per > 0f)) {
                    float global = Math.Min(close_range_damage_per, non_minion_projectile_damage_per);
                    close_range_damage_per -= global;
                    non_minion_projectile_damage_per -= global;
                    non_minion_per += global;
                    minion_per += global;
                }

                //apply bonuses...
                float bonus_per_point;
                float bonus_total;

                //all non-minion
                if (non_minion_per > 0f) {
                    bonus_per_point = non_minion_per * Attribute.Power.PER_POINT_DAMAGE;
                    bonus_total = bonus_per_point * points;
                    eacplayer.PSheet.Stats.NonMinionAll.Increase += bonus_total;
                    if (eacplayer.Fields.Is_Local) {
                        bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all non-minion damage* (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                        tooltip_damage_not_shown = true;
                    }
                }

                //all minion
                if (minion_per > 0f) {
                    bonus_per_point = minion_per * Attribute.Power.PER_POINT_DAMAGE;
                    bonus_total = bonus_per_point * points;
                    eacplayer.player.minionDamage += bonus_total;
                    if (eacplayer.Fields.Is_Local) {
                        bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all minion damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                    }
                }

                //close range
                if (close_range_damage_per > 0f) {
                    bonus_per_point = close_range_damage_per * Attribute.Power.PER_POINT_DAMAGE;
                    bonus_total = bonus_per_point * points;
                    eacplayer.PSheet.Stats.AllNearby.Increase += bonus_total;
                    if (eacplayer.Fields.Is_Local) {
                        bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all damage on nearby targets* (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                        tooltip_damage_not_shown = true;
                    }
                }

                //non-minon projectile
                if (non_minion_projectile_damage_per > 0f) {
                    bonus_per_point = non_minion_projectile_damage_per * Attribute.Power.PER_POINT_DAMAGE;
                    bonus_total = bonus_per_point * points;
                    eacplayer.PSheet.Stats.NonMinionProjectile.Increase += bonus_total;
                    if (eacplayer.Fields.Is_Local) {
                        bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% all non-minion projectile damage* (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                        tooltip_damage_not_shown = true;
                    }
                }

                //holy
                if (holy_damage_per > 0f) {
                    bonus_per_point = holy_damage_per * Attribute.Power.PER_POINT_DAMAGE;
                    bonus_total = bonus_per_point * points;
                    eacplayer.PSheet.Stats.Holy.Increase += bonus_total;
                    if (eacplayer.Fields.Is_Local) {
                        bonus += "\n+" + Math.Round(bonus_total * 100, 3) + "% holy damage (" + Math.Round(bonus_per_point * 100, 3) + " per point)";
                    }
                }

                //fish
                if (fish_per > 0f) {
                    bonus_per_point = fish_per * Attribute.Power.PER_POINT_FISH;
                    int bonus_total_int = (int)Math.Floor(bonus_per_point * points);
                    eacplayer.player.fishingSkill += bonus_total_int;
                    if (eacplayer.Fields.Is_Local) {
                        bonus += "\n+" + bonus_total_int + " fishing power (" + Math.Round(bonus_per_point, 3) + " per point)";
                    }
                }

                if (tooltip_damage_not_shown) {
                    bonus += "\n\n* bonus is not reflected in item tooltip damage";
                }

                return bonus;
            }

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Power Scaling ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            public class None : PowerScaling {
                public None() : base(IDs.None) {
                    //leave defaults
                }
            }

            public class CloseRange : PowerScaling {
                public CloseRange() : base(IDs.CloseRange) {
                    Primary_Types = "Hits Against Nearby Targets";
                    Damage_Close_Range = SCALE_PRIMARY + 0.2f;
                }
            }

            public class Rogue : PowerScaling {
                public Rogue() : base(IDs.Rogue) {
                    Primary_Types = "Hits Against Nearby Targets";
                    Damage_Close_Range = SCALE_PRIMARY + 0.1f;
                    Secondary_Types = "All Damage";
                    Damage_All_Non_Minion = SCALE_PRIMARY / 2f;
                    Minion = SCALE_PRIMARY / 2f;
                }
            }

            public class Projectile : PowerScaling {
                public Projectile() : base(IDs.Projectile) {
                    Primary_Types = "Any Non-Minion Projectiles";
                    Damage_Non_Minion_Projectile = SCALE_PRIMARY;
                }
            }

            public class ProjectileAndMinion : PowerScaling {
                public ProjectileAndMinion() : base(IDs.ProjectileAndMinion) {
                    Primary_Types = "Any Non-Minion Projectiles";
                    Damage_Non_Minion_Projectile = SCALE_PRIMARY;
                    Secondary_Types = "Minion";
                    Minion = SCALE_SECONDARY;
                }
            }

            public class AllCore : PowerScaling {
                public AllCore() : base(IDs.AllCore) {
                    Primary_Types = "All Damage";
                    Damage_All_Non_Minion = SCALE_PRIMARY;
                    Minion = SCALE_PRIMARY;
                }
            }

            public class Holy_AllCore : PowerScaling {
                public Holy_AllCore() : base(IDs.Holy_AllCore) {
                    Primary_Types = "Holy";
                    Damage_Holy = SCALE_PRIMARY;

                    Secondary_Types = "All Damage";
                    Damage_All_Non_Minion = SCALE_SECONDARY;
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
}