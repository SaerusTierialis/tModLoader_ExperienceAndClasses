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
            if (rate < 0)
                rate = 0;
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.worldExpModifier = rate;
                Main.NewText("The new exprate is " + (ExperienceAndClasses.worldExpModifier * 100) + "%.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
            else if (rate > 1)
                rate = 1;
            if (Main.netMode == 0)
            {
                ExperienceAndClasses.worldDeathPenalty = rate;
                Main.NewText("The new death penalty is " + (ExperienceAndClasses.worldDeathPenalty * 100) + "%.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
                ExperienceAndClasses.worldIgnoreCaps = !ExperienceAndClasses.worldIgnoreCaps;
                if (ExperienceAndClasses.worldIgnoreCaps)
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
                PacketSender.ClientRequestIgnoreCaps(mod, !ExperienceAndClasses.worldIgnoreCaps, text);
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
                ExperienceAndClasses.worldLevelCap = level;
                if (ExperienceAndClasses.worldLevelCap <= 0)
                {
                    Main.NewText("Level cap disabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                }
                else
                {
                    Main.NewText("Level cap set to " + ExperienceAndClasses.worldLevelCap + ".", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
                ExperienceAndClasses.worldClassDamageReduction = damageReductionPercent;
                if (damageReductionPercent <= 0)
                {
                    Main.NewText("Damage reduction disabled.", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                }
                else
                {
                    Main.NewText("Damage reduction set to " + ExperienceAndClasses.worldClassDamageReduction + ".", ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
                ExperienceAndClasses.worldRequireAuth = !ExperienceAndClasses.worldRequireAuth;
                if (ExperienceAndClasses.worldRequireAuth)
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
                ExperienceAndClasses.worldTrace = !ExperienceAndClasses.worldTrace;
                if (ExperienceAndClasses.worldTrace)
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

        public static void CommandDisplaySettings(Mod mod)
        {
            string lvlcap, dmgred, threshCD, ability_overhead;

            if (ExperienceAndClasses.worldLevelCap > 0)
            {
                lvlcap = ExperienceAndClasses.worldLevelCap.ToString();
            }
            else
            {
                lvlcap = "disabled";
            }

            if (ExperienceAndClasses.worldClassDamageReduction > 0)
            {
                dmgred = ExperienceAndClasses.worldClassDamageReduction.ToString() + "%";
            }
            else
            {
                dmgred = "disabled";
            }

            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);
            if (myPlayer.thresholdCDMsg >= 0f)
            {
                threshCD = myPlayer.thresholdCDMsg + " seconds";
            }
            else
            {
                threshCD = "disabled";
            }

            if (myPlayer.ability_message_overhead)
            {
                ability_overhead = "enabled";
            }
            else
            {
                ability_overhead = "disabled";
            }

            Main.NewTextMultiline("World - Require Authorization: " + ExperienceAndClasses.worldRequireAuth + "\nWorld - Experience Rate: " + (ExperienceAndClasses.worldExpModifier * 100) +
                        "%\nWorld - Ignore Class Caps: " + ExperienceAndClasses.worldIgnoreCaps + "\nWorld - Level Cap: " + lvlcap + "\nWorld - Class Damage Reduction: " +
                        dmgred + "\nWorld - Death Penalty: " + (ExperienceAndClasses.worldDeathPenalty * 100) + "%", false, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);

            Main.NewTextMultiline("Character - Enable AFK: " + myPlayer.allowAFK + "\nCharacter - Display XP Messages: " + myPlayer.displayExp + 
                "\nCharacter - Cooldown Messages Threshold: " + threshCD, false, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);

            Main.NewTextMultiline("Character - UI Show: " + myPlayer.UIShow + "\nCharacter - UI Force Inventory: " + myPlayer.UIInventory + 
                "\nCharacter - UI Exp Bar: " + myPlayer.UIExpBar + "\nCharacter - UI Cooldown Bars: " + myPlayer.UICDBars + 
                "\nCharacter - UI Ability Messages Overhead: " + ability_overhead, false, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
        }

        /// <summary>
        /// Command for showing the map's auth code in singleplayer
        /// </summary>
        public static void CommandShowmapAuthCode()
        {
            if (Main.netMode == 0)
            {
                Main.NewText("This map's expauth code is " + ExperienceAndClasses.worldAuthCode, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
            }
            else
            {
                Main.NewText("The expauth code is only viewable in-game in singleplayer mode.", ExperienceAndClasses.MESSAGE_COLOUR_RED);
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
                ExperienceAndClasses.worldAuthCode = newCode;
                Main.NewText("This map's auth code is now " + ExperienceAndClasses.worldAuthCode, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
