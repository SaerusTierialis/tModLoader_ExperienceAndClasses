using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
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
            Bard,
            Minstrel,

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
        private static readonly Color COLOUR_MUSIC_2 = new Color(204, 87, 138);
        private static readonly Color COLOUR_MUSIC_3 = new Color(193, 36, 104);

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

        private string InternalName;

        public string Tooltip_Attribute_Growth { get; private set; }

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

        public float XP_Multiplier_Combat { get; protected set; } = 1.0f;
        public float XP_Multiplier_NonCombat { get; protected set; } = 1.0f;

        public Ability[] Abilities = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
        public Ability[] Abilities_Alt = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public PlayerClass(IDs id) {
            //defaults
            ID = id;
            ID_num = (byte)id;

            InternalName = Enum.GetName(typeof(IDs), ID_num);
        }

        public string Name { get { return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Class_" + InternalName + "_Name"); } }
        public string Description {  get { return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Class_" + InternalName + "_Description"); } }

        public string Tooltip_Title {
            get {
                if (ID_num == (byte)Systems.PlayerClass.IDs.Explorer) {
                    return Name + " [Unique]";
                }
                else {
                    return Name + " [Tier " + new string('I', Tier) + "]";
                }
            }
        }

        public void LoadTexture() {
            //load texture
            if (Has_Texture) {
                Texture = ModContent.GetTexture("ExperienceAndClasses/Textures/Class/" + InternalName);
            }
            else {
                //no texture loaded, set blank
                Texture = Utilities.Textures.TEXTURE_CLASS_DEFAULT;
            }

            //also set attribute string
            bool first = true;
            Tooltip_Attribute_Growth = "";
            foreach (byte id in Systems.Attribute.ATTRIBUTES_UI_ORDER) {
                if (first) {
                    first = false;
                }
                else {
                    Tooltip_Attribute_Growth += "\n";
                }

                for (byte i = 0; i < 5; i++) {
                    if (Attribute_Growth[id] >= (i + 1)) {
                        Tooltip_Attribute_Growth += "★";
                    }
                    else if (Attribute_Growth[id] > i) {
                        Tooltip_Attribute_Growth += "✯";
                    }
                    else {
                        Tooltip_Attribute_Growth += "☆";
                    }
                }
            }
        }

        public string Tooltip_Main {
            get {
                //implementation status
                string implementation_status_text = "Implementation State: ";
                switch (implementation_status) {
                    case IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY:
                        implementation_status_text += "attributes only";
                        break;

                    case IMPLEMENTATION_STATUS.ATTRIBUTE_PLUS_PARTIAL_ABILITY:
                        implementation_status_text += "some abilities/passives";
                        break;

                    case IMPLEMENTATION_STATUS.COMPLETE:
                        implementation_status_text += "complete";
                        break;

                    case IMPLEMENTATION_STATUS.UNKNOWN:
                    default:
                        implementation_status_text += "unknown";
                        break;
                }

                //set tooltip
                string tooltip_main = implementation_status_text + "\n\n" + Description + "\n\n" + "POWER SCALING:\nPrimary:   " + Power_Scaling.Primary_Types + "\nSecondary: " + Power_Scaling.Secondary_Types + "\n\nATTRIBUTES:";
                bool first = true;
                string attribute_names = "";
                foreach (byte id in Systems.Attribute.ATTRIBUTES_UI_ORDER) {
                    if (first) {
                        first = false;
                    }
                    else {
                        attribute_names += "\n";
                    }
                    attribute_names += Systems.Attribute.LOOKUP[id].Specifc_Name + ":";
                }
                tooltip_main += "\n" + attribute_names;

                //return
                return tooltip_main;
            }
        }

        /// <summary>
        /// Check if player meets class prereqs
        /// </summary>
        /// <returns></returns>
        public bool MeetsClassPrereq(PSheet psheet) {
            Systems.PlayerClass pre = Prereq;
            while (pre != null) {
                if (!psheet.Classes.GetClassInfo(pre.ID_num).Maxed) {
                    //level requirement not met
                    return false;
                }
                else {
                    pre = pre.Prereq;
                }
            }
            return true;
        }

        /// <summary>
        /// Try to unlock this class (called from UI)
        /// </summary>
        /// <returns></returns>
        public bool LocalTryUnlockClass() {
            PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;
            PlayerSheet.ClassSheet.ClassInfo info = psheet.Classes.GetClassInfo(ID_num);

            //check locked
            if (info.Unlocked) {
                Utilities.Logger.Error("Trying to unlock already unlocked class " + Name);
                return false;
            }

            //tier 3 requirement
            if (Tier == 3 && !CanUnlockTier3(psheet)) {
                if (!psheet.Character.Defeated_WOF) {
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_Class_Fail_WOF"), UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Utilities.Logger.Error("LocalCanUnlockTier3 returned false for unknown reasons! Please Report!");
                }
                return false;
            }

            //level requirements
            if (!MeetsClassPrereq(psheet)) {
                Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_Class_Fail_Prereq", Prereq.Max_Level, Prereq.Name, Name), UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            //item requirements
            if (Unlock_Item != null) {
                if (!Shortcuts.LOCAL_PLAYER.player.HasItem(Unlock_Item.item.type)) {
                    //item requirement not met
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_Class_Fail_Item", Unlock_Item.Name, Name), UI.Constants.COLOUR_MESSAGE_ERROR);
                    return false;
                }
            }

            //requirements met..

            //take item
            Shortcuts.LOCAL_PLAYER.player.ConsumeItem(Unlock_Item.item.type);

            //unlock class
            info.Unlock(true);

            return true;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Local Setting Active Class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Return from LocalCheckClassValid
        /// </summary>
        private enum CLASS_VALIDITY : byte {
            VALID,
            INVALID_UNKNOWN,
            INVALID_LOCKED,
            INVALID_COMBINATION,
            INVALID_MINIONS,
            INVALID_COMBAT,
        }

        /// <summary>
        /// Check if switch to this class would be valid
        /// </summary>
        /// <param name="is_primary"></param>
        /// <returns></returns>
        private CLASS_VALIDITY LocalCheckClassValid(bool is_primary) {
            PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;

            if (psheet.Character.In_Combat) {
                return CLASS_VALIDITY.INVALID_COMBAT;
            }
            else if (ID_num == (byte)Systems.PlayerClass.IDs.None) {
                return CLASS_VALIDITY.VALID; //setting to no class is always allowed (unless in combat)
            }
            else {
                Systems.PlayerClass class_same_slot, class_other_slot;
                if (is_primary) {
                    class_same_slot = psheet.Classes.Primary.Class;
                    class_other_slot = psheet.Classes.Secondary.Class;
                }
                else {
                    class_same_slot = psheet.Classes.Secondary.Class;
                    class_other_slot = psheet.Classes.Primary.Class;
                }

                PlayerSheet.ClassSheet.ClassInfo info = psheet.Classes.GetClassInfo(ID_num);
                if (((info.Level <= 0) || !info.Unlocked || !Enabled) && (ID_num != (byte)Systems.PlayerClass.IDs.None)) {
                    return CLASS_VALIDITY.INVALID_LOCKED; //locked class
                }
                else {
                    if (ID_num != class_same_slot.ID_num) {
                        Systems.PlayerClass pre = class_other_slot;
                        while (pre != null) {
                            if (ID_num == pre.ID_num) {
                                return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                            }
                            else {
                                pre = pre.Prereq;
                            }
                        }
                        pre = Systems.PlayerClass.LOOKUP[ID_num].Prereq;
                        while (pre != null) {
                            if (class_other_slot.ID_num == pre.ID_num) {
                                return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                            }
                            else {
                                pre = pre.Prereq;
                            }
                        }

                        //valid choice
                        return CLASS_VALIDITY.VALID;
                    }
                }
                //default
                return CLASS_VALIDITY.INVALID_UNKNOWN;
            }
        }

        /// <summary>
        /// Set local class (with checks + updates) (called by UI)
        /// </summary>
        /// <param name="is_primary"></param>
        /// <returns></returns>
        public bool LocalTrySetClass(bool is_primary) {
            PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;

            //fail if secondary not allowed
            if (!is_primary && !psheet.Character.Secondary_Unlocked) {
                Main.NewText("Failed to set class because multiclassing is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            byte id_other;
            if (is_primary) {
                id_other = psheet.Classes.Secondary.Class.ID_num;
            }
            else {
                id_other = psheet.Classes.Primary.Class.ID_num;
            }
            if ((ID_num == id_other) && (ID_num != (byte)Systems.PlayerClass.IDs.None)) {
                //if setting to other set class, just swap
                if (is_primary)
                    psheet.Classes.SetPrimary(ID_num, true, true);
                else
                    psheet.Classes.SetSecondary(ID_num, true, true);
                return true;
            }
            else {
                CLASS_VALIDITY valid = LocalCheckClassValid(is_primary);
                switch (valid) {
                    case Systems.PlayerClass.CLASS_VALIDITY.VALID:
                        if (is_primary)
                            psheet.Classes.SetPrimary(ID_num, true, true);
                        else
                            psheet.Classes.SetSecondary(ID_num, true, true);

                        return true;

                    case CLASS_VALIDITY.INVALID_COMBINATION:
                        //SetClass_Fail_Combination
                        Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.SetClass_Fail_Combination"), UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_LOCKED:
                        Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.SetClass_Fail_Locked"), UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_COMBAT:
                        Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.SetClass_Fail_Combat"), UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    default:
                        Utilities.Logger.Error("Failed to set class for unknown reasons! (please report)");
                        break;
                }

                //default
                return false;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Check if player meets extra requirements for tier 3
        /// </summary>
        /// <returns></returns>
        public static bool CanUnlockTier3(PSheet psheet) {
            return psheet.Character.Defeated_WOF;
        }

        /// <summary>
        /// Try to unlock subclassing (called from UI)
        /// </summary>
        public static void LocalTryUnlockSubclass() {
            //check locked
            if (Shortcuts.LOCAL_PLAYER.PSheet.Character.Secondary_Unlocked) {
                Utilities.Logger.Error("Trying to unlock multiclassing when already unlocked");
            }
            else {
                //item requirements
                Item item = ModContent.GetInstance<Items.Unlock_Subclass>().item;
                if (!Shortcuts.LOCAL_PLAYER.player.HasItem(item.type)) {
                    //item requirement not met
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_Multiclass_NoItem", item.Name), UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    //requirements met..

                    //take item
                    Shortcuts.LOCAL_PLAYER.player.ConsumeItem(item.type);

                    //unlock class
                    Shortcuts.LOCAL_PLAYER.PSheet.Character.UnlockSecondary();
                }
            }
        }

        //TODO - add set class

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
                XP_Multiplier_Combat = 0.1f;
                XP_Multiplier_NonCombat = 10.0f;
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

        public class Bard : Tier2 {
            public Bard() : base(IDs.Bard, Attribute.PowerScaling.IDs.Musical) {
                Class_Locations[0, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_MUSIC_2;
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

        public class Minstrel : Tier3 {
            public Minstrel() : base(IDs.Minstrel, Attribute.PowerScaling.IDs.Musical, IDs.Bard) {
                Class_Locations[0, 5] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2.5f;
                Colour = COLOUR_MUSIC_3;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

    }
}
