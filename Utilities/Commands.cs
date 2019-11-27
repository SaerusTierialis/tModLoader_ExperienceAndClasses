using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Utilities {
    class Commands {
        internal class CommandLocalResetLevels : ModCommand {
            public override CommandType Type => CommandType.Chat;

            public override string Command => "eac_reset_level";

            public override string Description => "reset all character and class levels";

            public override string Usage => "/" + Command  + " yes";

            public override void Action(CommandCaller caller, string input, string[] args) {
                if ((args.Length > 0) && (args[0].ToLower() == "yes")) {
                    Shortcuts.LOCAL_PLAYER.PSheet.LocalResetLevels();
                    Main.NewText("Character and class levels have been reset.", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
                else {
                    Main.NewText("To confirm, please enter /" + Command + " yes", UI.Constants.COLOUR_MESSAGE_ERROR);
                }
            }
        }
    }
}
