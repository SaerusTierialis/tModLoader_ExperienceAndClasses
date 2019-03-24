using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses.Utilities {
    class Netmode {
        //Update netmode on startup (for server) and when joining a world (for singleplayer/client)
        public static bool IS_SERVER, IS_CLIENT, IS_SINGLEPLAYER;
        public static void UpdateNetmode() {
            IS_SERVER = (Main.netMode == NetmodeID.Server);
            IS_CLIENT = (Main.netMode == NetmodeID.MultiplayerClient);
            IS_SINGLEPLAYER = (Main.netMode == NetmodeID.SinglePlayer);
        }
    }
}
