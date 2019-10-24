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

        [Label("TEST")]
        [Tooltip("TEST")]
        [DefaultValue(true)]
        public bool Test { get; set; }

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

        [Header("$Mods.ExperienceAndClasses.Common.Config_Header_Permissions")]

        [Label("$Mods.ExperienceAndClasses.Common.Config_Password_Label")]
        [Tooltip("$Mods.ExperienceAndClasses.Common.Config_Password_Tooltip")]
        [DefaultValue("")]
        public string Password { get; set; }

        public override void OnChanged() {
            Systems.Password.UpdateLocalPassword();
        }
    }

}
