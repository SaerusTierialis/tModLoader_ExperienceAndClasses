using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;

namespace ExperienceAndClasses.Systems {
    class Attribute {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum ATTRIBUTE_IDS : byte {
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
        public static ATTRIBUTE_IDS[] ATTRIBUTES_UI_ORDER = new ATTRIBUTE_IDS[] { ATTRIBUTE_IDS.Power, ATTRIBUTE_IDS.Vitality, ATTRIBUTE_IDS.Mind, ATTRIBUTE_IDS.Spirit, ATTRIBUTE_IDS.Agility, ATTRIBUTE_IDS.Dexterity };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static Attribute[] ATTRIBUTE_LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static Attribute() {
            ATTRIBUTE_LOOKUP = new Attribute[(byte)ATTRIBUTE_IDS.NUMBER_OF_IDs];

            byte id_byte;
            string name, name_short, desc;
            bool active;

            for (ATTRIBUTE_IDS id = 0; id < ATTRIBUTE_IDS.NUMBER_OF_IDs; id++) {
                id_byte = (byte)id;

                //bonuses are defined in the instance because it would be a lot to pass to constructor

                //defaults
                name = "unknown";
                name_short = "unknown";
                desc = "unknown";
                active = true;

                switch (id) {
                    case ATTRIBUTE_IDS.Power:
                        name = "Power";
                        name_short = "PWR";
                        desc = "TODO_description";
                        break;

                    case ATTRIBUTE_IDS.Vitality:
                        name = "Vitality";
                        name_short = "VIT";
                        desc = "TODO_description";
                        break;

                    case ATTRIBUTE_IDS.Mind:
                        name = "Mind";
                        name_short = "MND";
                        desc = "TODO_description";
                        break;

                    case ATTRIBUTE_IDS.Spirit:
                        name = "Spirit";
                        name_short = "SPT";
                        desc = "TODO_description";
                        break;

                    case ATTRIBUTE_IDS.Agility:
                        name = "Agility";
                        name_short = "AGI";
                        desc = "TODO_description";
                        break;

                    case ATTRIBUTE_IDS.Dexterity:
                        name = "Dexterity";
                        name_short = "DEX";
                        desc = "TODO_description";
                        break;

                    default:
                        active = false;
                        break;
                }

                ATTRIBUTE_LOOKUP[id_byte] = new Attribute(id_byte, name, name_short, desc, active);
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

        //per point bonuses
        private const float POWER_DAMAGE = 0.01f;

        private const float VITALITY_LIFE = 1f;
        private const float VITALITY_LIFE_REGEN = 0.2f;
        private const float VITALITY_DEFENSE = 0.1f;

        private const float MIND_MANA = 1f;
        private const float MIND_MANA_REGEN = 0.5f;
        private const float MIND_MANA_DELAY = 0.5f;

        private const float SPIRIT_CRIT = 0.25f;
        private const float SPIRIT_MINION_CAP = 0.05f;
        private const float SPIRIT_HEAL = 1f;

        private const float AGILITY_MOVEMENT = 0.005f;
        private const float AGILITY_JUMP = 0.01f;
        private const float AGILITY_DODGE = 0.0025f;

        private const float DEXTERITY_ATTACK_SPEED = 0.0025f;
        private const float DEXTERITY_COOLDOWN = 0.01f;

        public void ApplyEffect(MPlayer mplayer, short points) {
            if (Active) {

                if (mplayer.Is_Local_Player) Bonus = "";
                float bf, bpp;
                int bi;

                switch ((ATTRIBUTE_IDS)ID) {
                    case ATTRIBUTE_IDS.Power:
                        float melee_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Melee, mplayer.Class_Secondary.Power_Scaling.Melee / 2);
                        float ranged_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Ranged, mplayer.Class_Secondary.Power_Scaling.Ranged / 2);
                        float magic_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Magic, mplayer.Class_Secondary.Power_Scaling.Magic / 2);
                        float throwing_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Throwing, mplayer.Class_Secondary.Power_Scaling.Throwing / 2);
                        float minion_per = Math.Max(mplayer.Class_Primary.Power_Scaling.Minion, mplayer.Class_Secondary.Power_Scaling.Minion / 2);

                        bpp = melee_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if(bpp > 0) {
                            mplayer.player.meleeDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% melee damage (" + Math.Round(bpp * 100, 2) + " per point)";
                        }

                        bpp = ranged_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.rangedDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% ranged damage (" + Math.Round(bpp * 100, 2) + " per point)";
                        }

                        bpp = magic_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.magicDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% magic damage (" + Math.Round(bpp * 100, 2) + " per point)";
                        }

