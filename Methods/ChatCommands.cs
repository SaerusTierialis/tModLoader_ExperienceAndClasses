using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Methods
{
    public static class ChatCommands
    {
        static Mod mod = ModLoader.GetMod("ExperienceAndClasses");

        public static void CommandSetExp(double exp, string text)
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
                Methods.PacketSender.ClientRequestAddExp(player.whoAmI, expAdd, text);
                Main.NewTextMultiline("Request that experience be set to " + exp + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public static void CommandExpRate()
        {
            if (Main.netMode == 0)
            {
                Main.NewText("Your current exprate is " + (Main.LocalPlayer.GetModPlayer<MyPlayer>(mod).experienceModifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientAsksExpRate();
                Main.NewText("Request for exprate has been sent to the server.");
            }
        }

        public static void CommandSetExpRate(double rate, string text)
        {
            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                myPlayer.experienceModifier = rate;
                Main.NewText("The new exprate is " + (myPlayer.experienceModifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientRequestExpRate(rate, text);
                Main.NewTextMultiline("Request that exprate be set to " + (rate * 100) + "% has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public static void CommandToggleCaps(string text)
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
                Methods.PacketSender.ClientRequestToggleClassCap(!ExperienceAndClasses.globalIgnoreCaps, text);
                Main.NewTextMultiline("Request to toggle the class caps feature has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public static void CommandAuth(double code)//code -1 to check auth
        {
            if (Main.netMode != 1) return;
            Methods.PacketSender.ClientTryAuth(code);
            Main.NewTextMultiline("Request to authenticate has been sent to the server." +
                                "\nIf successful, you will receive a response shortly.");
        }

        public static void CommandLvlCap(int level)
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
                Methods.PacketSender.ClientUpdateLvlCap(level);
                Main.NewText("Request to change level cap to " + level + " has been sent to the server.");
            }
        }

        public static void CommandDmgRed(int damageReductionPercent)
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
                Methods.PacketSender.ClientUpdateDmgRed(damageReductionPercent);
                Main.NewText("Request to change damage reduction to " + damageReductionPercent + "% has been sent to the server.");
            }
        }

        public static void CommandRequireAuth()
        {
            if (Main.netMode != 0)
            {
                Main.NewText("mod command functions only in singleplayer mode.");
            }
            else
            {
                ExperienceAndClasses.requireAuth = !ExperienceAndClasses.requireAuth;
                if (ExperienceAndClasses.requireAuth) Main.NewText("Require Auth has been enabled. mod map will now require auth in multiplayer mode.");
                else Main.NewText("Require Auth has been disabled. mod map will no longer require auth in multiplayer mode.");
            }
        }
    }
}
