using Terraria.ModLoader;
using ExperienceAndClasses.UI;
using Terraria.UI;
using Terraria.DataStructures;
using Terraria;
using System.Collections.Generic;
using System;
using System.IO;

//needed for compiling outside of Terraria
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
        ClientRequestToggleClassCap,
        ClientTryAuth,
        ClientUpdateLvlCap,
        ClientUpdateDmgRed,
        ServerFirstAscensionOrb,
        ServerRequestExperience,
        ServerForceExperience,
        ServerFullExpList,
        ServerToggleClassCap
    }

    class ExperienceAndClasses : Mod
    {
        //UI
        private UserInterface myUserInterface;
        public MyUI myUI;

        //EXP
        public const int MAX_LEVEL = 3000;
        public const double EXP_ITEM_VALUE = 1;
        public readonly static double[] EARLY_EXP_REQ = new double[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 };//{0, 0, 100, 250, 500, 750, 1000, 1500, 2000, 2500, 3000};
        public static double[] EXP_REQ = new double[MAX_LEVEL + 1];
        public static double[] EXP_REQ_TOTAL = new double[MAX_LEVEL + 1];

        //for multiplayer only
        public static double authCode = -1;
        public static bool requireAuth = true;
        public static double globalExpModifier = 1;
        public static bool globalIgnoreCaps = false;
        public static bool traceMap = false;//for debuging

        //const
        public ExperienceAndClasses()
        {
            Methods.Experience.CalcExpReqs();
            Properties = new ModProperties()
            {
                Autoload = true,
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
            double experience, expAdd, exprate;
            String text;
            Player player;
            MyPlayer myPlayer;
            bool newBool;
            int explvlcap, expdmgred;
            MyPlayer localMyPlayer = null;
            bool traceChar = false;
            if (Main.netMode!=2)
            {
                localMyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(this);
                traceChar = localMyPlayer.traceChar;
            }
            Mod mod = (this as Mod);
            switch (msgType)
            {
                //Server's initial request for player experience (also send hasLootedMonsterOrb, explvlcap, and expdmgred)
                case ExpModMessageType.ServerRequestExperience:
                    Methods.PacketSender.ClientTellExperience(mod);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerRequestExperience", 255, 255, 0, 255, 0);
                    break;

                //Player's response to server's request for experience (also send hasLootedMonsterOrb, explvlcap, and expdmgred)
                case ExpModMessageType.ClientTellExperience:
                    player = Main.player[reader.ReadInt32()];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.experience = reader.ReadDouble();
                    myPlayer.hasLootedMonsterOrb = reader.ReadBoolean();
                    myPlayer.explvlcap = reader.ReadInt32();
                    myPlayer.expdmgred = reader.ReadInt32();
                    NetMessage.SendData(25, -1, -1, "Experience synced for player #"+player.whoAmI+":"+player.name, 255, 255, 255, 0, 0);
                    Console.WriteLine("Experience synced for player #" + player.whoAmI + ":" + player.name);

                    //tell everyone else how much exp the new player has
                    int indNewPlayer = player.whoAmI;
                    Methods.PacketSender.ServerForceExperience(mod, player, -1, indNewPlayer);

                    //give new player full exp list
                    if (Main.netMode == 2)
                    {
                        Methods.PacketSender.ServerFullExpList(mod, indNewPlayer, -1);
                    }

                    //tell the players the current settings
                    string lvlcap, dmgred;
                    if (myPlayer.explvlcap > 0) lvlcap = myPlayer.explvlcap.ToString();
                        else lvlcap = "disabled";
                    if (myPlayer.expdmgred > 0) dmgred = myPlayer.expdmgred.ToString() +"%";
                        else dmgred = "disabled";
                    NetMessage.SendData(25, player.whoAmI, -1, "Require Auth: "+requireAuth+"\nExperience Rate: "+(globalExpModifier *100)+ 
                        "%\nIgnore Class Caps: "+globalIgnoreCaps+"\nLevel Cap: "+ lvlcap + "\nClass Damage Reduction: "+
                        dmgred, 255, 255, 255, 0, 0);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientTellExperience from player #" + indNewPlayer + ":" + player.name+" = "+ player.GetModPlayer<MyPlayer>(this).experience+" (has found first orb:"+ player.GetModPlayer<MyPlayer>(this).hasLootedMonsterOrb+")", 255, 255, 0, 255, 0);
                    break;

                //Server telling player that they have now recieved their first Ascension Orb
                case ExpModMessageType.ServerFirstAscensionOrb:
                    localMyPlayer.hasLootedMonsterOrb = true;
                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerFirstAscensionOrb", 255, 255, 0, 255, 0);
                    break;

                //Server telling everyone a player's new exp value
                case ExpModMessageType.ServerForceExperience:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    double newExp = reader.ReadDouble();
                    explvlcap = reader.ReadInt32();
                    expdmgred = reader.ReadInt32();
                    //ignore invalid requests
                    if (newExp < 0) break;
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    double expChange = newExp - myPlayer.experience;
                    myPlayer.experience = newExp;
                    myPlayer.ExpMsg(expChange);
                    myPlayer.explvlcap = explvlcap;
                    myPlayer.expdmgred = expdmgred;

                    if (Main.LocalPlayer.Equals(player)) myUI.updateValue(newExp);
                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerForceExperience for player #" + player.whoAmI + ":" + player.name + " = " + newExp, 255, 255, 0, 255, 0);
                    break;

                //Player telling the server to adjust experience (e.g., craft token)
                case ExpModMessageType.ClientTellAddExp:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    expAdd = reader.ReadDouble();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.AddExp(expAdd);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientTellAddExp from player #" + player.whoAmI +":"+player.name+" = " + expAdd, 255, 255, 0, 255, 0);
                    break;

                //Similar to ClientTellAddExp, but requires auth
                case ExpModMessageType.ClientRequestAddExp:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    expAdd = reader.ReadDouble();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !requireAuth)
                    {
                        myPlayer.AddExp(expAdd);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientRequestAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd, 255, 255, 0, 255, 0);
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
                    if (myPlayer.auth || !requireAuth)
                    {
                        globalExpModifier = exprate;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        NetMessage.SendData(25, -1, -1, "Experience Rate:" + (globalExpModifier*100)+"%", 255, 255, 255, 0, 0);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientRequestExpRate from player #" + player.whoAmI + ":" + player.name + " = " + exprate, 255, 255, 0, 255, 0);
                    break;

                //Player asking to toggle class caps, requires auth
                case ExpModMessageType.ClientRequestToggleClassCap:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    newBool = reader.ReadBoolean();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !requireAuth)
                    {
                        globalIgnoreCaps = newBool;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //share new status
                        Methods.PacketSender.ServerToggleClassCap(mod, globalIgnoreCaps);

                        //announce
                        NetMessage.SendData(25, -1, -1, "Ignore Class Caps:"+ globalIgnoreCaps, 255, 255, 255, 0, 0);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientRequestToggleCap from player #" + player.whoAmI + ":" + player.name, 255, 255, 0, 255, 0);
                    break;

                //Server setting class caps on/off
                case ExpModMessageType.ServerToggleClassCap:
                    if (Main.netMode != 1) break;
                    //read
                    newBool = reader.ReadBoolean();
                    //act
                    globalIgnoreCaps = newBool;

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerToggleCap = "+ globalIgnoreCaps, 255, 255, 0, 255, 0);
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
                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, message, 255, red, green, blue, 0);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved PlayerRequestAnnouncement from player #" + player.whoAmI + ":" + player.name + " = " + message, 255, 255, 0, 255, 0);
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ServerFullExpList", 255, 255, 0, 255, 0);
                    break;

                //Player asks server what the exprate is
                case ExpModMessageType.ClientAsksExpRate:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    //act
                    NetMessage.SendData(25, player.whoAmI, -1, "The current exprate is " + (globalExpModifier * 100) + "%.", 255, 255, 255, 0, 0);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientAsksExpRate from player #" + player.whoAmI + ":" + player.name, 255, 255, 0, 255, 0);
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
                        if (authCode == code)
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientTryAuth from player #" + player.whoAmI + ":" + player.name + " " + code, 255, 255, 0, 255, 0);
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientUpdateLvlCap from player #" + player.whoAmI + ":" + player.name + " " + explvlcap, 255, 255, 0, 255, 0);
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.SendData(25, -1, -1, "TRACE:Recieved ClientUpdateDmgRed from player #" + player.whoAmI + ":" + player.name + " " + expdmgred, 255, 255, 0, 255, 0);
                    break;

                default:
                    ErrorLogger.Log("Unknown Message type: " + msgType);
                    break;
            }
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ CHAT COMMANDS ~~~~~~~~~~~~~~~~~~~~~ */

        

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
