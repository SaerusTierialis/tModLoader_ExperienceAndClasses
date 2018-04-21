using System;
using Terraria;
using Terraria.Localization;
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
                Main.NewText("Set experience to " + exp + ".", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
            if (rate < 0) rate = 0;
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.mapExpModifier = rate;
                Main.NewText("The new exprate is " + (ExperienceAndClasses.mapExpModifier * 100) + "%.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
        /// Command to set death penalty (auth and global if multiplayer)
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="rate"></param>
        /// <param name="text"></param>
        public static void CommandSetDeathPenalty(Mod mod, double rate, string text)
        {
            if (rate < 0)
                rate = 0;
            else if (rate > 100)
                rate = 100;
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.mapDeathPenalty = rate;
                Main.NewText("The new death penalty is " + (ExperienceAndClasses.mapDeathPenalty * 100) + "%.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
            }
            else if (Main.netMode == 1)
            {
                PacketSender.ClientRequestDeathPenalty(mod, rate, text);
                Main.NewTextMultiline("Request that death penalty be set to " + (rate * 100) + "% has been sent to the server." +
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
                ExperienceAndClasses.mapIgnoreCaps = !ExperienceAndClasses.mapIgnoreCaps;
                if (ExperienceAndClasses.mapIgnoreCaps)
                {
                    Main.NewText("Class bonus caps disabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                }
                else
                {
                    Main.NewText("Class bonus caps enabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                }
            }
            else if (Main.netMode == 1)
            {
                PacketSender.ClientRequestIgnoreCaps(mod, !ExperienceAndClasses.mapIgnoreCaps, text);
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
                ExperienceAndClasses.mapLevelCap = level;
                if (ExperienceAndClasses.mapLevelCap <= 0)
                {
                    Main.NewText("Level cap disabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                }
                else
                {
                    Main.NewText("Level cap set to " + ExperienceAndClasses.mapLevelCap + ".", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
                ExperienceAndClasses.mapClassDamageReduction = damageReductionPercent;
                if (damageReductionPercent <= 0)
                {
                    Main.NewText("Damage reduction disabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                }
                else
                {
                    Main.NewText("Damage reduction set to " + ExperienceAndClasses.mapClassDamageReduction + ".", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
        public static void CommandmapRequireAuth(Mod mod, string text)
        {
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.mapRequireAuth = !ExperienceAndClasses.mapRequireAuth;
                if (ExperienceAndClasses.mapRequireAuth)
                    Main.NewText("Require expauth has been enabled. This map will now require expauth in multiplayer mode.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                else
                    Main.NewText("Require expauth has been disabled. This map will no longer require expauth in multiplayer mode.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
                ExperienceAndClasses.mapTrace = !ExperienceAndClasses.mapTrace;
                if (ExperienceAndClasses.mapTrace)
                    Main.NewText("Map trace is enabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                else
                    Main.NewText("Map trace is disabled", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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

            if (ExperienceAndClasses.mapLevelCap > 0)
            {
                lvlcap = ExperienceAndClasses.mapLevelCap.ToString();
            }
            else
            {
                lvlcap = "disabled";
            }

            if (ExperienceAndClasses.mapClassDamageReduction > 0)
            {
                dmgred = ExperienceAndClasses.mapClassDamageReduction.ToString() + "%";
            }
            else
            {
                dmgred = "disabled";
            }

            Main.NewTextMultiline("Require Authorization: " + ExperienceAndClasses.mapRequireAuth + "\nExperience Rate: " + (ExperienceAndClasses.mapExpModifier * 100) +
                        "%\nIgnore Class Caps: " + ExperienceAndClasses.mapIgnoreCaps + "\nLevel Cap: " + lvlcap + "\nClass Damage Reduction: " +
                        dmgred + "\nDeath Penalty: " + (ExperienceAndClasses.mapDeathPenalty * 100) + "%", false, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
        }

        /// <summary>
        /// Command for showing the map's auth code in singleplayer
        /// </summary>
        public static void CommandShowmapAuthCode(Mod mod)
        {
            if (Main.netMode == 0)
            {
                Main.NewText("This map's expauth code is " + ExperienceAndClasses.mapAuthCode, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
        public static void CommandSetmapAuthCode(Mod mod, double newCode, string text)
        {
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.mapAuthCode = newCode;
                Main.NewText("This map's auth code is now " + ExperienceAndClasses.mapAuthCode, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
            }
            else
            {
                PacketSender.ClientRequestSetmapAuthCode(mod, newCode, text);
                Main.NewTextMultiline("Request to set expauth code to " + newCode + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /expauth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public static void Trace(string text)
        {
            if (Main.netMode == 2)
            {
                Console.WriteLine(text);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(text), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
            }
            else
            {
                Main.NewText(text, ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
            }
        }
    }
}
