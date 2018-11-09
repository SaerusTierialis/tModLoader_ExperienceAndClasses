using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

//needed for compiling outside of Terraria
public class Application
{
    [STAThread]
    static void Main(string[] args) { }
}

namespace ExperienceAndClasses {
    class ExperienceAndClasses : Mod {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Debug ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static bool trace = true;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly byte[] VERSION = new byte[] { 2, 0, 0 };

        //updated client-side when entering game to detect client vs singleplayer mode
        public static bool IS_SERVER = (Main.netMode == 2);
        public static bool IS_CLIENT = (Main.netMode == 1);
        public static bool IS_SINGLEPLAYER = (Main.netMode == 0);

        public enum PACKET_TYPE : byte {
            BROADCAST_TRACE,
            FORCE_FULL,
            FORCE_CLASS,
            FORCE_ATTRIBUTE,
            HEAL,
        };

        //chat colors must go here or server gives "Error on message Terraria.MessageBuffer"
        public static readonly Color COLOUR_MESSAGE_ERROR = new Color(255, 25, 25);
        public static readonly Color COLOUR_MESSAGE_TRACE = new Color(255, 0, 255);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static MPlayer LOCAL_MPLAYER;
        public static Mod MOD;

        public static ModHotKey HOTKEY_UI;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static bool inventory_state = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public ExperienceAndClasses() {
            
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Load/Unload ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load() {
            MOD = this;

            //hotkeys
            HOTKEY_UI = RegisterHotKey("Show Class Interface", "P");

            if (!IS_SERVER) {
                Textures.LoadTextures();
            }
        }

        public override void Unload() {
            MOD = null;

            //hotkeys
            HOTKEY_UI = null;

        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void SetUIAutoStates() {
            inventory_state = Main.playerInventory;
            if (UI.UIClass.Instance.panel.Auto) UI.UIClass.Instance.Visibility = inventory_state;
            if (UI.UIBars.Instance.panel.Auto) UI.UIBars.Instance.Visibility = !inventory_state;
            if (UI.UIStatus.Instance.panel.Auto) UI.UIStatus.Instance.Visibility = !inventory_state;
        }

        public override void UpdateUI(GameTime gameTime) {
            //auto ui states
            if (inventory_state != Main.playerInventory) {
                SetUIAutoStates();
            }

            if (UI.UIStatus.Instance.Visibility) UI.UIStatus.Instance.UI.Update(gameTime);
            if (UI.UIBars.Instance.Visibility) UI.UIBars.Instance.UI.Update(gameTime);
            if (UI.UIClass.Instance.Visibility) UI.UIClass.Instance.UI.Update(gameTime);
            if (UI.UIInfo.Instance.Visibility) UI.UIInfo.Instance.UI.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (MouseTextIndex != -1) {
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer("EAC_UIMain",
                    delegate {
                        if (UI.UIStatus.Instance.Visibility) UI.UIStatus.Instance.state.Draw(Main.spriteBatch);
                        if (UI.UIBars.Instance.Visibility) UI.UIBars.Instance.state.Draw(Main.spriteBatch);
                        if (UI.UIClass.Instance.Visibility) UI.UIClass.Instance.state.Draw(Main.spriteBatch);
                        if (UI.UIInfo.Instance.Visibility) UI.UIInfo.Instance.state.Draw(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Packets ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            //first 2 bytes are always type and sender
            PACKET_TYPE packet_type = (PACKET_TYPE)reader.ReadByte();
            byte origin_id = reader.ReadByte();

            Player origin_player = Main.player[origin_id];
            MPlayer origin_mplayer = origin_player.GetModPlayer<MPlayer>(this);

            /*
            if (trace) {
                Commons.Trace("Recieved " + packet_type + " originating from player " + origin_id);
            }
            */

            PacketHandler.HandlePacketContents(origin_id, origin_player, origin_mplayer, packet_type, reader);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Other ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void CheckMultiplater() {
            IS_SERVER = (Main.netMode == 2);
            IS_CLIENT = (Main.netMode == 1);
            IS_SINGLEPLAYER = (Main.netMode == 0);
        }
    }
}
