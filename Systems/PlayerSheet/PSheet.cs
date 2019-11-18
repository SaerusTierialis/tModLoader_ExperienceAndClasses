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

        //these will probably change later
        public Utilities.Containers.LevelSortedPassives Passives { get; private set; }
        public Dictionary<Resource.IDs, Resource> Resources { get; private set; }
        public Ability[] Abilities_Primary { get; private set; }
        public Ability[] Abilities_Primary_Alt { get; private set; }
        public Ability[] Abilities_Secondary { get; private set; }
        public Ability[] Abilities_Secondary_Alt { get; private set; }

        public readonly ClassSheet Classes;
        public readonly AttributeSheet Attributes;
        public readonly StatsSheet Stats;
        public readonly CharacterSheet Character;

        public PSheet(EACPlayer owner) {
            eacplayer = owner;

            //these will probably change later
            Passives = new Utilities.Containers.LevelSortedPassives();
            Resources = new Dictionary<Resource.IDs, Resource>();
            Abilities_Primary = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
            Abilities_Primary_Alt = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
            Abilities_Secondary = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
            Abilities_Secondary_Alt = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];

            Stats = new StatsSheet(this);
            Attributes = new AttributeSheet(this);
            Character = new CharacterSheet(this);
            Classes = new ClassSheet(this);

            //init
            Classes.SetDefaultClass();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void PreUpdate() {
            Attributes.ResetBonuses();
            Stats.Reset();
        }

        public void PostUpdate() {
            Attributes.Apply();
            Stats.Limit();
            Stats.Apply();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void Load(TagCompound tag) {
            //Class
            Classes.Load(tag);

            //Character
            Character.Load(tag);

            //Attribute Allocations
            Attributes.Load(tag);

            //Misc
            Misc.Load(tag);
        }

        public TagCompound Save(TagCompound tag) {
            //Class
            tag = Classes.Save(tag);

            //Attribute Allocations
            tag = Attributes.Save(tag);

            //Character
            tag = Character.Save(tag);

            //Misc
            tag = Misc.Save(tag);

            return tag;
        }

        public MiscDataContainer Misc = new MiscDataContainer();
        public class MiscDataContainer {
            //defaults
            public float UIMain_Left { get; private set; } = 0;
            public float UIMain_Top { get; private set; } = 0;
            public float UIHUD_Left { get; private set; } = 0;
            public float UIHUD_Top { get; private set; } = 0;
            public int[] Loaded_Version { get; private set; } = new int[3];

            public TagCompound Save(TagCompound tag) {
                //UI
                tag.Add(TAG_NAMES.UI_UIMAIN_LEFT, (double)UI.UIMain.Instance.panel.GetLeft());
                tag.Add(TAG_NAMES.UI_UIMAIN_TOP, (double)UI.UIMain.Instance.panel.GetTop());
                tag.Add(TAG_NAMES.UI_UIHUD_LEFT, (double)UI.UIHUD.Instance.panel.GetLeft());
                tag.Add(TAG_NAMES.UI_UIHUD_TOP, (double)UI.UIHUD.Instance.panel.GetTop());

                //version
                tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.VERSION, Shortcuts.Version);

                return tag;
            }

            public void Load(TagCompound tag) {
                //UI
                UIMain_Left = (float)Utilities.Commons.TagTryGet<double>(tag, TAG_NAMES.UI_UIMAIN_LEFT, 0);
                UIMain_Top = (float)Utilities.Commons.TagTryGet<double>(tag, TAG_NAMES.UI_UIMAIN_TOP, 0);
                UIHUD_Left = (float)Utilities.Commons.TagTryGet<double>(tag, TAG_NAMES.UI_UIHUD_LEFT, 0);
                UIHUD_Top = (float)Utilities.Commons.TagTryGet<double>(tag, TAG_NAMES.UI_UIHUD_TOP, 0);

                //version
                Loaded_Version = Utilities.Commons.TagLoadListAsArray<int>(tag, TAG_NAMES.VERSION, 3);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Internal Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        protected void Passives_Clear() {
            Passives = new Utilities.Containers.LevelSortedPassives();
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

        //Attribute
        public static string Attributes_Allocated = PREFIX + "attribute_allocation";
        public static string Attributes_PowerScaling = PREFIX + "attribute_power_scaling";

        //Character
        public static string Character_Level = PREFIX + "character_level";
        public static string Character_XP = PREFIX + "character_xp";
        public static string WOF = PREFIX + "wof";
        public static string UNLOCK_SUBCLASS = PREFIX + "class_subclass_unlocked";

        //UI
        public static string UI_UIMAIN_LEFT = PREFIX + "ui_uimain_left";
        public static string UI_UIMAIN_TOP = PREFIX + "ui_uimain_top";
        public static string UI_UIHUD_LEFT = PREFIX + "ui_uihud_left";
        public static string UI_UIHUD_TOP = PREFIX + "ui_uihud_top";

        //misc
        public static string VERSION = PREFIX + "version";
    }

    /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Templates ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
    public abstract class ContainerTemplate {
        public readonly PSheet PSHEET;
        public ContainerTemplate(PSheet psheet) {
            PSHEET = psheet;
        }
    }
}
