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

        public bool is_local_player;
        private TagCompound load_tag;
        public byte[] class_levels;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public int sync_test = 0;

        public Systems.Class Class_Primary { get; private set; }
        public Systems.Class Class_Secondary { get; private set; }
        public byte Class_Primary_Level { get; private set; }
        public byte Class_Secondary_Level { get; private set; }
        public bool Allow_Secondary { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// instanced arrays must be initialized here (also called during cloning, etc)
        /// </summary>
        public override void Initialize() {
            //defaults
            is_local_player = false;
            Allow_Secondary = false;

            //default class level
            class_levels = new byte[(byte)Systems.Classes.ID.NUMBER_OF_IDs];
            class_levels[(byte)Systems.Classes.ID.Novice] = 1;

            //default class selection
            Class_Primary = Systems.Classes.CLASS_LOOKUP[(byte)Systems.Classes.ID.Novice];
            Class_Secondary = Systems.Classes.CLASS_LOOKUP[(byte)Systems.Classes.ID.None];
        }

        /// <summary>
        /// new player enters
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player) {
            // is this the local player?
            if (player.whoAmI == Main.LocalPlayer.whoAmI) {
                //this is the current local player
                is_local_player = true;
                ExperienceAndClasses.LOCAL_MPLAYER = this;

                //start timer for next full sync
                time_next_full_sync = DateTime.Now.AddTicks(TICKS_PER_FULL_SYNC);

                //apply saved ui settings
                ExperienceAndClasses.user_interface_state_main.SetPosition(Commons.TryGet<float>(load_tag, "eac_ui_main_left", 100f), Commons.TryGet<float>(load_tag, "eac_ui_main_top", 100f));
                ExperienceAndClasses.user_interface_state_main.SetAuto(Commons.TryGet<bool>(load_tag, "eac_ui_main_auto", true));
                ExperienceAndClasses.user_interface_state_main.SetPinned(Commons.TryGet<bool>(load_tag, "eac_ui_main_pinned", false));

                //update class info
                LocalUpdateClassInfo();
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PostUpdate() {
            if (is_local_player) {
                sync_test = player.statLife;
            }

            //Main.NewText(player.name + " " + sync_test); //working
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
            if (!is_local_player) return CLASS_VALIDITY.INVALID_NON_LOCAL;

            if (id == (byte)Systems.Classes.ID.None) {
                return CLASS_VALIDITY.VALID; //setting to no class is always allowed
            }
            else {
                if (id >= (byte)Systems.Classes.ID.NUMBER_OF_IDs) {
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

                    if ((class_levels[id] <= 0) && (id != (byte)Systems.Classes.ID.None)) {
                        return CLASS_VALIDITY.INVALID_LEVEL; //locked class
                    }
                    else {
                        if (id != id_same) {
                            byte id_pre = id_other;
                            while (id_pre != (byte)Systems.Classes.ID.New) {
                                if (id == id_pre) {
                                    return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                                }
                                else {
                                    id_pre = Systems.Classes.CLASS_LOOKUP[id_pre].ID_Prereq;
                                }
                            }
                            id_pre = Systems.Classes.CLASS_LOOKUP[id].ID_Prereq;
                            while (id_pre != (byte)Systems.Classes.ID.New) {
                                if (id_other == id_pre) {
                                    return CLASS_VALIDITY.INVALID_COMBINATION; //invalid combination (same as other class or one of its prereqs)
                                }
                                else {
                                    id_pre = Systems.Classes.CLASS_LOOKUP[id_pre].ID_Prereq;
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
            if (!is_local_player) {
                Main.NewText("ERROR: Tried to set non-local player with SetClass! (please report)", Shared.COLOUR_ERROR);
                return false;
            }

            //fail if secondary not allowed
            if (!is_primary && !Allow_Secondary) {
                Main.NewText("Failed to set class because secondary class feature is locked!", Shared.COLOUR_ERROR);
                return false;
            }

            byte id_other;
            if (is_primary) {
                id_other = Class_Secondary.ID;
            }
            else {
                id_other = Class_Primary.ID;
            }
            if ((id == id_other) && (id != (byte)Systems.Classes.ID.None)) {
                //if setting to other set class, just swap
                return LocalSwapClass();
            }
            else {
                CLASS_VALIDITY valid = LocalCheckClassValid(id, is_primary);
                switch (valid) {
                    case CLASS_VALIDITY.VALID:
                        if (is_primary) {
                            Class_Primary = Systems.Classes.CLASS_LOOKUP[id];
                        }
                        else {
                            Class_Secondary = Systems.Classes.CLASS_LOOKUP[id];
                        }
                        LocalUpdateClassInfo();
                        return true;

                    case CLASS_VALIDITY.INVALID_COMBINATION:
                        Main.NewText("Failed to set class because combination is invalid!", Shared.COLOUR_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_ID:
                        Main.NewText("Failed to set class because class id is invalid!", Shared.COLOUR_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_LEVEL:
                        Main.NewText("Failed to set class because it is locked!", Shared.COLOUR_ERROR);
                        break;

                    case CLASS_VALIDITY.INVALID_NON_LOCAL:
                        Main.NewText("ERROR: Tried to set non-local player with SetClass! (please report)", Shared.COLOUR_ERROR);
                        break;

                    default:
                        Main.NewText("ERROR: Failed to set class for unknown reasons! (please report)", Shared.COLOUR_ERROR);
                        break;
                }
            }

            //default
            return false;
        }

        public bool LocalSwapClass() {
            //local MPlayer only
            if (!is_local_player) return false;

            Systems.Class temp = Class_Primary;
            Class_Primary = Class_Secondary;
            Class_Secondary = temp;
            LocalUpdateClassInfo();
            return true;
        }

        public void LocalUpdateClassInfo() {
            //local MPlayer only
            if (!is_local_player) return;

            //prevent secondary without primary class (move secondary to primary)
            if ((Class_Primary.ID == (byte)Systems.Classes.ID.New) || (Class_Primary.ID == (byte)Systems.Classes.ID.None)) {
                Class_Primary = Class_Secondary;
                Class_Secondary = Systems.Classes.CLASS_LOOKUP[(byte)Systems.Classes.ID.None];
            }

            //any "new" class should be set
            if (Class_Primary.ID == (byte)Systems.Classes.ID.New) {
                Class_Primary = Systems.Classes.CLASS_LOOKUP[(byte)Systems.Classes.ID.Novice];
            }
            if (Class_Secondary.ID == (byte)Systems.Classes.ID.New) {
                Class_Secondary = Systems.Classes.CLASS_LOOKUP[(byte)Systems.Classes.ID.None];
            }

            //clear secondary if not allowed
            if (!Allow_Secondary) {
                Class_Secondary = Systems.Classes.CLASS_LOOKUP[(byte)Systems.Classes.ID.None];
            }

            //set current levels for easier checking
            Class_Primary_Level = class_levels[Class_Primary.ID];
            Class_Secondary_Level = class_levels[Class_Secondary.ID];

            //update UI
            ExperienceAndClasses.user_interface_state_main.UpdateClassInfo();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (ExperienceAndClasses.HOTKEY_UI.JustPressed) {
                ExperienceAndClasses.user_interface_state_main.Visible = !ExperienceAndClasses.user_interface_state_main.Visible;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Syncing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// store prior state to detect changes to sync
        /// </summary>
        /// <param name="clientClone"></param>
        public override void clientClone(ModPlayer clientClone) {
            MPlayer clone = clientClone as MPlayer;
            clone.sync_test = sync_test;

            //TODO: current classes
            //TODO: class levels
        }

        /// <summary>
        /// look for changes to sync + send any changes via packet
        /// </summary>
        /// <param name="clientPlayer"></param>
        public override void SendClientChanges(ModPlayer clientPlayer) {
            DateTime now = DateTime.Now;
            int me = player.whoAmI;
            if (now.CompareTo(time_next_full_sync) > 0) {
                //full sync
                time_next_full_sync = now.AddTicks(TICKS_PER_FULL_SYNC);
                SyncPlayer(-1, me, false);
            }
            else {
                //partial sync
                MPlayer clone = clientPlayer as MPlayer;
                if (clone.sync_test != sync_test) {
                    ModPacket packet = mod.GetPacket();
                    packet.Write((byte)ExperienceAndClasses.MessageType.SYNC_TEST);
                    packet.Write((byte)me);
                    packet.Write(sync_test);
                    packet.Send(-1, me);
                    Main.NewText("sent");
                }
            }

            //TODO: current classes
            //TODO: class levels
        }

        /// <summary>
        /// full sync (called to share current players with new players + new player with current players)
        /// </summary>
        /// <param name="toWho"></param>
        /// <param name="fromWho"></param>
        /// <param name="newPlayer"></param>
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {

            //TODO: current classes
            //TODO: class levels
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
            return new TagCompound {
                {"eac_ui_main_left", ExperienceAndClasses.user_interface_state_main.GetLeft() },
                {"eac_ui_main_top", ExperienceAndClasses.user_interface_state_main.GetTop() },
                {"eac_ui_main_auto", ExperienceAndClasses.user_interface_state_main.GetAuto() },
                {"eac_ui_main_pinned", ExperienceAndClasses.user_interface_state_main.GetPinned() },
                {"eac_class_levels", class_levels },
                {"eac_class_current_primary", Class_Primary.ID },
                {"eac_class_current_secondary", Class_Secondary.ID },
                {"eac_class_subclass_unlocked", Allow_Secondary },
            };
            //TODO: current classes
            //TODO: class levels
        }

        public override void Load(TagCompound tag) {
            //some settings must be applied after init
            load_tag = tag;

            //subclass unlocked
            Allow_Secondary = Commons.TryGet<bool>(tag, "eac_class_current_primary", Allow_Secondary);

            //current classes
            Class_Primary = Systems.Classes.CLASS_LOOKUP[Commons.TryGet<byte>(load_tag, "eac_class_current_primary", Class_Primary.ID)];
            Class_Secondary = Systems.Classes.CLASS_LOOKUP[Commons.TryGet<byte>(load_tag, "eac_class_current_secondary", Class_Secondary.ID)];

            //class levels
            byte[] class_levels_loaded = Commons.TryGet<byte[]>(load_tag, "eac_class_levels", new byte[0]);
            for (int i = 0; i < class_levels_loaded.Length; i++) {
                class_levels[i] = class_levels_loaded[i];
            }
        }

    }
}
