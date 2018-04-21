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
        ClientRequestSetmapAuthCode,
        ClientRequestExpRate,
        ClientRequestIgnoreCaps,
        ClientRequestDamageReduction,
        ClientRequestLevelCap,
        ClientRequestNoAuth,
        ClientRequestMapTrace,
        ClientRequestDeathPenalty,

        ClientTryAuth,

        ClientAFK,
        ClientUnAFK,

        ClientAbility,

        //Server packets are sent by server to clients

        ServerNewPlayerSync,
        ServerForceExperience,
        ServerSyncExp,
        ServerUpdateMapSettings,
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

        //misc
        public const int LEVEL_START_APPLYING_DEATH_PENALTY = 10;
        public const long AFK_TIME_TICKS = 60 * TimeSpan.TicksPerSecond;

        //map settings constants
        public const double DEFAULT_EXPERIENCE_MODIFIER = 1;
        public const bool DEFAULT_IGNORE_CAPS = false;
        public const int DEFAULT_DAMAGE_REDUCTION = 0;
        public const int DEFAULT_LEVEL_CAP = 100;
        public const double DEFAULT_DEATH_PENALTY = 10.0;

        //map settings
        public static double mapExpModifier = DEFAULT_EXPERIENCE_MODIFIER;
        public static bool mapIgnoreCaps = DEFAULT_IGNORE_CAPS;
        public static int mapClassDamageReduction = DEFAULT_DAMAGE_REDUCTION;
        public static int mapLevelCap = DEFAULT_LEVEL_CAP;
        public static double mapDeathPenalty = DEFAULT_DEATH_PENALTY;
        public static double mapAuthCode = -1;
        public static bool mapRequireAuth = true;
        public static bool mapTrace = false;

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
            double experience, expAdd, exprate, newCode, newDouble;
            String text;
            Player player;
            MyPlayer myPlayer;
            bool newBool;
            int newInt, pIndex;
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
                    mapClassDamageReduction = reader.ReadInt32();
                    mapExpModifier = reader.ReadDouble();
                    mapIgnoreCaps = reader.ReadBoolean();
                    mapLevelCap = reader.ReadInt32();
                    mapRequireAuth = reader.ReadBoolean();
                    mapTrace = reader.ReadBoolean();
                    mapDeathPenalty = reader.ReadDouble();

                    //send back personal exp
                    Methods.PacketSender.ClientTellExperience(mod);

                    //display settings locally
                    Methods.ChatCommands.CommandDisplaySettings();

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerNewPlayerSync");
                    break;

                //Player's response to server's request for experience
                case ExpModMessageType.ClientTellExperience:
                    player = Main.player[reader.ReadInt32()];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.experience = reader.ReadDouble();
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience synced for player #" +player.whoAmI+":"+player.name), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    Console.WriteLine("Experience synced for player #" + player.whoAmI + ":" + player.name);

                    //full sync of exp
                    if (Main.netMode == 2)
                    {
                        Methods.PacketSender.ServerSyncExp(mod, true);
                    }
                    
                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientTellExperience from player #" + player.whoAmI + ":" + player.name+" = "+ player.GetModPlayer<MyPlayer>(this).experience);
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

                    if ((Main.netMode==2 && mapTrace) || (Main.netMode==1 && traceChar))
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

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientTellAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd);
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
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        myPlayer.AddExp(expAdd);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd);
                    break;

                //Player requests (always needs auth) to set expauth code
                case ExpModMessageType.ClientRequestSetmapAuthCode:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    newCode = reader.ReadDouble();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth)
                    {
                        mapAuthCode = newCode;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                        Console.WriteLine("New expauth code: " + mapAuthCode);
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
                    else if (!mapRequireAuth)
                    {
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("This command requires expauth even when noauth is enabled."), MESSAGE_COLOUR_RED, player.whoAmI);
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestSetmapAuthCode from player #" + player.whoAmI + ":" + player.name + " = " + newCode);
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
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        mapExpModifier = exprate;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience Rate: " + (mapExpModifier*100)+"%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestExpRate from player #" + player.whoAmI + ":" + player.name + " = " + exprate);
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
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        mapLevelCap = newInt;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Level Cap:" + mapLevelCap), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestLevelCap from player #" + player.whoAmI + ":" + player.name + " = " + newInt);
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
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        mapClassDamageReduction = newInt;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Class damage reduction: " + mapClassDamageReduction + "%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestDamageReduction from player #" + player.whoAmI + ":" + player.name + " = " + newInt);
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
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        mapIgnoreCaps = newBool;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Ignore Class Caps: " + mapIgnoreCaps), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestToggleCap from player #" + player.whoAmI + ":" + player.name);
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
                        mapRequireAuth = !mapRequireAuth;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Require Expauth: " + mapRequireAuth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else if (!mapRequireAuth)
                    {
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("This command requires expauth even when noauth is enabled."), MESSAGE_COLOUR_RED, player.whoAmI);
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestNoAuth from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Player requesting (needs auth) to toggle noauth
                case ExpModMessageType.ClientRequestMapTrace:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        mapTrace = !mapTrace;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Map Trace: " + mapTrace), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestMapTrace from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Player requesting (needs auth) to set death penalty
                case ExpModMessageType.ClientRequestDeathPenalty:
                    if (Main.netMode != 2) break;
                    //read
                    player = Main.player[reader.ReadInt32()];
                    newDouble = reader.ReadDouble();
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !mapRequireAuth)
                    {
                        mapDeathPenalty = newDouble;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience Rate: " + (mapExpModifier*100)+"%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestDeathPenalty from player #" + player.whoAmI + ":" + player.name + " = " + newDouble);
                    break;

                //Player telling server that they are away
                case ExpModMessageType.ClientAFK:
                    if (Main.netMode != 2) break;
                    //read
                    pIndex = reader.ReadInt32();
                    //act
                    player = Main.player[pIndex];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.afk = true;
                    NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("You are now AFK. You will not recieve death penalties to experience but you cannot gain experience either."), MESSAGE_COLOUR_RED, pIndex);
                    Console.WriteLine(pIndex + ":" + player.name + " is now AFK.");

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientAFK from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Player telling server that they are back
                case ExpModMessageType.ClientUnAFK:
                    if (Main.netMode != 2) break;
                    //read
                    pIndex = reader.ReadInt32();
                    //act
                    player = Main.player[pIndex];
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    myPlayer.afk = false;
                    NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("You are no longer AFK."), MESSAGE_COLOUR_YELLOW, pIndex);
                    Console.WriteLine(pIndex + ":" + player.name + " is no longer AFK.");

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientUnAFK from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Server updating map settings
                case ExpModMessageType.ServerUpdateMapSettings:
                    if (Main.netMode != 1) break;

                    //damage reduction
                    newInt = reader.ReadInt32();
                    if (newInt != mapClassDamageReduction)
                    {
                        mapClassDamageReduction = newInt;
                        Main.NewText("Updated Class Damage Reduction: " + mapClassDamageReduction + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    //experience rate
                    experience = reader.ReadDouble();
                    if (experience != mapExpModifier)
                    {
                        mapExpModifier = experience;
                        Main.NewText("Updated Experience Rate: " + (mapExpModifier*100) + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    //ignore class caps
                    newBool = reader.ReadBoolean();
                    if (newBool != mapIgnoreCaps)
                    {
                        mapIgnoreCaps = newBool;
                        Main.NewText("Updated Ignore Class Caps: " + mapIgnoreCaps, MESSAGE_COLOUR_YELLOW);
                    }

                    //level cap
                    newInt = reader.ReadInt32();
                    if (newInt != mapLevelCap)
                    {
                        mapLevelCap = newInt;
                        Main.NewText("Updated Level Cap: " + mapLevelCap, MESSAGE_COLOUR_YELLOW);
                    }

                    //require auth
                    newBool = reader.ReadBoolean();
                    if (newBool != mapRequireAuth)
                    {
                        mapRequireAuth = newBool;
                        Main.NewText("Updated Require Authorization: " + mapRequireAuth, MESSAGE_COLOUR_YELLOW);
                    }

                    //map trace
                    newBool = reader.ReadBoolean();
                    if (newBool != mapTrace)
                    {
                        mapTrace = newBool;
                        Main.NewText("Updated Map Trace: " + mapTrace, MESSAGE_COLOUR_YELLOW);
                    }

                    //death penalty
                    newDouble = reader.ReadDouble();
                    if (newDouble != mapDeathPenalty)
                    {
                        mapDeathPenalty = newDouble;
                        Main.NewText("Updated Death Penalty: " + (mapDeathPenalty * 100) + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerUpdateMapSettings = " + mapClassDamageReduction + " " + mapExpModifier + " " + mapIgnoreCaps + " " + mapLevelCap + " " + mapRequireAuth + " " + mapTrace + " " + mapDeathPenalty);
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
                    if ((Main.netMode==2 && mapTrace) || (Main.netMode==1 && traceChar))
                    {
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), new Color(red, green, blue));
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved PlayerRequestAnnouncement from player #" + player.whoAmI + ":" + player.name + " = " + message);
                    break;

                //Server resyncs any recent exp changes
                case ExpModMessageType.ServerSyncExp:
                    if (Main.netMode != 1) break;

                    //read and set exp
                    newInt = reader.ReadInt32();
                    for (int ind = 0; ind < newInt; ind++)
                    {
                        pIndex = reader.ReadInt32();
                        newExp = reader.ReadDouble();

                        player = Main.player[pIndex];
                        if (player.active && (newExp >= 0))
                        {
                            myPlayer = player.GetModPlayer<MyPlayer>(this);
                            double expChange = newExp - myPlayer.experience;
                            myPlayer.experience = newExp;
                            myPlayer.ExpMsg(expChange);

                            if (Main.LocalPlayer.Equals(player))
                                myUI.updateValue(newExp);
                        }
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerSyncExp");
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
                        if (mapAuthCode == code)
                        {
                            myPlayer.auth = true;
                            NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Auth: " + myPlayer.auth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                            Console.WriteLine("Accepted auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                        else
                        {
                            Console.WriteLine("Rejected auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code + "\nExperience&Classes expauth code: " + ExperienceAndClasses.mapAuthCode);
                        }
                    }

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientTryAuth from player #" + player.whoAmI + ":" + player.name + " " + code);
                    break;

                //Player tells server that they are performing an ability
                case ExpModMessageType.ClientAbility:
                    //read
                    player = Main.player[reader.ReadInt32()];

                    //act
                    Console.WriteLine("ABILITY_A");
                    Main.NewText("ABILITY_B");

                    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientAbility from player #" + player.whoAmI + ":" + player.name);
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
