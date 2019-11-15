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
        public float XPRate { get; set; }

        [Label("$Mods.ExperienceAndClasses.Common.Config_XP_DeathPenalty_Label")]
        [Range(0f, 1f)]
        [Increment(.01f)]
        [DefaultValue(0.05f)]
        public float XPDeathPenalty { get; set; } //TODO - unused

        [Label("$Mods.ExperienceAndClasses.Common.Config_Reward_ModPerPlayer_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Reward_ModPerPlayer_Tooltip")]
        [Range(0f, 1f)]
        [Increment(0.01f)]
        [DefaultValue(0.2f)]
        public float RewardModPerPlayer { get; set; }

        [Label("$Mods.ExperienceAndClasses.Common.Config_Reward_Distance_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Reward_Distance_Tooltip")]
        [Range(0f, 10000f)]
        [Increment(100f)]
        [DefaultValue(1000f)]
        public float RewardDistance { get; set; }



        //Attributes
        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_Attrbutes")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Attribute_Effect_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Attribute_Effect_Tooltip")]
        [Range(0f, 10f)]
        [Increment(0.1f)]
        [DefaultValue(1f)]
        public float AttributeEffect { get; set; }


        //AFK
        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_AFK")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Enabled")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_AFK_Enabled_Tooltip")]
        [DefaultValue(true)]
        public bool AFKEnabled { get; set; }

        [Label("$Mods.ExperienceAndClasses.Common.Config_AFK_Seconds_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_AFK_Seconds_Tooltip")]
        [Range(30,1800)]
        [DefaultValue(60)]
        public int AFKSeconds { get; set; }



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

        public override void OnChanged() {
            Systems.Attribute.ATTRIBUTE_BONUS_MULTIPLIER = AttributeEffect;
            if (Shortcuts.IS_PLAYER && Shortcuts.LOCAL_PLAYER_VALID)
                Shortcuts.LOCAL_PLAYER.PSheet.Attributes.Apply(false);
        }

    }

    class ConfigClient : ModConfig {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_UIMain")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIDrag_Label")]
        [DefaultValue(true)]
        public bool UIMain_Drag { get; set; }

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIAutoMode_Label")]
        [DefaultValue(UIAutoMode.InventoryOpen)]
        [DrawTicks]
        public UIAutoMode UIMain_AutoMode { get; set; }



        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_UIHUD")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIDrag_Label")]
        [DefaultValue(true)]
        public bool UIHUD_Drag { get; set; } = true;

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIAutoMode_Label")]
        [DefaultValue(UIAutoMode.Always)]
        [DrawTicks]
        public UIAutoMode UIHUD_AutoMode { get; set; }



        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_UIMessages")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_UIMessages_XPOverhead_Label")]
        [DefaultValue(true)]
        public bool UIMisc_XPOverhead { get; set; } = true;

        

        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_Permissions")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Password_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Password_Tooltip")]
        [DefaultValue("")]
        public string Password { get; set; }



        public override void OnChanged() {
            //UI
            Shortcuts.ApplyUIConfig();

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
