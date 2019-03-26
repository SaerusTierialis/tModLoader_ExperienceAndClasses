using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ExperienceAndClasses.Systems {
    public class Attribute {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum IDs : byte {
            Power,
            Vitality,
            Mind,
            Spirit,
            Agility,
            Dexterity,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        //this may be reordered, UI uses this order
        public static IDs[] ATTRIBUTES_UI_ORDER = new IDs[] { IDs.Power, IDs.Vitality, IDs.Mind, IDs.Spirit, IDs.Agility, IDs.Dexterity };

        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY = 0.8f;
        public const float SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY = 0.6f;

        //class attribute growth
        public const byte ATTRIBUTE_GROWTH_LEVELS = 10;

        //allocation points
        public static readonly int[] ALLOCATION_POINTS_PER_LEVEL_TIERS = new int[] { 0, 1, 2, 3 };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attribute Effects (per point) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const float POWER_DAMAGE = 0.005f;
        private const float POWER_FISH = 0.1f;

        private const float VITALITY_LIFE = 1f;
        private const float VITALITY_LIFE_REGEN = 0.2f;
        private const float VITALITY_DEFENSE = 0.1f;

        private const float MIND_MANA = 1f;
        private const float MIND_MANA_REGEN = 0.5f;
        private const float MIND_MANA_DELAY = 0.5f;

        private const float SPIRIT_CRIT = 0.125f;
        private const float SPIRIT_MINION_CAP = 0.025f;
        private const float SPIRIT_HEAL = 0.5f;

        private const float AGILITY_MOVEMENT = 0.005f;
        private const float AGILITY_JUMP = 0.01f;
        private const float AGILITY_DODGE = 0.0025f;
        private const float AGILITY_FLY = 0.5f;

        private const float DEXTERITY_USE_SPEED = 0.0025f;
        private const float DEXTERITY_ABILITY_DELAY_REDUCTION = 0.01f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static Attribute[] LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

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
        public static int AllocationPointTotal(MPlayer mplayer) {
            int sum = 0;

            for (byte i = 0; i< mplayer.Class_Levels.Length; i++) {
                if (mplayer.Class_Unlocked[i] && Class.LOOKUP[i].Gives_Allocation_Attributes && Class.LOOKUP[i].Tier > 0) {
                    sum += Math.Min(mplayer.Class_Levels[i], Class.LOOKUP[i].Max_Level) * ALLOCATION_POINTS_PER_LEVEL_TIERS[Class.LOOKUP[i].Tier];
                }
            }

            return sum;
        }

        /// <summary>
        /// total allocation points spent by player
        /// </summary>
        /// <param name="mplayer"></param>
        /// <returns></returns>
        public static int AllocationPointSpent(MPlayer mplayer) {
            int sum = 0;

            for (byte i = 0; i< mplayer.Attributes_Allocated.Length; i++) {
                if (LOOKUP[i].Active) {
                    sum += AllocationPointCostTotal(mplayer.Attributes_Allocated[i]);
                }
            }

            return sum;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static Attribute() {
            LOOKUP = new Attribute[(byte)IDs.NUMBER_OF_IDs];

            byte id_byte;
            string name, name_short, desc;
            bool active;

            for (IDs id = 0; id < IDs.NUMBER_OF_IDs; id++) {
                id_byte = (byte)id;

                //bonuses are defined in the instance because it would be a lot to pass to constructor

                //defaults
                name = "unknown";
                name_short = "unknown";
                desc = "unknown";
                active = true;

                switch (id) {
                    case IDs.Power:
                        name = "Power";
                        name_short = "PWR";
                        desc = "TODO_description";
                        break;

                    case IDs.Vitality:
                        name = "Vitality";
                        name_short = "VIT";
                        desc = "TODO_description";
                        break;

                    case IDs.Mind:
                        name = "Mind";
                        name_short = "MND";
                        desc = "TODO_description";
                        break;

                    case IDs.Spirit:
                        name = "Spirit";
                        name_short = "SPT";
                        desc = "TODO_description";
                        break;

                    case IDs.Agility:
                        name = "Agility";
                        name_short = "AGI";
                        desc = "TODO_description";
                        break;

                    case IDs.Dexterity:
                        name = "Dexterity";
                        name_short = "DEX";
                        desc = "TODO_description";
                        break;

                    default:
                        active = false;
                        break;
                }

                LOOKUP[id_byte] = new Attribute(id_byte, name, name_short, desc, active);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public byte ID { get; private set; }
        public string Name { get; private set; }
        public string Name_Short { get; private set; }
        public string Description { get; private set; }
        public bool Active { get; private set; }
        public string Bonus { get; private set; }

        public Attribute(byte id, string name, string name_short, string description, bool active) {
            ID = id;
            Name = name;
            Name_Short = name_short;
            Description = description;
            Active = active;
            Bonus = "";
        }

        public void ApplyEffect(MPlayer mplayer, int points) {
            if (Active) {

                if (mplayer.Is_Local_Player) Bonus = "";
                float bf, bpp;
                int bi;

                float melee_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Melee, mplayer.Class_Secondary.Power_Scaling.Melee / 2);
                float ranged_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Ranged, mplayer.Class_Secondary.Power_Scaling.Ranged / 2);
                float magic_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Magic, mplayer.Class_Secondary.Power_Scaling.Magic / 2);
                float throwing_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Throwing, mplayer.Class_Secondary.Power_Scaling.Throwing / 2);
                float minion_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Minion, mplayer.Class_Secondary.Power_Scaling.Minion / 2);
                float tool_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Tool, mplayer.Class_Secondary.Power_Scaling.Tool / 2);

                switch ((IDs)ID) {
                    case IDs.Power:
                        bpp = melee_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if(bpp > 0) {
                            mplayer.player.meleeDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% melee damage (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = ranged_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.rangedDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% ranged damage (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = magic_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.magicDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% magic damage (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = throwing_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.thrownDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% throwing damage (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = minion_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.minionDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% minion damage (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = tool_per * POWER_FISH;
                        bi = (int)Math.Floor(bpp * points);
                        if (bpp > 0) {
                            mplayer.player.fishingSkill += bi;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " fishing power (" + bpp + " per point)";
                        }

                        break;

                    case IDs.Vitality:
                        //life
                        bpp = VITALITY_LIFE;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.statLifeMax2 += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " maximum life (" + bpp + " per point)";

                        //life regen
                        bpp = VITALITY_LIFE_REGEN;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.lifeRegen += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " life regen (" + bpp + " per point)";

                        //defense
                        bpp = VITALITY_DEFENSE;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.statDefense += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " defense (" + bpp + " per point)";

                        break;

                    case IDs.Mind:
                        //mana
                        bpp = MIND_MANA;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.statManaMax2 += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " maximum mana (" + bpp + " per point)";

                        //mana regen
                        bpp = MIND_MANA_REGEN;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.manaRegenBonus += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " mana regen (" + bpp + " per point)";

                        //mana regen delay (do not reduce too low - causes instant regen)
                        bpp = MIND_MANA_DELAY;
                        bi = (int)Math.Floor(bpp * points);
                        if (mplayer.player.manaRegenDelay > 50) {
                            int new_delay = (int)Math.Max(Math.Round(mplayer.player.manaRegenDelay * (100f / (100f + bi))), 50);
                            mplayer.player.manaRegenDelayBonus += mplayer.player.manaRegenDelay - new_delay;
                        }
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + "% reduced mana delay (" + bpp + " per point)";

                        break;

                    case IDs.Spirit:
                        //crit
                        bpp = SPIRIT_CRIT;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.meleeCrit += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + "% melee critical chance (" + bpp + " per point)";
                        mplayer.player.rangedCrit += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + "% ranged critical chance (" + bpp + " per point)";
                        mplayer.player.magicCrit += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + "% magic critical chance (" + bpp + " per point)";
                        mplayer.player.thrownCrit += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + "% throwing critical chance (" + bpp + " per point)";

                        //minion cap
                        bpp = SPIRIT_MINION_CAP;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.maxMinions += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " maximum minions (" + bpp + " per point)";

                        //healing
                        bpp = SPIRIT_HEAL;
                        bf = bpp * points;
                        mplayer.heal_damage += bf;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bf + "% healing (" + bpp + " per point)";

                        break;

                    case IDs.Agility:
                        //run
                        bpp = AGILITY_MOVEMENT;
                        bf = bpp * points;
                        mplayer.player.maxRunSpeed *= (1f + bf);
                        mplayer.player.runAcceleration *= (1f + bf);
                        mplayer.player.runSlowdown *= (1f / (1f + bf));
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% movement speed (" + Math.Round(bpp * 100, 3) + " per point)";

                        //jump
                        bpp = AGILITY_JUMP;
                        bf = bpp * points;
                        mplayer.player.jumpSpeedBoost += (bf * 5);
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% increased jump (" + Math.Round(bpp * 100, 3) + " per point)";

                        //dodge
                        bpp = AGILITY_DODGE;
                        bf = bpp * points;
                        mplayer.dodge_chance += bf;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% dodge chance (" + Math.Round(bpp * 100, 3) + " per point)";

                        //max fly time
                        bpp = AGILITY_FLY;
                        bi = (int)Math.Floor(bpp * points);
                        mplayer.player.wingTimeMax += bi;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + bi + " wing time (" + bpp + " per point)";

                        break;

                    case IDs.Dexterity:
                        //ability after use delay
                        bpp = DEXTERITY_ABILITY_DELAY_REDUCTION;
                        bf = bpp * points;
                        mplayer.ability_delay_reduction += bf;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% reduced ability delay (" + Math.Round(bpp * 100, 3) + " per point)";

                        //use speeds...

                        bpp = melee_per * DEXTERITY_USE_SPEED;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.use_speed_melee += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% melee attack speed (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = ranged_per * DEXTERITY_USE_SPEED;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.use_speed_ranged += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% ranged attack speed (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = magic_per * DEXTERITY_USE_SPEED;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.use_speed_magic += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% magic cast speed (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = throwing_per * DEXTERITY_USE_SPEED;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.use_speed_throwing += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% throwing attack speed (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = minion_per * DEXTERITY_USE_SPEED;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.use_speed_minion += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% minion cast speed (" + Math.Round(bpp * 100, 3) + " per point)";
                        }

                        bpp = tool_per * DEXTERITY_USE_SPEED;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.use_speed_tool += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 3) + "% tool use speed (" + Math.Round(bpp * 100, 3) + " per point)";
                        }
                        
                        break;
                }
            }
        }
    }

    public class PowerScaling {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public enum IDs : byte {
            None,
            Melee,
            Ranged,
            Magic,
            Throwing,
            Minion,
            All,
            Rogue,
            Tool,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static PowerScaling[] LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static PowerScaling() {
            LOOKUP = new PowerScaling[(byte)IDs.NUMBER_OF_IDs];

            byte id_byte;
            string name;
            float melee, ranged, magic, throwing, minion, tool;

            for (IDs id = 0; id < IDs.NUMBER_OF_IDs; id++) {
                id_byte = (byte)id;

                //defaults
                name = "";
                melee = 0f;
                ranged = 0f;
                magic = 0f;
                throwing = 0f;
                minion = 0f;
                tool = 0f;

                switch (id) {
                    case IDs.None:
                        name = "None";
                        break;

                    case IDs.Melee:
                        name = "Melee";
                        melee = 1f;
                        break;

                    case IDs.Ranged:
                        name = "Ranged";
                        ranged = 1f;
                        break;

                    case IDs.Magic:
                        name = "Magic";
                        magic = 1f;
                        break;

                    case IDs.Throwing:
                        name = "Throwing";
                        throwing = 1f;
                        break;

                    case IDs.Minion:
                        name = "Minion";
                        minion = 1f;
                        break;

                    case IDs.All:
                        name = "Melee, Ranged, Magic, Throwing, Minion";
                        melee = 1f;
                        ranged = 1f;
                        magic = 1f;
                        throwing = 1f;
                        minion = 1f;
                        break;

                    case IDs.Rogue:
                        name = "Melee, Ranged, Magic, Throwing";
                        melee = 1f;
                        ranged = 1f;
                        magic = 1f;
                        throwing = 1f;
                        break;

                    case IDs.Tool:
                        name = "Packaxe, Axe, Hammer, Fishing";
                        tool = 1f;
                        break;
                }

                LOOKUP[id_byte] = new PowerScaling(id_byte, name, melee, ranged, magic, throwing, minion, tool);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public byte ID { get; private set; }
        public string Name { get; private set; }
        public float Melee { get; private set; }
        public float Ranged { get; private set; }
        public float Magic { get; private set; }
        public float Throwing { get; private set; }
        public float Minion { get; private set; }
        public float Tool { get; private set; }

        public PowerScaling(byte id, string name, float melee, float ranged, float magic, float throwing, float minion, float tool) {
            ID = id;
            Name = name;
            Melee = melee;
            Ranged = ranged;
            Magic = magic;
            Throwing = throwing;
            Minion = minion;
            Tool = tool;
        }
    }
}
