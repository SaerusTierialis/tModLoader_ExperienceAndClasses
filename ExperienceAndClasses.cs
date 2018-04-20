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
        //Client packets are sent by clients to server

        ClientTellAddExp,
        ClientTellAnnouncement,
        ClientTellExperience,

        ClientRequestAddExp,
        ClientRequestSetAuthCode,
        ClientRequestExpRate,
        ClientRequestIgnoreCaps,
        ClientRequestDamageReduction,
        ClientRequestLevelCap,
        ClientRequestNoAuth,
        ClientRequestMapTrace, //to do

        ClientTryAuth,

        //Server packets are sent by server to clients

        ServerNewPlayerSync,
        ServerForceExperience,
        ServerFullExpList,
        ServerUpdateSettings,
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
        public const float PERCENT_CHANCE_ASCENSION_ORB = 0.7f;

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
        public static int globalClassDamageReduction = DEFAULT_DAMAGE_REDUCTION;
        public static int globalLevelCap = DEFAULT_LEVEL_CAP;

        //map settings (for multiplayer only)
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
            double experience, expAdd, exprate, newCode;
            String text;
            Player player;
            MyPlayer myPlayer;
            bool newBool;
            int newInt;
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
                //Server's request to new player (includes map settings)
                case ExpModMessageType.ServerNewPlayerSync:
                    //set map settings
                    globalClassDamageReduction = reader.ReadInt32();
                    globalExpModifier = reader.ReadDouble();
                    globalIgnoreCaps = reader.ReadBoolean();
                    globalLevelCap = reader.ReadInt32();
                    requireAuth = reader.ReadBoolean();

                    //send back personal exp
                    Methods.PacketSender.ClientTellExperience(mod);

                    //display settings locally
                    Methods.ChatCommands.CommandDisplaySettings();

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerNewPlayerSync"), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player's response to server's request for experience
                case ExpModMessageType.ClientTellExperience:
                    player = Main.player[reader.ReadInt32()];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.experience = reader.ReadDouble();
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

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientTellExperience from player #" + indNewPlayer + ":" + player.name+" = "+ player.GetModPlayer<MyPlayer>(this).experience), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Server telling everyone a player's new exp value
                case ExpModMessageType.ServerForceExperience:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    double newExp = reader.ReadDouble();

                    //act
                    if (newExp > 0)
                    {
                        myPlayer = player.GetModPlayer<MyPlayer>(this);
                        double expChange = newExp - myPlayer.experience;
                        myPlayer.experience = newExp;
                        myPlayer.ExpMsg(expChange);

                        if (Main.LocalPlayer.Equals(player))
                            myUI.updateValue(newExp);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar))
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerForceExperience for player #" + player.whoAmI + ":" + player.name + " = " + newExp), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player requests (always needs auth) to set expauth code
                case ExpModMessageType.ClientRequestSetAuthCode:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    newCode = reader.ReadDouble();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth)
                    {
                        authCode = newCode;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                        Console.WriteLine("New expauth code: " + authCode);
                        //remove expauth from everyone
                        for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                        {
                            if (Main.player[playerIndex].active)
                            {
                                myPlayer = player.GetModPlayer<MyPlayer>(this);
                                myPlayer.auth = false;
                            }
                        }
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The expauth code has been changed by " + player.name + ". Authorizations have been reset."), MESSAGE_COLOUR_RED);
                    }
                    else if (!requireAuth)
                    {
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("This command requires expauth even when noauth is enabled."), MESSAGE_COLOUR_RED, player.whoAmI);
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestSetAuthCode from player #" + player.whoAmI + ":" + player.name + " = " + newCode), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player asking to set experience rate, requires auth
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
                        Methods.PacketSender.ServerUpdateSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience Rate: " + (globalExpModifier*100)+"%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestExpRate from player #" + player.whoAmI + ":" + player.name + " = " + exprate), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player requests(needs auth) to set level cap
                case ExpModMessageType.ClientRequestLevelCap:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    newInt = reader.ReadInt32();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !requireAuth)
                    {
                        globalLevelCap = newInt;
                        Methods.PacketSender.ServerUpdateSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Level Cap:" + globalLevelCap), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestLevelCap from player #" + player.whoAmI + ":" + player.name + " = " + newInt), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player requests (needs auth) to set class damage reduction
                case ExpModMessageType.ClientRequestDamageReduction:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    newInt = reader.ReadInt32();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !requireAuth)
                    {
                        globalClassDamageReduction = newInt;
                        Methods.PacketSender.ServerUpdateSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Class damage reduction: " + globalClassDamageReduction + "%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestDamageReduction from player #" + player.whoAmI + ":" + player.name + " = " + newInt), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player asking to toggle class caps, requires auth
                case ExpModMessageType.ClientRequestIgnoreCaps:
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
                        Methods.PacketSender.ServerUpdateSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Ignore Class Caps: " + globalIgnoreCaps), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestToggleCap from player #" + player.whoAmI + ":" + player.name), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player requesting (needs auth) to toggle noauth
                case ExpModMessageType.ClientRequestNoAuth:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth)
                    {
                        requireAuth = !requireAuth;
                        Methods.PacketSender.ServerUpdateSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Require Expauth: " + requireAuth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else if (!requireAuth)
                    {
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("This command requires expauth even when noauth is enabled."), MESSAGE_COLOUR_RED, player.whoAmI);
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestNoAuth from player #" + player.whoAmI + ":" + player.name), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Player requesting (needs auth) to toggle noauth
                case ExpModMessageType.ClientRequestMapTrace:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !requireAuth)
                    {
                        traceMap = !traceMap;
                        Methods.PacketSender.ServerUpdateSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Map Trace: " + traceMap), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                    }

                    if ((Main.netMode == 2 && traceMap) || (Main.netMode == 1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientRequestMapTrace from player #" + player.whoAmI + ":" + player.name), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                //Server updating map settings
                case ExpModMessageType.ServerUpdateSettings:
                    if (Main.netMode != 1) break;

                    //damage reduction
                    newInt = reader.ReadInt32();
                    if (newInt != globalClassDamageReduction)
                    {
                        globalClassDamageReduction = newInt;
                        Main.NewText("Updated Class Damage Reduction: " + globalClassDamageReduction + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    //experience rate
                    experience = reader.ReadDouble();
                    if (experience != globalExpModifier)
                    {
                        globalExpModifier = experience;
                        Main.NewText("Updated Experience Rate: " + (globalExpModifier*100) + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    //ignore class caps
                    newBool = reader.ReadBoolean();
                    if (newBool != globalIgnoreCaps)
                    {
                        globalIgnoreCaps = newBool;
                        Main.NewText("Updated Ignore Class Caps: " + globalIgnoreCaps, MESSAGE_COLOUR_YELLOW);
                    }

                    //level cap
                    newInt = reader.ReadInt32();
                    if (newInt != globalLevelCap)
                    {
                        globalLevelCap = newInt;
                        Main.NewText("Updated Level Cap: " + globalLevelCap, MESSAGE_COLOUR_YELLOW);
                    }

                    //require auth
                    newBool = reader.ReadBoolean();
                    if (newBool != requireAuth)
                    {
                        requireAuth = newBool;
                        Main.NewText("Updated Require Authorization: " + requireAuth, MESSAGE_COLOUR_YELLOW);
                    }

                    //require auth
                    newBool = reader.ReadBoolean();
                    if (newBool != traceMap)
                    {
                        traceMap = newBool;
                        Main.NewText("Updated Map Trace: " + traceMap, MESSAGE_COLOUR_YELLOW);
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerUpdateSettings = " + globalClassDamageReduction + " " + globalExpModifier + " " + globalIgnoreCaps + " " + globalLevelCap + " " + requireAuth + " " + traceMap), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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

                //Server sends full exp list to new player
                case ExpModMessageType.ServerFullExpList:
                    if (Main.netMode != 1) break;

                    //read and set exp
                    for (int i = 0; i <= 255; i++)
                    {
                        experience = reader.ReadDouble();
                        player = Main.player[i];
                        if (!Main.LocalPlayer.Equals(player) && Main.player[i].active && experience>=0)
                        {
                            myPlayer = player.GetModPlayer<MyPlayer>(this);
                            myPlayer.experience = experience;
                        }
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ServerFullExpList"), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
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
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Auth: " + myPlayer.auth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                    }
                    else 
                    {
                        if (authCode == code)
                        {
                            myPlayer.auth = true;
                            NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Auth: " + myPlayer.auth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                            Console.WriteLine("Accepted auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                        else
                        {
                            Console.WriteLine("Rejected auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code + "\nExperience&Classes expauth code: " + ExperienceAndClasses.authCode);
                        }
                    }

                    if ((Main.netMode==2 && traceMap) || (Main.netMode==1 && traceChar)) NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("TRACE:Recieved ClientTryAuth from player #" + player.whoAmI + ":" + player.name + " " + code), ExperienceAndClasses.MESSAGE_COLOUR_MAGENTA);
                    break;

                default:
                    //ErrorLogger.Log("Unknown Message type: " + msgType);
                    break;
            }
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ CHAT COMMANDS ~~~~~~~~~~~~~~~~~~~~~ */

        //moved

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
