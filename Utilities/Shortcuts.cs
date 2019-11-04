using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    class Shortcuts {

        //Mod Shortcut
        public static Mod MOD { get; private set; }

        //Hotkeys
        public static ModHotKey HOTKEY_UI { get; private set;}

        //Netmode
        public static bool IS_SERVER { get; private set; }
        public static bool IS_CLIENT { get; private set; }
        public static bool IS_SINGLEPLAYER { get; private set; }
        public static bool IS_PLAYER { get; private set; }
        public static int WHO_AM_I { get; private set; }

        //ModPlayer
        public static EACPlayer LOCAL_PLAYER { get; private set; }
        public static bool LOCAL_PLAYER_VALID { get; private set; }

        //UI
        public static UI.UIStateCombo[] UIs = new UI.UIStateCombo[0]; //set on entering world
        public static bool Inventory_Open { get; private set; } = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Shortcuts ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //shortcuts to config so I don't have to keep adding ModContent
        public static ConfigClient GetConfigClient { get { return GetInstance<ConfigClient>(); } }
        public static ConfigServer GetConfigServer { get { return GetInstance<ConfigServer>(); } }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void DoModLoad(Mod mod) {
            //mod
            MOD = mod;

            //hotkeys
            HOTKEY_UI = MOD.RegisterHotKey(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Hotkey_UI"), "P");

            //netmode
            UpdateNetmode();

            //clear local player;
            LocalPlayerClear();

            //TODO: textures
            //TODO: sounds
        }

        public static void DoModUnload() {
            //mod
            MOD = null;

            //hotkeys
            HOTKEY_UI = null;

            //clear local player;
            LocalPlayerClear();

            //TODO: textures
            //TODO: sounds
        }

        public static void UpdateNetmode() {
            IS_SERVER = (Main.netMode == NetmodeID.Server);
            IS_CLIENT = (Main.netMode == NetmodeID.MultiplayerClient);
            IS_SINGLEPLAYER = (Main.netMode == NetmodeID.SinglePlayer);

            IS_PLAYER = IS_CLIENT || IS_SINGLEPLAYER;
        }

        public static void LocalPlayerClear() {
            LOCAL_PLAYER = null;
            LOCAL_PLAYER_VALID = false;
            WHO_AM_I = -1;
        }

        public static void LocalPlayerSet(EACPlayer eacplayer) {
            LOCAL_PLAYER = eacplayer;
            LOCAL_PLAYER_VALID = true;
            WHO_AM_I = LOCAL_PLAYER.player.whoAmI;
            LOCAL_PLAYER.Fields.Is_Local = true;
        }

        public static void UpdateUIs(GameTime gameTime) {
            //inventory auto states
            if (Inventory_Open != Main.playerInventory) {
                SetUIAutoStates();
            }

            //update UIs
            foreach (UI.UIStateCombo ui in UIs) {
                ui.Update(gameTime);
            }
        }

        public static void SetUIAutoStates() {
            Inventory_Open = Main.playerInventory;

            ConfigClient config = GetConfigClient;
            //ApplyUIAuto(UI.UIMain.Instance, config.UIMain_AutoMode);
            //ApplyUIAuto(UI.UIHUD.Instance, config.UIHUD_AutoMode);
        }

        public static void ApplyUIAuto(UI.UIStateCombo ui, UIAutoMode mode) {
            switch (mode) {
                case UIAutoMode.Always:
                    ui.Visibility = true;
                    break;
                case UIAutoMode.InventoryClosed:
                    ui.Visibility = !Inventory_Open;
                    break;
                case UIAutoMode.InventoryOpen:
                    ui.Visibility = Inventory_Open;
                    break;
                case UIAutoMode.Never:
                    //manual
                    break;
            }
        }

    }
}
