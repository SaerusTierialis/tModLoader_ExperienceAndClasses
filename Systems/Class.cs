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
        public enum IDs : byte {
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

        public static Class[] LOOKUP { get; private set; }

        //which classes to show in ui and where
        public static byte[,] Class_Locations { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static Class() {
            Class_Locations = new byte[5, 7];
            LOOKUP = new Class[(byte)Class.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Class)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Class).FullName + "+" + IDs[i]));
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

        public Class(IDs id) {
            //defaults
            ID = (byte)id;
            Name = "Undefined_Name";
            Description = "Undefined_Desc";
            Tier = 0;
            Prereq = null;
            Power_Scaling = PowerScaling.LOOKUP[(byte)PowerScaling.IDs.None];
            Gives_Allocation_Attributes = false;
            Max_Level = 0;
            Unlock_Item = null;

            Attribute_Growth = new float[(byte)Attribute.IDs.NUMBER_OF_IDs];
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
            public RealClass(IDs id, PowerScaling.IDs power_scaling) : base(id) {
                Gives_Allocation_Attributes = true;
                Power_Scaling = PowerScaling.LOOKUP[(byte)power_scaling];
            }
        }

        public abstract class Tier1 : RealClass {
            public Tier1(IDs id, PowerScaling.IDs power_scaling) : base(id, power_scaling) {
                Tier = 1;
                Max_Level = TIER_MAX_LEVELS[Tier];
            }
        }

        public abstract class Tier2 : RealClass {
            public Tier2(IDs id, PowerScaling.IDs power_scaling) : base(id, power_scaling) {
                Tier = 2;
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Tier2>();
                Max_Level = TIER_MAX_LEVELS[Tier];
                Prereq = LOOKUP[(byte)IDs.Novice];
            }
        }

        public abstract class Tier3 : RealClass {
            public Tier3(IDs id, PowerScaling.IDs power_scaling, IDs prereq) : base(id, power_scaling) {
                Tier = 3;
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Tier3>();
                Max_Level = TIER_MAX_LEVELS[Tier];
                Prereq = LOOKUP[(byte)prereq];
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Special Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class New : Class {
            public New() : base(IDs.New) {
            }
        }

        public class None : Class {
            public None() : base(IDs.None) {
                Name = "None";
                Description = "";
            }
        }

        public class Explorer : Tier2 {
            public Explorer() : base(IDs.Explorer, PowerScaling.IDs.Tool) {
                Name = "Explorer";
                Description = "TODO_desc";
                Max_Level = TIER_MAX_LEVELS[3]; //tier 2 class with tier 3 level cap
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Explorer>();
                Class_Locations[0, 6] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2f;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 1 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Novice : Tier1 {
            public Novice() : base(IDs.Novice, PowerScaling.IDs.All) {
                Name = "Novice";
                Description = "TODO_desc";
                Class_Locations[0, 3] = ID;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 2 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Warrior : Tier2 {
            public Warrior() : base(IDs.Warrior, PowerScaling.IDs.Melee) {
                Name = "Warrior";
                Description = "TODO_desc";
                Class_Locations[1, 0] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Ranger : Tier2 {
            public Ranger() : base(IDs.Ranger, PowerScaling.IDs.Ranged) {
                Name = "Ranger";
                Description = "TODO_desc";
                Class_Locations[1, 1] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Mage : Tier2 {
            public Mage() : base(IDs.Mage, PowerScaling.IDs.Magic) {
                Name = "Mage";
                Description = "TODO_desc";
                Class_Locations[1, 2] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
            }
        }

        public class Rogue : Tier2 {
            public Rogue() : base(IDs.Rogue, PowerScaling.IDs.Rogue) {
                Name = "Rogue";
                Description = "TODO_desc";
                Class_Locations[1, 3] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
            }
        }

        public class Summoner : Tier2 {
            public Summoner() : base(IDs.Summoner, PowerScaling.IDs.Minion) {
                Name = "Summoner";
                Description = "TODO_desc";
                Class_Locations[1, 4] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
            }
        }

        public class Cleric : Tier2 {
            public Cleric() : base(IDs.Cleric, PowerScaling.IDs.All) {
                Name = "Cleric";
                Description = "TODO_desc";
                Class_Locations[1, 5] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
            }
        }

        public class Hybrid : Tier2 {
            public Hybrid() : base(IDs.Hybrid, PowerScaling.IDs.All) {
                Name = "Hybrid";
                Description = "TODO_desc";
                Class_Locations[1, 6] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 3 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Unnamed1 : Tier3 {
            public Unnamed1() : base(IDs.Unnamed1, PowerScaling.IDs.Melee, IDs.Warrior) {
                Name = "Unnamed1";
                Description = "TODO_desc";
                Class_Locations[2, 0] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
            }
        }

        public class Berserker : Tier3 {
            public Berserker() : base(IDs.Berserker, PowerScaling.IDs.Melee, IDs.Warrior) {
                Name = "Berserker";
                Description = "TODO_desc";
                Class_Locations[3, 0] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 4;
            }
        }

        public class Guardian : Tier3 {
            public Guardian() : base(IDs.Guardian, PowerScaling.IDs.Melee, IDs.Warrior) {
                Name = "Guardian";
                Description = "TODO_desc";
                Class_Locations[4, 0] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 5;
            }
        }

        public class Sniper : Tier3 {
            public Sniper() : base(IDs.Sniper, PowerScaling.IDs.Ranged, IDs.Ranger) {
                Name = "Sniper";
                Description = "TODO_desc";
                Class_Locations[2, 1] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
            }
        }

        public class Engineer : Tier3 {
            public Engineer() : base(IDs.Engineer, PowerScaling.IDs.Ranged, IDs.Ranger) {
                Name = "Engineer";
                Description = "TODO_desc";
                Class_Locations[3, 1] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Elementalist : Tier3 {
            public Elementalist() : base(IDs.Elementalist, PowerScaling.IDs.Magic, IDs.Mage) {
                Name = "Elementalist";
                Description = "TODO_desc";
                Class_Locations[2, 2] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
            }
        }

        public class Sage : Tier3 {
            public Sage() : base(IDs.Sage, PowerScaling.IDs.Magic, IDs.Mage) {
                Name = "Sage";
                Description = "TODO_desc";
                Class_Locations[3, 2] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 4;
            }
        }

        public class Assassin : Tier3 {
            public Assassin() : base(IDs.Assassin, PowerScaling.IDs.Rogue, IDs.Rogue) {
                Name = "Assassin";
                Description = "TODO_desc";
                Class_Locations[2, 3] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
            }
        }

        public class Chrono : Tier3 {
            public Chrono() : base(IDs.Chrono, PowerScaling.IDs.Rogue, IDs.Rogue) {
                Name = "Chrono";
                Description = "TODO_desc";
                Class_Locations[3, 3] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 5;
            }
        }

        public class Ninja : Tier3 {
            public Ninja() : base(IDs.Ninja, PowerScaling.IDs.Throwing, IDs.Rogue) {
                Name = "Ninja";
                Description = "TODO_desc";
                Class_Locations[4, 3] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
            }
        }

        public class SoulBinder : Tier3 {
            public SoulBinder() : base(IDs.SoulBinder, PowerScaling.IDs.Minion, IDs.Summoner) {
                Name = "Soul Binder";
                Description = "TODO_desc";
                Class_Locations[2, 4] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
            }
        }

        public class Hivemind : Tier3 {
            public Hivemind() : base(IDs.Hivemind, PowerScaling.IDs.Minion, IDs.Summoner) {
                Name = "Hivemind";
                Description = "TODO_desc";
                Class_Locations[3, 4] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
            }
        }

        public class Saint : Tier3 {
            public Saint() : base(IDs.Saint, PowerScaling.IDs.All, IDs.Cleric) {
                Name = "Saint";
                Description = "TODO_desc";
                Class_Locations[2, 5] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
            }
        }

        public class HybridPrime : Tier3 {
            public HybridPrime() : base(IDs.HybridPrime, PowerScaling.IDs.All, IDs.Hybrid) {
                Name = "Hybrid Prime";
                Description = "TODO_desc";
                Class_Locations[2, 6] = ID;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2.5f;
            }
        }

    }
}
