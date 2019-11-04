using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public class PlayerClass {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum IDs : byte {
            New, //no longer used
            None, //no class selected (intentionally)
            Novice,
            Warrior,
            Ranger,
            Traveler,
            Rogue,
            Summoner,
            Cleric,
            Hybrid,
            BloodKnight,
            Berserker,
            Guardian,
            Tinkerer,
            Artillery,
            Chrono,
            Controller,
            Shadow,
            Assassin,
            SoulBinder,
            Tactician,
            Saint,
            HybridPrime,
            Explorer,
            Oracle,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        public enum IMPLEMENTATION_STATUS : byte {
            UNKNOWN,
            ATTRIBUTE_ONLY,
            ATTRIBUTE_PLUS_PARTIAL_ABILITY,
            COMPLETE,
        }

        public const byte MAX_TIER = 3;
        public static readonly byte[] MAX_TIER_LEVEL = new byte[] { 0, 10, 50, 100 };

        public static readonly Color COLOUR_DEFAULT = new Color(255, 255, 255);
        private static readonly Color COLOUR_NOVICE = new Color(168, 185, 127);
        private static readonly Color COLOUR_NONCOMBAT = new Color(165, 98, 77);
        private static readonly Color COLOUR_CLOSE_RANGE_2 = new Color(204, 89, 89);
        private static readonly Color COLOUR_CLOSE_RANGE_3 = new Color(198, 43, 43);
        private static readonly Color COLOUR_PROJECTILE_2 = new Color(127, 146, 255);
        private static readonly Color COLOUR_PROJECTILE_3 = new Color(81, 107, 255);
        private static readonly Color COLOUR_UTILITY_2 = new Color(158, 255, 255);
        private static readonly Color COLOUR_UTILITY_3 = new Color(49, 160, 160);
        private static readonly Color COLOUR_MINION_2 = new Color(142, 79, 142);
        private static readonly Color COLOUR_MINION_3 = new Color(145, 37, 145);
        private static readonly Color COLOUR_SUPPORT_2 = new Color(255, 204, 153);
        private static readonly Color COLOUR_SUPPORT_3 = new Color(255, 174, 94);
        private static readonly Color COLOUR_TRICKERY_2 = new Color(158, 158, 158);
        private static readonly Color COLOUR_TRICKERY_3 = new Color(107, 107, 107);
        private static readonly Color COLOUR_HYBRID_2 = new Color(204, 87, 138);
        private static readonly Color COLOUR_HYBRID_3 = new Color(193, 36, 104);

        public readonly static PlayerClass[] LOOKUP;

        //which classes to show in ui and where
        public readonly static byte[,] Class_Locations;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static PlayerClass() {
            Class_Locations = new byte[5, 7];
            LOOKUP = new PlayerClass[(byte)PlayerClass.IDs.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<PlayerClass>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Fields ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public readonly IDs ID;
        public readonly byte ID_num;

        public readonly string Name;
        public readonly string Description;

        public string Tooltip_Main { get; private set; } = "???_Tooltip";
        public string Tooltip_Title { get; private set; } = "???_Tooltip_Title";
        public string Tooltip_Attribute_Growth { get; private set; } = "???_Tooltip_Attribute_Growth";

        public byte Tier { get; protected set; } = 0;
        public byte Max_Level { get; protected set; } = 0;

        public PlayerClass Prereq { get; protected set; } = null;
        public Items.Unlock Unlock_Item { get; protected set; } = null;

        public bool Gives_Allocation_Attributes { get; protected set; } = false;
        public float[] Attribute_Growth { get; protected set; } = Enumerable.Repeat(1f, (byte)Attribute.IDs.NUMBER_OF_IDs).ToArray();
        public Attribute.PowerScaling Power_Scaling { get; protected set; } = Attribute.PowerScaling.LOOKUP[(byte)Attribute.PowerScaling.IDs.None];

        public bool Enabled { get; protected set; } = false;

        public bool Has_Texture { get; protected set; } = false;
        public Texture2D Texture { get; protected set; }
        public Color Colour { get; protected set; } = COLOUR_DEFAULT;

        protected IMPLEMENTATION_STATUS implementation_status = IMPLEMENTATION_STATUS.UNKNOWN;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public PlayerClass(IDs id) {
            //defaults
            ID = id;
            ID_num = (byte)id;

            Name = Language.GetTextValue("Mods.ExperienceAndClasses.Common.Class_" + Enum.GetName(typeof(IDs), id) + "_Name");
            Description = Language.GetTextValue("Mods.ExperienceAndClasses.Common.Class_" + Enum.GetName(typeof(IDs), id) + "_Description");
        }

        //TODO - add back several methods

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Subtypes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public abstract class RealClass : PlayerClass {
            public RealClass(IDs id, Attribute.PowerScaling.IDs power_scaling) : base(id) {
                Gives_Allocation_Attributes = true;
                Power_Scaling = Attribute.PowerScaling.LOOKUP[(byte)power_scaling];
                Has_Texture = true;
                Enabled = true;
            }
        }

        public abstract class Tier1 : RealClass {
            public Tier1(IDs id, Attribute.PowerScaling.IDs power_scaling) : base(id, power_scaling) {
                Tier = 1;
                Max_Level = MAX_TIER_LEVEL[Tier];
            }
        }

        public abstract class Tier2 : RealClass {
            public Tier2(IDs id, Attribute.PowerScaling.IDs power_scaling) : base(id, power_scaling) {
                Tier = 2;
                Unlock_Item = ModContent.GetInstance<Items.Unlock_Tier2>();
                Max_Level = MAX_TIER_LEVEL[Tier];
                Prereq = LOOKUP[(byte)IDs.Novice];
            }
        }

        public abstract class Tier3 : RealClass {
            public Tier3(IDs id, Attribute.PowerScaling.IDs power_scaling, IDs prereq) : base(id, power_scaling) {
                Tier = 3;
                Unlock_Item = ModContent.GetInstance<Items.Unlock_Tier3>();
                Max_Level = MAX_TIER_LEVEL[Tier];
                Prereq = LOOKUP[(byte)prereq];
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Special Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class New : PlayerClass {
            public New() : base(IDs.New) {
                Enabled = true;
            }
        }

        public class None : PlayerClass {
            public None() : base(IDs.None) {
                Enabled = true;
            }
        }

        public class Explorer : Tier2 {
            public Explorer() : base(IDs.Explorer, Attribute.PowerScaling.IDs.NonCombat) {
                Max_Level = MAX_TIER_LEVEL[3]; //tier 2 class with tier 3 level cap
                Unlock_Item = ModContent.GetInstance<Items.Unlock_Explorer>();
                Class_Locations[0, 6] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2f;
                Colour = COLOUR_NONCOMBAT;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 1 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Novice : Tier1 {
            public Novice() : base(IDs.Novice, Attribute.PowerScaling.IDs.AllCore) {
                Class_Locations[0, 3] = ID_num;
                Colour = COLOUR_NOVICE;
                implementation_status = IMPLEMENTATION_STATUS.COMPLETE;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 2 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Warrior : Tier2 {
            public Warrior() : base(IDs.Warrior, Attribute.PowerScaling.IDs.CloseRange) {
                Class_Locations[1, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_CLOSE_RANGE_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Ranger : Tier2 {
            public Ranger() : base(IDs.Ranger, Attribute.PowerScaling.IDs.Projectile) {
                Class_Locations[1, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_PROJECTILE_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Traveler : Tier2 {
            public Traveler() : base(IDs.Traveler, Attribute.PowerScaling.IDs.AllCore) {
                Class_Locations[1, 2] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Colour = COLOUR_UTILITY_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Rogue : Tier2 {
            public Rogue() : base(IDs.Rogue, Attribute.PowerScaling.IDs.Rogue) {
                Class_Locations[1, 3] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Colour = COLOUR_TRICKERY_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Summoner : Tier2 {
            public Summoner() : base(IDs.Summoner, Attribute.PowerScaling.IDs.MinionOnly) {
                Class_Locations[1, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Colour = COLOUR_MINION_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Cleric : Tier2 {
            public Cleric() : base(IDs.Cleric, Attribute.PowerScaling.IDs.Holy_AllCore) {
                Class_Locations[1, 5] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Colour = COLOUR_SUPPORT_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Hybrid : Tier2 {
            public Hybrid() : base(IDs.Hybrid, Attribute.PowerScaling.IDs.AllCore) {
                Class_Locations[1, 6] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_HYBRID_2;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 3 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class BloodKnight : Tier3 {
            public BloodKnight() : base(IDs.BloodKnight, Attribute.PowerScaling.IDs.CloseRange, IDs.Warrior) {
                Class_Locations[2, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Colour = COLOUR_CLOSE_RANGE_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Berserker : Tier3 {
            public Berserker() : base(IDs.Berserker, Attribute.PowerScaling.IDs.CloseRange, IDs.Warrior) {
                Class_Locations[3, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 4;
                Colour = COLOUR_CLOSE_RANGE_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Guardian : Tier3 {
            public Guardian() : base(IDs.Guardian, Attribute.PowerScaling.IDs.CloseRange, IDs.Warrior) {
                Class_Locations[4, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 5;
                Colour = COLOUR_CLOSE_RANGE_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Artillery : Tier3 {
            public Artillery() : base(IDs.Artillery, Attribute.PowerScaling.IDs.Projectile, IDs.Ranger) {
                Class_Locations[2, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Colour = COLOUR_PROJECTILE_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Tinkerer : Tier3 {
            public Tinkerer() : base(IDs.Tinkerer, Attribute.PowerScaling.IDs.ProjectileAndMinion, IDs.Ranger) {
                Class_Locations[3, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_PROJECTILE_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Controller : Tier3 {
            public Controller() : base(IDs.Controller, Attribute.PowerScaling.IDs.AllCore, IDs.Traveler) {
                Class_Locations[2, 2] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Colour = COLOUR_UTILITY_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Shadow : Tier3 {
            public Shadow() : base(IDs.Shadow, Attribute.PowerScaling.IDs.Rogue, IDs.Traveler) {
                Class_Locations[3, 3] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 4;
                Colour = COLOUR_TRICKERY_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Assassin : Tier3 {
            public Assassin() : base(IDs.Assassin, Attribute.PowerScaling.IDs.Rogue, IDs.Rogue) {
                Class_Locations[2, 3] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Colour = COLOUR_TRICKERY_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Chrono : Tier3 {
            public Chrono() : base(IDs.Chrono, Attribute.PowerScaling.IDs.Projectile, IDs.Ranger) {
                Class_Locations[4, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 5;
                Colour = COLOUR_PROJECTILE_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class SoulBinder : Tier3 {
            public SoulBinder() : base(IDs.SoulBinder, Attribute.PowerScaling.IDs.MinionOnly, IDs.Summoner) {
                Class_Locations[2, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Colour = COLOUR_MINION_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Tactician : Tier3 {
            public Tactician() : base(IDs.Tactician, Attribute.PowerScaling.IDs.MinionOnly, IDs.Summoner) {
                Class_Locations[3, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Colour = COLOUR_MINION_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Saint : Tier3 {
            public Saint() : base(IDs.Saint, Attribute.PowerScaling.IDs.Holy_AllCore, IDs.Cleric) {
                Class_Locations[2, 5] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Colour = COLOUR_SUPPORT_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Oracle : Tier3 {
            public Oracle() : base(IDs.Oracle, Attribute.PowerScaling.IDs.Holy_AllCore, IDs.Cleric) {
                Class_Locations[3, 5] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Colour = COLOUR_SUPPORT_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class HybridPrime : Tier3 {
            public HybridPrime() : base(IDs.HybridPrime, Attribute.PowerScaling.IDs.AllCore, IDs.Hybrid) {
                Class_Locations[2, 6] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2.5f;
                Colour = COLOUR_HYBRID_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

    }
}
