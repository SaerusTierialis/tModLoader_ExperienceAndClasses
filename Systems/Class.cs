using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Class {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum CLASS_IDS : byte {
            New, //newly created chracters will momentarily have this class before being set to novice
            None, //no class selected (intentionally)
            Novice,
            Warrior,
            Ranger,
            Mage,
            Rogue,
            Summoner,
            Cleric,
            Hybrid,
            Unnamed1,
            Berserker,
            Guardian,
            Engineer,
            Sniper,
            Elementalist,
            Sage,
            Assassin,
            Chrono,
            Ninja,
            Hivemind,
            SoulBinder,
            Saint,
            HybridPrime,
            Explorer,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        public static readonly byte[] TIER_MAX_LEVELS = new byte[] {0, 10, 50, 100};

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Class[] CLASS_LOOKUP { get; private set; }

        //which classes to show in ui and where
        public static byte[,] Class_Locations { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static Class() {
            Class_Locations = new byte[5, 7];
            CLASS_LOOKUP = new Class[(byte)CLASS_IDS.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(CLASS_IDS));
            for (byte i = 0; i < CLASS_LOOKUP.Length; i++) {
                CLASS_LOOKUP[i] = (Class)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Class).FullName + "+" + IDs[i]));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public byte ID { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public byte Tier { get; protected set; }
        public Texture2D Texture { get; protected set; }
        public Class Prereq { get; protected set; }
        public PowerScaling Power_Scaling { get; protected set; }
        public float[] Attribute_Growth { get; protected set; }
        public bool Gives_Allocation_Attributes { get; protected set; }
        public byte Max_Level { get; protected set; }
        public Items.Unlock Unlock_Item { get; protected set; }

        public Class(CLASS_IDS id) {
            //defaults
            ID = (byte)id;
            Name = "Undefined_Name";
            Description = "Undefined_Desc";
            Tier = 0;
            Prereq = null;
            Power_Scaling = PowerScaling.POWER_SCALING_LOOKUP[(byte)PowerScaling.POWER_SCALING_TYPES.None];
            Gives_Allocation_Attributes = false;
            Max_Level = 0;
            Unlock_Item = null;

            Attribute_Growth = new float[(byte)Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            for (byte i = 0; i < Attribute_Growth.Length; i++) {
                Attribute_Growth[i] = 1f;
            }
        }

        public void LoadTexture() {
            try {
                Texture = ModLoader.GetTexture("ExperienceAndClasses/Textures/Class/" + Name);
            }
            catch {
                //no texture loaded, set blank to prevent crash
                Texture = Textures.TEXTURE_BLANK;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Subtypes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public abstract class RealClass : Class {
            public RealClass(CLASS_IDS id, PowerScaling.POWER_SCALING_TYPES power_scaling) : base(id) {
                Gives_Allocation_Attributes = true;
                Power_Scaling = PowerScaling.POWER_SCALING_LOOKUP[(byte)power_scaling];
            }
        }

        public abstract class Tier1 : RealClass {
            public Tier1(CLASS_IDS id, PowerScaling.POWER_SCALING_TYPES power_scaling) : base(id, power_scaling) {
                Tier = 1;
                Max_Level = TIER_MAX_LEVELS[Tier];
            }
        }

        public abstract class Tier2 : RealClass {
            public Tier2(CLASS_IDS id, PowerScaling.POWER_SCALING_TYPES power_scaling) : base(id, power_scaling) {
                Tier = 2;
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Tier2>();
                Max_Level = TIER_MAX_LEVELS[Tier];
                Prereq = CLASS_LOOKUP[(byte)CLASS_IDS.Novice];
            }
        }

        public abstract class Tier3 : RealClass {
            public Tier3(CLASS_IDS id, PowerScaling.POWER_SCALING_TYPES power_scaling, CLASS_IDS prereq) : base(id, power_scaling) {
                Tier = 3;
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Tier3>();
                Max_Level = TIER_MAX_LEVELS[Tier];
                Prereq = CLASS_LOOKUP[(byte)prereq];
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Special Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class New : Class {
            public New() : base(CLASS_IDS.New) {
            }
        }

        public class None : Class {
            public None() : base(CLASS_IDS.None) {
                Name = "None";
                Description = "";
            }
        }

        public class Explorer : Tier2 {
            public Explorer() : base(CLASS_IDS.Explorer, PowerScaling.POWER_SCALING_TYPES.Tool) {
                Name = "Explorer";
                Description = "TODO_desc";
                Max_Level = TIER_MAX_LEVELS[3]; //tier 2 class with tier 3 level cap
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Explorer>();
                Class_Locations[0, 6] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 2f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 2f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 2f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 2f;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 1 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Novice : Tier1 {
            public Novice() : base(CLASS_IDS.Novice, PowerScaling.POWER_SCALING_TYPES.All) {
                Name = "Novice";
                Description = "TODO_desc";
                Class_Locations[0, 3] = ID;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 2 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Warrior : Tier2 {
            public Warrior() : base(CLASS_IDS.Warrior, PowerScaling.POWER_SCALING_TYPES.Melee) {
                Name = "Warrior";
                Description = "TODO_desc";
                Class_Locations[1, 0] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 2;
            }
        }

        public class Ranger : Tier2 {
            public Ranger() : base(CLASS_IDS.Ranger, PowerScaling.POWER_SCALING_TYPES.Ranged) {
                Name = "Ranger";
                Description = "TODO_desc";
                Class_Locations[1, 1] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 2;
            }
        }

        public class Mage : Tier2 {
            public Mage() : base(CLASS_IDS.Mage, PowerScaling.POWER_SCALING_TYPES.Magic) {
                Name = "Mage";
                Description = "TODO_desc";
                Class_Locations[1, 2] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 3;
            }
        }

        public class Rogue : Tier2 {
            public Rogue() : base(CLASS_IDS.Rogue, PowerScaling.POWER_SCALING_TYPES.Rogue) {
                Name = "Rogue";
                Description = "TODO_desc";
                Class_Locations[1, 3] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 3;
            }
        }

        public class Summoner : Tier2 {
            public Summoner() : base(CLASS_IDS.Summoner, PowerScaling.POWER_SCALING_TYPES.Minion) {
                Name = "Summoner";
                Description = "TODO_desc";
                Class_Locations[1, 4] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 3;
            }
        }

        public class Cleric : Tier2 {
            public Cleric() : base(CLASS_IDS.Cleric, PowerScaling.POWER_SCALING_TYPES.All) {
                Name = "Cleric";
                Description = "TODO_desc";
                Class_Locations[1, 5] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 3;
            }
        }

        public class Hybrid : Tier2 {
            public Hybrid() : base(CLASS_IDS.Hybrid, PowerScaling.POWER_SCALING_TYPES.All) {
                Name = "Hybrid";
                Description = "TODO_desc";
                Class_Locations[1, 6] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 2;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 3 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Unnamed1 : Tier3 {
            public Unnamed1() : base(CLASS_IDS.Unnamed1, PowerScaling.POWER_SCALING_TYPES.Melee, CLASS_IDS.Warrior) {
                Name = "Unnamed1";
                Description = "TODO_desc";
                Class_Locations[2, 0] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 5;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 3;
            }
        }

        public class Berserker : Tier3 {
            public Berserker() : base(CLASS_IDS.Berserker, PowerScaling.POWER_SCALING_TYPES.Melee, CLASS_IDS.Warrior) {
                Name = "Berserker";
                Description = "TODO_desc";
                Class_Locations[3, 0] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 4;
            }
        }

        public class Guardian : Tier3 {
            public Guardian() : base(CLASS_IDS.Guardian, PowerScaling.POWER_SCALING_TYPES.Melee, CLASS_IDS.Warrior) {
                Name = "Guardian";
                Description = "TODO_desc";
                Class_Locations[4, 0] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 5;
            }
        }

        public class Sniper : Tier3 {
            public Sniper() : base(CLASS_IDS.Sniper, PowerScaling.POWER_SCALING_TYPES.Ranged, CLASS_IDS.Ranger) {
                Name = "Sniper";
                Description = "TODO_desc";
                Class_Locations[2, 1] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 4;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 4;
            }
        }

        public class Engineer : Tier3 {
            public Engineer() : base(CLASS_IDS.Engineer, PowerScaling.POWER_SCALING_TYPES.Ranged, CLASS_IDS.Ranger) {
                Name = "Engineer";
                Description = "TODO_desc";
                Class_Locations[3, 1] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 2;
            }
        }

        public class Elementalist : Tier3 {
            public Elementalist() : base(CLASS_IDS.Elementalist, PowerScaling.POWER_SCALING_TYPES.Magic, CLASS_IDS.Mage) {
                Name = "Elementalist";
                Description = "TODO_desc";
                Class_Locations[2, 2] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 5;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 3;
            }
        }

        public class Sage : Tier3 {
            public Sage() : base(CLASS_IDS.Sage, PowerScaling.POWER_SCALING_TYPES.Magic, CLASS_IDS.Mage) {
                Name = "Sage";
                Description = "TODO_desc";
                Class_Locations[3, 2] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 4;
            }
        }

        public class Assassin : Tier3 {
            public Assassin() : base(CLASS_IDS.Assassin, PowerScaling.POWER_SCALING_TYPES.Rogue, CLASS_IDS.Rogue) {
                Name = "Assassin";
                Description = "TODO_desc";
                Class_Locations[2, 3] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 3;
            }
        }

        public class Chrono : Tier3 {
            public Chrono() : base(CLASS_IDS.Chrono, PowerScaling.POWER_SCALING_TYPES.Rogue, CLASS_IDS.Rogue) {
                Name = "Chrono";
                Description = "TODO_desc";
                Class_Locations[3, 3] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 5;
            }
        }

        public class Ninja : Tier3 {
            public Ninja() : base(CLASS_IDS.Ninja, PowerScaling.POWER_SCALING_TYPES.Throwing, CLASS_IDS.Rogue) {
                Name = "Ninja";
                Description = "TODO_desc";
                Class_Locations[4, 3] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 3;
            }
        }

        public class SoulBinder : Tier3 {
            public SoulBinder() : base(CLASS_IDS.SoulBinder, PowerScaling.POWER_SCALING_TYPES.Minion, CLASS_IDS.Summoner) {
                Name = "Soul Binder";
                Description = "TODO_desc";
                Class_Locations[2, 4] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 5;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2;
            }
        }

        public class Hivemind : Tier3 {
            public Hivemind() : base(CLASS_IDS.Hivemind, PowerScaling.POWER_SCALING_TYPES.Minion, CLASS_IDS.Summoner) {
                Name = "Hivemind";
                Description = "TODO_desc";
                Class_Locations[3, 4] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 2;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 4;
            }
        }

        public class Saint : Tier3 {
            public Saint() : base(CLASS_IDS.Saint, PowerScaling.POWER_SCALING_TYPES.All, CLASS_IDS.Cleric) {
                Name = "Saint";
                Description = "TODO_desc";
                Class_Locations[2, 5] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 3;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 5;
            }
        }

        public class HybridPrime : Tier3 {
            public HybridPrime() : base(CLASS_IDS.HybridPrime, PowerScaling.POWER_SCALING_TYPES.All, CLASS_IDS.Hybrid) {
                Name = "Hybrid Prime";
                Description = "TODO_desc";
                Class_Locations[2, 6] = ID;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Power] = 2.5f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Vitality] = 2.5f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Mind] = 2.5f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Spirit] = 2.5f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Agility] = 2.5f;
                Attribute_Growth[(byte)Attribute.ATTRIBUTE_IDS.Dexterity] = 2.5f;
            }
        }

    }
}
