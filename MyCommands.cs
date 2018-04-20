using System;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses
{
    class Command_expbar : CommandTemplate
    {
        public Command_expbar()
        {
            name = "expbar";
            argstr = "";
            desc = "toggle exp bar visibility";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);
            localMyPlayer.UIShow = !localMyPlayer.UIShow;
            if (localMyPlayer.UIShow)
                Main.NewText("Experience bar enabled. Display will be visible while wearing a Class Token.");
            else
                Main.NewText("Experience bar hidden.");
        }
    }

    class Command_expbartrans : CommandTemplate
    {
        public Command_expbartrans()
        {
            name = "expbartrans";
            argstr = "";
            desc = "toggle experience bar transparency";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);
            UI.MyUI myUI = (mod as ExperienceAndClasses).myUI;

            localMyPlayer.UITrans = !localMyPlayer.UITrans;
            myUI.setTrans(localMyPlayer.UITrans);
            if
                (localMyPlayer.UITrans) Main.NewText("Experience bar is now transparent.");
            else
                Main.NewText("Experience bar is now opaque.");
        }
    }

    class Command_expbarreset : CommandTemplate
    {
        public Command_expbarreset()
        {
            name = "expbarreset";
            argstr = "";
            desc = "reset experience bar";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);
            UI.MyUI myUI = (mod as ExperienceAndClasses).myUI;

            myUI.setPosition(400f, 100f);
            localMyPlayer.UIShow = true;
            localMyPlayer.UITrans = false;
            myUI.setTrans(localMyPlayer.UITrans);
            Main.NewText("Experience bar reset.");
        }
    }

    class Command_explist : CommandTemplate
    {
        public Command_explist()
        {
            name = "explist";
            argstr = "";
            desc = "lists the level and class of all players";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Player player;
            double exp = 0, expHave, expNeed;
            int level;
            string job, message = "Current Players:";
            for (int playerIndex = 0; playerIndex < 255; playerIndex++)
            {
                player = Main.player[playerIndex];
                if (player.active)
                {
                    //temp
                    exp = player.GetModPlayer<MyPlayer>(mod).GetExp();
                    //Main.NewText(player.name + "=" + exp);

                    job = Methods.Experience.GetClass(player);
                    level = Methods.Experience.GetLevel(exp);

                    expHave = Methods.Experience.GetExpTowardsNextLevel(exp);
                    expNeed = Methods.Experience.GetExpReqForLevel(level + 1, false);

                    message += "\n" + player.name + ", Level " + level + "(" + Math.Round((double)expHave / (double)expNeed * 100, 2) + "%), " + job;
                }
            }
            Main.NewTextMultiline(message);
        }
    }

    class Command_expadd : CommandTemplate
    {
        public Command_expadd()
        {
            name = "expadd";
            argstr = "exp_amount";
            desc = "adds an amount of experience (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);

            double exp = Double.Parse(args[0]);
            Methods.ChatCommands.CommandSetExp(mod, localMyPlayer.GetExp() + exp, input);
        }
    }

    class Command_expsub : CommandTemplate
    {
        public Command_expsub()
        {
            name = "expsub";
            argstr = "exp_amount";
            desc = "subtracts an amount of experience (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);

            double exp = Double.Parse(args[0]);
            Methods.ChatCommands.CommandSetExp(mod, localMyPlayer.GetExp() - exp, input);
        }
    }

    class Command_expset : CommandTemplate
    {
        public Command_expset()
        {
            name = "expset";
            argstr = "exp_amount";
            desc = "sets amount of experience (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            double exp = Double.Parse(args[0]);
            Methods.ChatCommands.CommandSetExp(mod, exp, input);
        }
    }

    class Command_exprate : CommandTemplate
    {
        public Command_exprate()
        {
            name = "exprate";
            argstr = "[new_rate]";
            desc = "displays or sets experience rate (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                Main.NewText("The current exprate is " + (ExperienceAndClasses.globalExpModifier * 100) + "%.");
            }
            else
            {
                double rate = Double.Parse(args[0]);
                Methods.ChatCommands.CommandSetExpRate(mod, rate / 100, input);
            }
        }
    }

    class Command_explvladd : CommandTemplate
    {
        public Command_explvladd()
        {
            name = "explvladd";
            argstr = "[level_amount]";
            desc = "add an amount of levels (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);

            int amt;
            if (args.Length == 0) amt = 1;
            else amt = Int32.Parse(args[0]);

            double exp = localMyPlayer.GetExp();
            int level = Methods.Experience.GetLevel(exp) + amt;
            exp = Methods.Experience.GetExpReqForLevel(level, true);

            Methods.ChatCommands.CommandSetExp(mod, exp, input);
        }
    }

    class Command_explvlsub : CommandTemplate
    {
        public Command_explvlsub()
        {
            name = "explvlsub";
            argstr = "[level_amount]";
            desc = "subtract an amount of levels (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);

            int amt;
            if (args.Length == 0) amt = 1;
            else amt = Int32.Parse(args[0]);

            double exp = localMyPlayer.GetExp();
            int level = Methods.Experience.GetLevel(exp) - amt;
            exp = Methods.Experience.GetExpReqForLevel(level, true);
            if (exp < 0) exp = 0;

            Methods.ChatCommands.CommandSetExp(mod, exp, input);
        }
    }

    class Command_explvlset : CommandTemplate
    {
        public Command_explvlset()
        {
            name = "explvlset";
            argstr = "level_amount";
            desc = "set amount of levels (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0) return;

            int level = Int32.Parse(args[0]);
            Methods.ChatCommands.CommandSetExp(mod, Methods.Experience.GetExpReqForLevel(level, true), input);
        }
    }

    class Command_expmsg : CommandTemplate
    {
        public Command_expmsg()
        {
            name = "expmsg";
            argstr = "";
            desc = "toggle exp gain messages";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);

            localMyPlayer.displayExp = !localMyPlayer.displayExp;
            if (localMyPlayer.displayExp)
            {
                Main.NewText("Experience messages enabled.");
            }
            else
            {
                Main.NewText("Experience messages disabled.");
            }
        }
    }

    class Command_expclasscaps : CommandTemplate
    {
        public Command_expclasscaps()
        {
            name = "expclasscaps";
            argstr = "";
            desc = "toggle enforcing class bonus caps (map setting, requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Methods.ChatCommands.CommandToggleCaps(mod, input);
        }
    }

    class Command_expauth : CommandTemplate
    {
        public Command_expauth()
        {
            name = "expauth";
            argstr = "[auth_code]";
            desc = "displays auth status or attempts authorization with the auth_code";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode == 0)
                Main.NewText("Auth is only for multiplayer use.");
            else
            {
                if (args.Length == 0)
                {
                    Methods.ChatCommands.CommandAuth(mod, -1);
                }
                else
                {
                    double code = Double.Parse(args[0]);
                    Methods.ChatCommands.CommandAuth(mod, code);
                }
            }
        }
    }

    class Command_explvlcap : CommandTemplate
    {
        public Command_explvlcap()
        {
            name = "explvlcap";
            argstr = "[level_cap]";
            desc = "display or set the level cap (map setting, requires expauth in multiplayer, 0 to disable)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                if (ExperienceAndClasses.globalLevelCap <= 0)
                {
                    Main.NewText("Level cap is disabled.");
                }
                else
                {
                    Main.NewText("Level cap is " + ExperienceAndClasses.globalLevelCap + ".");
                }
            }
            else
            {
                int lvl = Int32.Parse(args[0]);
                Methods.ChatCommands.CommandLvlCap(mod, lvl, input);
            }
        }
    }

    class Command_expdmgred : CommandTemplate
    {
        public Command_expdmgred()
        {
            name = "expdmgred";
            argstr = "[damage_reduction_percent]";
            desc = "display or set the class damage reduction (map setting, requires expauth in multiplayer, 0 to disable)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                if (ExperienceAndClasses.globalClassDamageReduction <= 0)
                {
                    Main.NewText("Damage reduction is disabled.");
                }
                else
                {
                    Main.NewText("Damage reduction is " + ExperienceAndClasses.globalClassDamageReduction + "%.");
                }
            }
            else
            {
                int dmgred = Int32.Parse(args[0]);
                if (dmgred <= 0) dmgred = -1;
                Methods.ChatCommands.CommandDmgRed(mod, dmgred, input);
            }
        }
    }

    class Command_expnoauth : CommandTemplate
    {
        public Command_expnoauth()
        {
            name = "expnoauth";
            argstr = "";
            desc = "toggles auth requirement for current map (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Methods.ChatCommands.CommandRequireAuth(mod, input);
        }
    }

    class Command_exptrace : CommandTemplate
    {
        public Command_exptrace()
        {
            name = "exptrace";
            argstr = "";
            desc = "toggles trace for debuging this character";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            MyPlayer localMyPlayer = caller.Player.GetModPlayer<MyPlayer>(mod);
            localMyPlayer.traceChar = !localMyPlayer.traceChar;
            if (localMyPlayer.traceChar)
                Main.NewText("Character trace is enabled.");
            else
                Main.NewText("Character trace is disabled");
        }
    }

    class Command_expmaptrace : CommandTemplate
    {
        public Command_expmaptrace()
        {
            name = "expmaptrace";
            argstr = "";
            desc = "toggles trace for debuging this map (requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Methods.ChatCommands.CommandMapTrace(mod, input);
        }
    }

    class Command_expauthcode : CommandTemplate
    {
        public Command_expauthcode()
        {
            name = "expauthcode";
            argstr = "[new_auth_code]";
            desc = "display or change the map expauth code (code changes always requires expauth in multiplayer)";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                Methods.ChatCommands.CommandShowAuthCode(mod);
            }
            else
            {
                double newCode = Double.Parse(args[0]);
                if (newCode < 0) newCode = 0;
                newCode = Math.Round(newCode);
                Methods.ChatCommands.CommandSetAuthCode(mod, newCode, input);
            }
        }
    }

    class Command_expsettings : CommandTemplate
    {
        public Command_expsettings()
        {
            name = "expsettings";
            argstr = "";
            desc = "displays the current settings";
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Methods.ChatCommands.CommandDisplaySettings();
        }
    }

    public abstract class CommandTemplate : ModCommand
    {
        public string name, argstr, desc;

        public override CommandType Type
        {
            get { return CommandType.Chat; }
        }

        public override string Command
        {
            get { return name; }
        }

        public override string Usage
        {
            get { return "/"+name+" "+argstr; }
        }

        public override string Description
        {
            get { return "ExperienceAndClasses: "+desc; }
        }
    }
}
