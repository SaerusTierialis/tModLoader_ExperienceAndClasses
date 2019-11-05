using ExperienceAndClasses.Systems.PlayerSheet;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems {
    public class PSheet {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Main ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public readonly EACPlayer eacplayer;

        public readonly ClassSheet Classes;
        public readonly AttributeSheet Attributes;
        public readonly StatsSheet Stats;
        public readonly CharacterSheet Character;

        public PSheet(EACPlayer owner) {
            eacplayer = owner;
            Stats = new StatsSheet(this);
            Attributes = new AttributeSheet(this);
            Character = new CharacterSheet(this);
            Classes = new ClassSheet(this);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void PreUpdate() {
            Attributes.ResetBonuses();
            Stats.Reset();
        }

        public void PostUpdate() {
            Attributes.Apply();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void Load(TagCompound tag) {
            //Class
            Classes.Load(tag);

            //Attribute Allocations
            Attributes.Load(tag);

            //Character
            Character.Load(tag);
        }

        public TagCompound Save(TagCompound tag) {
            //Class
            tag = Classes.Save(tag);

            //Attribute Allocations
            tag = Attributes.Save(tag);

            //Character
            tag = Character.Save(tag);

            return tag;
        }

    }
}

namespace ExperienceAndClasses.Systems.PlayerSheet {
    /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tag Names ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
    public static class TAG_NAMES {
        public static string PREFIX = "eac_";

        //Class
        public static string Class_Unlock = PREFIX + "class_unlock";
        public static string Class_XP = PREFIX + "class_xp";
        public static string Class_Level = PREFIX + "class_level";
        public static string Class_Active_Primary = PREFIX + "class_current_primary";
        public static string Class_Active_Secondary = PREFIX + "class_current_secondary";

        //Attribute Allocations
        public static string Attributes_Allocated = PREFIX + "attribute_allocation";

        //Character
        public static string Character_Level = PREFIX + "character_level";
        public static string Character_XP = PREFIX + "character_xp";
        public static string WOF = PREFIX + "wof";
        public static string UNLOCK_SUBCLASS = PREFIX + "class_subclass_unlocked";
    }

    /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Templates ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
    public abstract class ContainerTemplate {
        protected readonly PSheet PSHEET;
        public ContainerTemplate(PSheet psheet) {
            PSHEET = psheet;
        }
    }
}
