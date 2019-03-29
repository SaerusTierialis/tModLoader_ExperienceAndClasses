using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses {
    public class MPlayer : ModPlayer {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const long TICKS_PER_FULL_SYNC = TimeSpan.TicksPerMinute * 2;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Vars ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static DateTime time_next_full_sync;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (non-syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public bool Is_Local_Player { get; private set; }

        /// <summary>
        /// Mod version loaded from
        /// </summary>
        public int[] Load_Version { get; private set; }

        /// <summary>
        /// Track wall of flesh progress for tier 3 unlock
        /// </summary>
        public bool Defeated_WOF { get; private set; } //TODO use this to limit tier 3 unlock

        /// <summary>
        /// Can have a secondary class active
        /// </summary>
        public bool Allow_Secondary { get; private set; }

        /// <summary>
        /// Set true for local player during OnEnterWorld. Set true for non-local players when first full sync is recieved.
        /// </summary>
        public bool initialized;

        /// <summary>
        /// Show xp gain overhead
        /// </summary>
        private bool show_xp;

        public byte[] Class_Levels { get; private set; }
        public uint[] Class_XP { get; private set; }
        public bool[] Class_Unlocked { get; private set; }

        /// <summary>
        /// Earning XP when all active classes are maxed stores the extra here
        /// </summary>
        public uint Extra_XP { get; private set; }

        /// <summary>
        /// Base values from current classes
        /// </summary>
        public int[] Attributes_Base { get; private set; }

        /// <summary>
        /// Allocated points
        /// </summary>
        public int[] Attributes_Allocated { get; private set; }

        /// <summary>
        /// Bonus points from statuses, etc.
        /// </summary>
        public int[] Attributes_Bonus { get; private set; }

        /// <summary>
        /// Available allocation points
        /// </summary>
        public int Allocation_Points_Unallocated { get; private set; }

        /// <summary>
        /// Spent allocation points
        /// </summary>
        private int Allocation_Points_Spent;

        /// <summary>
        /// Total allocation points
        /// </summary>
        private int Allocation_Points_Total;

        public float heal_damage; //TODO
        public float dodge_chance; //TODO
        public float ability_delay_reduction; //TODO
        public float use_speed_melee, use_speed_ranged, use_speed_magic, use_speed_throwing, use_speed_minion, use_speed_tool;
        public float tool_power;

        /// <summary>
        /// List of minions including sentries. Includes each part of multi-part minions. Updates on CheckMinions().
        /// </summary>
        public List<Projectile> minions { get; private set;  }

        /// <summary>
        /// Container of statuses on this player
        /// </summary>
        public Utilities.Containers.StatusList Statuses { get; private set; }

        public Utilities.Containers.LoadedUIData loaded_ui_main, loaded_ui_hud;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Systems.Class Class_Primary { get; private set; }
        public Systems.Class Class_Secondary { get; private set; }
        public byte Class_Primary_Level_Effective { get; private set; }
        public byte Class_Secondary_Level_Effective { get; private set; }

        /// <summary>
        /// A value summarizing overall progress used to scale orb drops and value
        /// </summary>
        public int Progression { get; private set; }

        public int[] Attributes_Final { get; private set; }
        public bool AFK { get; private set; } //TODO local set
        public bool IN_COMBAT { get; private set; } //TODO local set

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// instanced arrays must be initialized here (also called during cloning, etc)
        /// </summary>
        public override void Initialize() {
            //defaults
            Is_Local_Player = false;
            Allow_Secondary = false;
            initialized = false;
            Load_Version = new int[3];
            AFK = false;
            IN_COMBAT = false;
            Defeated_WOF = false;
            show_xp = true;
            minions = new List<Projectile>();
            Statuses = new Utilities.Containers.StatusList();
            Progression = 0;
            Extra_XP = 0;

            //ui
            loaded_ui_main = new Utilities.Containers.LoadedUIData();
            loaded_ui_hud = new Utilities.Containers.LoadedUIData();

            //default level/xp/unlock
            Class_Levels = new byte[(byte)Systems.Class.IDs.NUMBER_OF_IDs];
            Class_XP = new uint[(byte)Systems.Class.IDs.NUMBER_OF_IDs];
            Class_Unlocked = new bool[(byte)Systems.Class.IDs.NUMBER_OF_IDs];

            //default unlocks
            Class_Levels[(byte)Systems.Class.IDs.Novice] = 1;
            Class_Unlocked[(byte)Systems.Class.IDs.New] = true;
            Class_Unlocked[(byte)Systems.Class.IDs.None] = true;
            Class_Unlocked[(byte)Systems.Class.IDs.Novice] = true;

            //default class selection
            Class_Primary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.Novice];
            Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];

            //initialize attributes
            Attributes_Base = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Allocated = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Bonus = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Final = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Allocation_Points_Unallocated = 0;
            Allocation_Points_Spent = 0;
            Allocation_Points_Total = 0;

            //stats
            heal_damage = 1f;
            dodge_chance = 0f;
            use_speed_melee = use_speed_ranged = use_speed_magic = use_speed_throwing = use_speed_minion = use_speed_tool = 1f;
            ability_delay_reduction = 1f;
            tool_power = 1f;
        }

        /// <summary>
        /// player enters (not other players)
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player) {
            base.OnEnterWorld(player);
            if (!Utilities.Netmode.IS_SERVER) {
                //this is the current local player
                Is_Local_Player = true;
                ExperienceAndClasses.LOCAL_MPLAYER = this;

                //singleplayer or client?
                Utilities.Netmode.UpdateNetmode();

                //start timer for next full sync
                time_next_full_sync = DateTime.Now.AddTicks(TICKS_PER_FULL_SYNC);

                //grab UI-state combos to display
                ExperienceAndClasses.UIs = new UI.UIStateCombo[] { UI.UIStatus.Instance, UI.UIHUD.Instance, UI.UIMain.Instance, UI.UIInfo.Instance };

                //(re)initialize ui
                foreach (UI.UIStateCombo ui in ExperienceAndClasses.UIs) {
                    ui.Initialize();
                }

                //temp: show bars and status //TODO something here?
                UI.UIHUD.Instance.Visibility = true;
                UI.UIStatus.Instance.Visibility = true;

                //apply ui auto
                ExperienceAndClasses.SetUIAutoStates();

                //update class info
                LocalUpdateClassInfo();

                //initialized
                initialized = true;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PreUpdate() {
            base.PreUpdate();
            if (initialized) {
                //defaults before update
                heal_damage = 1f;
                dodge_chance = 0f;
                use_speed_melee = use_speed_ranged = use_speed_magic = use_speed_throwing = use_speed_minion = use_speed_tool = 1f;
                ability_delay_reduction = 1f;
                tool_power = 1f;
            }

            //Systems.Status.Heal.Add(player, this, 10);
        }

        public override void PostUpdateEquips() {
            base.PostUpdateEquips();
            if (initialized) {
                ApplyAttributes();
                UpdateStatus();
            }
        }

        public override void PostUpdate() {
            base.PostUpdate();

            //timed events...
            DateTime now = DateTime.Now;

            //server/singleplayer
            if (!Utilities.Netmode.IS_CLIENT) {

                

            }

            //local events
            if (Is_Local_Player) {
                //ui
                UI.UIStatus.Instance.Update();
                
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP & Class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum CLASS_VALIDITY : byte {
            VALID,
            INVALID_UNKNOWN,
            INVALID_ID,
            INVALID_LOCKED,
            INVALID_COMBINATION,
            INVALID_NON_LOCAL,
            INVALID_MINIONS,
            INVALID_COMBAT,
        }
        public CLASS_VALIDITY LocalCheckClassValid(byte id, bool is_primary) {
            //local MPlayer only
            if (!Is_Local_Player) return CLASS_VALIDITY.INVALID_NON_LOCAL;

            if (IN_COMBAT) {
                return CLASS_VALIDITY.INVALID_COMBAT;
            }
            else if (id == (byte)Systems.Class.IDs.None) {
                return CLASS_VALIDITY.VALID; //setting to no class is always allowed (unless in combat)
            }
            else {
                if (id >= (byte)Systems.Class.IDs.NUMBER_OF_IDs) {
                    return CLASS_VALIDITY.INVALID_ID; //invalid idsss
                }
                else {
                    Systems.Class class_same_slot, class_other_slot;
                    if (is_primary) {
                        class_same_slot = Class_Primary;
                        class_other_slot = Class_Secondary;
                    }
                    else {
                        class_same_slot = Class_Secondary;
                        class_other_slot = Class_Primary;
                    }

                    if (((Class_Levels[id] <= 0) || !Class_Unlocked[id]) && (id != (byte)Systems.Class.IDs.None)) {
                        return CLASS_VALIDITY.INVALID_LOCKED; //locked class
                    }
                    else {
                        if (id != class_same_slot.ID) {
                            Systems.Class pre = class_other_slot;
                            while (pre != null) {
                                if (id == pre.ID) {
                                    return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                                }
                                else {
                                    pre = pre.Prereq;
                                }
                            }
                            pre = Systems.Class.LOOKUP[id].Prereq;
                            while (pre != null) {
                                if (class_other_slot.ID == pre.ID) {
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
                //default
                return CLASS_VALIDITY.INVALID_UNKNOWN;
            }
        }

        public bool LocalSetClass(byte id, bool is_primary) {
            //local MPlayer only
            if (!Is_Local_Player) {
                Utilities.Commons.Error("Tried to set non-local player with SetClass! (please report)");
                return false;
            }

            //fail if secondary not allowed
            if (!is_primary && !Allow_Secondary) {
                Main.NewText("Failed to set class because multiclassing is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            byte id_other;
            if (is_primary) {
                id_other = Class_Secondary.ID;
            }
            else {
                id_other = Class_Primary.ID;
            }
            if ((id == id_other) && (id != (byte)Systems.Class.IDs.None)) {
                //if setting to other set class, just swap
                return LocalSwapClass();
            }
            else {
                CLASS_VALIDITY valid = LocalCheckClassValid(id, is_primary);
                switch (valid) {
                    case CLASS_VALIDITY.VALID:

                        //destroy all minions
                        CheckMinions();
                        if (minions.Count > 0) {
                            Main.NewText("Your minions have been despawned because you changed classes!", UI.Constants.COLOUR_MESSAGE_ERROR);
                            foreach (Projectile p in minions) {
                                p.Kill();
                            }
                        }

                        if (is_primary) {
                            Class_Primary = Systems.Class.LOOKUP[id];
                        }
                        else {
                            Class_Secondary = Systems.Class.LOOKUP[id];
                        }
                        LocalUpdateClassInfo();
                        return true;

                    case CLASS_VALIDITY.INVALID_COMBINATION:
                        Main.NewText("Failed to set class because combination is invalid!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_ID:
                        Main.NewText("Failed to set class because class id is invalid!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_LOCKED:
                        Main.NewText("Failed to set class because it is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_NON_LOCAL:
                        Utilities.Commons.Error("Tried to set non-local player with SetClass! (please report)");
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

        public bool UnlockClass(Systems.Class c) {
            //check locked
            if (Class_Unlocked[c.ID]) {
                Utilities.Commons.Error("Trying to unlock already unlocked class " + c.Name);
                return false;
            }

            //tier 3 requirement
            if (c.Tier==3 && !CanUnlockTier3()) {
                if (!Defeated_WOF) {
                    Main.NewText("You must defeat the Wall of Flesh to unlock tier 3 classes!", UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Utilities.Commons.Error("CanUnlockTier3 returned false for unknown reasons! Please Report!");
                }
                return false;
            }

            //level requirements
            if (!HasClassPrereq(c)) {
                Main.NewText("You must reach level " + c.Prereq.Max_Level + " " + c.Prereq.Name + " to unlock " + c.Name + "!", UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            //item requirements
            if (c.Unlock_Item != null) {
                if (!player.HasItem(c.Unlock_Item.item.type)) {
                    //item requirement not met
                    Main.NewText("You require a " + c.Unlock_Item.item.Name + " to unlock " + c.Name + "!", UI.Constants.COLOUR_MESSAGE_ERROR);
                    return false;
                }
            }

            //requirements met..

            //take item
            player.ConsumeItem(c.Unlock_Item.item.type);

            //unlock class
            Class_Unlocked[c.ID] = true;
            if (Class_Levels[c.ID] < 1) {
                Class_Levels[c.ID] = 1;
            }

            //success
            Main.NewText("You have unlocked " + c.Name + "!", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);

            //add extra xp (after penalty)
            uint extra_xp_add = (uint)(Extra_XP * Systems.XP.EXTRA_XP_POOL_MULTIPLIER);
            if (extra_xp_add > 0) {
                //add xp
                AddXP(c.ID, extra_xp_add);

                //clear pool
                Extra_XP = 0;

                //levelup?
                while ((Class_Levels[c.ID] < c.Max_Level) && (Class_XP[c.ID] >= Systems.XP.Requirements.GetXPReq(c, Class_Levels[c.ID]))) {
                    SubtractXP(c.ID, Systems.XP.Requirements.GetXPReq(c, Class_Levels[c.ID]));
                    Class_Levels[c.ID]++;
                    AnnounceLevel(c);
                }

                //tell player
                Main.NewText(extra_xp_add + " unclaimed XP has been transferred to " + c.Name + "!", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
            }

            //update
            LocalUpdateClassInfo();

            return true;
        }

        public bool HasClassPrereq(Systems.Class c) {
            Systems.Class pre = c.Prereq;
            while (pre != null) {
                if (Class_Levels[pre.ID] < pre.Max_Level) {
                    //level requirement not met
                    return false;
                }
                else {
                    pre = pre.Prereq;
                }
            }
            return true;
        }

        public bool UnlockSubclass() {
            //check locked
            if (Allow_Secondary) {
                Utilities.Commons.Error("Trying to unlock multiclassing when already unlocked");
                return false;
            }

            //item requirements
            Item item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Subclass>().item;
            if (!player.HasItem(item.type)) {
                //item requirement not met
                Main.NewText("You require a " + item.Name + " to unlock multiclassing!", UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            //requirements met..

            //take item
            player.ConsumeItem(item.type);

            //unlock class
            Allow_Secondary = true;

            //update
            LocalUpdateClassInfo();

            //success
            Main.NewText("You can now multiclass! Right click a class to set it as your subclass.", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
            return true;
        }

        private bool LocalSwapClass() {
            //local MPlayer only
            if (!Is_Local_Player) return false;

            Systems.Class temp = Class_Primary;
            Class_Primary = Class_Secondary;
            Class_Secondary = temp;
            LocalUpdateClassInfo();
            return true;
        }

        public void LocalUpdateClassInfo() {
            //local MPlayer only
            if (!Is_Local_Player) return;

            //prevent secondary without primary class (move secondary to primary)
            if ((Class_Primary.ID == (byte)Systems.Class.IDs.New) || (Class_Primary.ID == (byte)Systems.Class.IDs.None)) {
                Class_Primary = Class_Secondary;
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //any "new" class should be set
            if (Class_Primary.ID == (byte)Systems.Class.IDs.New) {
                Class_Primary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.Novice];
            }
            if (Class_Secondary.ID == (byte)Systems.Class.IDs.New) {
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //clear secondary if not allowed
            if (!Allow_Secondary) {
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //effective levels
            SetEffectiveLevels();

            //base class attributes
            float sum_primary, sum_secondary;
            Systems.Class c;
            for (byte id = 0; id < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; id++) {
                sum_primary = 0;
                sum_secondary = 0;

                c = Class_Primary;
                while ((c != null) && (c.Tier > 0)) {
                    sum_primary += (c.Attribute_Growth[id] * Math.Min(Class_Levels[c.ID], c.Max_Level));
                    c = c.Prereq;
                }

                c = Class_Secondary;
                while ((c != null) && (c.Tier > 0)) {
                    sum_secondary += (c.Attribute_Growth[id] * Math.Min(Class_Levels[c.ID], c.Max_Level));
                    c = c.Prereq;
                }

                if (Class_Secondary_Level_Effective > 0) {
                    Attributes_Base[id] = (int)Math.Floor((sum_primary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS * Systems.Attribute.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY) +
                                                            (sum_secondary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS * Systems.Attribute.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY));
                }
                else {
                    Attributes_Base[id] = (int)Math.Floor(sum_primary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS);
                }
            }

            //allocated attribute points
            Allocation_Points_Total = Systems.Attribute.AllocationPointTotal(this);
            Allocation_Points_Spent = Systems.Attribute.AllocationPointSpent(this);
            Allocation_Points_Unallocated = Allocation_Points_Total - Allocation_Points_Spent;

            //sum attributes
            LocalCalculateFinalAttributes();

            //calclate progression value
            RecalculateProgression();

            //update UI
            UI.UIMain.Instance.UpdateClassInfo();
            UI.UIHUD.Instance.Update();

            //update class features
            UpdateClassInfo();
        }

        public void UpdateClassInfo() {

        }

        private void SetEffectiveLevels() {
            //set current levels for easier checking
            Class_Primary_Level_Effective = Class_Levels[Class_Primary.ID];
            Class_Secondary_Level_Effective = Class_Levels[Class_Secondary.ID];

            //level cap primary
            if (Class_Primary_Level_Effective > Class_Primary.Max_Level) {
                Class_Primary_Level_Effective = Class_Primary.Max_Level;
            }

            //subclass secondary effective level penalty
            if (Class_Secondary.Tier > Class_Primary.Tier) {
                //subclass of higher tier limited to lv1
                Class_Secondary_Level_Effective = 1;
            }
            else if (Class_Secondary.Tier == Class_Primary.Tier) {
                //subclass of same tier limited to half primary
                Class_Secondary_Level_Effective = (byte)Math.Min(Class_Secondary_Level_Effective, Class_Primary_Level_Effective / 2);

                //prevent effective level 0 if using two level 1s of same tier
                if (Class_Secondary.Tier > 0)
                    Class_Secondary_Level_Effective = Math.Max(Class_Secondary_Level_Effective, (byte)1);
            }//subclass of lower tier has no penalty

            //level cap secondary
            if (Class_Secondary_Level_Effective > Class_Secondary.Max_Level) {
                Class_Secondary_Level_Effective = Class_Secondary.Max_Level;
            }
        }

        public bool CanGainXP() {
            return (CanGainXPPrimary() || CanGainXPSecondary());
        }

        public bool CanGainXPPrimary() {
            return (Is_Local_Player && (Class_Primary.Tier > 0) && (Class_Levels[Class_Primary.ID] < Class_Primary.Max_Level));
        }

        public bool CanGainXPSecondary() {
            return (Is_Local_Player && Allow_Secondary && (Class_Secondary.Tier > 0) && (Class_Levels[Class_Secondary.ID] < Class_Secondary.Max_Level));
        }

        public void AddXP(uint xp) {
            if (Is_Local_Player) {
                if (show_xp) {
                    CombatText.NewText(Main.LocalPlayer.getRect(), UI.Constants.COLOUR_XP_BRIGHT, "+" + xp + " XP");
                }

                if (CanGainXP()) {
                    bool add_primary = CanGainXPPrimary();
                    bool add_secondary = CanGainXPSecondary();

                    if (add_primary && add_secondary) {
                        AddXP(Class_Primary.ID, (uint)Math.Ceiling(xp * Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY));
                        AddXP(Class_Secondary.ID, (uint)Math.Ceiling(xp * Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY));
                    }
                    else if (add_primary) {
                        AddXP(Class_Primary.ID, xp);
                    }
                    else if (add_secondary) {
                        AddXP(Class_Secondary.ID, xp);
                    }
                    else {
                        //shouldn't be reachable unless something is changed later
                        Extra_XP = Math.Max(Extra_XP, Extra_XP + xp); //prevent overflow
                        return;
                    }

                    CheckForLevel();
                }
                else {
                    Extra_XP = Math.Max(Extra_XP, Extra_XP + xp); //prevent overflow
                }
            }
        }

        private void CheckForLevel() {
            //store prior levels to detect level-up
            byte effective_primary = Class_Primary_Level_Effective;
            byte effective_secondary = Class_Secondary_Level_Effective;

            //level up
            while ((Class_Levels[Class_Primary.ID] < Class_Primary.Max_Level) && (Class_XP[Class_Primary.ID] >= Systems.XP.Requirements.GetXPReq(Class_Primary, Class_Levels[Class_Primary.ID]))) {
                SubtractXP(Class_Primary.ID, Systems.XP.Requirements.GetXPReq(Class_Primary, Class_Levels[Class_Primary.ID]));
                Class_Levels[Class_Primary.ID]++;
                AnnounceLevel(Class_Primary);
            }
            while ((Class_Levels[Class_Secondary.ID] < Class_Secondary.Max_Level) && (Class_XP[Class_Secondary.ID] >= Systems.XP.Requirements.GetXPReq(Class_Secondary, Class_Levels[Class_Secondary.ID]))) {
                SubtractXP(Class_Secondary.ID, Systems.XP.Requirements.GetXPReq(Class_Secondary, Class_Levels[Class_Secondary.ID]));
                Class_Levels[Class_Secondary.ID]++;
                AnnounceLevel(Class_Secondary);
            }

            //adjust effective levels
            SetEffectiveLevels();

            //update class info if needed
            if ((effective_primary != Class_Primary_Level_Effective) || (effective_secondary != Class_Secondary_Level_Effective)) {
                LocalUpdateClassInfo();
            }
            else {
                //otherwise just update xp bars
                UI.UIHUD.Instance.Update();
            }
        }

        private void AddXP(byte class_id, uint amount) {
            uint new_value = Class_XP[class_id] + amount;
            if (new_value > Class_XP[class_id]) {
                Class_XP[class_id] = new_value;
            }
            else {
                Class_XP[class_id] = uint.MaxValue;
            }
        }
        private void SubtractXP(byte class_id, uint amount) {
            if (Class_XP[class_id] > amount) {
                Class_XP[class_id] -= amount;
            }
            else {
                Class_XP[class_id] = 0;
            }
        }

        public void DefeatWOF() {
            if (!Defeated_WOF) {
                Defeated_WOF = true;
                Main.NewText("You have defeated the Wall of Flesh!", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                if (CanUnlockTier3()) {
                    Main.NewText("You can now unlock tier 3 classes!", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
            }
        }
        
        public bool CanUnlockTier3() {
            return Defeated_WOF;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Announce ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void AnnounceLevel(Systems.Class c) {
            //client/singleplayer only
            if (Utilities.Netmode.IS_SERVER)
                return;

            byte level = Class_Levels[c.ID];

            string message = "";
            if (level == c.Max_Level) {
                message = "You are now a MAX level " + c.Name + "!";
            }
            else {
                message = "You are now a level " + level + " " + c.Name + "!";
            }

            Main.NewText(message, UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Minions ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void CheckMinions() {
            minions = new List<Projectile>();
            foreach (Projectile p in Main.projectile) {
                if (p.active && (p.minion || p.sentry) && (p.owner == player.whoAmI)) {
                    minions.Add(p);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (ExperienceAndClasses.HOTKEY_UI.JustPressed) {
                UI.UIMain.Instance.Visibility = !UI.UIMain.Instance.Visibility;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Syncing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void clientClone(ModPlayer clientClone) {
            MPlayer clone = clientClone as MPlayer;

            clone.Class_Primary = Class_Primary;
            clone.Class_Secondary = Class_Secondary;
            clone.Class_Primary_Level_Effective = Class_Primary_Level_Effective;
            clone.Class_Secondary_Level_Effective = Class_Secondary_Level_Effective;

            Attributes_Final.CopyTo(clone.Attributes_Final, 0);

            clone.Progression = Progression;

            clone.AFK = AFK;
            clone.IN_COMBAT = IN_COMBAT;
        }

        /// <summary>
        /// look for changes to sync + send any changes via packet
        /// </summary>
        /// <param name="clientPlayer"></param>
        public override void SendClientChanges(ModPlayer clientPlayer) {
            DateTime now = DateTime.Now;
            if (now.CompareTo(time_next_full_sync) >= 0) {
                //full sync
                time_next_full_sync = now.AddTicks(TICKS_PER_FULL_SYNC);
                FullSync();
            }
            else {
                //partial sync
                MPlayer clone = clientPlayer as MPlayer;
                Byte me = (byte)player.whoAmI;

                //class and class levels
                if ((clone.Class_Primary.ID != Class_Primary.ID) || (clone.Class_Secondary.ID != Class_Secondary.ID) ||
                    (clone.Class_Primary_Level_Effective != Class_Primary_Level_Effective) || (clone.Class_Secondary_Level_Effective != Class_Secondary_Level_Effective)) {
                    Utilities.PacketHandler.ForceClass.Send(-1, me, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective);
                }

                //final attribute
                for (byte i=0; i<(byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    if (clone.Attributes_Final[i] != Attributes_Final[i]) {
                        Utilities.PacketHandler.ForceAttribute.Send(-1, me, Attributes_Final);
                        break;
                    }
                }

                //measure of character progression
                if (clone.Progression != Progression) {
                    Utilities.PacketHandler.Progression.Send(-1, me, Progression);
                }

                //afk
                if (clone.AFK != AFK) {
                    Utilities.PacketHandler.AFK.Send(-1, me, AFK);
                }

                //combat
                if (clone.IN_COMBAT != IN_COMBAT) {
                    Utilities.PacketHandler.InCombat.Send(-1, me, IN_COMBAT);
                }

            }
        }

        /// <summary>
        /// full sync (called to share current players with new players + new player with current players)
        /// </summary>
        /// <param name="toWho"></param>
        /// <param name="fromWho"></param>
        /// <param name="newPlayer"></param>
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            base.SyncPlayer(toWho, fromWho, newPlayer);
            FullSync();
        }

        /// <summary>
        /// sync all neccessary mod vars
        /// </summary>
        private void FullSync() {
            //send one packet with everything needed
            Utilities.PacketHandler.ForceFull.Send(-1, (byte)player.whoAmI, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective, Attributes_Final, AFK, IN_COMBAT, Progression);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sync Force Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ForceClass(byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            if (Is_Local_Player) {
                Utilities.Commons.Error("Cannot force class packet for local player");
                return;
            }

            Class_Primary = Systems.Class.LOOKUP[primary_id];
            Class_Primary_Level_Effective = primary_level;
            Class_Secondary = Systems.Class.LOOKUP[secondary_id];
            Class_Secondary_Level_Effective = secondary_level;

            UpdateClassInfo();
        }

        public void ForceAttribute(int[] attributes) {
            if (Is_Local_Player) {
                Utilities.Commons.Error("Cannot force attribute packet for local player");
                return;
            }

            for (byte i = 0; i < attributes.Length; i++) {
                Attributes_Final[i] = attributes[i];
            }

            UpdateClassInfo();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void ApplyAttributes() {
            for (byte i=0; i<(byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                Systems.Attribute.LOOKUP[i].ApplyEffect(this, Attributes_Final[i]);
            }
        }

        public void LocalAttributeAllocation1Point(byte id, bool add) {
            if (!Is_Local_Player) {
                Utilities.Commons.Error("Cannot set attribute allocation for non-local player");
                return;
            }
            
            int adjustment = +1;
            if (!add) {
                adjustment = -1;
            }

            if ((Attributes_Allocated[id] < 0 && adjustment > 0) || (Allocation_Points_Unallocated < 0 && adjustment < 0) || 
                (((adjustment < 0) || (Allocation_Points_Unallocated >= Systems.Attribute.AllocationPointCost(Attributes_Allocated[id]))) && ((Attributes_Allocated[id] + adjustment) >= 0))) {
                Attributes_Allocated[id] += adjustment;
                LocalUpdateClassInfo();
            }
        }

        public void LocalCalculateFinalAttributes() {
            if (!Is_Local_Player) {
                Utilities.Commons.Error("Cannot calculate final attribute for non-local player");
                return;
            }

            for (byte id = 0; id < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; id++) {
                Attributes_Final[id] = Attributes_Base[id] + Attributes_Allocated[id] + Attributes_Bonus[id];
            }
        }

        //dexterity
        public override float UseTimeMultiplier(Item item) {
            if (item.melee)
                return base.UseTimeMultiplier(item) * use_speed_melee;

            if (item.ranged)
                return base.UseTimeMultiplier(item) * use_speed_ranged;

            if (item.magic)
                return base.UseTimeMultiplier(item) * use_speed_magic;

            if (item.thrown)
                return base.UseTimeMultiplier(item) * use_speed_throwing;

            if (item.summon || item.sentry)
                return base.UseTimeMultiplier(item) * use_speed_minion;

            else if (item.hammer>0 || item.axe>0 || item.pick>0 || item.fishingPole>0)
                return base.UseTimeMultiplier(item) * use_speed_tool;

            else
                return base.UseTimeMultiplier(item);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Drawing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void DrawEffects(PlayerDrawInfo drawInfo, bool is_behind) {
            /*
            Player drawPlayer = drawInfo.drawPlayer;
            //Mod mod = ModLoader.GetMod("ExperienceAndClasses");
            //ExamplePlayer modPlayer = drawPlayer.GetModPlayer<ExamplePlayer>();
            //Texture2D texture = mod.GetTexture("NPCs/Lock");

            if (drawPlayer.statLife < 50) {
                return;
            }

            Texture2D texture = Main.flameTexture;

            int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X - texture.Width / 2f);
            int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y - texture.Height / 2f);

            DrawData data = new DrawData(texture, new Vector2(drawX, drawY), Color.Green);
            Main.playerDrawData.Add(data);
            */
        }

        public static readonly PlayerLayer MiscEffectsBehind = new PlayerLayer("ExperienceAndClasses", "MiscEffectsBack", PlayerLayer.MiscEffectsBack, delegate (PlayerDrawInfo drawInfo) {
            DrawEffects(drawInfo, true);
        });

        public static readonly PlayerLayer MiscEffects = new PlayerLayer("ExperienceAndClasses", "MiscEffects", PlayerLayer.MiscEffectsFront, delegate (PlayerDrawInfo drawInfo) {
            DrawEffects(drawInfo, false);
        });

        public override void ModifyDrawLayers(List<PlayerLayer> layers) {
            MiscEffectsBehind.visible = true;
            layers.Insert(0, MiscEffectsBehind);
            MiscEffects.visible = true;
            layers.Add(MiscEffects);
        }


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override TagCompound Save() {
            //must be byte[], int[], or List<>
            List<int> attributes_allocated = new List<int>();
            foreach (int value in Attributes_Allocated) {
                attributes_allocated.Add(value);
            }
            List<bool> class_unlocked = new List<bool>();
            foreach (bool value in Class_Unlocked) {
                class_unlocked.Add(value);
            }
            List<uint> class_xp = new List<uint>();
            foreach (uint value in Class_XP) {
                class_xp.Add(value);
            }
            List<byte> class_level = new List<byte>();
            foreach (byte value in Class_Levels) {
                class_level.Add(value);
            }

            //version
            Version version = ExperienceAndClasses.MOD.Version;
            int[] version_array = new int[] { version.Major, version.Minor, version.Build };

            //ui positions
            float ui_main_left, ui_main_top, ui_hud_left, ui_hud_top;
            bool ui_main_auto, ui_hud_auto;
            if (UI.UIMain.Instance.panel != null) {
                ui_main_left = UI.UIMain.Instance.panel.GetLeft();
                ui_main_top = UI.UIMain.Instance.panel.GetTop();
                ui_main_auto = UI.UIMain.Instance.panel.Auto;

                ui_hud_left = UI.UIHUD.Instance.panel.GetLeft();
                ui_hud_top = UI.UIHUD.Instance.panel.GetTop();
                ui_hud_auto = UI.UIHUD.Instance.panel.Auto;
            }
            else {
                ui_main_left = UI.Constants.DEFAULT_UI_MAIN_LEFT;
                ui_main_top = UI.Constants.DEFAULT_UI_MAIN_TOP;
                ui_main_auto = UI.Constants.DEFAULT_UI_MAIN_AUTO;

                ui_hud_left = UI.Constants.DEFAULT_UI_HUD_LEFT;
                ui_hud_top = UI.Constants.DEFAULT_UI_HUD_TOP;
                ui_hud_auto = UI.Constants.DEFAULT_UI_HUD_AUTO;
            }

            return new TagCompound {
                {"eac_version", version_array },
                {"eac_ui_class_left", ui_main_left },
                {"eac_ui_class_top", ui_main_top },
                {"eac_ui_class_auto", ui_main_auto },
                {"eac_ui_hud_left", ui_hud_left },
                {"eac_ui_hud_top", ui_hud_top },
                {"eac_ui_hud_auto", ui_hud_auto },
                {"eac_class_unlock", class_unlocked },
                {"eac_class_xp", class_xp },
                {"eac_class_level", class_level },
                {"eac_class_current_primary", Class_Primary.ID },
                {"eac_class_current_secondary", Class_Secondary.ID },
                {"eac_class_subclass_unlocked", Allow_Secondary },
                {"eac_attribute_allocation", attributes_allocated },
                {"eac_wof", Defeated_WOF },
                {"eac_settings_show_xp", show_xp},
                {"eac_extra_xp", Extra_XP},
            };
        }

        public override void Load(TagCompound tag) {
            //get version in case needed
            Load_Version = Utilities.Commons.TryGet<int[]>(tag, "eac_version", new int[3]);

            //has killed wof
            Defeated_WOF = Utilities.Commons.TryGet<bool>(tag, "eac_wof", Defeated_WOF);

            //subclass unlocked
            Allow_Secondary = Utilities.Commons.TryGet<bool>(tag, "eac_class_subclass_unlocked", Allow_Secondary);

            //extra xp pool
            Extra_XP = Utilities.Commons.TryGet<uint>(tag, "eac_extra_xp", Extra_XP);

            //settings
            show_xp = Utilities.Commons.TryGet<bool>(tag, "eac_settings_show_xp", show_xp);

            //current classes
            Class_Primary = Systems.Class.LOOKUP[Utilities.Commons.TryGet<byte>(tag, "eac_class_current_primary", Class_Primary.ID)];
            Class_Secondary = Systems.Class.LOOKUP[Utilities.Commons.TryGet<byte>(tag, "eac_class_current_secondary", Class_Secondary.ID)];

            //class unlocked
            List<bool> class_unlock_loaded = Utilities.Commons.TryGet<List<bool>>(tag, "eac_class_unlock", new List<bool>());
            for (byte i = 0; i < class_unlock_loaded.Count; i++) {
                Class_Unlocked[i] = class_unlock_loaded[i];
            }

            //class level
            List<byte> class_level_loaded = Utilities.Commons.TryGet<List<byte>>(tag, "eac_class_level", new List<byte>());
            for (byte i = 0; i < class_level_loaded.Count; i++) {
                Class_Levels[i] = class_level_loaded[i];
            }

            //class xp
            List<uint> class_xp_loaded = Utilities.Commons.TryGet<List<uint>>(tag, "eac_class_xp", new List<uint>());
            for (byte i = 0; i < class_xp_loaded.Count; i++) {
                Class_XP[i] = class_xp_loaded[i];
            }

            //fix any potential issues...
            for (byte id = 0; id < (byte)Systems.Class.IDs.NUMBER_OF_IDs; id++) {

                //level up if required xp changed
                while ((Class_Levels[id] < Systems.Class.LOOKUP[id].Max_Level) && (Class_XP[id] >= Systems.XP.Requirements.GetXPReq(Systems.Class.LOOKUP[id], Class_Levels[id]))) {
                    SubtractXP(id, Systems.XP.Requirements.GetXPReq(Systems.Class.LOOKUP[id], Class_Levels[id]));
                    Class_Levels[id]++;
                }

                //if unlocked, level should be at least one
                if (Class_Unlocked[id] && (Class_Levels[id] < 1)) {
                    Class_Levels[id] = 1;
                }

            }

            //if not allowed secondary, set none
            if (!Allow_Secondary) {
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //if selected class is now locked for some reason, select no class
            if ((!Class_Unlocked[Class_Primary.ID]) || (!Class_Unlocked[Class_Secondary.ID])) {
                Class_Primary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //allocated attributes
            List<int> attribute_allocation = Utilities.Commons.TryGet<List<int>>(tag, "eac_attribute_allocation", new List<int>());
            for(byte i = 0; i < attribute_allocation.Count; i++) {
                if (Systems.Attribute.LOOKUP[i].Active) {
                    Attributes_Allocated[i] = attribute_allocation[i];
                }
            }

            //UI data
            loaded_ui_main = new Utilities.Containers.LoadedUIData(
                Utilities.Commons.TryGet<float>(tag, "eac_ui_class_left", UI.Constants.DEFAULT_UI_MAIN_LEFT),
                Utilities.Commons.TryGet<float>(tag, "eac_ui_class_top", UI.Constants.DEFAULT_UI_MAIN_TOP),
                Utilities.Commons.TryGet<bool>(tag, "eac_ui_class_auto", UI.Constants.DEFAULT_UI_MAIN_AUTO));

            loaded_ui_hud = new Utilities.Containers.LoadedUIData(
                Utilities.Commons.TryGet<float>(tag, "eac_ui_hud_left", UI.Constants.DEFAULT_UI_HUD_LEFT),
                Utilities.Commons.TryGet<float>(tag, "eac_ui_hud_top", UI.Constants.DEFAULT_UI_HUD_TOP),
                Utilities.Commons.TryGet<bool>(tag, "eac_ui_hud_auto", UI.Constants.DEFAULT_UI_HUD_AUTO));
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Misc ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void SetAFK(bool afk) {
            AFK = afk;
            if (Is_Local_Player) {
                if (AFK) {
                    Main.NewText("You are now AFK. You will not gain or lose XP.", UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Main.NewText("You are no longer AFK. You can gain and lose XP again.", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
            }
        }

        public void SetInCombat(bool in_combat) {
            IN_COMBAT = in_combat;
        }

        private void RecalculateProgression() {
            SetProgression(Allocation_Points_Total);
        }

        public void SetProgression(int player_progression) {
            Progression = player_progression;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Status ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public bool HasStatus(Systems.Status.IDs id) {
            return Statuses.ContainsStatus(id);
        }

        public void UpdateStatus() {
            foreach (List<Systems.Status> s in Statuses.GetAllStatuses()) {
                //TODO
            }
        }
        
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Ability ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /*
        public void Heal(int amount_life, int amount_mana) {
            if (Is_Local_Player) {
                Main.NewText("do " + amount_life + " " + amount_mana);

                //life
                amount_life = Math.Min(amount_life, player.statLifeMax2 - player.statLife);
                if (amount_life > 0) {
                    player.statLife += amount_life;
                    player.HealEffect(amount_life, true);
                }

                //mana
                amount_mana = Math.Min(amount_mana, player.statManaMax2 - player.statMana);
                if (amount_mana > 0) {
                    player.statMana += amount_mana;
                    player.ManaEffect(amount_mana);
                }
            }
            else {
                Utilities.PacketHandler.Heal.Send((byte)player.whoAmI, (byte)Main.LocalPlayer.whoAmI, amount_life, amount_mana);
            }
        }
        */

    }
}
