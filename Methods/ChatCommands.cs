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
                double exp_add = exp - myPlayer.GetExp();
                Methods.PacketSender.ClientRequestAddExp(player.whoAmI, exp_add, text);
                Main.NewTextMultiline("Request that experience be set to " + exp + " has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public static void CommandExpRate()
        {
            if (Main.netMode == 0)
            {
                Main.NewText("Your current exprate is " + (Main.LocalPlayer.GetModPlayer<MyPlayer>(mod).experience_modifier * 100) + "%.");
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
                myPlayer.experience_modifier = rate;
                Main.NewText("The new exprate is " + (myPlayer.experience_modifier * 100) + "%.");
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
                myPlayer.ignore_caps = !myPlayer.ignore_caps;
                if (myPlayer.ignore_caps)
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
                Methods.PacketSender.ClientRequestToggleCap(!ExperienceAndClasses.global_ignore_caps, text);
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
                int prior_level = Methods.Experience.GetLevel(myPlayer.GetExp());
                myPlayer.explvlcap = level;
                myPlayer.LimitExp();
                myPlayer.LevelUp(prior_level);
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

        public static void CommandDmgRed(int damage_reduction_percent)
        {
            if (damage_reduction_percent < -1 || damage_reduction_percent > 100) return;

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            if (Main.netMode == 0)
            {
                myPlayer.expdmgred = damage_reduction_percent;
                if (damage_reduction_percent == -1) Main.NewText("Damage reduction disabled.");
                else Main.NewText("Damage reduction set to " + myPlayer.expdmgred + ".");
            }
            else if (Main.netMode == 1)
            {
                Methods.PacketSender.ClientUpdateDmgRed(damage_reduction_percent);
                Main.NewText("Request to change damage reduction to " + damage_reduction_percent + "% has been sent to the server.");
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
                ExperienceAndClasses.require_auth = !ExperienceAndClasses.require_auth;
                if (ExperienceAndClasses.require_auth) Main.NewText("Require Auth has been enabled. mod map will now require auth in multiplayer mode.");
                else Main.NewText("Require Auth has been disabled. mod map will no longer require auth in multiplayer mode.");
            }
        }
    }
}
