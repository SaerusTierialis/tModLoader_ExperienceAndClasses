using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class PlayerClass {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum IDs : byte {
            New, //no longer used
            None, //no class selected (intentionally)
            Novice,
            Vanguard,
            EagleEye,
            Windwalker,
            Rogue,
            Summoner,
            Eclipse,
            Hybrid,
            BloodKnight,
            Berserker,
            Guardian,
            ProjTurretClass,
            Sharpshooter,
            Chrono,
            ForceSeer,
            Shadow,
            Assassin,
            SoulBinder,
            Tactician,
            Penumbra,
            HybridPrime,
            Tinkerer,
            Oracle,
            Bard,
            Minstrel,
            Engineer,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        public enum IMPLEMENTATION_STATUS : byte {
            UNKNOWN,
            ATTRIBUTE_ONLY,
            ATTRIBUTE_PLUS_PARTIAL_ABILITY,
            COMPLETE,
        }

        public enum RECOMMENDED_WEAPON : byte {
            UNKNOWN,
            ANY,
            MINION,
            NON_MINION,
            PROJECTILE,
        }

        public const byte MAX_TIER = 3;
        public static readonly byte[] MAX_TIER_LEVEL = new byte[] { 0, 10, 50, 100 };

        public readonly static PlayerClass[] LOOKUP;

        //which classes to show in ui and where
        public readonly static byte[,] Class_Locations;

        public readonly static byte Count = (byte)IDs.NUMBER_OF_IDs;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static PlayerClass() {
            Class_Locations = new byte[5, 9];
            LOOKUP = new PlayerClass[(byte)PlayerClass.IDs.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<PlayerClass>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Fields ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public readonly IDs ID;
        public readonly byte ID_num;

        private string INTERNAL_NAME;

        public string Tooltip_Attribute_Growth { get; private set; }

        public byte Tier { get; protected set; } = 0;
        public byte Max_Level { get; protected set; } = 0;

        public PlayerClass Prereq { get; protected set; } = null;
        public Items.Unlock Unlock_Item { get; protected set; } = null;

        public bool Gives_Allocation_Attributes { get; protected set; } = false;
        public float[] Attribute_Growth { get; protected set; } = Enumerable.Repeat(1f, (byte)Attribute.IDs.NUMBER_OF_IDs).ToArray();

        public bool Enabled { get; protected set; } = false;

        public bool Has_Texture { get; protected set; } = false;
        public Texture2D Texture { get; protected set; }

        public readonly PlayerClassCategory Category;

        protected IMPLEMENTATION_STATUS implementation_status = IMPLEMENTATION_STATUS.UNKNOWN;

        public Ability[] Abilities = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
        public Ability[] Abilities_Alt = new Ability[Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public PlayerClass(IDs id, PlayerClassCategory.TYPES category = PlayerClassCategory.TYPES.Novice) {
            //defaults
            ID = id;
            ID_num = (byte)id;
            Category = PlayerClassCategory.LOOKUP[(byte)category];

            INTERNAL_NAME = Enum.GetName(typeof(IDs), ID_num);
        }

        public string Name { get; private set; } = "?";
        public string Description { get; private set; } = "?";

        public Color Colour { get
            {
                if (Tier == 0)
                    return PlayerClassCategory.COLOUR_DEFAULT;
                else
                    return Category.Colours[Tier - 1];
            }
        }

        public string Tooltip_Title {
            get {
                return Name + " [" + Category.Name + "] [Tier " + new string('I', Tier) + "]";
            }
        }

        public void LoadLocalizedText()
        {
            Name = Language.GetTextValue("Mods.ExperienceAndClasses.Common.Class_" + INTERNAL_NAME + "_Name");
            Description = Language.GetTextValue("Mods.ExperienceAndClasses.Common.Class_" + INTERNAL_NAME + "_Description");
        }

        public void LoadTextureAndItem() {
            if (!Enabled)
                return;

            //load texture
            if (Has_Texture) {
                Texture = ModContent.GetTexture("ExperienceAndClasses/Textures/Class/" + INTERNAL_NAME);
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

            Unlock_Item = GetUnlockItem();
        }

        protected virtual Items.Unlock GetUnlockItem() {
            return null;
        }

        public string Tooltip_Main {
            get {
                //implementation status
                string implementation_status_text = "";
                if (implementation_status != IMPLEMENTATION_STATUS.COMPLETE)
                {
                    implementation_status_text = "WARNING: The implementation status for this class is ";
                    switch (implementation_status)
                    {
                        case IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY:
                            implementation_status_text += "attributes only";
                            break;

                        case IMPLEMENTATION_STATUS.ATTRIBUTE_PLUS_PARTIAL_ABILITY:
                            implementation_status_text += "some abilities/passives";
                            break;

                        case IMPLEMENTATION_STATUS.UNKNOWN:
                        default:
                            implementation_status_text += "unknown";
                            break;
                    }
                    implementation_status_text += "\n\n";
                }

                //set tooltip
                string tooltip_main = implementation_status_text + Description + "\n\n" + Category.Recommended_Weapon + "\n\nAttribute Bonus Per " + Attribute.LEVELS_PER_ATTRIBUTE_POINT_PER_STAR + " Levels (★ = 1 point):";
                bool first = true;
                string attribute_names = "";
                foreach (byte id in Systems.Attribute.ATTRIBUTES_UI_ORDER) {
                    if (first) {
                        first = false;
                    }
                    else {
                        attribute_names += "\n";
                    }
                    attribute_names += Systems.Attribute.LOOKUP[id].Name + ":";
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
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_Class_Fail_Item", Unlock_Item.item.Name, Name), UI.Constants.COLOUR_MESSAGE_ERROR);
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
                    if (ID_num == class_same_slot.ID_num) {
                        //toggling off a class
                        return CLASS_VALIDITY.VALID;
                    }
                    else {
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
            public RealClass(IDs id, PlayerClassCategory.TYPES category) : base(id, category) {
                Gives_Allocation_Attributes = true;
                Has_Texture = true;
                Enabled = true;
            }
        }

        public abstract class Tier1 : RealClass {
            public Tier1(IDs id, PlayerClassCategory.TYPES category) : base(id, category) {
                Tier = 1;
                Max_Level = MAX_TIER_LEVEL[Tier];
            }
        }

        public abstract class Tier2 : RealClass {
            public Tier2(IDs id, PlayerClassCategory.TYPES category) : base(id, category) {
                Tier = 2;
                Max_Level = MAX_TIER_LEVEL[Tier];
                Prereq = LOOKUP[(byte)IDs.Novice];
            }

            protected override Items.Unlock GetUnlockItem() {
                return ModContent.GetInstance<Items.Unlock_Tier2>();
            }
        }

        public abstract class Tier3 : RealClass {
            public Tier3(IDs id, IDs prereq) : base(id, LOOKUP[(byte)prereq].Category.ID) {
                Tier = 3;
                Max_Level = MAX_TIER_LEVEL[Tier];
                Prereq = LOOKUP[(byte)prereq];
            }

            protected override Items.Unlock GetUnlockItem() {
                return ModContent.GetInstance<Items.Unlock_Tier3>();
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 1 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Novice : Tier1 {
            public Novice() : base(IDs.Novice, PlayerClassCategory.TYPES.Novice) {
                Class_Locations[0, 4] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.COMPLETE;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 1;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 2 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Vanguard : Tier2 {
            public Vanguard() : base(IDs.Vanguard, PlayerClassCategory.TYPES.CloseCombat) {
                Class_Locations[1, 0] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 1;
            }
        }

        public class EagleEye : Tier2 {
            public EagleEye() : base(IDs.EagleEye, PlayerClassCategory.TYPES.Projectile) {
                Class_Locations[1, 1] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Windwalker : Tier2 {
            public Windwalker() : base(IDs.Windwalker, PlayerClassCategory.TYPES.Control) {
                Class_Locations[1, 2] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
            }
        }

        public class Rogue : Tier2 {
            public Rogue() : base(IDs.Rogue, PlayerClassCategory.TYPES.Stealth) {
                Class_Locations[1, 3] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 1;
            }
        }

        public class Summoner : Tier2 {
            public Summoner() : base(IDs.Summoner, PlayerClassCategory.TYPES.Minion) {
                Class_Locations[1, 4] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 0;
            }
        }

        public class Eclipse : Tier2 {
            public Eclipse() : base(IDs.Eclipse, PlayerClassCategory.TYPES.Eclipse) {
                Class_Locations[1, 5] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Bard : Tier2 {
            public Bard() : base(IDs.Bard, PlayerClassCategory.TYPES.Musical) {
                Class_Locations[1, 6] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Tinkerer : Tier2 {
            public Tinkerer() : base(IDs.Tinkerer, PlayerClassCategory.TYPES.Mechanical) {
                Class_Locations[1, 7] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Hybrid : Tier2 {
            public Hybrid() : base(IDs.Hybrid, PlayerClassCategory.TYPES.Hybrid) {
                Class_Locations[1, 8] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 3 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class BloodKnight : Tier3 {
            public BloodKnight() : base(IDs.BloodKnight, IDs.Vanguard) {
                
                //DISABLED
                //Class_Locations[3, 0] = ID_num;
                Enabled = false;

                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class Berserker : Tier3 {
            public Berserker() : base(IDs.Berserker, IDs.Vanguard) {
                Class_Locations[3, 0] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 5;
            }
        }

        public class Guardian : Tier3 {
            public Guardian() : base(IDs.Guardian, IDs.Vanguard) {
                Class_Locations[2, 0] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Sharpshooter : Tier3 {
            public Sharpshooter() : base(IDs.Sharpshooter, IDs.EagleEye) {
                Class_Locations[2, 1] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
            }
        }

        public class Chrono : Tier3 {
            public Chrono() : base(IDs.Chrono, IDs.EagleEye) {
                Class_Locations[3, 1] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 5;
            }
        }

        public class ProjTurretClass : Tier3 {
            public ProjTurretClass() : base(IDs.ProjTurretClass, IDs.EagleEye) {

                //DISABLED
                //Class_Locations[3, 1] = ID_num;
                Enabled = false;

                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;
            }
        }

        public class ForceSeer : Tier3 {
            public ForceSeer() : base(IDs.ForceSeer, IDs.Windwalker) {
                Class_Locations[2, 2] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 5;
            }
        }

        public class Shadow : Tier3 {
            public Shadow() : base(IDs.Shadow, IDs.Rogue) {
                Class_Locations[3, 3] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
            }
        }

        public class Assassin : Tier3 {
            public Assassin() : base(IDs.Assassin, IDs.Rogue) {
                Class_Locations[2, 3] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 1;
            }
        }

        public class SoulBinder : Tier3 {
            public SoulBinder() : base(IDs.SoulBinder, IDs.Summoner) {
                Class_Locations[2, 4] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 0;
            }
        }

        public class Tactician : Tier3 {
            public Tactician() : base(IDs.Tactician, IDs.Summoner) {
                Class_Locations[3, 4] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 1;
            }
        }

        public class Penumbra : Tier3 {
            public Penumbra() : base(IDs.Penumbra, IDs.Eclipse) {
                Class_Locations[2, 5] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class Oracle : Tier3 {
            public Oracle() : base(IDs.Oracle, IDs.Eclipse) {
                Class_Locations[3, 5] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 0;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
            }
        }

        public class Minstrel : Tier3 {
            public Minstrel() : base(IDs.Minstrel, IDs.Bard) {
                Class_Locations[2, 6] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;

            }
        }

        public class Engineer : Tier3 {
            public Engineer() : base(IDs.Engineer, IDs.Tinkerer) {
                Class_Locations[2, 7] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 1;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
            }
        }

        public class HybridPrime : Tier3 {
            public HybridPrime() : base(IDs.HybridPrime, IDs.Hybrid) {
                Class_Locations[2, 8] = ID_num;
                implementation_status = IMPLEMENTATION_STATUS.ATTRIBUTE_ONLY;

                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
            }
        }

    }
}
