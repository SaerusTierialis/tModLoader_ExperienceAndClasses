using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    class ConfigServer : ModConfig {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_Debug")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Trace_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Trace_Tooltip")]
        [DefaultValue(true)]
        public bool PacketTrace { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
            if (Systems.Password.PlayerHasAccess(whoAmI)) {
                return true;
            }
            else {
                string client_password = "";
                if ((whoAmI >= 0) && (whoAmI < Main.maxPlayers) && (Main.player[whoAmI].active)) {
                    client_password = Main.player[whoAmI].GetModPlayer<EACPlayer>().password;
                }
                message = "Client password does not match world password. Password attempted:"+client_password;
                return false;
            }
        }

    }

    class ConfigClient : ModConfig {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_UIMain")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIDrag_Label")]
        [DefaultValue(true)]
        public bool UIMain_Drag;

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIAutoMode_Label")]
        [DefaultValue(UIAutoMode.InventoryOpen)]
        [DrawTicks]
        public UIAutoMode UIMain_AutoMode;

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIPosition_Label")]
        public int[] UIMain_Position = new int[] { 100 , 100 };



        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_UIHUD")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIDrag_Label")]
        [DefaultValue(true)]
        public bool UIHUD_Drag = true;

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIAutoMode_Label")]
        [DefaultValue(UIAutoMode.Always)]
        [DrawTicks]
        public UIAutoMode UIHUD_AutoMode;

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIPosition_Label")]
        public int[] UIHUD_Position = new int[] { 50, 50 };


        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_Permissions")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Password_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Password_Tooltip")]
        [DefaultValue("")]
        public string Password { get; set; }



        public override void OnChanged() {
            //TODO - redraw UI


            //update password
            Systems.Password.UpdateLocalPassword();
        }
    }

    public enum UIAutoMode {
        Never,
        InventoryOpen,
        InventoryClosed,
        Always
    }

}
