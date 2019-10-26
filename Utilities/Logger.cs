using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;

namespace ExperienceAndClasses.Utilities {
    class Logger {
        public static void Trace (string message) {
            if (Shortcuts.IS_SERVER) {
                message = "TRACE from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_TRACE);
            }
            else {
                if (Shortcuts.IS_CLIENT) {
                    message = "TRACE from Client" + Main.LocalPlayer.whoAmI + ": " + message;
                    Main.NewText("Sending " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                    PacketHandler.Broadcast.Send(-1, (byte)Main.LocalPlayer.whoAmI, message, PacketHandler.Broadcast.BROADCAST_TYPE.TRACE);
                }
                else {
                    Main.NewText("TRACE: " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                }
            }
            Shortcuts.MOD.Logger.Debug(message);
        }

        public static void Error(string message) {
            message = message + " (please report)";
            if (Shortcuts.IS_SERVER) {
                message = "ExperienceAndClasses-ERROR from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_ERROR);
            }
            else {
                if (Shortcuts.IS_CLIENT) {
                    message = "ExperienceAndClasses-ERROR from Client" + Main.LocalPlayer.whoAmI + ": " + message;
                    Main.NewText("Sending " + message, UI.Constants.COLOUR_MESSAGE_ERROR);
                    PacketHandler.Broadcast.Send(-1, (byte)Main.LocalPlayer.whoAmI, message, PacketHandler.Broadcast.BROADCAST_TYPE.ERROR);
                }
                else {
                    Main.NewText("ExperienceAndClasses-ERROR: " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                }
            }
            Shortcuts.MOD.Logger.Error(message);
        }
    }
}
