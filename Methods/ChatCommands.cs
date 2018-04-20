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
                ExperienceAndClasses.globalExpModifier = rate;
                Main.NewText("The new exprate is " + (ExperienceAndClasses.globalExpModifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                PacketSender.ClientRequestExpRate(mod, rate, text);
                Main.NewTextMultiline("Request that experience rate be set to " + (rate * 100) + "% has been sent to the server." +
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
                ExperienceAndClasses.globalIgnoreCaps = !ExperienceAndClasses.globalIgnoreCaps;
                if (ExperienceAndClasses.globalIgnoreCaps)
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
                PacketSender.ClientRequestIgnoreCaps(mod, !ExperienceAndClasses.globalIgnoreCaps, text);
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
            PacketSender.ClientTryAuth(mod, code);
            Main.NewTextMultiline("Request to authenticate has been sent to the server." +
                                "\nIf successful, you will receive a response shortly.");
        }


        /// <summary>
        /// Command to set level cap (request if multiplayer)
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="level"></param>
        public static void CommandLvlCap(Mod mod, int level, string text)
        {
            if (level > ExperienceAndClasses.MAX_LEVEL)
            {
                level = ExperienceAndClasses.MAX_LEVEL;
            }

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                int priorLevel = Experience.GetLevel(myPlayer.GetExp());
                ExperienceAndClasses.globalLevelCap = level;
                if (ExperienceAndClasses.globalLevelCap <= 0)
                {
                    Main.NewText("Level cap disabled.");
                }
                else
                {
                    Main.NewText("Level cap set to " + ExperienceAndClasses.globalLevelCap + ".");
                }
            }
            else if (Main.netMode == 1)
            {
                PacketSender.ClientRequestLevelCap(mod, level, text);
                Main.NewTextMultiline("Request to set level cap to " + level + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }


        /// <summary>
        /// Command to set class damage reduction (request if multiplayer)
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="damageReductionPercent"></param>
        public static void CommandDmgRed(Mod mod, int damageReductionPercent, string text)
        {
            if (damageReductionPercent <= 0)
            {
                damageReductionPercent = 0;
            }
            else if (damageReductionPercent > 100)
            {
                damageReductionPercent = 100; ;
            }

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.globalClassDamageReduction = damageReductionPercent;
                if (damageReductionPercent <= 0)
                {
                    Main.NewText("Damage reduction disabled.");
                }
                else
                {
                    Main.NewText("Damage reduction set to " + ExperienceAndClasses.globalClassDamageReduction + ".");
                }
            }
            else if (Main.netMode == 1)
            {
                PacketSender.ClientRequestDamageReduction(mod, damageReductionPercent, text);
                Main.NewTextMultiline("Request to set class damage reduction to " + damageReductionPercent + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        /// <summary>
        /// Command for toggling auth requirement of a map
        /// </summary>
        public static void CommandRequireAuth(Mod mod, string text)
        {
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.requireAuth = !ExperienceAndClasses.requireAuth;
                if (ExperienceAndClasses.requireAuth)
                    Main.NewText("Require expauth has been enabled. This map will now require expauth in multiplayer mode.");
                else
                    Main.NewText("Require expauth has been disabled. This map will no longer require expauth in multiplayer mode.");
            }
            else
            {
                PacketSender.ClientRequestNoAuth(mod, text);
                Main.NewTextMultiline("Request to toggle noauth mode has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        /// <summary>
        /// Command for toggling map trace
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="text"></param>
        public static void CommandMapTrace(Mod mod, string text)
        {
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.traceMap = !ExperienceAndClasses.traceMap;
                if (ExperienceAndClasses.traceMap)
                    Main.NewText("Map trace is enabled.");
                else
                    Main.NewText("Map trace is disabled");
            }
            else
            {
                PacketSender.ClientRequestMapTrace(mod, text);
                Main.NewTextMultiline("Request to toggle map trace has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public static void CommandDisplaySettings()
        {
            string lvlcap, dmgred;

            if (ExperienceAndClasses.globalLevelCap > 0)
            {
                lvlcap = ExperienceAndClasses.globalLevelCap.ToString();
            }
            else
            {
                lvlcap = "disabled";
            }

            if (ExperienceAndClasses.globalClassDamageReduction > 0)
            {
                dmgred = ExperienceAndClasses.globalClassDamageReduction.ToString() + "%";
            }
            else
            {
                dmgred = "disabled";
            }

            Main.NewTextMultiline("Require Authorization: " + ExperienceAndClasses.requireAuth + "\nExperience Rate: " + (ExperienceAndClasses.globalExpModifier * 100) +
                        "%\nIgnore Class Caps: " + ExperienceAndClasses.globalIgnoreCaps + "\nLevel Cap: " + lvlcap + "\nClass Damage Reduction: " +
                        dmgred, false, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
        }

        /// <summary>
        /// Command for showing the map's auth code in singleplayer
        /// </summary>
        public static void CommandShowAuthCode(Mod mod)
        {
            if (Main.netMode == 0)
            {
                Main.NewText("This map's expauth code is " + ExperienceAndClasses.authCode);
            }
            else
            {
                Main.NewText("The expauth code is only visible in the console while hosting multiplayer or by using this command in singleplayer mode.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
            }
        }

        /// <summary>
        /// Command for setting the map's auth code (in multiplayer, expauth is always needed)
        /// </summary>
        /// <param name="newCode"></param>
        public static void CommandSetAuthCode(Mod mod, double newCode, string text)
        {
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.authCode = newCode;
                Main.NewText("This map's auth code is now " + ExperienceAndClasses.authCode);
            }
            else
            {
                PacketSender.ClientRequestSetAuthCode(mod, newCode, text);
                Main.NewTextMultiline("Request to set expauth code to " + newCode + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }
    }
}
