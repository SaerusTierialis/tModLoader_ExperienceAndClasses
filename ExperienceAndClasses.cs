using Terraria.ModLoader;
using ExperienceAndClasses.UI;
using Terraria.UI;
using Terraria.DataStructures;
using Terraria;
using System.Collections.Generic;
using System;
using System.IO;

public class Application
{
    [STAThread]
    static void Main(string[] args) { }
}

namespace ExperienceAndClasses
{
    /* For Packets */
    enum ExpModMessageType : byte
    {
        //Player packets are sent by player, Server packets are sent by server
        ClientTellAddExp,
        ClientRequestAddExp,
        ClientTellAnnouncement,
        ClientTellExperience,
        ClientAsksExpRate,
        ClientRequestExpRate,
        ClientRequestToggleCap,
        ClientTryAuth,
        ClientUpdateLvlCap,
        ClientUpdateDmgRed,
        ServerFirstAscensionOrb,
        ServerRequestExperience,
        ServerForceExperience,
        ServerFullExpList,
        ServerToggleCap
    }

    class ExperienceAndClasses : Mod
    {
        public static bool TRACE = false;//for debuging

        //UI
        private UserInterface myUserInterface;
        internal MyUI myUI;

        //EXP
        public const int MAX_LEVEL = 3000;
        public const double EXP_ITEM_VALUE = 1;
        public static double[] EARLY_EXP_REQ = new double[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 };//{0, 0, 100, 250, 500, 750, 1000, 1500, 2000, 2500, 3000};
        public static double[] EXP_REQ = new double[MAX_LEVEL + 1];
        public static double[] EXP_REQ_TOTAL = new double[MAX_LEVEL + 1];

        //for multiplayer only
        public static double AUTH_CODE = -1;
        public static bool require_auth = true;
        public static double global_exp_modifier = 1;
        public static bool global_ignore_caps = false;

        //const
        public ExperienceAndClasses()
        {
            Methods.Experience.CalcExpReqs();
            Properties = new ModProperties()
            {
                Autoload = true,
                AutoloadGores = true,
                AutoloadSounds = true
            };
        }

