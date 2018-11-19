using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses {
    public class MPlayer : ModPlayer {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const long TICKS_PER_FULL_SYNC = TimeSpan.TicksPerMinute * 2;
        private const long TICKS_PER_XP_MESSAGE = TimeSpan.TicksPerSecond * 1;

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
        private double show_xp_value;
        private DateTime show_xp_when;

        public byte[] Class_Levels { get; private set; }
        public uint[] Class_XP { get; private set; }
        public bool[] Class_Unlocked { get; private set; }

        public short[] Attributes_Base { get; private set; }
        public short[] Attributes_Allocated { get; private set; }
        public short[] Attributes_Bonus { get; private set; }
        public short Attribute_Points_Unallocated { get; private set; }
        private short Attribute_Points_Allocated;
        private short Attribute_Points_Total;
        public byte Levels_To_Next_Point { get; private set; }

        public float heal_damage; //TODO
        public float dodge_chance; //TODO
        public float attack_cast_speed;
        public float ability_delay_reduction;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Systems.Class Class_Primary { get; private set; }
        public Systems.Class Class_Secondary { get; private set; }
        public byte Class_Primary_Level_Effective { get; private set; }
        public byte Class_Secondary_Level_Effective { get; private set; }

        public short[] Attributes_Final { get; private set; }
        public bool AFK { get; private set; }

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
            Killed_WOF = false;
            show_xp = true;
            show_xp_value = 0;
            show_xp_when = DateTime.MinValue;

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
            Attributes_Base = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attributes_Allocated = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attributes_Bonus = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attributes_Final = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
            Attribute_Points_Unallocated = 0;
            Attribute_Points_Allocated = 0;
            Attribute_Points_Total = 0;
            Levels_To_Next_Point = 0;

            //stats
            heal_damage = 1f;
            dodge_chance = 0f;
            attack_cast_speed = 1f;
            ability_delay_reduction = 1f;
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

                //(re)initialize ui
                UI.UIInfo.Instance.Initialize();
                UI.UIClass.Instance.Initialize();
                UI.UIBars.Instance.Initialize();
                UI.UIStatus.Instance.Initialize();

                //apply saved ui settings
                UI.UIClass.Instance.panel.SetPosition(Commons.TryGet<float>(load_tag, "eac_ui_class_left", 300f), Commons.TryGet<float>(load_tag, "eac_ui_class_top", 300f));
                UI.UIClass.Instance.panel.Auto =Commons.TryGet<bool>(load_tag, "eac_ui_class_auto", true);
                UI.UIClass.Instance.panel.Pinned = Commons.TryGet<bool>(load_tag, "eac_ui_class_pinned", false);

                UI.UIBars.Instance.panel.SetPosition(Commons.TryGet<float>(load_tag, "eac_ui_bars_left", 480f), Commons.TryGet<float>(load_tag, "eac_ui_bars_top", 10f));
                UI.UIBars.Instance.panel.Auto = Commons.TryGet<bool>(load_tag, "eac_ui_bars_auto", true);
                UI.UIBars.Instance.panel.Pinned = Commons.TryGet<bool>(load_tag, "eac_ui_bars_pinned", false);

                //temp: show bars and status
                UI.UIBars.Instance.Visibility = true;
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
                attack_cast_speed = 1f;
                ability_delay_reduction = 1f;
            }
        }

        public override void PostUpdateEquips() {
            base.PostUpdateEquips();
            if (initialized) {
                ApplyAttributes();
            }

            //local events
            if (Is_Local_Player) {
                //ui
                UI.UIStatus.Instance.Update();

                //timed events
                DateTime now = DateTime.Now;

                if (show_xp && (show_xp_value > 0)) {
                    if (now.CompareTo(show_xp_when) >= 0) {
                        show_xp_when = now.AddTicks(TICKS_PER_XP_MESSAGE);
                        CombatText.NewText(Main.LocalPlayer.getRect(), UI.Constants.COLOUR_XP, "+" + Math.Max(Math.Floor(show_xp_value), 1) + " XP");
                        show_xp_value = 0;
                    }
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP & Class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum CLASS_VALIDITY : byte {
            VALID,
            INVALID_UNKNOWN,
            INVALID_ID,
            INVALID_LEVEL,
            INVALID_COMBINATION,
            INVALID_NON_LOCAL,
        }
        public CLASS_VALIDITY LocalCheckClassValid(byte id, bool is_primary) {
            //local MPlayer only
            if (!Is_Local_Player) return CLASS_VALIDITY.INVALID_NON_LOCAL;

            if (id == (byte)Systems.Class.CLASS_IDS.None) {
                return CLASS_VALIDITY.VALID; //setting to no class is always allowed
            }
            else {
                if (id >= (byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs) {
                    return CLASS_VALIDITY.INVALID_ID; //invalid idsss
                }
                else {
                    byte id_same, id_other;
                    if (is_primary) {
                        id_same = Class_Primary.ID;
                        id_other = Class_Secondary.ID;
                    }
                    else {
                        id_same = Class_Secondary.ID;
                        id_other = Class_Primary.ID;
                    }

                    if (((Class_Levels[id] <= 0) || !Class_Unlocked[id]) && (id != (byte)Systems.Class.CLASS_IDS.None)) {
                        return CLASS_VALIDITY.INVALID_LEVEL; //locked class
                    }
                    else {
                        if (id != id_same) {
                            byte id_pre = id_other;
                            while (id_pre != (byte)Systems.Class.CLASS_IDS.New) {
                                if (id == id_pre) {
                                    return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                                }
                                else {
                                    id_pre = Systems.Class.CLASS_LOOKUP[id_pre].ID_Prereq;
                                }
                            }
                            id_pre = Systems.Class.CLASS_LOOKUP[id].ID_Prereq;
                            while (id_pre != (byte)Systems.Class.CLASS_IDS.New) {
                                if (id_other == id_pre) {
                                    return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                                }
                                else {
                                    id_pre = Systems.Class.CLASS_LOOKUP[id_pre].ID_Prereq;
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
                Main.NewText("Failed to set class because secondary class feature is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
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

                    case CLASS_VALIDITY.INVALID_LEVEL:
                        Main.NewText("Failed to set class because it is locked!", UI.Constants.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_NON_LOCAL:
                        Commons.Error("Tried to set non-local player with SetClass! (please report)");
                        break;

                    default:
                        Commons.Error("Failed to set class for unknown reasons! (please report)");
                        break;
                }
            }

            //default
            return false;
        }

        public bool LocalSwapClass() {
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
                while (c.Tier > 0) {
                    sum_primary += (c.Attribute_Growth[id] * Math.Min(Class_Levels[c.ID], Systems.Class.MAX_LEVEL[c.Tier]));
                    c = Systems.Class.CLASS_LOOKUP[c.ID_Prereq];
                }

                c = Class_Secondary;
                while (c.Tier > 0) {
                    sum_secondary += (c.Attribute_Growth[id] * Math.Min(Class_Levels[c.ID], Systems.Class.MAX_LEVEL[c.Tier]));
                    c = Systems.Class.CLASS_LOOKUP[c.ID_Prereq];
                }

                if (Class_Secondary_Level_Effective > 0) {
                    Attributes_Base[id] = (short)Math.Floor((sum_primary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS * Systems.Attribute.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY) +
                                                            (sum_secondary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS * Systems.Attribute.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY));
                }
                else {
                    Attributes_Base[id] = (short)Math.Floor(sum_primary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS);
                }
            }

            //allocated attributes
            short class_sum = 0;
            for (byte id = 0; id < (byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs; id++) {
                if (Systems.Class.CLASS_LOOKUP[id].Gives_Allocation_Attributes && Class_Unlocked[id]) {
                    class_sum += Math.Min(Class_Levels[id], Systems.Class.MAX_LEVEL[Systems.Class.CLASS_LOOKUP[id].Tier]);
                }
            }
            int temp;
            Attribute_Points_Total = (byte)Math.DivRem(class_sum, Systems.Attribute.LEVELS_PER_ATTRIBUTE, out temp);
            Levels_To_Next_Point = (byte)temp;
            Attribute_Points_Allocated = 0;
            foreach (short allocated in Attributes_Allocated) {
                Attribute_Points_Allocated += allocated;
            }
            Attribute_Points_Unallocated = (short)(Attribute_Points_Total - Attribute_Points_Allocated);

            //sum attributes
            LocalCalculateFinalAttributes();

            //TODO: unlock (temp)
            byte level_req;
            for (byte id = 0; id<(byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs; id++) {
                if (Class_Unlocked[id])
                    continue;

                c = Systems.Class.CLASS_LOOKUP[id];
                switch (c.Tier) {
                    case 2:
                        level_req = Systems.Class.LEVEL_REQUIRED_TIER_2;
                        break;

                    case 3:
                        level_req = Systems.Class.LEVEL_REQUIRED_TIER_3;
                        break;

                    default:
                        continue;
                }
                if (Class_Levels[c.ID_Prereq] >= level_req) {
                    Class_Unlocked[id] = true;
                    if (Class_Levels[id] < 1) Class_Levels[id] = 1;
                    AnnounceClassUnlock(c);
                }
            }

            //update UI
            UI.UIClass.Instance.UpdateClassInfo();
            UI.UIBars.Instance.Update();

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
            if (Class_Primary_Level_Effective > Systems.Class.MAX_LEVEL[Class_Primary.Tier]) {
                Class_Primary_Level_Effective = Systems.Class.MAX_LEVEL[Class_Primary.Tier];
            }

            //subclass secondary effective level penalty
            if (Class_Secondary.Tier > Class_Primary.Tier) {
                //subclass of higher tier limited to lv1
                Class_Secondary_Level_Effective = 1;
            }
            else if (Class_Secondary.Tier == Class_Primary.Tier) {
                //subclass of same tier limited to half primary
                Class_Secondary_Level_Effective = (byte)Math.Min(Class_Secondary_Level_Effective, Class_Primary_Level_Effective / 2);
            }//subclass of lower tier has no penalty

            //level cap secondary
            if (Class_Secondary_Level_Effective > Systems.Class.MAX_LEVEL[Class_Secondary.Tier]) {
                Class_Secondary_Level_Effective = Systems.Class.MAX_LEVEL[Class_Secondary.Tier];
            }
        }

        public void LocalAddXP(double xp) {
            if (!Is_Local_Player) return;

            //no xp if no class
            if (Class_Primary_Level_Effective > 0) {
                //5% bonus xp if well fed
                if (player.wellFed)
                    xp *= 1.05d;

                //display
                if (show_xp) {
                    show_xp_value += Math.Max(Math.Floor(xp), 1);
                }

                //store current effective levels
                byte effective_primary = Class_Primary_Level_Effective;
                byte effective_secondary = Class_Secondary_Level_Effective;

                //add xp
                if (Class_Secondary_Level_Effective > 0) {
                    //subclass penalty
                    AddXP(Class_Primary.ID, (uint)Math.Max(Math.Floor(xp * Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY), 1));
                    AddXP(Class_Secondary.ID, (uint)Math.Max(Math.Floor(xp * Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY), 1));
                }
                else {
                    //single class
                    AddXP(Class_Primary.ID, (uint)Math.Max(Math.Floor(xp), 1));
                }

                //level up
                while ((Class_Levels[Class_Primary.ID] < Systems.Class.MAX_LEVEL[Class_Primary.Tier]) && (Class_XP[Class_Primary.ID] >= Systems.XP.GetXPReq(Class_Primary.Tier, Class_Levels[Class_Primary.ID]))) {
                    SubtractXP(Class_Primary.ID, Systems.XP.GetXPReq(Class_Primary.Tier, Class_Levels[Class_Primary.ID]));
                    Class_Levels[Class_Primary.ID]++;
                    AnnounceLevel(Class_Primary);
                }
                while ((Class_Levels[Class_Secondary.ID] < Systems.Class.MAX_LEVEL[Class_Secondary.Tier]) && (Class_XP[Class_Secondary.ID] >= Systems.XP.GetXPReq(Class_Secondary.Tier, Class_Levels[Class_Secondary.ID]))) {
                    SubtractXP(Class_Secondary.ID, Systems.XP.GetXPReq(Class_Secondary.Tier, Class_Levels[Class_Secondary.ID]));
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
                    UI.UIBars.Instance.Update();
                }
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

        public void AnnounceClassUnlock(Systems.Class c) {
            //client/singleplayer only
            if (ExperienceAndClasses.IS_SERVER)
                return;

            Main.NewText("You have unlocked " + c.Name + "!", UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
        }

        public void AnnounceLevel(Systems.Class c) {
            //client/singleplayer only
            if (ExperienceAndClasses.IS_SERVER)
                return;

            byte level = Class_Levels[c.ID];

            string message = "";
            if (level == Systems.Class.MAX_LEVEL[c.Tier]) {
                message = "You are now a MAX level " + c.Name + "!";
            }
            else {
                message = "You are now a level " + level + " " + c.Name + "!";
            }

            Main.NewText(message, UI.Constants.COLOUR_MESSAGE_ANNOUNCE);
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
                    PacketHandler.SendForceClass(me, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective);
                }

                //final attribute
                for (byte i=0; i<(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                    if (clone.Attributes_Final[i] != Attributes_Final[i]) {
                        PacketHandler.SendForceAttribute(me, Attributes_Final);
                        break;
                    }
                }

                //afk
                if (clone.AFK != AFK) {
                    PacketHandler.SendAFK(me, AFK);
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
            PacketHandler.SendForceFull((byte)player.whoAmI, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective, Attributes_Final, AFK);
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

        public void ForceAttribute(short[] attributes) {
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

        public void LocalAttributeAllocation(byte id, short adjustment) {
            if (!Is_Local_Player) {
                Commons.Error("Cannot set attribute allocation for non-local player");
                return;
            }

        if ((Attributes_Allocated[id] < 0 && adjustment > 0) || ((Attribute_Points_Unallocated >= adjustment) && ((Attributes_Allocated[id] + adjustment) >= 0))) {
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
                Attributes_Final[id] = (short)(Attributes_Base[id] + Attributes_Allocated[id] + Attributes_Bonus[id]);
            }
        }

        //dexterity
        public override float UseTimeMultiplier(Item item) {
            if (item.damage > 0 || item.summon || item.sentry)
                return base.UseTimeMultiplier(item) * attack_cast_speed;
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
            List<short> attributes_allocated = new List<short>();
            foreach (short value in Attributes_Allocated) {
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
                {"eac_ui_class_pinned", UI.UIClass.Instance.panel.Pinned },
                {"eac_ui_bars_left", UI.UIBars.Instance.panel.GetLeft() },
                {"eac_ui_bars_top", UI.UIBars.Instance.panel.GetTop() },
                {"eac_ui_bars_auto", UI.UIBars.Instance.panel.Auto },
                {"eac_ui_bars_pinned", UI.UIBars.Instance.panel.Pinned },
                {"eac_class_unlock", class_unlocked },
                {"eac_class_xp", class_xp },
                {"eac_class_level", class_level },
                {"eac_class_current_primary", Class_Primary.ID },
                {"eac_class_current_secondary", Class_Secondary.ID },
                {"eac_class_subclass_unlocked", Allow_Secondary },
                {"eac_attribute_allocation", attributes_allocated },
                {"eac_wof", Killed_WOF },
                {"eac_settings_show_xp", show_xp},
            };
        }

        public override void Load(TagCompound tag) {
            //some settings must be applied after init
            load_tag = tag;

            //get version in case needed
            Load_Version = Commons.TryGet<int[]>(load_tag, "eac_version", new int[3]);

            //has killed wof
            Killed_WOF = Commons.TryGet<bool>(load_tag, "eac_wof", Killed_WOF);

            //subclass unlocked
            Allow_Secondary = Commons.TryGet<bool>(load_tag, "eac_class_current_primary", Allow_Secondary);

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
                while ((Class_Levels[id] < Systems.Class.MAX_LEVEL[Systems.Class.CLASS_LOOKUP[id].Tier]) && (Class_XP[id] >= Systems.XP.GetXPReq(Systems.Class.CLASS_LOOKUP[id].Tier, Class_Levels[id]))) {
                    SubtractXP(id, Systems.XP.GetXPReq(Systems.Class.CLASS_LOOKUP[id].Tier, Class_Levels[id]));
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
            List<short> attribute_allocation = Commons.TryGet<List<short>>(load_tag, "eac_attribute_allocation", new List<short>());
            for(byte i = 0; i < attribute_allocation.Count; i++) {
                if (Systems.Attribute.ATTRIBUTE_LOOKUP[i].Active) {
                    Attributes_Allocated[i] = attribute_allocation[i];
                }
            }
            
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Misc ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void SetAfk(bool afk) {
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
                PacketHandler.SendHeal((byte)Main.LocalPlayer.whoAmI, (byte)player.whoAmI, amount_life, amount_mana);
            }
        }

    }
}
