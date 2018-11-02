using Terraria.ModLoader;
using Terraria.UI;
using Terraria;
using System.Collections.Generic;
using System;
using System.IO;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System.Reflection;

//needed for compiling outside of Terraria
public class Application
{
    [STAThread]
    static void Main(string[] args) { }
}

namespace ExperienceAndClasses
{
    class ExperienceAndClasses : Mod {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly bool IS_SERVER = (Main.netMode == 2);
        public static readonly bool IS_CLIENT = (Main.netMode == 1);
        public static readonly bool IS_SINGLEPLAYER = (Main.netMode == 0);

        public enum MessageType : byte {
            SYNC_TEST,
        };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static MPlayer LOCAL_MPLAYER;
        public static Mod MOD;

        public static ModHotKey HOTKEY_UI;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static UserInterface user_interface_main;
        public static UI.UIMain user_interface_state_main;

        public static bool inventory_state = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public ExperienceAndClasses() {
            Properties = new ModProperties() {
                Autoload = true,
            };
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Load/Unload ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load() {
            MOD = this;

            //hotkeys
            HOTKEY_UI = RegisterHotKey("Show Class Interface", "P");

            //main ui
            user_interface_state_main = new UI.UIMain();
            user_interface_state_main.Activate();
            user_interface_main = new UserInterface();
            user_interface_main.SetState(user_interface_state_main);
        }

        public override void Unload() {
            //hotkeys
            HOTKEY_UI = null;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void UpdateUI(GameTime gameTime) {
            if (user_interface_state_main.GetAuto()) {
                if (inventory_state != Main.playerInventory) {
                    inventory_state = Main.playerInventory;
                    user_interface_state_main.Visible = inventory_state;
                }
            }

            if (user_interface_main != null && user_interface_state_main.Visible)
                user_interface_main.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (MouseTextIndex != -1) {
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer("EAC_UIMain",
                    delegate {
                        if (user_interface_state_main.Visible) {
                            user_interface_state_main.Draw(Main.spriteBatch);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Packets ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            MessageType message_type = (MessageType)reader.ReadByte();
            byte player_ind;
            Player player;
            MPlayer mplayer;
            int int1;
            switch (message_type) {
                case MessageType.SYNC_TEST:
                    //read
                    player_ind = reader.ReadByte();
                    int1 = reader.ReadInt32();

                    //apply
                    player = Main.player[player_ind]; //sender
                    mplayer = player.GetModPlayer<MPlayer>(this);
                    mplayer.sync_test = int1;

                    //relay
                    if (IS_SERVER) {
                        ModPacket packet = MOD.GetPacket();
                        packet.Write((byte)ExperienceAndClasses.MessageType.SYNC_TEST);
                        packet.Write((byte)player_ind);
                        packet.Write(int1);
                        packet.Send(-1, player_ind);
                    }
                    break;

                default:
                    //unknown type
                    break;
            }
        }

    }
}
