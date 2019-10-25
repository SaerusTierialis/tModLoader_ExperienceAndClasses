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

//Fields: Local
        /// <summary>
        /// Local fields
        /// </summary>
        public FieldContainerLocal FieldsLocal { get; private set; }
        public class FieldContainerLocal {
            /// <summary>
            /// True when player is the local player
            /// </summary>
            public bool Is_Local_Player = false;
        }

//Fields: Synced local to server only
        /// <summary>
        /// Fields synced from local to server
        /// </summary>
        public FieldContainerSyncServer FieldsSyncServer { get; private set; }
        public class FieldContainerSyncServer {
            /// <summary>
            /// Client password for multiplayer authentication
            /// </summary>
            public string password = "";

            /// <summary>
            /// True while player is AFK
            /// </summary>
            public bool AFK = false;
        }

//Fields: Synced to all
        /// <summary>
        /// Fields synced between all
        /// </summary>
        public FieldContainerSyncAll FieldsSyncAll { get; private set; }
        public struct FieldContainerSyncAll {

        }

//Init
        public override void Initialize() {
            FieldsLocal = new FieldContainerLocal();
            FieldsSyncServer = new FieldContainerSyncServer();
            FieldsSyncAll = new FieldContainerSyncAll();
        }

//Overrides
        public override void OnEnterWorld(Player player) {
            //Update netmode
            Shortcuts.UpdateNetmode();

            //set local player
            FieldsLocal.Is_Local_Player = true;
            Shortcuts.LocalPlayerSet(this);

            //Set world password when entering in singleplayer, send password to server when entering multiplayer
            Systems.Password.UpdateLocalPassword();

            //TODO - sync class etc.
        }



    }
}
