using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    class EACPlayer : ModPlayer {

        public bool Is_Local_Player { get; private set; }
        public string password;

        public override void Initialize() {
            Is_Local_Player = false;
            password = "";
        }

        public override void OnEnterWorld(Player player) {
            //Update netmode
            Shortcuts.UpdateNetmode();

            //set local player
            Is_Local_Player = true;
            Shortcuts.LocalPlayerSet(this);

            //Set world password when entering in singleplayer
            Systems.Password.UpdateLocalPassword();
        }

        public override void PreUpdate() {
            if (Shortcuts.IS_PLAYER) {
                //
            }
        }
    }
}
