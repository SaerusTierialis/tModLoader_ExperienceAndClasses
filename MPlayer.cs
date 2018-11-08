using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses {
    class MPlayer : ModPlayer {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const long TICKS_PER_FULL_SYNC = TimeSpan.TicksPerMinute * 2;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Vars ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static DateTime time_next_full_sync = DateTime.MaxValue;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (non-syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private TagCompound load_tag;
        public bool Initialized { get; private set; }
        public bool Is_Local_Player { get; private set; }

        public byte[] Class_Levels { get; private set; }
        
        public short[] Attributes_Base { get; private set; }
        public short[] Attributes_Allocated { get; private set; }
        public short[] Attributes_Bonus { get; private set; }
        public short Attribute_Points_Unallocated { get; private set; }
        private short Attribute_Points_Allocated;
        private short Attribute_Points_Total;
        public byte Levels_To_Next_Point { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Systems.Class Class_Primary { get; private set; }
        public Systems.Class Class_Secondary { get; private set; }
        public byte Class_Primary_Level_Effective { get; private set; }
        public byte Class_Secondary_Level_Effective { get; private set; }

        public bool Allow_Secondary { get; private set; }
        public short[] Attributes_Final { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// instanced arrays must be initialized here (also called during cloning, etc)
        /// </summary>
        public override void Initialize() {
            //defaults
            Is_Local_Player = false;
            Allow_Secondary = false;
            Initialized = false;

            //default class level
            Class_Levels = new byte[(byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs];
            Class_Levels[(byte)Systems.Class.CLASS_IDS.Novice] = 1;

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
        }

        /// <summary>
        /// new player enters
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player) {
            // is this the local player?
            if (player.whoAmI == Main.LocalPlayer.whoAmI) {
                //this is the current local player
                Is_Local_Player = true;
                ExperienceAndClasses.LOCAL_MPLAYER = this;

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

                UI.UIStatus.Instance.panel.SetPosition(Commons.TryGet<float>(load_tag, "eac_ui_status_left", 14f), Commons.TryGet<float>(load_tag, "eac_ui_status_top", 100f));
                UI.UIStatus.Instance.panel.Auto = Commons.TryGet<bool>(load_tag, "eac_ui_status_auto", false);
                UI.UIStatus.Instance.panel.Pinned = Commons.TryGet<bool>(load_tag, "eac_ui_status_pinned", false);

                //rehide status buttons
                UI.UIStatus.Instance.panel.HideButtons();

                //temp: show bars and status
                UI.UIBars.Instance.Visibility = true;
                UI.UIStatus.Instance.Visibility = true;

                //apply ui auto
                ExperienceAndClasses.SetUIAutoStates();

                //update class info
                LocalUpdateClassInfo();

                //enter game complete
                Initialized = true;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PostUpdate() {
            if (Initialized) {

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

                    if ((Class_Levels[id] <= 0) && (id != (byte)Systems.Class.CLASS_IDS.None)) {
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
                Main.NewText("Failed to set class because secondary class feature is locked!", ExperienceAndClasses.COLOUR_MESSAGE_ERROR);
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
                        Main.NewText("Failed to set class because combination is invalid!", ExperienceAndClasses.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_ID:
                        Main.NewText("Failed to set class because class id is invalid!", ExperienceAndClasses.COLOUR_MESSAGE_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_LEVEL:
                        Main.NewText("Failed to set class because it is locked!", ExperienceAndClasses.COLOUR_MESSAGE_ERROR);
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

            //set current levels for easier checking
            Class_Primary_Level_Effective = Class_Levels[Class_Primary.ID];
            Class_Secondary_Level_Effective = Class_Levels[Class_Secondary.ID];

            //level cap primary
            if (Class_Primary_Level_Effective > Shared.MAX_LEVEL[Class_Primary.Tier]) {
                Class_Primary_Level_Effective = Shared.MAX_LEVEL[Class_Primary.Tier];
            }

            //limit secondary to half of primary
            Class_Secondary_Level_Effective = (byte)Math.Min(Class_Secondary_Level_Effective, Class_Primary_Level_Effective / 2);

            //level cap secondary
            if (Class_Secondary_Level_Effective > Shared.MAX_LEVEL[Class_Secondary.Tier]) {
                Class_Secondary_Level_Effective = Shared.MAX_LEVEL[Class_Secondary.Tier];
            }

            //base class attributes
            float sum_primary, sum_secondary;
            Systems.Class c;
            for (byte id = 0; id < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; id++) {
                sum_primary = 0;
                sum_secondary = 0;

                c = Class_Primary;
                while (c.Tier > 0) {
                    sum_primary += (c.Attribute_Growth[id] * Math.Min(Class_Levels[c.ID], Shared.MAX_LEVEL[c.Tier]));
                    c = Systems.Class.CLASS_LOOKUP[c.ID_Prereq];
                }

                c = Class_Secondary;
                while (c.Tier > 0) {
                    sum_secondary += (c.Attribute_Growth[id] * Math.Min(Class_Levels[c.ID], Shared.MAX_LEVEL[c.Tier]));
                    c = Systems.Class.CLASS_LOOKUP[c.ID_Prereq];
                }

                if (Class_Secondary_Level_Effective > 0) {
                    Attributes_Base[id] = (short)Math.Floor((sum_primary / Shared.ATTRIBUTE_GROWTH_LEVELS * Shared.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY) +
                                                            (sum_secondary / Shared.ATTRIBUTE_GROWTH_LEVELS * Shared.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY));
                }
                else {
                    Attributes_Base[id] = (short)Math.Floor(sum_primary / Shared.ATTRIBUTE_GROWTH_LEVELS);
                }
            }

            //allocated attributes
            short class_sum = 0;
            for (byte id = 0; id < (byte)Systems.Class.CLASS_IDS.NUMBER_OF_IDs; id++) {
                class_sum += Math.Min(Class_Levels[id], Shared.MAX_LEVEL[Systems.Class.CLASS_LOOKUP[id].Tier]);
            }
            int temp;
            Attribute_Points_Total = (byte)Math.DivRem(class_sum, Shared.LEVELS_PER_ATTRIBUTE, out temp);
            Levels_To_Next_Point = (byte)temp;
            Attribute_Points_Allocated = 0;
            foreach (short allocated in Attributes_Allocated) {
                Attribute_Points_Allocated += allocated;
            }
            Attribute_Points_Unallocated = (short)(Attribute_Points_Total - Attribute_Points_Allocated);

            //sum attributes
            CalculateFinalAttributes();

            //update UI
            UI.UIClass.Instance.UpdateClassInfo();

            //update class features
            UpdateClassInfo();
        }

        public void UpdateClassInfo() {

        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (ExperienceAndClasses.HOTKEY_UI.JustPressed) {
                UI.UIClass.Instance.Visibility = !UI.UIClass.Instance.Visibility;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Syncing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// store prior state to detect changes to sync
        /// </summary>
        /// <param name="clientClone"></param>
        public override void clientClone(ModPlayer clientClone) {
            MPlayer clone = clientClone as MPlayer;

            clone.Class_Primary = Class_Primary;
            clone.Class_Secondary = Class_Secondary;
            clone.Class_Primary_Level_Effective = Class_Primary_Level_Effective;
            clone.Class_Secondary_Level_Effective = Class_Secondary_Level_Effective;
        }

        /// <summary>
        /// look for changes to sync + send any changes via packet
        /// </summary>
        /// <param name="clientPlayer"></param>
        public override void SendClientChanges(ModPlayer clientPlayer) {
            DateTime now = DateTime.Now;
            byte me = (byte)player.whoAmI;
            if (now.CompareTo(time_next_full_sync) > 0) {
                //full sync
                time_next_full_sync = now.AddTicks(TICKS_PER_FULL_SYNC);
                SyncPlayer(-1, me, false);
            }
            else {
                //partial sync...
                MPlayer clone = clientPlayer as MPlayer;

                //class selections and levels
                if ((clone.Class_Primary.ID != Class_Primary.ID) || (clone.Class_Secondary.ID != Class_Secondary.ID) || 
                    (clone.Class_Primary_Level_Effective != Class_Primary_Level_Effective) || (clone.Class_Secondary_Level_Effective != Class_Secondary_Level_Effective)) {
                    PacketSender.SendForceClass(me, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective);
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
            byte me = (byte)player.whoAmI;

            //class selections and levels
            PacketSender.SendForceClass(me, Class_Primary.ID, Class_Primary_Level_Effective, Class_Secondary.ID, Class_Secondary_Level_Effective);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sync Force Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ForceClass(byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            if (Is_Local_Player) {
                Commons.Error("Cannot force class packet for local player");
                return;
            }

            Class_Primary = Systems.Class.CLASS_LOOKUP[primary_id];
            Class_Primary_Level_Effective = primary_level;
            Class_Secondary = Systems.Class.CLASS_LOOKUP[secondary_id];
            Class_Secondary_Level_Effective = secondary_level;

            UpdateClassInfo();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void LocalAttributeAllocation(byte id, short value) {
            if ((Attribute_Points_Unallocated >= value) && ((Attributes_Allocated[id] + value) >= 0)) {
                Attributes_Allocated[id] += value;
                LocalUpdateClassInfo();
            }
        }

        public void CalculateFinalAttributes() {
            for (byte id = 0; id < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; id++) {
                Attributes_Final[id] = (short)(Attributes_Base[id] + Attributes_Allocated[id] + Attributes_Bonus[id]);
            }
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

            return new TagCompound {
                {"eac_save_version", ExperienceAndClasses.VERSION },
                {"eac_ui_class_left", UI.UIClass.Instance.panel.GetLeft() },
                {"eac_ui_class_top", UI.UIClass.Instance.panel.GetTop() },
                {"eac_ui_class_auto", UI.UIClass.Instance.panel.Auto },
                {"eac_ui_class_pinned", UI.UIClass.Instance.panel.Pinned },
                {"eac_ui_bars_left", UI.UIBars.Instance.panel.GetLeft() },
                {"eac_ui_bars_top", UI.UIBars.Instance.panel.GetTop() },
                {"eac_ui_bars_auto", UI.UIBars.Instance.panel.Auto },
                {"eac_ui_bars_pinned", UI.UIBars.Instance.panel.Pinned },
                {"eac_ui_status_left", UI.UIStatus.Instance.panel.GetLeft() },
                {"eac_ui_status_top", UI.UIStatus.Instance.panel.GetTop() },
                {"eac_ui_status_auto", UI.UIStatus.Instance.panel.Auto },
                {"eac_ui_status_pinned", UI.UIStatus.Instance.panel.Pinned },
                {"eac_class_levels", Class_Levels },
                {"eac_class_current_primary", Class_Primary.ID },
                {"eac_class_current_secondary", Class_Secondary.ID },
                {"eac_class_subclass_unlocked", Allow_Secondary },
                {"eac_attribute_allocation", attributes_allocated },
            };
        }

        public override void Load(TagCompound tag) {
            //some settings must be applied after init
            load_tag = tag;

            //subclass unlocked
            Allow_Secondary = Commons.TryGet<bool>(tag, "eac_class_current_primary", Allow_Secondary);

            //current classes
            Class_Primary = Systems.Class.CLASS_LOOKUP[Commons.TryGet<byte>(load_tag, "eac_class_current_primary", Class_Primary.ID)];
            Class_Secondary = Systems.Class.CLASS_LOOKUP[Commons.TryGet<byte>(load_tag, "eac_class_current_secondary", Class_Secondary.ID)];

            //class levels
            byte[] class_levels_loaded = Commons.TryGet<byte[]>(load_tag, "eac_class_levels", new byte[0]);
            for (byte i = 0; i < class_levels_loaded.Length; i++) {
                Class_Levels[i] = class_levels_loaded[i];
            }

            //allocated attributes
            List<short> attribute_allocation = Commons.TryGet<List<short>>(load_tag, "eac_attribute_allocation", new List<short>());
            for(byte i = 0; i < attribute_allocation.Count; i++) {
                if (Systems.Attribute.ATTRIBUTE_LOOKUP[i].Active) {
                    Attributes_Allocated[i] = attribute_allocation[i];
                }
            }
            
        }
    }
}
