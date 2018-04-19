using Terraria.ModLoader;
using ExperienceAndClasses.UI;
using Terraria.UI;
using Terraria.DataStructures;
using Terraria;
using System.Collections.Generic;
using System;
using System.IO;
using Terraria.Localization;
using Microsoft.Xna.Framework;

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
        //ClientAbility,
        ServerRequestExperience,
        ServerForceExperience,
        ServerFullExpList,
        ServerToggleClassCap,
        //ServerAbility,
        //ServerAbilityOutcome,
    }

    class ExperienceAndClasses : Mod
    {
        //UI
        private UserInterface myUserInterface;
        public MyUI myUI;

        //message colours
        public static readonly Color MESSAGE_COLOUR_RED = new Color(255, 0, 0);
        public static readonly Color MESSAGE_COLOUR_GREEN = new Color(0, 255, 0);
        public static readonly Color MESSAGE_COLOUR_YELLOW = new Color(255, 255, 0);
        public static readonly Color MESSAGE_COLOUR_MAGENTA = new Color(255, 0, 255);
        public static readonly Color MESSAGE_COLOUR_BOSS_ORB = new Color(233, 36, 91);
        public static readonly Color MESSAGE_COLOUR_ASCENSION_ORB = new Color(4, 195, 249);

        //active abilities
        public static ModHotKey HOTKEY_ACTIVATE_ABILITY;
        public static ModHotKey HOTKEY_MODIFIER_1;
        public static ModHotKey HOTKEY_MODIFIER_2;
        public static ModHotKey HOTKEY_MODIFIER_3;
        public static ModHotKey HOTKEY_MODIFIER_4;
        public static readonly int MAXIMUM_NUMBER_OF_ABILITIES = 4;

        //exp requirements and cap
        public const int MAX_LEVEL = 3000;
        public const double EXP_ITEM_VALUE = 1;
        public static readonly double[] EARLY_EXP_REQ = new double[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 };//{0, 0, 100, 250, 500, 750, 1000, 1500, 2000, 2500, 3000};
        public static readonly double[] EXP_REQ = new double[MAX_LEVEL + 1];
        public static readonly double[] EXP_REQ_TOTAL = new double[MAX_LEVEL + 1];

        //awarding experience and drops
        public const float RANGE_EXP_AND_ASCENSION_ORB = 5000f;
        public const float PERCENT_CHANCE_BOSS_ORB = 25.0f;
        public const float PERCENT_CHANCE_ASCENSION_ORB = 25f; //0.7f;

        //multiplayer constants
        public const long TIME_TICKS_SYNC_EXP_AFTER_KILL = 500 * TimeSpan.TicksPerMillisecond;

        //map settings constants
        public const double DEFAULT_EXPERIENCE_MODIFIER = 1;
        public const bool DEFAULT_IGNORE_CAPS = false;
        public const int DEFAULT_DAMAGE_REDUCTION = 0;
        public const int DEFAULT_LEVEL_CAP = 100;

        //map settings
        public static double globalExpModifier = DEFAULT_EXPERIENCE_MODIFIER;
        public static bool globalIgnoreCaps = DEFAULT_IGNORE_CAPS;
        public static int globalDamageReduction = DEFAULT_DAMAGE_REDUCTION;
        public static int globalLevelCap = DEFAULT_LEVEL_CAP;

        //for multiplayer only (non-constants)
        public static double authCode = -1;
        public static bool requireAuth = true;
        public static bool traceMap = false;//for debuging

        //start
        public ExperienceAndClasses()
        {
            Methods.Experience.CalcExpReqs();
            Abilities.Initialize();
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
            HOTKEY_ACTIVATE_ABILITY = RegisterHotKey("Ability", "Q");
            HOTKEY_MODIFIER_1 = RegisterHotKey("Modifier 1", "Q");
            HOTKEY_MODIFIER_2 = RegisterHotKey("Modifier 2", "W");
            HOTKEY_MODIFIER_3 = RegisterHotKey("Modifier 3", "LeftShift");
            HOTKEY_MODIFIER_4 = RegisterHotKey("Modifier 4", "LeftControl");
        }

        public override void Unload()
        {
            HOTKEY_ACTIVATE_ABILITY = null;
            HOTKEY_MODIFIER_1 = null;
            HOTKEY_MODIFIER_2 = null;
            HOTKEY_MODIFIER_3 = null;
            HOTKEY_MODIFIER_4 = null;
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
            int explvlcap, expdmgred; //, abilityID, level;
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerRequestExperience"), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player's response to server's request for experience (also send hasLootedMonsterOrb, explvlcap, and expdmgred)
                case ExpModMessageType.ClientTellExperience:
                    player = Main.player[reader.ReadInt32()];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.experience = reader.ReadDouble();
                    myPlayer.explvlcap = reader.ReadInt32();
                    myPlayer.expdmgred = reader.ReadInt32();
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience synced for player #" +player.whoAmI+":"+player.name), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
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
                    NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Require Auth: " + requireAuth + "\nExperience Rate: " + (globalExpModifier * 100) +
                        "%\nIgnore Class Caps: " + globalIgnoreCaps + "\nLevel Cap: " + lvlcap + "\nClass Damage Reduction: " +
                        dmgred), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientTellExperience from player #" + indNewPlayer + ":" + player.name+" = "+ player.GetModPlayer<MyPlayer>(this).experience), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar))
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerForceExperience for player #" + player.whoAmI + ":" + player.name + " = " + newExp), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                ////Server telling client the outcome of an ability request
                //case ExpModMessageType.ServerAbilityOutcome:
                //    //read
                //    player = Main.player[reader.ReadInt32()];
                //    abilityID = reader.ReadInt32();
                //    outcome = reader.ReadInt32();
                //    //act
                //    myPlayer = player.GetModPlayer<MyPlayer>(this);
                //    if (myPlayer.showFailMessages && (outcome != myPlayer.latestAbilityFail))
                //    {
                //        myPlayer.latestAbilityFail = outcome;
                //        if (myPlayer.latestAbilityFail == Abilities.RETURN_SUCCESS) myPlayer.showFailMessages = false;
                //        Abilities.DoReturnMessage(myPlayer.latestAbilityFail, abilityID);
                //    }

                //    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerAbilityOutcome for player #" + player.whoAmI + ":" + player.name + " = " + abilityID + " " + outcome), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                //    break;

                //case ExpModMessageType.ServerAbility:
                //    if (Main.netMode != 1) break; //client only
                //    //read
                //    player = Main.player[reader.ReadInt32()];
                //    abilityID = reader.ReadInt32();
                //    level = reader.ReadInt32();
                //    //act
                //    myPlayer = player.GetModPlayer<MyPlayer>(this);
                //    Abilities.DoAbility(myPlayer, abilityID, level, true);

                //    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar))
                //        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerAbility for player #" + player.whoAmI + ":" + player.name + " = " + abilityID + " " + level), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                //    break;

                //Player telling the server to adjust experience (e.g., craft token)
                case ExpModMessageType.ClientTellAddExp:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    expAdd = reader.ReadDouble();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.AddExp(expAdd);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientTellAddExp from player #" + player.whoAmI +":"+player.name+" = " + expAdd), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience Rate:" + (globalExpModifier*100)+"%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestExpRate from player #" + player.whoAmI + ":" + player.name + " = " + exprate), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Ignore Class Caps:" + globalIgnoreCaps), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestToggleCap from player #" + player.whoAmI + ":" + player.name), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Server setting class caps on/off
                case ExpModMessageType.ServerToggleClassCap:
                    if (Main.netMode != 1) break;
                    //read
                    newBool = reader.ReadBoolean();
                    //act
                    globalIgnoreCaps = newBool;

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerToggleCap = " + globalIgnoreCaps), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar))
                    {
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), new Color(red, green, blue));
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved PlayerRequestAnnouncement from player #" + player.whoAmI + ":" + player.name + " = " + message), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerFullExpList"), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player asks server what the exprate is
                case ExpModMessageType.ClientAsksExpRate:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    //act
                    NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("The current exprate is " + (globalExpModifier * 100) + "%."), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientAsksExpRate from player #" + player.whoAmI + ":" + player.name), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Auth:" + myPlayer.auth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                    }
                    else 
                    {
                        if (authCode == code)
                        {
                            myPlayer.auth = true;
                            NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Auth:" + myPlayer.auth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                            Console.WriteLine("Accepted auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                        else
                        {
                            Console.WriteLine("Rejected auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientTryAuth from player #" + player.whoAmI + ":" + player.name + " " + code), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                    if (myPlayer.explvlcap == -1) NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Level cap is disabled."), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                    else NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Level cap is " + myPlayer.explvlcap + "."), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientUpdateLvlCap from player #" + player.whoAmI + ":" + player.name + " " + explvlcap), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                    if (myPlayer.expdmgred == -1) NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Damage reduction is disabled."), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                    else NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Damage reduction is " + myPlayer.expdmgred + "%."), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientUpdateDmgRed from player #" + player.whoAmI + ":" + player.name + " " + expdmgred), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                ////Player tells server that they would like to perform an ability.
                //case ExpModMessageType.ClientAbility:
                //    if (Main.netMode != 2) break; //server only
                //    //read
                //    player = Main.player[reader.ReadInt32()];
                //    abilityID = reader.ReadInt32();
                //    level = reader.ReadInt32();
                //    //act
                //    myPlayer = player.GetModPlayer<MyPlayer>(this);
                //    Abilities.DoAbility(myPlayer, abilityID, level, true);
                //    Methods.PacketSender.ServerAbility(mod, player.whoAmI, abilityID, level, -1, player.whoAmI);

                //    //Methods.PacketSender.ServerAbilityOutcome(mod, abilityID, outcome, player.whoAmI, -1);

                //    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientAbility from player #" + player.whoAmI + ":" + player.name + " " + abilityID + " " + level), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                //    break;

                default:
                    ErrorLogger.Log("Unknown Message type: " + msgType);
                    break;
            }
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ CHAT COMMANDS ~~~~~~~~~~~~~~~~~~~~~ */



        /* ~~~~~~~~~~~~~~~~~~~~~ MISC OVERRIDES ~~~~~~~~~~~~~~~~~~~~~ */

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (MouseTextIndex != -1)
            {
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
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
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}
