using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Methods
{
    public static class ChatCommands
    {
        /// <summary>
        /// Command to set current character's experience total (auth if multiplayer).
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="exp"></param>
        /// <param name="text"></param>
        public static void CommandSetExp(Mod mod, double exp, string text)
        {
            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                myPlayer.SetExp(exp);
                Main.NewText("Set experience to " + exp + ".");
            }
            else if (Main.netMode == 1)
            {
                double expAdd = exp - myPlayer.GetExp();
                Methods.PacketSender.ClientRequestAddExp(mod, player.whoAmI, expAdd, text);
                Main.NewTextMultiline("Request that experience be set to " + exp + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }


        /// <summary>
        /// Command to check experience rate (global if multiplayer).
        /// </summary>
        /// <param name="mod"></param>
        public static void CommandExpRate(Mod mod)
        {
            if (Main.netMode == 0)
            {
                Main.NewText("Your current exprate is " + (Main.LocalPlayer.GetModPlayer<MyPlayer>(mod).experienceModifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientAsksExpRate(mod);
                Main.NewText("Request for exprate has been sent to the server.");
            }
        }


        /// <summary>
        /// Command to set experience rate (auth and global if multiplayer).
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="rate"></param>
        /// <param name="text"></param>
        public static void CommandSetExpRate(Mod mod, double rate, string text)
        {
            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                myPlayer.experienceModifier = rate;
                Main.NewText("The new exprate is " + (myPlayer.experienceModifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientRequestExpRate(mod, rate, text);
                Main.NewTextMultiline("Request that exprate be set to " + (rate * 100) + "% has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }


        /// <summary>
        /// Command to toggle class caps (auth and global if multiplayer).
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="text"></param>
        public static void CommandToggleCaps(Mod mod, string text)
        {
            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                myPlayer.ignoreCaps = !myPlayer.ignoreCaps;
                if (myPlayer.ignoreCaps)
                {
                    Main.NewText("Class bonus caps disabled.");
                }
                else
                {
                    Main.NewText("Class bonus caps enabled.");
                }
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientRequestToggleClassCap(mod, !ExperienceAndClasses.globalIgnoreCaps, text);
                Main.NewTextMultiline("Request to toggle the class caps feature has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }


        /// <summary>
        /// Command to attempt auth in multiplayer.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="code"></param>
        public static void CommandAuth(Mod mod, double code)//code -1 to check auth
        {
            if (Main.netMode != 1) return;
            Methods.PacketSender.ClientTryAuth(mod, code);
            Main.NewTextMultiline("Request to authenticate has been sent to the server." +
                                "\nIf successful, you will receive a response shortly.");
        }


        /// <summary>
        /// Command to set current character's level cap (request if multiplayer).
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="level"></param>
        public static void CommandLvlCap(Mod mod, int level)
        {
            if (level < -1 || level == 0 || level > ExperienceAndClasses.MAX_LEVEL) return;

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                int priorLevel = Methods.Experience.GetLevel(myPlayer.GetExp());
                myPlayer.explvlcap = level;
                myPlayer.LimitExp();
                myPlayer.LevelUp(priorLevel);
                (mod as ExperienceAndClasses).myUI.updateValue(myPlayer.GetExp());
                if (level == -1) Main.NewText("Level cap disabled.");
                else Main.NewText("Level cap set to " + myPlayer.explvlcap + ".");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientUpdateLvlCap(mod, level);
                Main.NewText("Request to change level cap to " + level + " has been sent to the server.");
            }
        }


        /// <summary>
        /// Command to set current character's class damage reduction (request if multiplayer).
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="damageReductionPercent"></param>
        public static void CommandDmgRed(Mod mod, int damageReductionPercent)
        {
            if (damageReductionPercent < -1 || damageReductionPercent > 100) return;

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                myPlayer.expdmgred = damageReductionPercent;
                if (damageReductionPercent == -1) Main.NewText("Damage reduction disabled.");
                else Main.NewText("Damage reduction set to " + myPlayer.expdmgred + ".");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientUpdateDmgRed(mod, damageReductionPercent);
                Main.NewText("Request to change damage reduction to " + damageReductionPercent + "% has been sent to the server.");
            }
        }

        /// <summary>
        /// Command for toggling auth requirement of a map while in singleplayer.
        /// </summary>
        public static void CommandRequireAuth()
        {
            if (Main.netMode != 0)
            {
                Main.NewText("This command functions only in singleplayer mode.");
            }
            else
            {
                ExperienceAndClasses.requireAuth = !ExperienceAndClasses.requireAuth;
                if (ExperienceAndClasses.requireAuth) Main.NewText("Require Auth has been enabled. mod map will now require auth in multiplayer mode.");
                else Main.NewText("Require Auth has been disabled. mod map will no longer require auth in multiplayer mode.");
            }
        }

        /// <summary>
        /// Command for showing the map's auth code in singleplayer.
        /// </summary>
        public static void CommandShowAuthCode()
        {
            if (Main.netMode != 0)
            {
                Main.NewText("This command functions only in singleplayer mode.");
            }
            else
            {
                Main.NewText("This map's auth code is " + ExperienceAndClasses.authCode);
            }
        }

        /// <summary>
        /// Command for setting teh map's auth code in singleplayer.
        /// </summary>
        /// <param name="newCode"></param>
        public static void CommandSetAuthCode(double newCode)
        {
            if (Main.netMode != 0)
            {
                Main.NewText("This command functions only in singleplayer mode.");
            }
            else
            {
                ExperienceAndClasses.authCode = newCode;
                Main.NewText("This map's auth code is now " + ExperienceAndClasses.authCode);
            }
        }
    }
}
