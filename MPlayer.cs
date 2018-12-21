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
        private const long TICKS_PER_XP_SEND = (long)(TimeSpan.TicksPerSecond * 0.5);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Vars ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static DateTime time_next_full_sync;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (non-syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private TagCompound load_tag;
        public bool Is_Local_Player { get; private set; }
        private bool initialized;
        public int[] Load_Version { get; private set; }
        public bool Killed_WOF { get; private set; }
        public bool Allow_Secondary { get; private set; }

        private bool show_xp;
        private DateTime send_xp_when;

        public byte[] Class_Levels { get; private set; }
        public uint[] Class_XP { get; private set; }
        public bool[] Class_Unlocked { get; private set; }

        public int[] Attributes_Base { get; private set; }
        public int[] Attributes_Allocated { get; private set; }
        public int[] Attributes_Bonus { get; private set; }
        public int Allocation_Points_Unallocated { get; private set; }
        private int Allocation_Points_Spent;
        private int Allocation_Points_Total;

        public float heal_damage; //TODO
        public float dodge_chance; //TODO
        public float ability_delay_reduction; //TODO
        public float use_speed_melee, use_speed_ranged, use_speed_magic, use_speed_throwing, use_speed_minion, use_speed_tool;
        public float tool_power;

        public List<Projectile> minions;

        public double old_xp; //pre-revamp xp

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Systems.Class Class_Primary { get; private set; }
        public Systems.Class Class_Secondary { get; private set; }
        public byte Class_Primary_Level_Effective { get; private set; }
        public byte Class_Secondary_Level_Effective { get; private set; }

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
            Killed_WOF = false;
            show_xp = true;
            send_xp_when = DateTime.MinValue;
            minions = new List<Projectile>();
            old_xp = 0;

            //default level/xp/unlock
            Class_Levels = new byte[(byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs];
            Class_XP = new uint[(byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs];
            Class_Unlocked = new bool[(byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs];

            //default unlocks
            Class_Levels[(byte)Systems.Class.CLASS_IDS.Novice] = 1;
            Class_Unlocked[(byte)Systems.Class.CLASS_IDS.New] = true;
            Class_Unlocked[(byte)Systems.Class.CLASS_IDS.None] = true;
            Class_Unlocked[(byte)Systems.Class.CLASS_IDS.Novice] = true;

            //default class selection
            Class_Primary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.Novice];
            Class_Secondary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];

            //initialize attributes
            Attributes_Base = new int[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attributes_Allocated = new int[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attributes_Bonus = new int[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attributes_Final = new int[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Allocation_Points_Unallocated = 0;
            Allocation_Points_Spent = 0;
            Allocation_Points_Total = 0;

            //stats
            heal_damage = 1f;
            dodge_chance = 0f;
            use_speed_melee = use_speed_ranged = use_speed_magic = use_speed_throwing = use_speed_minion = use_speed_tool = 1f;
            ability_delay_reduction = 1f;
            tool_power = 1f;

            //xp
            Systems.XP.TRACK_PLAYER_XP[player.whoAmI] = 0;
        }

        /// <summary>
        /// player enters (not other players)
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player) {
            base.OnEnterWorld(player);
            if (!ExperienceAndClasses.IS_SERVER) {
                //this is the current local player
                Is_Local_Player = true;
                ExperienceAndClasses.LOCAL_MPLAYER = this;

                //is this multiplayer?
                ExperienceAndClasses.CheckMultiplater();

                //start timer for next full sync
                time_next_full_sync = DateTime.Now.AddTicks(TICKS_PER_FULL_SYNC);

                //grab UI-state combos to display
                ExperienceAndClasses.UIs = new UI.UIStateCombo[] { UI.UIStatus.Instance, UI.UIAbility.Instance, UI.UIClass.Instance, UI.UIInfo.Instance };

                //(re)initialize ui
                foreach (UI.UIStateCombo ui in ExperienceAndClasses.UIs) {
                    ui.Initialize();
                }

                //apply saved ui settings
                UI.UIClass.Instance.panel.SetPosition(Commons.TryGet<float>(load_tag, "eac_ui_class_left", 300f), Commons.TryGet<float>(load_tag, "eac_ui_class_top", 300f));
                UI.UIClass.Instance.panel.Auto =Commons.TryGet<bool>(load_tag, "eac_ui_class_auto", true);

                UI.UIAbility.Instance.panel.SetPosition(Commons.TryGet<float>(load_tag, "eac_ui_bars_left", 480f), Commons.TryGet<float>(load_tag, "eac_ui_bars_top", 10f));
                UI.UIAbility.Instance.panel.Auto = Commons.TryGet<bool>(load_tag, "eac_ui_bars_auto", true);

                //temp: show bars and status
                UI.UIAbility.Instance.Visibility = true;
                UI.UIStatus.Instance.Visibility = true;

                //apply ui auto
                ExperienceAndClasses.SetUIAutoStates();

                //update class info
                LocalUpdateClassInfo();

                //initialized
                initialized = true;

                //get old xp one time if loaded old save
                if (Commons.VersionIsOlder(Load_Version, new int[] { 2, 0, 0 })) {
                    old_xp = player.GetModPlayer<Legacy.MyPlayer>(mod).old_xp;
                }

                //convert old items (call twice in case inventory is pretty full and first call makes room)
                Legacy.ConvertLegacyItems(player);
                Legacy.ConvertLegacyItems(player);
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
        }

        public override void PostUpdateEquips() {
            base.PostUpdateEquips();
            if (initialized) {
                ApplyAttributes();
            }
        }

        public override void PostUpdate() {
            base.PostUpdate();

            //timed events...
            DateTime now = DateTime.Now;

            //server/singleplayer
            if (!ExperienceAndClasses.IS_CLIENT) {

                //sending xp packets (or handle locally in chunks)
                uint xp = Systems.XP.TRACK_PLAYER_XP[player.whoAmI];
                if ((xp > 0) && (now.CompareTo(send_xp_when) >= 0)) {

                    send_xp_when = now.AddTicks(TICKS_PER_XP_SEND);
                    Systems.XP.TRACK_PLAYER_XP[player.whoAmI] = 0;

                    if (ExperienceAndClasses.IS_SERVER) {
                        PacketHandler.XP.Send(player.whoAmI, -1, xp);
                    }
                    else {
                        AddXP(xp);
                    }
                }

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
            else if (id == (byte)Systems.Class.CLASS_IDS.None) {
                return CLASS_VALIDITY.VALID; //setting to no class is always allowed (unless in combat)
            }
            else {
                if (id >= (byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs) {
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

                    if (((Class_Levels[id] <= 0) || !Class_Unlocked[id]) && (id != (byte)Systems.Class.CLASS_IDS.None)) {
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
                            pre = Systems.Class.CLASS_LOOKUP[id].Prereq;
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
                Commons.Error("Tried to set non-local player with SetClass! (please report)");
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
            if ((id == id_other) && (id != (byte)Systems.Class.CLASS_IDS.None)) {
                //if setting to other set class, just swap
                return LocalSwapClass();
            }
            else {
                CLASS_VALIDITY valid = LocalCheckClassValid(id, is_primary);
                switch (valid) {
                    case CLASS_VALIDITY.VALID:

                        //destroy all minions
                        CheckMinions();
                        foreach (Projectile p in minions) {
                            if (p.active && (p.minion || p.sentry) && (p.owner == player.whoAmI)) {
                                p.Kill();
                            }
                        }

                        if (is_primary) {
                            Class_Primary = Systems.Class.CLASS_LOOKUP[id];
                        }
                        else {
                            Class_Secondary = Systems.Class.CLASS_LOOKUP[id];
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
                        Commons.Error("Tried to set non-local player with SetClass! (please report)");
                        break;

                    case CLASS_VALIDITY.INVALID_COMBAT:
                        Main.NewText("Failed to set class because you are in combat!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    default:
                        Commons.Error("Failed to set class for unknown reasons! (please report)");
                        break;
                }

                //default
                return false;
            }
        }

        public bool UnlockClass(Systems.Class c) {
            //check locked
            if (Class_Unlocked[c.ID]) {
                Commons.Error("Trying to unlock already unlocked class " + c.Name);
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

            //update
            LocalUpdateClassInfo();

            //success
            Main.NewText("You have unlocked " + c.Name + "!", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
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
                Commons.Error("Trying to unlock multiclassing when already unlocked");
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
            if ((Class_Primary.ID == (byte)Systems.Class.CLASS_IDS.New) || (Class_Primary.ID == (byte)Systems.Class.CLASS_IDS.None)) {
                Class_Primary = Class_Secondary;
                Class_Secondary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];
            }

            //any "new" class should be set
            if (Class_Primary.ID == (byte)Systems.Class.CLASS_IDS.New) {
                Class_Primary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.Novice];
            }
            if (Class_Secondary.ID == (byte)Systems.Class.CLASS_IDS.New) {
                Class_Secondary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];
            }

            //clear secondary if not allowed
            if (!Allow_Secondary) {
                Class_Secondary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];
            }

            //effective levels
            SetEffectiveLevels();

            //base class attributes
            float sum_primary, sum_secondary;
            Systems.Class c;
            for (byte id = 0; id < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; id++) {
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

            //update UI
            UI.UIClass.Instance.UpdateClassInfo();
            UI.UIAbility.Instance.Update();

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

        public void AddOrbXP(uint xp_primary, uint xp_secondary) {
            AddXP(Class_Primary.ID, xp_primary);
            AddXP(Class_Secondary.ID, xp_secondary);
            CheckForLevel();
        }

        public void AddXP(uint xp) {
            if (CanGainXP()) {
                if (show_xp) {
                    CombatText.NewText(Main.LocalPlayer.getRect(), UI.Constants.COLOUR_XP_BRIGHT, "+" + xp + " XP");
                }

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
                    //can't gain xp (max level, etc.)
                    return;
                }

                CheckForLevel();
            }
        }

        private void CheckForLevel() {
            //store prior levels to detect level-up
            byte effective_primary = Class_Primary_Level_Effective;
            byte effective_secondary = Class_Secondary_Level_Effective;

            //level up
            while ((Class_Levels[Class_Primary.ID] < Class_Primary.Max_Level) && (Class_XP[Class_Primary.ID] >= Systems.XP.GetXPReq(Class_Primary, Class_Levels[Class_Primary.ID]))) {
                SubtractXP(Class_Primary.ID, Systems.XP.GetXPReq(Class_Primary, Class_Levels[Class_Primary.ID]));
                Class_Levels[Class_Primary.ID]++;
                AnnounceLevel(Class_Primary);
            }
            while ((Class_Levels[Class_Secondary.ID] < Class_Secondary.Max_Level) && (Class_XP[Class_Secondary.ID] >= Systems.XP.GetXPReq(Class_Secondary, Class_Levels[Class_Secondary.ID]))) {
                SubtractXP(Class_Secondary.ID, Systems.XP.GetXPReq(Class_Secondary, Class_Levels[Class_Secondary.ID]));
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
                UI.UIAbility.Instance.Update();
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
        

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Announce ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void AnnounceLevel(Systems.Class c) {
            //client/singleplayer only
            if (ExperienceAndClasses.IS_SERVER)
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

        //do not count dragon body or tail (only head)
        //private static readonly short[] minions_ignore = new short[] { ProjectileID.StardustDragon2 , ProjectileID.StardustDragon3, ProjectileID.StardustDragon4 };

        public void CheckMinions() {
            minions = new List<Projectile>();
            foreach (Projectile p in Main.projectile) {
                if (p.active && (p.minion || p.sentry) && (p.owner == player.whoAmI)) {
                    //must be part of minion that takes slots + not on ignore list
                    //if ((p.minionSlots > 0f) && (Array.IndexOf(minions_ignore, (short)p.type) == -1)) {

                    minions.Add(p);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (ExperienceAndClasses.HOTKEY_UI.JustPressed) {
                UI.UIClass.Instance.Visibility = !UI.UIClass.Instance.Visibility;
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
                    PacketHandler.ForceClass.Send(-1, me, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective);
                }

                //final attribute
                for (byte i=0; i<(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                    if (clone.Attributes_Final[i] != Attributes_Final[i]) {
                        PacketHandler.ForceAttribute.Send(-1, me, Attributes_Final);
                        break;
                    }
                }

                //afk
                if (clone.AFK != AFK) {
                    PacketHandler.AFK.Send(-1, me, AFK);
                }

                //combat
                if (clone.IN_COMBAT != IN_COMBAT) {

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
            PacketHandler.ForceFull.Send(-1, (byte)player.whoAmI, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective, Attributes_Final, AFK);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sync Force Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ForceClass(byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            if (Is_Local_Player) {
                Commons.Error("Cannot force class packet for local player");
                return;
            }

            if (!initialized) {
                initialized = true;
            }

            Class_Primary = Systems.Class.CLASS_LOOKUP[primary_id];
            Class_Primary_Level_Effective = primary_level;
            Class_Secondary = Systems.Class.CLASS_LOOKUP[secondary_id];
            Class_Secondary_Level_Effective = secondary_level;

            UpdateClassInfo();
        }

        public void ForceAttribute(int[] attributes) {
            if (Is_Local_Player) {
                Commons.Error("Cannot force attribute packet for local player");
                return;
            }

            if (!initialized) {
                initialized = true;
            }

            for (byte i = 0; i < attributes.Length; i++) {
                Attributes_Final[i] = attributes[i];
            }

            UpdateClassInfo();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void ApplyAttributes() {
            for (byte i=0; i<(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                Systems.Attribute.ATTRIBUTE_LOOKUP[i].ApplyEffect(this, Attributes_Final[i]);
            }
        }

        public void LocalAttributeAllocation1Point(byte id, bool add) {
            if (!Is_Local_Player) {
                Commons.Error("Cannot set attribute allocation for non-local player");
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
                Commons.Error("Cannot calculate final attribute for non-local player");
                return;
            }

            for (byte id = 0; id < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; id++) {
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

            return new TagCompound {
                {"eac_version", version_array },
                {"eac_ui_class_left", UI.UIClass.Instance.panel.GetLeft() },
                {"eac_ui_class_top", UI.UIClass.Instance.panel.GetTop() },
                {"eac_ui_class_auto", UI.UIClass.Instance.panel.Auto },
                {"eac_ui_bars_left", UI.UIAbility.Instance.panel.GetLeft() },
                {"eac_ui_bars_top", UI.UIAbility.Instance.panel.GetTop() },
                {"eac_ui_bars_auto", UI.UIAbility.Instance.panel.Auto },
                {"eac_class_unlock", class_unlocked },
                {"eac_class_xp", class_xp },
                {"eac_class_level", class_level },
                {"eac_class_current_primary", Class_Primary.ID },
                {"eac_class_current_secondary", Class_Secondary.ID },
                {"eac_class_subclass_unlocked", Allow_Secondary },
                {"eac_attribute_allocation", attributes_allocated },
                {"eac_wof", Killed_WOF },
                {"eac_settings_show_xp", show_xp},
                {"old_experience", old_xp},
            };
        }

        public override void Load(TagCompound tag) {
            //some settings must be applied after init
            load_tag = tag;

            //old xp spent
            old_xp = Commons.TryGet<double>(load_tag, "old_experience", 0);

            //get version in case needed
            Load_Version = Commons.TryGet<int[]>(load_tag, "eac_version", new int[3]);

            //has killed wof
            Killed_WOF = Commons.TryGet<bool>(load_tag, "eac_wof", Killed_WOF);

            //subclass unlocked
            Allow_Secondary = Commons.TryGet<bool>(load_tag, "eac_class_subclass_unlocked", Allow_Secondary);

            //settings
            show_xp = Commons.TryGet<bool>(load_tag, "eac_settings_show_xp", show_xp);

            //current classes
            Class_Primary = Systems.Class.CLASS_LOOKUP[Commons.TryGet<byte>(load_tag, "eac_class_current_primary", Class_Primary.ID)];
            Class_Secondary = Systems.Class.CLASS_LOOKUP[Commons.TryGet<byte>(load_tag, "eac_class_current_secondary", Class_Secondary.ID)];

            //class unlocked
            List<bool> class_unlock_loaded = Commons.TryGet<List<bool>>(load_tag, "eac_class_unlock", new List<bool>());
            for (byte i = 0; i < class_unlock_loaded.Count; i++) {
                Class_Unlocked[i] = class_unlock_loaded[i];
            }

            //class level
            List<byte> class_level_loaded = Commons.TryGet<List<byte>>(load_tag, "eac_class_level", new List<byte>());
            for (byte i = 0; i < class_level_loaded.Count; i++) {
                Class_Levels[i] = class_level_loaded[i];
            }

            //class xp
            List<uint> class_xp_loaded = Commons.TryGet<List<uint>>(load_tag, "eac_class_xp", new List<uint>());
            for (byte i = 0; i < class_xp_loaded.Count; i++) {
                Class_XP[i] = class_xp_loaded[i];
            }

            //fix any potential issues...
            for (byte id = 0; id < (byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs; id++) {

                //level up if required xp changed
                while ((Class_Levels[id] < Systems.Class.CLASS_LOOKUP[id].Max_Level) && (Class_XP[id] >= Systems.XP.GetXPReq(Systems.Class.CLASS_LOOKUP[id], Class_Levels[id]))) {
                    SubtractXP(id, Systems.XP.GetXPReq(Systems.Class.CLASS_LOOKUP[id], Class_Levels[id]));
                    Class_Levels[id]++;
                }

                //if unlocked, level should be at least one
                if (Class_Unlocked[id] && (Class_Levels[id] < 1)) {
                    Class_Levels[id] = 1;
                }

            }

            //if not allowed secondary, set none
            if (!Allow_Secondary) {
                Class_Secondary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];
            }

            //if selected class is now locked for some reason, select no class
            if ((!Class_Unlocked[Class_Primary.ID]) || (!Class_Unlocked[Class_Secondary.ID])) {
                Class_Primary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];
                Class_Secondary = Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.None];
            }

            //allocated attributes
            List<int> attribute_allocation = Commons.TryGet<List<int>>(load_tag, "eac_attribute_allocation", new List<int>());
            for(byte i = 0; i < attribute_allocation.Count; i++) {
                if (Systems.Attribute.ATTRIBUTE_LOOKUP[i].Active) {
                    Attributes_Allocated[i] = attribute_allocation[i];
                }
            }
            
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Ability & Status ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

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
                PacketHandler.Heal.Send((byte)player.whoAmI, (byte)Main.LocalPlayer.whoAmI, amount_life, amount_mana);
            }
        }

    }
}
