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

        

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly after entering map ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //updated client-side when entering game to detect client vs singleplayer mode
        public static bool IS_SERVER = (Main.netMode == 2);
        public static bool IS_CLIENT = (Main.netMode == 1);
        public static bool IS_SINGLEPLAYER = (Main.netMode == 0);

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
            //make Mod easily available
            MOD = this;

            //hotkeys
            HOTKEY_UI = RegisterHotKey("Show Class Interface", "P");

            //textures
            if (!IS_SERVER) {
                Textures.LoadTextures();
            }

            //calculate xp requirements
            Systems.XP.CalcXPRequirements();
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
            UI.UIStatus.Instance.Visibility = !inventory_state;
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
            PacketHandler.PACKET_TYPE packet_type = (PacketHandler.PACKET_TYPE)reader.ReadByte();
            byte origin_id = reader.ReadByte();

            Player origin_player;
            MPlayer origin_mplayer;
            if ((origin_id >= 0) && (origin_id <= 255)) {
                origin_player = Main.player[origin_id];
                origin_mplayer = origin_player.GetModPlayer<MPlayer>(this);
            }
            else {
                origin_player = null;
                origin_mplayer = null;
            }

            /*
            if (trace) {
                Commons.Trace("Recieved " + packet_type + " originating from " + origin_id);
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