                        bpp = throwing_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.thrownDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% throwing damage (" + Math.Round(bpp * 100, 2) + " per point)";
                        }

                        bpp = minion_per * POWER_DAMAGE;
                        bf = bpp * points;
                        if (bpp > 0) {
                            mplayer.player.minionDamage += bf;
                            if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% minion damage (" + Math.Round(bpp * 100, 2) + " per point)";
                        }

                        break;

                    case ATTRIBUTE_IDS.Vitality:
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

                    case ATTRIBUTE_IDS.Mind:
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

                    case ATTRIBUTE_IDS.Spirit:
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

                    case ATTRIBUTE_IDS.Agility:
                        //run
                        bpp = AGILITY_MOVEMENT;
                        bf = bpp * points;
                        mplayer.player.maxRunSpeed *= (1f + bf);
                        mplayer.player.runAcceleration *= (1f + bf);
                        mplayer.player.runSlowdown *= (1f / (1f + bf));
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% movement speed (" + Math.Round(bpp * 100, 2) + " per point)";

                        //jump
                        bpp = AGILITY_JUMP;
                        bf = bpp * points;
                        mplayer.player.jumpSpeedBoost += (bf * 5);
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% increased jump (" + Math.Round(bpp * 100, 2) + " per point)";

                        //dodge
                        bpp = AGILITY_DODGE;
                        bf = bpp * points;
                        mplayer.dodge_chance += bf;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% dodge chance (" + Math.Round(bpp * 100, 2) + " per point)";

                        break;

                    case ATTRIBUTE_IDS.Dexterity:
                        //attack speed
                        bpp = DEXTERITY_ATTACK_SPEED;
                        bf = bpp * points;
                        mplayer.player.meleeSpeed += bf;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% melee attack speed (" + Math.Round(bpp * 100, 2) + " per point)";

                        //cooldown reduction
                        bpp = DEXTERITY_COOLDOWN;
                        bf = bpp * points;
                        mplayer.cooldown_reduction += bf;
                        if (mplayer.Is_Local_Player) Bonus += "\n+" + Math.Round(bf * 100, 2) + "% cooldown reduction (" + Math.Round(bpp * 100, 2) + " per point)";

                        break;
                }
            }
        }
    }

    class PowerScaling {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public enum POWER_SCALING_TYPES : byte {
            None,
            Melee,
            Ranged,
            Magic,
            Throwing,
            Minion,
            All,
            Rogue,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static PowerScaling[] POWER_SCALING_LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static PowerScaling() {
            POWER_SCALING_LOOKUP = new PowerScaling[(byte)POWER_SCALING_TYPES.NUMBER_OF_IDs];

            byte id_byte;
            string name;
            float melee, ranged, magic, throwing, minion;

            for (POWER_SCALING_TYPES id = 0; id < POWER_SCALING_TYPES.NUMBER_OF_IDs; id++) {
                id_byte = (byte)id;

                //defaults
                name = "";
                melee = 0f;
                ranged = 0f;
                magic = 0f;
                throwing = 0f;
                minion = 0f;

                switch (id) {
                    case POWER_SCALING_TYPES.None:
                        name = "None";
                        break;

                    case POWER_SCALING_TYPES.Melee:
                        name = "Melee";
                        melee = 1f;
                        break;

                    case POWER_SCALING_TYPES.Ranged:
                        name = "Ranged";
                        ranged = 1f;
                        break;

                    case POWER_SCALING_TYPES.Magic:
                        name = "Magic";
                        magic = 1f;
                        break;

                    case POWER_SCALING_TYPES.Throwing:
                        name = "Throwing";
                        throwing = 1f;
                        break;

                    case POWER_SCALING_TYPES.Minion:
                        name = "Minion";
                        minion = 1f;
                        break;

                    case POWER_SCALING_TYPES.All:
                        name = "Melee, Ranged, Magic, Throwing, Minion";
                        melee = 1f;
                        ranged = 1f;
                        magic = 1f;
                        throwing = 1f;
                        minion = 1f;
                        break;

                    case POWER_SCALING_TYPES.Rogue:
                        name = "Melee, Throwing";
                        melee = 1f;
                        throwing = 1f;
                        break;
                }

                POWER_SCALING_LOOKUP[id_byte] = new PowerScaling(id_byte, name, melee, ranged, magic, throwing, minion);
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

        public PowerScaling(byte id, string name, float melee, float ranged, float magic, float throwing, float minion) {
            ID = id;
            Name = name;
            Melee = melee;
            Ranged = ranged;
            Magic = magic;
            Throwing = throwing;
            Minion = minion;
        }
    }
}