        //load
        public override void Load()
        {
            myUI = new MyUI();
            myUI.Activate();
            myUserInterface = new UserInterface();
            myUserInterface.SetState(myUI);
            MyUI.visible = true;
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ HANDLE PACKETS ~~~~~~~~~~~~~~~~~~~~~ */

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ExpModMessageType msgType = (ExpModMessageType)reader.ReadByte();
            double experience, exp_add, exprate;
            String text;
            Player player;
            MyPlayer myPlayer;
            bool new_bool;
            MyPlayer local_MyPlayer = null;
            int explvlcap, expdmgred;
            if (Main.netMode!=2) local_MyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(this);
            switch (msgType)
            {
                //Server's initial request for player experience (also send has_looted_monster_orb, explvlcap, and expdmgred)
                case ExpModMessageType.ServerRequestExperience:
                    Methods.PacketSender.ClientTellExperience();

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerRequestExperience", 255, 255, 0, 255, 0);
                    break;

                //Player's response to server's request for experience (also send has_looted_monster_orb, explvlcap, and expdmgred)
                case ExpModMessageType.ClientTellExperience:
                    player = Main.player[reader.ReadInt32()];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.experience = reader.ReadDouble();
                    myPlayer.has_looted_monster_orb = reader.ReadBoolean();
                    myPlayer.explvlcap = reader.ReadInt32();
                    myPlayer.expdmgred = reader.ReadInt32();
                    NetMessage.SendData(25, -1, -1, "Experience synced for player #"+player.whoAmI+":"+player.name, 255, 255, 255, 0, 0);
                    Console.WriteLine("Experience synced for player #" + player.whoAmI + ":" + player.name);

                    //tell everyone else how much exp the new player has
                    int ind_new_player = player.whoAmI;
                    Methods.PacketSender.ServerForceExperience(player, -1, ind_new_player);

                    //give new player full exp list
                    if (Main.netMode == 2)
                    {
                        Methods.PacketSender.ServerFullExpList(ind_new_player, -1);
                    }

                    //tell the players the current settings
                    string lvlcap, dmgred;
                    if (myPlayer.explvlcap > 0) lvlcap = myPlayer.explvlcap.ToString();
                        else lvlcap = "disabled";
                    if (myPlayer.expdmgred > 0) dmgred = myPlayer.expdmgred.ToString() +"%";
                        else dmgred = "disabled";
                    NetMessage.SendData(25, player.whoAmI, -1, "Require Auth: "+require_auth+"\nExperience Rate: "+(global_exp_modifier *100)+ 
                        "%\nIgnore Class Caps: "+global_ignore_caps+"\nLevel Cap: "+ lvlcap + "\nClass Damage Reduction: "+
                        dmgred, 255, 255, 255, 0, 0);

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientTellExperience from player #" + ind_new_player + ":" + player.name+" = "+ player.GetModPlayer<MyPlayer>(this).experience+" (has found first orb:"+ player.GetModPlayer<MyPlayer>(this).has_looted_monster_orb+")", 255, 255, 0, 255, 0);
                    break;

                //Server telling player that they have now recieved their first Ascension Orb
                case ExpModMessageType.ServerFirstAscensionOrb:
                    local_MyPlayer.has_looted_monster_orb = true;
                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerFirstAscensionOrb", 255, 255, 0, 255, 0);
                    break;

                //Server telling everyone a player's new exp value
                case ExpModMessageType.ServerForceExperience:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    double new_exp = reader.ReadDouble();
                    explvlcap = reader.ReadInt32();
                    expdmgred = reader.ReadInt32();
                    //ignore invalid requests
                    if (new_exp < 0) break;
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    double exp_change = new_exp - myPlayer.experience;
                    myPlayer.experience = new_exp;
                    myPlayer.ExpMsg(exp_change);
                    myPlayer.explvlcap = explvlcap;
                    myPlayer.expdmgred = expdmgred;

                    if (Main.LocalPlayer.Equals(player)) myUI.updateValue(new_exp);
                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerForceExperience for player #" + player.whoAmI + ":" + player.name + " = " + new_exp, 255, 255, 0, 255, 0);
                    break;

                //Player telling the server to adjust experience (e.g., craft token)
                case ExpModMessageType.ClientTellAddExp:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    exp_add = reader.ReadDouble();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.AddExp(exp_add);

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientTellAddExp from player #" + player.whoAmI +":"+player.name+" = " + exp_add, 255, 255, 0, 255, 0);
                    break;

                //Similar to ClientTellAddExp, but requires auth
                case ExpModMessageType.ClientRequestAddExp:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    exp_add = reader.ReadDouble();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !require_auth)
                    {
                        myPlayer.AddExp(exp_add);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientRequestAddExp from player #" + player.whoAmI + ":" + player.name + " = " + exp_add, 255, 255, 0, 255, 0);
                    break;

                //Player asking to set exprate, requires auth
                case ExpModMessageType.ClientRequestExpRate:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    exprate = reader.ReadDouble();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !require_auth)
                    {
                        global_exp_modifier = exprate;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        NetMessage.SendData(25, -1, -1, "Experience Rate:" + (global_exp_modifier*100)+"%", 255, 255, 255, 0, 0);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientRequestExpRate from player #" + player.whoAmI + ":" + player.name + " = " + exprate, 255, 255, 0, 255, 0);
                    break;

                //Player asking to toggle class caps, requires auth
                case ExpModMessageType.ClientRequestToggleCap:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    new_bool = reader.ReadBoolean();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !require_auth)
                    {
                        global_ignore_caps = new_bool;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //share new status
                        Methods.PacketSender.ServerToggleCap(global_ignore_caps);

                        //announce
                        NetMessage.SendData(25, -1, -1, "Ignore Class Caps:"+ global_ignore_caps, 255, 255, 255, 0, 0);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientRequestToggleCap from player #" + player.whoAmI + ":" + player.name, 255, 255, 0, 255, 0);
                    break;

                //Server setting class caps on/off
                case ExpModMessageType.ServerToggleCap:
                    if (Main.netMode != 1) break;
                    //read
                    new_bool = reader.ReadBoolean();
                    //act
                    global_ignore_caps = new_bool;

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerToggleCap = "+ global_ignore_caps, 255, 255, 0, 255, 0);
                    break;

                //Player telling server to make an announcement
                case ExpModMessageType.ClientTellAnnouncement:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    String message = reader.ReadString();
                    int red = reader.ReadInt32();
                    int green = reader.ReadInt32();
                    int blue = reader.ReadInt32();
                    //act
                    if (TRACE) NetMessage.SendData(25, -1, -1, message, 255, red, green, blue, 0);

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved PlayerRequestAnnouncement from player #" + player.whoAmI + ":" + player.name + " = " + message, 255, 255, 0, 255, 0);
                    break;

                //Server sends full exp list to new player (also explvlcap and expdmgred)
                case ExpModMessageType.ServerFullExpList:
                    if (Main.netMode != 1) break;

                    //read and set exp
                    for (int i = 0; i <= 255; i++)
                    {
                        experience = reader.ReadDouble();
                        explvlcap = reader.ReadInt32();
                        expdmgred = reader.ReadInt32();
                        player = Main.player[i];
                        if (!Main.LocalPlayer.Equals(player) && Main.player[i].active && experience>=0)
                        {
                            myPlayer = player.GetModPlayer<MyPlayer>(this);
                            myPlayer.experience = experience;
                            myPlayer.explvlcap = explvlcap;
                            myPlayer.expdmgred = expdmgred;
                        }
                    }

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerFullExpList", 255, 255, 0, 255, 0);
                    break;

                //Player asks server what the exprate is
                case ExpModMessageType.ClientAsksExpRate:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    //act
                    NetMessage.SendData(25, player.whoAmI, -1, "The current exprate is " + (global_exp_modifier * 100) + "%.", 255, 255, 255, 0, 0);

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientAsksExpRate from player #" + player.whoAmI + ":" + player.name, 255, 255, 0, 255, 0);
                    break;

                //Player attempt auth
                case ExpModMessageType.ClientTryAuth:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    double code = reader.ReadDouble();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (code == -1 || myPlayer.auth)
                    {
                        NetMessage.SendData(25, player.whoAmI, -1, "Auth:" + myPlayer.auth, 255, 255, 255, 0, 0);
                    }
                    else 
                    {
                        if (AUTH_CODE == code)
                        {
                            myPlayer.auth = true;
                            NetMessage.SendData(25, player.whoAmI, -1, "Auth:" + myPlayer.auth, 255, 255, 255, 0, 0);
                            Console.WriteLine("Accepted auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                        else
                        {
                            Console.WriteLine("Rejected auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                    }

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientTryAuth from player #" + player.whoAmI + ":" + player.name + " " + code, 255, 255, 0, 255, 0);
                    break;

                //Player tells server that they would like to change their level cap
                case ExpModMessageType.ClientUpdateLvlCap:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    explvlcap = reader.ReadInt32();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.explvlcap = explvlcap;
                    myPlayer.AddExp(0); //easy way to update all
                    if (myPlayer.explvlcap == -1) NetMessage.SendData(25, player.whoAmI, -1, "Level cap is disabled.", 255, 255, 255, 0, 0);
                        else NetMessage.SendData(25, player.whoAmI, -1, "Level cap is " + myPlayer.explvlcap+".", 255, 255, 255, 0, 0);

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientUpdateLvlCap from player #" + player.whoAmI + ":" + player.name + " " + explvlcap, 255, 255, 0, 255, 0);
                    break;

                //Player tells server that they would like to change their damage reduction
                case ExpModMessageType.ClientUpdateDmgRed:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    expdmgred = reader.ReadInt32();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.expdmgred = expdmgred;
                    myPlayer.AddExp(0); //easy way to update all
                    if (myPlayer.expdmgred == -1) NetMessage.SendData(25, player.whoAmI, -1, "Damage reduction is disabled.", 255, 255, 255, 0, 0);
                     else NetMessage.SendData(25, player.whoAmI, -1, "Damage reduction is " + myPlayer.expdmgred+ "%.", 255, 255, 255, 0, 0);

                    if (TRACE) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientUpdateDmgRed from player #" + player.whoAmI + ":" + player.name + " " + expdmgred, 255, 255, 0, 255, 0);
                    break;

                default:
                    ErrorLogger.Log("Unknown Message type: " + msgType);
                    break;
            }
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ CHAT COMMANDS ~~~~~~~~~~~~~~~~~~~~~ */

        public override void ChatInput(string text, ref bool broadcast)
        {
            if (text[0] != '/')
            {
                return;
            }
            text = text.Substring(1);
            int index = text.IndexOf(' ');
            string command;
            string[] args;
            if (index < 0)
            {
                command = text;
                args = new string[0];
            }
            else
            {
                command = text.Substring(0, index);
                args = text.Substring(index + 1).Split(' ');
            }

            MyPlayer localMyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(this);

            broadcast = false;

            try
            {

                if (command == "expbar")
                {
                    localMyPlayer.UI_show = !localMyPlayer.UI_show;
                    if (localMyPlayer.UI_show) Main.NewText("Experience bar enabled. Display will be visible while wearing a Class Token.");
                    else Main.NewText("Experience bar hidden.");
                }
                else if (command == "expbartrans")
                {
                    localMyPlayer.UI_trans = !localMyPlayer.UI_trans;
                    myUI.setTrans(localMyPlayer.UI_trans);
                    if (localMyPlayer.UI_trans) Main.NewText("Experience bar is now transparent.");
                    else Main.NewText("Experience bar is now opaque.");
                }
                else if (command == "expbarreset")
                {
                    myUI.setPosition(400f, 100f);
                    localMyPlayer.UI_show = true;
                    localMyPlayer.UI_trans = false;
                    myUI.setTrans(localMyPlayer.UI_trans);
                    Main.NewText("Experience bar reset.");
                }
                else if (command == "explist")
                {
                    Player player;
                    double exp = 0, exp_have, exp_need;
                    int level;
                    string job, message = "Current Players:";
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        player = Main.player[playerIndex];
                        if (player.active)
                        {
                            //temp
                            exp = player.GetModPlayer<MyPlayer>(this).GetExp();
                            //Main.NewText(player.name + "=" + exp);

                            job = Methods.Experience.GetClass(player);
                            level = Methods.Experience.GetLevel(exp);

                            exp_have = Methods.Experience.GetExpTowardsNextLevel(exp);
                            exp_need = Methods.Experience.GetExpReqForLevel(level + 1, false);

                            message += "\n" + player.name + ", Level " + level + "(" + Math.Round((double)exp_have / (double)exp_need * 100, 2) + "%), " + job;
                        }
                    }
                    Main.NewTextMultiline(message);
                }
                else if (command == "expadd" && args.Length > 0)
                {
                    double exp = Double.Parse(args[0]);
                    Methods.ChatCommands.CommandSetExp(localMyPlayer.GetExp() + exp, text);
                }
                else if (command == "expsub" && args.Length > 0)
                {
                    double exp = Double.Parse(args[0]);
                    Methods.ChatCommands.CommandSetExp(localMyPlayer.GetExp() - exp, text);
                }
                else if (command == "expset" && args.Length > 0)
                {
                    double exp = Double.Parse(args[0]);
                    Methods.ChatCommands.CommandSetExp(exp, text);
                }
                else if (command == "exprate" && args.Length == 0)
                {
                    Methods.ChatCommands.CommandExpRate();
                }
                else if (command == "exprate" && args.Length > 0)
                {
                    double rate = Double.Parse(args[0]);
                    Methods.ChatCommands.CommandSetExpRate(rate / 100, text);
                }
                /* Disabled /expuse and /expcraft in v1.1.4 (switched to direct crafting)
                else if (command == "expuse")
                {
                    int numUse = -1;
                    double numUsed = 0; ;
                    if (args.Length>0)
                    {
                        numUse = Int32.Parse(args[0]);
                    }
                    while (Main.LocalPlayer.CountItem(ItemType("Experience")) > 0)
                    {
                        Main.LocalPlayer.ConsumeItem(ItemType("Experience"));
                        numUsed++;
                        if (numUse!=-1 && numUsed >= numUse) break;
                    }
                    if (Main.netMode == 0)
                    {
                        localMyPlayer.AddExp(numUsed * EXP_ITEM_VALUE);
                    }
                    else
                    {
                        Methods.PacketSender.PacketSend_ClientTellAddExp(numUsed * EXP_ITEM_VALUE);
                    }
                    Main.NewText("Used " + numUsed + " experience items.");
                }
                else if (command == "expcraft" && args.Length > 0)
                {
                    double numCraft = Double.Parse(args[0]);
                    if (localMyPlayer.GetExp() < (numCraft* EXP_ITEM_VALUE)) numCraft = Math.Floor(localMyPlayer.GetExp()/ EXP_ITEM_VALUE);

                    if (Main.netMode == 0)
                    {
                        localMyPlayer.SubtractExp(numCraft* EXP_ITEM_VALUE);
                    }
                    else
                    {
                        Methods.PacketSender.PacketSend_ClientTellAddExp(-1 * numCraft * EXP_ITEM_VALUE);
                    }

                    Main.NewText("Crafted " + numCraft + " experience items.");

                    int numCrafrNow;
                    while (numCraft>0)
                    {
                        if (numCraft > Int16.MaxValue) numCrafrNow = Int16.MaxValue;
                        else numCrafrNow = (int)numCraft;

                        Main.LocalPlayer.QuickSpawnItem(ItemType("Experience"), numCrafrNow);
                        numCraft -= numCrafrNow;
                    }
                }
                */
                else if (command == "explvladd")
                {
                    int amt = 0;
                    if (args.Length == 0) amt = 1;
                    else amt = Int32.Parse(args[0]);

                    double exp = localMyPlayer.GetExp();
                    int level = Methods.Experience.GetLevel(exp) + amt;
                    exp = Methods.Experience.GetExpReqForLevel(level, true);

                    Methods.ChatCommands.CommandSetExp(exp, text);
                }
                else if (command == "explvlsub")
                {
                    int amt = 0;
                    if (args.Length == 0) amt = 1;
                    else amt = Int32.Parse(args[0]);

                    double exp = localMyPlayer.GetExp();
                    int level = Methods.Experience.GetLevel(exp) - amt;
                    exp = Methods.Experience.GetExpReqForLevel(level, true);
                    if (exp < 0) exp = 0;

                    Methods.ChatCommands.CommandSetExp(exp, text);
                }
                else if (command == "explvlset" && args.Length > 0)
                {
                    int level = Int32.Parse(args[0]);
                    Methods.ChatCommands.CommandSetExp(Methods.Experience.GetExpReqForLevel(level, true), text);
                }
                else if (command == "expmsg")
                {
                    localMyPlayer.display_exp = !localMyPlayer.display_exp;
                    if (localMyPlayer.display_exp)
                    {
                        Main.NewText("Experience messages enabled.");
                    }
                    else
                    {
                        Main.NewText("Experience messages disabled.");
                    }
                }
                else if (command == "expclasscaps")
                {
                    Methods.ChatCommands.CommandToggleCaps(text);
                }
                else if (command == "expauth" && args.Length==0)
                {
                    if (Main.netMode == 0) Main.NewText("Auth is only for multiplayer use.");
                        else Methods.ChatCommands.CommandAuth(-1);
                }
                else if (command == "expauth" && args.Length>0)
                {
                    if (Main.netMode == 0) Main.NewText("Auth is only for multiplayer use.");
                    else
                    {
                        double code = Double.Parse(args[0]);
                        Methods.ChatCommands.CommandAuth(code);
                    }
                }
                else if (command == "explvlcap" && args.Length > 0)
                {
                    int lvl = Int32.Parse(args[0]);
                    Methods.ChatCommands.CommandLvlCap(lvl);
                }
                else if (command == "explvlcap" && args.Length == 0)
                {
                    if (localMyPlayer.explvlcap == -1) Main.NewText("Level cap is disabled.");
                    else Main.NewText("Level cap is " + localMyPlayer.explvlcap + ".");
                }
                else if (command == "expdmgred" && args.Length > 0)
                {
                    int dmgred = Int32.Parse(args[0]);
                    if (dmgred == 0) dmgred = -1;
                    Methods.ChatCommands.CommandDmgRed(dmgred);
                }
                else if (command == "expdmgred" && args.Length == 0)
                {
                    if (localMyPlayer.expdmgred == -1) Main.NewText("Damage reduction is disabled.");
                    else Main.NewText("Damage reduction is " + localMyPlayer.expdmgred + "%.");
                }
                else if (command == "expnoauth")
                {
                    Methods.ChatCommands.CommandRequireAuth();
                }
                else
                {
                    broadcast = true;
                }

            }
            catch
            {
                Main.NewText("Invalid command or parameter.");
            }

            //do base
            base.ChatInput(text, ref broadcast);
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ MISC OVERRIDES ~~~~~~~~~~~~~~~~~~~~~ */

        public override void ModifyInterfaceLayers(List<MethodSequenceListItem> layers)
        {
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (MouseTextIndex != -1)
            {
                layers.Insert(MouseTextIndex, new MethodSequenceListItem(
                    "Experience UI",
                    delegate
                    {
                        if (MyUI.visible)
                        {
                            myUserInterface.Update(Main._drawInterfaceGameTime);
                            myUI.Draw(Main.spriteBatch);
                        }
                        return true;
                    },
                    null)
                );
            }
        }
    }
}
