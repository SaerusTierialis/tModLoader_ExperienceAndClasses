using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            Placeholder,
            Saint,
            HybridPrime,
            Explorer,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        public const byte MAX_TIER = 3;
        public static readonly byte[] TIER_MAX_LEVELS = new byte[] {0, 10, 50, 100};

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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Class[] LOOKUP { get; private set; }

        //which classes to show in ui and where
        public static byte[,] Class_Locations { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static Class() {
            Class_Locations = new byte[5, 7];
            LOOKUP = new Class[(byte)Class.IDs.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Class>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; protected set; }
        public byte ID_num { get; protected set; }
        public string Name { get; protected set; } = "Undefined_Name";
        public string Description { get; protected set; } = "Undefined_Desc";
        public byte Tier { get; protected set; } = 0;
        public Texture2D Texture { get; protected set; }
        public Class Prereq { get; protected set; } = null;
        public PowerScaling Power_Scaling { get; protected set; } = PowerScaling.LOOKUP[(byte)PowerScaling.IDs.None];
        public float[] Attribute_Growth { get; protected set; } = Enumerable.Repeat(1f, (byte)Attribute.IDs.NUMBER_OF_IDs).ToArray();
        public bool Gives_Allocation_Attributes { get; protected set; } = false;
        public byte Max_Level { get; protected set; } = 0;
        public Items.Unlock Unlock_Item { get; protected set; } = null;
        public bool Has_Texture { get; protected set; } = false;
        public bool Enabled { get; protected set; } = false;
        public Systems.Ability.IDs[] AbilitiesID { get; protected set; } = Enumerable.Repeat(Systems.Ability.IDs.NONE, ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS).ToArray();
        public Systems.Ability.IDs[] AbilitiesID_Alt { get; protected set; } = Enumerable.Repeat(Systems.Ability.IDs.NONE, ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS).ToArray();
        public Color Colour { get; protected set; } = COLOUR_DEFAULT;

        public Class(IDs id) {
            //defaults
            ID = id;
            ID_num = (byte)id;
        }

        public void LoadTexture() {
            if (Has_Texture) {
                Texture = ModLoader.GetTexture("ExperienceAndClasses/Textures/Class/" + Name);
            }
            else {
                //no texture loaded, set blank
                Texture = Utilities.Textures.TEXTURE_CLASS_DEFAULT;
            }
        }

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
            if (ExperienceAndClasses.LOCAL_MPLAYER.IN_COMBAT) {
                return CLASS_VALIDITY.INVALID_COMBAT;
            }
            else if (ID_num == (byte)Systems.Class.IDs.None) {
                return CLASS_VALIDITY.VALID; //setting to no class is always allowed (unless in combat)
            }
            else {
                Systems.Class class_same_slot, class_other_slot;
                if (is_primary) {
                    class_same_slot = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary;
                    class_other_slot = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary;
                }
                else {
                    class_same_slot = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary;
                    class_other_slot = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary;
                }

                if (((ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num] <= 0) || !ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[ID_num]) && (ID_num != (byte)Systems.Class.IDs.None)) {
                    return CLASS_VALIDITY.INVALID_LOCKED; //locked class
                }
                else {
                    if (ID_num != class_same_slot.ID_num) {
                        Systems.Class pre = class_other_slot;
                        while (pre != null) {
                            if (ID_num == pre.ID_num) {
                                return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                            }
                            else {
                                pre = pre.Prereq;
                            }
                        }
                        pre = Systems.Class.LOOKUP[ID_num].Prereq;
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
        /// Set local class (with checks + updates)
        /// </summary>
        /// <param name="is_primary"></param>
        /// <returns></returns>
        public bool LocalTrySetClass(bool is_primary) {
            //fail if secondary not allowed
            if (!is_primary && !ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary) {
                Main.NewText("Failed to set class because multiclassing is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            byte id_other;
            if (is_primary) {
                id_other = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num;
            }
            else {
                id_other = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num;
            }
            if ((ID_num == id_other) && (ID_num != (byte)Systems.Class.IDs.None)) {
                //if setting to other set class, just swap
                LocalSwapClass();
                return true;
            }
            else {
                CLASS_VALIDITY valid = LocalCheckClassValid(is_primary);
                switch (valid) {
                    case Systems.Class.CLASS_VALIDITY.VALID:

                        //destroy all minions
                        ExperienceAndClasses.LOCAL_MPLAYER.CheckMinions();
                        if (ExperienceAndClasses.LOCAL_MPLAYER.minions.Count > 0) {
                            Main.NewText("Your minions have been despawned because you changed classes!", UI.Constants.COLOUR_MESSAGE_ERROR);
                            foreach (Projectile p in ExperienceAndClasses.LOCAL_MPLAYER.minions) {
                                p.Kill();
                            }
                        }

                        MPlayer.LocalForceClass(this, is_primary);
                        return true;

                    case CLASS_VALIDITY.INVALID_COMBINATION:
                        Main.NewText("Failed to set class because combination is invalid!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_LOCKED:
                        Main.NewText("Failed to set class because it is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_COMBAT:
                        Main.NewText("Failed to set class because you are in combat!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    default:
                        Utilities.Commons.Error("Failed to set class for unknown reasons! (please report)");
                        break;
                }

                //default
                return false;
            }
        }

        /// <summary>
        /// Swap local player's primary and secondary classes
        /// </summary>
        private static void LocalSwapClass() {
            MPlayer.LocalForceClasses(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary, ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary);
            MPlayer.LocalUpdateAll();
        }

        /// <summary>
        /// Try to unlock this class (called from UI)
        /// </summary>
        /// <returns></returns>
        public bool LocalTryUnlockClass() {
            //check locked
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[ID_num]) {
                Utilities.Commons.Error("Trying to unlock already unlocked class " + Name);
                return false;
            }

            //tier 3 requirement
            if (Tier == 3 && !LocalCanUnlockTier3()) {
                if (!ExperienceAndClasses.LOCAL_MPLAYER.Defeated_WOF) {
                    Main.NewText("You must defeat the Wall of Flesh to unlock tier 3 classes!", UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Utilities.Commons.Error("LocalCanUnlockTier3 returned false for unknown reasons! Please Report!");
                }
                return false;
            }

            //level requirements
            if (!LocalHasClassPrereq()) {
                Main.NewText("You must reach level " + Prereq.Max_Level + " " + Prereq.Name + " to unlock " + Name + "!", UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            //item requirements
            if (Unlock_Item != null) {
                if (!ExperienceAndClasses.LOCAL_MPLAYER.player.HasItem(Unlock_Item.item.type)) {
                    //item requirement not met
                    Main.NewText("You require a " + Unlock_Item.item.Name + " to unlock " + Name + "!", UI.Constants.COLOUR_MESSAGE_ERROR);
                    return false;
                }
            }

            //requirements met..

            //take item
            ExperienceAndClasses.LOCAL_MPLAYER.player.ConsumeItem(Unlock_Item.item.type);

            //unlock class
            ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[ID_num] = true;
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num] < 1) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num] = 1;
            }

            //success
            Main.NewText("You have unlocked " + Name + "!", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);

            //add extra xp (after penalty)
            uint extra_xp_add = (uint)(ExperienceAndClasses.LOCAL_MPLAYER.extra_xp * Systems.XP.EXTRA_XP_POOL_MULTIPLIER);
            if (extra_xp_add > 0) {
                //add xp
                Systems.XP.Adjusting.LocalAddXPToClass(ID_num, extra_xp_add);

                //clear pool
                ExperienceAndClasses.LOCAL_MPLAYER.extra_xp = 0;

                //levelup?
                LocalCheckDoLevelup();

                //tell player
                Main.NewText(extra_xp_add + " unclaimed XP has been transferred to " + Name + "!", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
            }

            //update
            MPlayer.LocalUpdateAll();

            return true;
        }

        /// <summary>
        /// Do any levelups on this class for local player
        /// </summary>
        /// <param name="announce"></param>
        /// <returns></returns>
        public bool LocalCheckDoLevelup(bool announce = true) {
            uint xp_req;
            bool any_levels = false;
            while (ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num] < Max_Level) {
                xp_req = Systems.XP.Requirements.GetXPReq(this, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num]);
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[ID_num] < xp_req) {
                    break;
                }
                else {
                    Systems.XP.Adjusting.LocalSubtractXPFromClass(ID_num, xp_req);
                    ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num]++;
                    if (announce) {
                        LocalAnnounceLevel();
                    }
                    any_levels = true;
                }
            }
            return any_levels;
        }

        /// <summary>
        /// Display levelup text
        /// </summary>
        private void LocalAnnounceLevel() {
            //client/singleplayer only
            if (!Utilities.Netmode.IS_SERVER) {
                byte level = ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ID_num];

                string message = "";
                if (level == Max_Level) {
                    message = "You are now a MAX level " + Name + "!";
                }
                else {
                    message = "You are now a level " + level + " " + Name + "!";
                }

                Main.NewText(message, UI.Constants.COLOUR_MESSAGE_ANNOUNCE);

                if ((level == Max_Level) && (Tier < MAX_TIER)) {
                    Main.NewText("Tier " + new string('I', Tier + 1) + " Requirement Met: " + Name + " Level " + Max_Level, UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
            }
        }

        /// <summary>
        /// Check if local player meets class prereqs
        /// </summary>
        /// <returns></returns>
        public bool LocalHasClassPrereq() {
            Systems.Class pre = Prereq;
            while (pre != null) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[pre.ID_num] < pre.Max_Level) {
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
        /// Try to unlock subclassing (called from UI)
        /// </summary>
        public static void LocalTryUnlockSubclass() {
            //check locked
            if (ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary) {
                Utilities.Commons.Error("Trying to unlock multiclassing when already unlocked");
            }
            else {
                //item requirements
                Item item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Subclass>().item;
                if (!ExperienceAndClasses.LOCAL_MPLAYER.player.HasItem(item.type)) {
                    //item requirement not met
                    Main.NewText("You require a " + item.Name + " to unlock multiclassing!", UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    //requirements met..

                    //take item
                    ExperienceAndClasses.LOCAL_MPLAYER.player.ConsumeItem(item.type);

                    //unlock class
                    ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary = true;

                    //update
                    MPlayer.LocalUpdateAll();

                    //success
                    Main.NewText("You can now multiclass! Right click a class to set it as your subclass.", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
                }
            }
        }

        /// <summary>
        /// Returns class abilities in array the size of NUMBER_ABILITY_SLOTS_PER_CLASS.
        /// Also updates the passives and unlock status of returned abilities.
        /// </summary>
        /// <param name="alternate"></param>
        /// <returns></returns>
        public Systems.Ability[] GetAbilities(bool primary, bool alternate) {
            Systems.Ability.IDs id;
            Systems.Ability[] abilities = new Systems.Ability[ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS];
            for (int i = 0; i < AbilitiesID.Length; i++) {
                if (alternate) {
                    id = AbilitiesID_Alt[i];
                }
                else {
                    id = AbilitiesID[i];
                }

                if (id != Systems.Ability.IDs.NONE) {
                    abilities[i] = Systems.Ability.LOOKUP[(ushort)id];
                    abilities[i].UpdatePassives();
                    abilities[i].UpdateUnlock();
                    abilities[i].AddHotkeyData(primary, alternate, i);
                }
            }
            return abilities;
        }

        /// <summary>
        /// Check if local player meets extra requirements for tier 3
        /// </summary>
        /// <returns></returns>
        public static bool LocalCanUnlockTier3() {
            return ExperienceAndClasses.LOCAL_MPLAYER.Defeated_WOF;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Subtypes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public abstract class RealClass : Class {
            public RealClass(IDs id, PowerScaling.IDs power_scaling) : base(id) {
                Gives_Allocation_Attributes = true;
                Power_Scaling = PowerScaling.LOOKUP[(byte)power_scaling];
                Has_Texture = true;
                Enabled = true;
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
                AbilitiesID[0] = Prereq.AbilitiesID[0];
                AbilitiesID[1] = Prereq.AbilitiesID[1];
                AbilitiesID_Alt[0] = Prereq.AbilitiesID_Alt[0];
                AbilitiesID_Alt[1] = Prereq.AbilitiesID_Alt[1];
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Special Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class New : Class {
            public New() : base(IDs.New) {
                Enabled = true;
            }
        }

        public class None : Class {
            public None() : base(IDs.None) {
                Name = "None";
                Description = "";
                Enabled = true;
            }
        }

        public class Explorer : Tier2 {
            public Explorer() : base(IDs.Explorer, PowerScaling.IDs.NonCombat) {
                Name = "Explorer";
                Description = "TODO_desc";
                Max_Level = TIER_MAX_LEVELS[3]; //tier 2 class with tier 3 level cap
                Unlock_Item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Explorer>();
                Class_Locations[0, 6] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2f;
                Colour = COLOUR_NONCOMBAT;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 1 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Novice : Tier1 {
            public Novice() : base(IDs.Novice, PowerScaling.IDs.AllCore) {
                Name = "Novice";
                Description = "TODO_desc";
                Class_Locations[0, 3] = ID_num;
                Colour = COLOUR_NOVICE;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 2 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Warrior : Tier2 {
            public Warrior() : base(IDs.Warrior, PowerScaling.IDs.CloseRangeMelee) {
                Name = "Warrior";
                Description = "TODO_desc";
                Class_Locations[1, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_CLOSE_RANGE_2;
                AbilitiesID[0] = Systems.Ability.IDs.Warrior_Block;
            }
        }

        public class Ranger : Tier2 {
            public Ranger() : base(IDs.Ranger, PowerScaling.IDs.Projectile) {
                Name = "Ranger";
                Description = "TODO_desc";
                Class_Locations[1, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_PROJECTILE_2;
            }
        }

        public class Traveler : Tier2 {
            public Traveler() : base(IDs.Traveler, PowerScaling.IDs.AllCore) {
                Name = "Traveler";
                Description = "TODO_desc";
                Class_Locations[1, 2] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Colour = COLOUR_UTILITY_2;
            }
        }

        public class Rogue : Tier2 {
            public Rogue() : base(IDs.Rogue, PowerScaling.IDs.CloseRangeAll) {
                Name = "Rogue";
                Description = "TODO_desc";
                Class_Locations[1, 3] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Colour = COLOUR_TRICKERY_2;
            }
        }

        public class Summoner : Tier2 {
            public Summoner() : base(IDs.Summoner, PowerScaling.IDs.MinionOnly) {
                Name = "Summoner";
                Description = "TODO_desc";
                Class_Locations[1, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Colour = COLOUR_MINION_2;
            }
        }

        public class Cleric : Tier2 {
            public Cleric() : base(IDs.Cleric, PowerScaling.IDs.Holy_AllCore) {
                Name = "Cleric";
                Description = "TODO_desc";
                Class_Locations[1, 5] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 3;
                Colour = COLOUR_SUPPORT_2;
            }
        }

        public class Hybrid : Tier2 {
            public Hybrid() : base(IDs.Hybrid, PowerScaling.IDs.AllCore) {
                Name = "Hybrid";
                Description = "TODO_desc";
                Class_Locations[1, 6] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_HYBRID_2;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Tier 3 Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class BloodKnight : Tier3 {
            public BloodKnight() : base(IDs.BloodKnight, PowerScaling.IDs.CloseRangeMelee, IDs.Warrior) {
                Name = "Blood Knight";
                Description = "TODO_desc";
                Class_Locations[2, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Colour = COLOUR_CLOSE_RANGE_3;
            }
        }

        public class Berserker : Tier3 {
            public Berserker() : base(IDs.Berserker, PowerScaling.IDs.CloseRangeMelee, IDs.Warrior) {
                Name = "Berserker";
                Description = "TODO_desc";
                Class_Locations[3, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 4;
                Colour = COLOUR_CLOSE_RANGE_3;
            }
        }

        public class Guardian : Tier3 {
            public Guardian() : base(IDs.Guardian, PowerScaling.IDs.CloseRangeMelee, IDs.Warrior) {
                Name = "Guardian";
                Description = "TODO_desc";
                Class_Locations[4, 0] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 5;
                Colour = COLOUR_CLOSE_RANGE_3;
            }
        }

        public class Artillery : Tier3 {
            public Artillery() : base(IDs.Artillery, PowerScaling.IDs.Projectile, IDs.Ranger) {
                Name = "Artillery";
                Description = "TODO_desc";
                Class_Locations[2, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Colour = COLOUR_PROJECTILE_3;
            }
        }

        public class Tinkerer : Tier3 {
            public Tinkerer() : base(IDs.Tinkerer, PowerScaling.IDs.ProjectileMinion, IDs.Ranger) {
                Name = "Tinkerer";
                Description = "TODO_desc";
                Class_Locations[3, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Colour = COLOUR_PROJECTILE_3;
            }
        }

        public class Controller : Tier3 {
            public Controller() : base(IDs.Controller, PowerScaling.IDs.AllCore, IDs.Traveler) {
                Name = "Controller";
                Description = "TODO_desc";
                Class_Locations[2, 2] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Colour = COLOUR_UTILITY_3;
            }
        }

        public class Shadow : Tier3 {
            public Shadow() : base(IDs.Shadow, PowerScaling.IDs.AllCore, IDs.Traveler) {
                Name = "Shadow";
                Description = "TODO_desc";
                Class_Locations[3, 3] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 4;
                Colour = COLOUR_TRICKERY_3;
            }
        }

        public class Assassin : Tier3 {
            public Assassin() : base(IDs.Assassin, PowerScaling.IDs.CloseRangeAll, IDs.Rogue) {
                Name = "Assassin";
                Description = "TODO_desc";
                Class_Locations[2, 3] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 3;
                Colour = COLOUR_TRICKERY_3;
            }
        }

        public class Chrono : Tier3 {
            public Chrono() : base(IDs.Chrono, PowerScaling.IDs.Projectile, IDs.Ranger) {
                Name = "Chrono";
                Description = "TODO_desc";
                Class_Locations[4, 1] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 5;
                Colour = COLOUR_PROJECTILE_3;
            }
        }

        public class SoulBinder : Tier3 {
            public SoulBinder() : base(IDs.SoulBinder, PowerScaling.IDs.MinionOnly, IDs.Summoner) {
                Name = "Soul Binder";
                Description = "TODO_desc";
                Class_Locations[2, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 5;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2;
                Colour = COLOUR_MINION_3;
            }
        }

        public class Placeholder : Tier3 {
            public Placeholder() : base(IDs.Placeholder, PowerScaling.IDs.MinionOnly, IDs.Summoner) {
                Name = "Placeholder";
                Description = "TODO_desc";
                Class_Locations[3, 4] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 4;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 4;
                Colour = COLOUR_MINION_3;
            }
        }

        public class Saint : Tier3 {
            public Saint() : base(IDs.Saint, PowerScaling.IDs.Holy_AllCore, IDs.Cleric) {
                Name = "Saint";
                Description = "TODO_desc";
                Class_Locations[2, 5] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 3;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 5;
                Colour = COLOUR_SUPPORT_3;
            }
        }

        public class HybridPrime : Tier3 {
            public HybridPrime() : base(IDs.HybridPrime, PowerScaling.IDs.AllCore, IDs.Hybrid) {
                Name = "Hybrid Prime";
                Description = "TODO_desc";
                Class_Locations[2, 6] = ID_num;
                Attribute_Growth[(byte)Attribute.IDs.Power] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Vitality] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Mind] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Spirit] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Agility] = 2.5f;
                Attribute_Growth[(byte)Attribute.IDs.Dexterity] = 2.5f;
                Colour = COLOUR_HYBRID_3;
            }
        }

    }
}
