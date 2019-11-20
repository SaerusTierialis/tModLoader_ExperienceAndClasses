using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    class Shortcuts {

        //Mod Shortcut
        public static Mod MOD { get; private set; }

        //Time (wall)
        public static DateTime Now { get; private set; }

        //Hotkeys
        public static ModHotKey HOTKEY_UI { get; private set;}
        public static ModHotKey HOTKEY_ALTERNATE_EFFECT { get; private set; }
        public static ModHotKey[] HOTKEY_ABILITY_PRIMARY { get; private set; } = new ModHotKey[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
        public static ModHotKey[] HOTKEY_ABILITY_SECONDARY { get; private set; } = new ModHotKey[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];

        //Netmode
        public static bool IS_SERVER { get; private set; }
        public static bool IS_CLIENT { get; private set; }
        public static bool IS_SINGLEPLAYER { get; private set; }
        public static bool IS_PLAYER { get; private set; }
        public static bool IS_EFFECTIVELY_SERVER { get; private set; }
        public static int WHO_AM_I { get; private set; }

        //ModPlayer
        public static EACPlayer LOCAL_PLAYER { get; private set; }
        public static bool LOCAL_PLAYER_VALID { get; private set; }

        //UI
        public static bool UI_Initialized { get; private set; } = false;
        private static UI.UIStateCombo[] UIs = new UI.UIStateCombo[] { UI.UIOverlay.Instance, UI.UIHUD.Instance, UI.UIMain.Instance, UI.UIPopup.Instance, UI.UIHelp.Instance };
        public static bool Inventory_Open { get; private set; } = false;

        //Recipe
        public const string RECIPE_GROUP_MECHANICAL_SOUL = "ExperienceAndClasses:MechanicalSoul";

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Shortcuts ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //shortcuts to config so I don't have to keep adding ModContent
        public static ConfigClient GetConfigClient { get { return GetInstance<ConfigClient>(); } }
        public static ConfigServer GetConfigServer { get { return GetInstance<ConfigServer>(); } }

        public static int[] Version {
            get {
                return new int[] { MOD.Version.Major, MOD.Version.Minor, MOD.Version.Build };
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Mod Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void DoModLoad(Mod mod) {
            //mod
            MOD = mod;

            //hotkeys (don't use localization text, causes issues)
            HOTKEY_UI = MOD.RegisterHotKey("Toggle UI", "P");
            HOTKEY_ALTERNATE_EFFECT = MOD.RegisterHotKey("Ability Alternate Effect", "LeftShift");
            HOTKEY_ABILITY_PRIMARY[0] = MOD.RegisterHotKey("Primary Class Ability 1", "Q");
            HOTKEY_ABILITY_PRIMARY[1] = MOD.RegisterHotKey("Primary Class Ability 2", "E");
            HOTKEY_ABILITY_PRIMARY[2] = MOD.RegisterHotKey("Primary Class Ability 3", "R");
            HOTKEY_ABILITY_PRIMARY[3] = MOD.RegisterHotKey("Primary Class Ability 4", "F");
            HOTKEY_ABILITY_SECONDARY[0] = MOD.RegisterHotKey("Secondary Class Ability 1", "Z");
            HOTKEY_ABILITY_SECONDARY[1] = MOD.RegisterHotKey("Secondary Class Ability 2", "X");
            HOTKEY_ABILITY_SECONDARY[2] = MOD.RegisterHotKey("Secondary Class Ability 3", "C");
            HOTKEY_ABILITY_SECONDARY[3] = MOD.RegisterHotKey("Secondary Class Ability 4", "V");
            
            //netmode
            UpdateNetmode();

            //clear local player;
            LocalPlayerClear();

            //textures
            if (!IS_SERVER)
                Utilities.Textures.LoadTextures();

            //set global item instances
            Systems.Attribute.RESET_COST_ITEM = GetInstance<Items.Orb_Monster>();

            //TODO: sounds
        }

        public static void DoModUnload() {
            //mod
            MOD = null;

            //hotkeys
            HOTKEY_UI = null;
            HOTKEY_ALTERNATE_EFFECT = null;
            HOTKEY_ABILITY_PRIMARY = new ModHotKey[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];
            HOTKEY_ABILITY_SECONDARY = new ModHotKey[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS];

            //clear local player;
            LocalPlayerClear();

            //not initialized
            UI_Initialized = false;

            //TODO: textures
            //TODO: sounds
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Net Mode ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void UpdateNetmode() {
            IS_SERVER = (Main.netMode == NetmodeID.Server);
            IS_CLIENT = (Main.netMode == NetmodeID.MultiplayerClient);
            IS_SINGLEPLAYER = (Main.netMode == NetmodeID.SinglePlayer);

            IS_PLAYER = IS_CLIENT || IS_SINGLEPLAYER;
            IS_EFFECTIVELY_SERVER = IS_SERVER || IS_SINGLEPLAYER;
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Timing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void UpdateTime() {
            Now = DateTime.Now;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overall UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void InitializeUIs() {
            foreach (UI.UIStateCombo ui in UIs) {
                ui.Initialize();
            }

            UI_Initialized = true;

            ApplyUIConfig();
            UpdateUIPSheet(LOCAL_PLAYER.PSheet);
        }

        public static void UpdateUIs(GameTime gameTime) {
            //update time if non-server
            UpdateTime();

            if (UI_Initialized) {

                //inventory auto states
                if (Inventory_Open != Main.playerInventory) {
                    SetUIAutoStates();
                }

                //update UIs
                foreach (UI.UIStateCombo ui in UIs) {
                    ui.Update(gameTime);
                }

            }
        }

        public static void SetUILayers(List<GameInterfaceLayer> layers) {
            if (UI_Initialized) {
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
                UI.UIOverlay.Instance.UpdatePosition();
            }
        }

        public static void SetUIAutoStates(bool hide_if_never = false) {
            Inventory_Open = Main.playerInventory;
            foreach (UI.UIStateCombo ui in UIs) {
                ApplyUIAuto(ui, hide_if_never);
            }
        }

        private static void ApplyUIAuto(UI.UIStateCombo ui, bool hide_if_never = false) {
            if (UI_Initialized) {
                switch (ui.auto) {
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
                        if (hide_if_never)
                            ui.Visibility = false;
                        break;
                }
            }
        }

        public static void UpdateUIPSheet(Systems.PSheet psheet) {
            if (UI_Initialized && psheet.eacplayer.Fields.Is_Local) {
                //update attribute text
                psheet.Attributes.Apply(false);

                UI.UIHUD.Instance.UpdatePSheet(psheet);
                UI.UIMain.Instance.UpdatePSheet(psheet);
            }
        }

        public static void UpdateUIXP() {
            if (UI_Initialized) {
                UI.UIHUD.Instance.UpdateXP();
                UI.UIMain.Instance.UpdateXP();
            }
        }

        public static void ApplyUIConfig() {
            if (UI_Initialized) {
                ConfigClient config = GetConfigClient;

                UI.UIMain.Instance.auto = config.UIMain_AutoMode;
                UI.UIMain.Instance.panel.can_drag = config.UIMain_Drag;

                UI.UIHUD.Instance.auto = config.UIHUD_AutoMode;
                UI.UIHUD.Instance.panel.can_drag = config.UIHUD_Drag;

                SetUIAutoStates(true);
            }
        }

    }
}
