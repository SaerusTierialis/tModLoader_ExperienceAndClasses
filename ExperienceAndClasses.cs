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
            //first 2 bytes are always type and origin
            byte packet_type = reader.ReadByte();
            int origin = reader.ReadInt32();

            if (packet_type >= 0 && packet_type < (byte)PacketHandler.PACKET_TYPE.NUMBER_OF_TYPES) {
                PacketHandler.LOOKUP[packet_type].Recieve(reader, origin);
            }
            else {
                Commons.Error("Cannot handle packet type id " + packet_type + " originating from " + origin);
            }

            /*
            switch (packet_type) {
                case PacketHandler.PACKET_TYPE.BROADCAST_TRACE:
                    PacketHandler.Broadcast.Recieve(reader, origin);
                    break;

                case PacketHandler.PACKET_TYPE.FORCE_FULL:
                    PacketHandler.ForceFull.Recieve(reader, origin);
                    break;

                case PacketHandler.PACKET_TYPE.FORCE_CLASS:
                    PacketHandler.ForceClass.Recieve(reader, origin);
                    break;

                case PacketHandler.PACKET_TYPE.FORCE_ATTRIBUTE:
                    PacketHandler.ForceAttribute.Recieve(reader, origin);
                    break;

                case PacketHandler.PACKET_TYPE.HEAL:
                    PacketHandler.HEAL.Recieve(reader, origin);
                    break;

                case PacketHandler.PACKET_TYPE.AFK:
                    PacketHandler.AFK.Recieve(reader, origin);
                    break;

                case PacketHandler.PACKET_TYPE.XP:
                    PacketHandler.XP.Recieve(reader, origin);
                    break;

                default:
                    if (trace) {

                        Commons.Trace("Unsupported packet type originating from " + origin);
                    }
                    break;
            }
            */
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Other ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void CheckMultiplater() {
            IS_SERVER = (Main.netMode == 2);
            IS_CLIENT = (Main.netMode == 1);
            IS_SINGLEPLAYER = (Main.netMode == 0);
        }
    }
}
