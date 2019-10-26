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

        //Rewards
        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_XP")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_XP_Rate_Label")]
        [Range(0f,10f)]
        [Increment(.05f)]
        [DefaultValue(1f)]
        public float XPRate { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_XP_DeathPenalty_Label")]
        [Range(0f, 1f)]
        [Increment(.01f)]
        [DefaultValue(0.05f)]
        public float XPDeathPenalty { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_XP_ModPerPlayer_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_XP_ModPerPlayer_Tooltip")]
        [Range(0f, 1f)]
        [Increment(0.01f)]
        [DefaultValue(0.2f)]
        public float XPModPerPlayer { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_Reward_Distance_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Reward_Distance_Tooltip")]
        [Range(0f, 10000f)]
        [Increment(100f)]
        [DefaultValue(1000f)]
        public float RewardDistance { get; set; } //TODO - unused



        //AFK
        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_AFK")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Enabled")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_AFK_Enabled_Tooltip")]
        [DefaultValue(true)]
        public bool AFKEnabled { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_AFK_Seconds_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_AFK_Seconds_Tooltip")]
        [Range(30,1800)]
        [DefaultValue(60)]
        public int AFKSeconds { get; set; } //TODO - unused



        //DEBUG
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
                    client_password = Main.player[whoAmI].GetModPlayer<EACPlayer>().Fields.password;
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
        public bool UIMain_Drag { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIAutoMode_Label")]
        [DefaultValue(UIAutoMode.InventoryOpen)]
        [DrawTicks]
        public UIAutoMode UIMain_AutoMode { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIPosition_Label")]
        public int[] UIMain_Position { get; set; } = new int[] { 100 , 100 }; //TODO - unused



        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_UIHUD")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIDrag_Label")]
        [DefaultValue(true)]
        public bool UIHUD_Drag { get; set; } = true; //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIAutoMode_Label")]
        [DefaultValue(UIAutoMode.Always)]
        [DrawTicks]
        public UIAutoMode UIHUD_AutoMode { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIPosition_Label")]
        public int[] UIHUD_Position { get; set; } = new int[] { 50, 50 }; //TODO - unused



        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_Permissions")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Password_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Password_Tooltip")]
        [DefaultValue("")]
        public string Password { get; set; }



        public override void OnChanged() {
            //TODO - hide ui if set never (main and hud)


            //apply new auto ui states
            Shortcuts.SetUIAutoStates();

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
