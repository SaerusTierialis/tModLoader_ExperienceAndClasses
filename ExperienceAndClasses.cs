using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
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

        public const string RECIPE_GROUP_MECHANICAL_SOUL = "ExperienceAndClasses:MechanicalSoul";

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly after entering map ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //updated client-side when entering game to detect client vs singleplayer mode
        public static bool IS_SERVER, IS_CLIENT, IS_SINGLEPLAYER;

        public static MPlayer LOCAL_MPLAYER;
        public static Mod MOD;

        public static ModHotKey HOTKEY_UI;

        public static UI.UIStateCombo[] UIs = new UI.UIStateCombo[0]; //set on entering world

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static bool inventory_state = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public ExperienceAndClasses() {
            CheckMultiplater();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Load/Unload ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load() {
            //make Mod easily available
            MOD = this;

            //hotkeys
            HOTKEY_UI = RegisterHotKey("Show Class Interface", "P");

            //Textures
            if (!IS_SERVER) {
                Utilities.Textures.LoadTextures();
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
            if (UI.UIMain.Instance.panel.Auto) UI.UIMain.Instance.Visibility = inventory_state;
            if (UI.UIAbility.Instance.panel.Auto) UI.UIAbility.Instance.Visibility = !inventory_state;
            UI.UIStatus.Instance.Visibility = !inventory_state;
        }

        public override void UpdateUI(GameTime gameTime) {
            //auto ui states
            if (inventory_state != Main.playerInventory) {
                SetUIAutoStates();
            }

            foreach (UI.UIStateCombo ui in UIs) {
                ui.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (MouseTextIndex != -1) {
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer("EAC_UIMain",
                    delegate {
                        foreach (UI.UIStateCombo ui in UIs) {
                            ui.Draw();
                        }
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

            if (packet_type >= 0 && packet_type < (byte)Utilities.PacketHandler.PACKET_TYPE.NUMBER_OF_TYPES) {
                Utilities.PacketHandler.LOOKUP[packet_type].Recieve(reader, origin);
            }
            else {
                Utilities.Commons.Error("Cannot handle packet type id " + packet_type + " originating from " + origin);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ RecipeGroup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void AddRecipeGroups() {
            base.AddRecipeGroups();

            // Creates a new recipe group
            RecipeGroup mechanical_soul = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " Mechanical Boss Soul", new int[]
            {
                ItemID.SoulofFright,
                ItemID.SoulofMight,
                ItemID.SoulofSight
            });
            // Registers the new recipe group with the specified name
            RecipeGroup.RegisterGroup(RECIPE_GROUP_MECHANICAL_SOUL, mechanical_soul);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Other ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void CheckMultiplater() {
            IS_SERVER = (Main.netMode == NetmodeID.Server);
            IS_CLIENT = (Main.netMode == NetmodeID.MultiplayerClient);
            IS_SINGLEPLAYER = (Main.netMode == NetmodeID.SinglePlayer);
        }
    }
}
