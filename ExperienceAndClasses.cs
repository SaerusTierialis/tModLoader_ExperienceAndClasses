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
using Terraria.ID;
using System.Reflection;

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

        ClientSendDebuffImmunity,

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

        //Server packets are sent by server to clients

        ServerDebuffImmunity,
        ServerNewPlayerSync,
        ServerForceExperience,
        ServerSyncExp,
        ServerUpdateMapSettings,
    }

    class ExperienceAndClasses : Mod
    {
        //UI
        private UserInterface myUserInterface;
        public UIExp uiExp;

        //message colours
        public static readonly Color MESSAGE_COLOUR_RED = new Color(255, 0, 0);
        public static readonly Color MESSAGE_COLOUR_GREEN = new Color(0, 255, 0);
        public static readonly Color MESSAGE_COLOUR_YELLOW = new Color(255, 255, 0);
        public static readonly Color MESSAGE_COLOUR_MAGENTA = new Color(255, 0, 255);
        public static readonly Color MESSAGE_COLOUR_BOSS_ORB = new Color(233, 36, 91);
        public static readonly Color MESSAGE_COLOUR_ASCENSION_ORB = new Color(4, 195, 249);
        public static readonly Color MESSAGE_COLOUR_OFF_COOLDOWN = new Color(163,73, 164);
        //public static readonly Color MESSAGE_COLOUR_EXP = new Color(21, 111, 48);

        //active abilities
        public static readonly int NUMBER_OF_ABILITY_SLOTS = 4;
        private static string[] HOTKEY_DEFAULTS = { "Q", "E", "R", "F" };
        public static ModHotKey[] HOTKEY_ABILITY = new ModHotKey[NUMBER_OF_ABILITY_SLOTS];
        public static ModHotKey HOTKEY_ALTERNATE_EFFECT;

        //exp requirements and cap
        public const int MAX_LEVEL = 3000;
        public const double EXP_ITEM_VALUE = 1;
        public static readonly double[] EARLY_EXP_REQ = new double[] { 0, 0, 10, 25, 50, 75, 100, 125, 150, 200, 350 };
        public static readonly double[] EXP_REQ = new double[MAX_LEVEL + 1];
        public static readonly double[] EXP_REQ_TOTAL = new double[MAX_LEVEL + 1];

        //awarding experience and drops
        public const float RANGE_EXP_AND_ASCENSION_ORB = 2500f;
        public const float PERCENT_CHANCE_BOSS_ORB_FIXED = 5.0f;
        public const float PERCENT_CHANCE_BOSS_ORB_FIXED_SINGLEPLAYER_BONUS = 5.0f;
        public const float PERCENT_CHANCE_BOSS_ORB_VARIABLE = 45.0f;
        public const float PERCENT_CHANCE_ASCENSION_ORB = 0.6f;
        public const float PERCENT_CHANCE_ASCENSION_ORB_EXPERT = 0.7f; 

        //misc
        public const int LEVEL_START_APPLYING_DEATH_PENALTY = 10;
        public const long AFK_TIME_TICKS_SEC = 60;

        //map settings constants
        public const double DEFAULT_EXPERIENCE_MODIFIER = 1.0;
        public const bool DEFAULT_IGNORE_CAPS = false;
        public const int DEFAULT_DAMAGE_REDUCTION = 0;
        public const int DEFAULT_LEVEL_CAP = 100;
        public const double DEFAULT_DEATH_PENALTY = 0.1;

        //map settings
        public static double worldExpModifier = DEFAULT_EXPERIENCE_MODIFIER;
        public static bool worldIgnoreCaps = DEFAULT_IGNORE_CAPS;
        public static int worldClassDamageReduction = DEFAULT_DAMAGE_REDUCTION;
        public static int worldLevelCap = DEFAULT_LEVEL_CAP;
        public static double worldDeathPenalty = DEFAULT_DEATH_PENALTY;
        public static double worldAuthCode = -1;
        public static bool worldRequireAuth = true;
        public static bool worldTrace = false;

        //shortcuts
        public static MyPlayer localMyPlayer;
        public static Mod mod;

        //syncing
        public static bool sync_local_proj = false;
        public static bool sync_local_status = false;

        //ability constants
        public const float HEAL_POWER_PER_IMMUNITY = 0.1f;
        public const float MAX_HEAL_POWER_IMMUNITY_BONUS = 1.0f;
        public enum STATUSES : byte
        {
            HolyLight,
            Blessing,
            DivineIntervention,
            Paragon,
            Renew,
            COUNT,
        }

        //debuffs
        public static readonly string[] DEBUFF_NAMES = {
            "Bleeding",
            "Poisoned",
            "OnFire",
            "Venom",
            "Darkness",
            "Blackout",
            "Silenced",
            "Cursed",
            "Confused",
            "Slow",
            "Slimed",
            "OgreSpit",
            "Weak",
            "BrokenArmor",
            "WitheredArmor",
            "WitheredWeapon",
            "CursedInferno",
            "Ichor",
            "Chilled",
            "Frozen",
            "Webbed",
            "Stoned",
            "VortexDebuff",
            "Obstructed",
            "Electrified",
            "Rabies",
            "Burning",
            "Frostburn",
            "Oiled",
            "ShadowFlame",
            "BetsysCurse",
            "Dazed",
        };
        public static readonly int NUMBER_OF_DEBUFFS = DEBUFF_NAMES.Length;
        public static int[] DEBUFFS = new int[NUMBER_OF_DEBUFFS];

        //undead
        public static readonly string[] UNDEAD_NAMES = 
        {
            "skel",
            "zomb",
            "groom",
            "bride",
            "undead",
            "viking",
            "eyezor",
            "bone",
            "ghost",
            "ghast",
            "dark caster",
            "skull",
            "dungeon guardian",
            "mummy",
            "tim",
            "ghoul",
            "diabolist",
            "floaty gross",
            "necromancer",
            "ragged caster",
            "wraith",
            "rune wizard",
            "vampire miner",
            "frankenstein",
            "reaper",
            "undead",
            "headless",
        };

        //start
        public ExperienceAndClasses()
        {
            Methods.Experience.CalcExpReqs();
            Properties = new ModProperties()
            {
                Autoload = true,
            };

            //get debuff id from names
            FieldInfo[] fields = typeof(BuffID).GetFields();
            foreach (FieldInfo f in fields)
            {
                for (int i = 0; i<NUMBER_OF_DEBUFFS; i++)
                {
                    if (f.Name.Equals(DEBUFF_NAMES[i]))
                    {
                        DEBUFFS[i] = (int)f.GetValue(null);
                    }
                }
            }
        }

        //load
        public override void Load()
        {
            //setup hotkeys
            for (int i=0; i<NUMBER_OF_ABILITY_SLOTS; i++)
            {
                HOTKEY_ABILITY[i] = RegisterHotKey("Ability " + (i + 1), HOTKEY_DEFAULTS[i]);
            }
            HOTKEY_ALTERNATE_EFFECT = RegisterHotKey("Alternate Effect", "LeftShift");

            uiExp = new UIExp();
            uiExp.Activate();
            myUserInterface = new UserInterface();
            myUserInterface.SetState(uiExp);

            mod = this;
        }

        public override void Unload()
        {
            //remove hotkeys
            HOTKEY_ABILITY = new ModHotKey[NUMBER_OF_ABILITY_SLOTS];
            HOTKEY_ALTERNATE_EFFECT = null;
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
            int newInt, newInt2, pIndex;
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
                //Player telling server to give another player debuff immunities
                case ExpModMessageType.ClientSendDebuffImmunity:
                    if (Main.netMode != 2) break;

                    //read
                    player = Main.player[reader.ReadInt32()]; //sender
                    newInt = reader.ReadInt32(); //target
                    newDouble = reader.ReadDouble(); //duration
                    newInt2 = reader.ReadInt32(); //number of debuffs
                    List<int> immunities = new List<int>();
                    for (int i=0; i< newInt2; i++)
                    {
                        immunities.Add(reader.ReadInt32());
                    }

                    //send to target
                    if (Main.player[newInt].active)
                    {
                        Methods.PacketSender.ServerDebuffImmunity(player.whoAmI, newInt, immunities, newDouble);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientSendDebuffImmunity from player #" + player.whoAmI + ":" + player.name + " for player #" + newInt + ":" + Main.player[newInt].name);
                    break;

                //Server giving a player debuff immunities
                case ExpModMessageType.ServerDebuffImmunity:
                    if (Main.netMode != 1) break;

                    //read
                    player = Main.player[reader.ReadInt32()]; //sender
                    newDouble = reader.ReadDouble(); //duration
                    newInt2 = reader.ReadInt32(); //number of debuffs
                    for (int i = 0; i < newInt2; i++)
                    {
                        MyPlayer.GrantDebuffImunity(reader.ReadInt32(), DateTime.Now, newDouble);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerDebuffImmunity from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Server's request to new player (includes map settings)
                case ExpModMessageType.ServerNewPlayerSync:
                    //set map settings
                    worldClassDamageReduction = reader.ReadInt32();
                    worldExpModifier = reader.ReadDouble();
                    worldIgnoreCaps = reader.ReadBoolean();
                    worldLevelCap = reader.ReadInt32();
                    worldRequireAuth = reader.ReadBoolean();
                    worldTrace = reader.ReadBoolean();
                    worldDeathPenalty = reader.ReadDouble();

                    //send back personal exp
                    Methods.PacketSender.ClientTellExperience(mod);

                    //display settings locally
                    Methods.ChatCommands.CommandDisplaySettings(mod);

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerNewPlayerSync");
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

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientTellExperience from player #" + player.whoAmI + ":" + player.name+" = "+ player.GetModPlayer<MyPlayer>(this).experience);
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

                        //if (Main.LocalPlayer.Equals(player))
                        //    uiExp.Update();
                    }

                    if ((Main.netMode==2 && worldTrace) || (Main.netMode==1 && traceChar))
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

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientTellAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd);
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
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        myPlayer.AddExp(expAdd);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestAddExp from player #" + player.whoAmI + ":" + player.name + " = " + expAdd);
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
                        worldAuthCode = newCode;
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);
                        Console.WriteLine("New expauth code: " + worldAuthCode);
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
                    else if (!worldRequireAuth)
                    {
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("This command requires expauth even when noauth is enabled."), MESSAGE_COLOUR_RED, player.whoAmI);
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestSetmapAuthCode from player #" + player.whoAmI + ":" + player.name + " = " + newCode);
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
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        worldExpModifier = exprate;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience Rate: " + (mapExpModifier*100)+"%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestExpRate from player #" + player.whoAmI + ":" + player.name + " = " + exprate);
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
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        worldLevelCap = newInt;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Level Cap:" + mapLevelCap), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestLevelCap from player #" + player.whoAmI + ":" + player.name + " = " + newInt);
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
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        worldClassDamageReduction = newInt;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Class damage reduction: " + mapClassDamageReduction + "%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestDamageReduction from player #" + player.whoAmI + ":" + player.name + " = " + newInt);
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
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        worldIgnoreCaps = newBool;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Ignore Class Caps: " + mapIgnoreCaps), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestToggleCap from player #" + player.whoAmI + ":" + player.name);
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
                        worldRequireAuth = !worldRequireAuth;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Require Expauth: " + mapRequireAuth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else if (!worldRequireAuth)
                    {
                        NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("This command requires expauth even when noauth is enabled."), MESSAGE_COLOUR_RED, player.whoAmI);
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestNoAuth from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Player requesting (needs auth) to toggle noauth
                case ExpModMessageType.ClientRequestMapTrace:
                    //read
                    player = Main.player[reader.ReadInt32()];
                    text = reader.ReadString();
                    //act
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        worldTrace = !worldTrace;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Map Trace: " + mapTrace), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestMapTrace from player #" + player.whoAmI + ":" + player.name);
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
                    if (myPlayer.auth || !worldRequireAuth)
                    {
                        worldDeathPenalty = newDouble;
                        Methods.PacketSender.ServerUpdateMapSettings(mod);
                        Console.WriteLine("Accepted command request from player #" + player.whoAmI + ":" + player.name + " " + text);

                        //announce
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Experience Rate: " + (mapExpModifier*100)+"%"), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    }
                    else
                    {
                        Console.WriteLine("Rejected command request from player #" + player.whoAmI + ":" + player.name + " " + text + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientRequestDeathPenalty from player #" + player.whoAmI + ":" + player.name + " = " + newDouble);
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

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientAFK from player #" + player.whoAmI + ":" + player.name);
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

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientUnAFK from player #" + player.whoAmI + ":" + player.name);
                    break;

                //Server updating map settings
                case ExpModMessageType.ServerUpdateMapSettings:
                    if (Main.netMode != 1) break;

                    //damage reduction
                    newInt = reader.ReadInt32();
                    if (newInt != worldClassDamageReduction)
                    {
                        worldClassDamageReduction = newInt;
                        Main.NewText("Updated Class Damage Reduction: " + worldClassDamageReduction + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    //experience rate
                    experience = reader.ReadDouble();
                    if (experience != worldExpModifier)
                    {
                        worldExpModifier = experience;
                        Main.NewText("Updated Experience Rate: " + (worldExpModifier*100) + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    //ignore class caps
                    newBool = reader.ReadBoolean();
                    if (newBool != worldIgnoreCaps)
                    {
                        worldIgnoreCaps = newBool;
                        Main.NewText("Updated Ignore Class Caps: " + worldIgnoreCaps, MESSAGE_COLOUR_YELLOW);
                    }

                    //level cap
                    newInt = reader.ReadInt32();
                    if (newInt != worldLevelCap)
                    {
                        worldLevelCap = newInt;
                        Main.NewText("Updated Level Cap: " + worldLevelCap, MESSAGE_COLOUR_YELLOW);
                    }

                    //require auth
                    newBool = reader.ReadBoolean();
                    if (newBool != worldRequireAuth)
                    {
                        worldRequireAuth = newBool;
                        Main.NewText("Updated Require Authorization: " + worldRequireAuth, MESSAGE_COLOUR_YELLOW);
                    }

                    //map trace
                    newBool = reader.ReadBoolean();
                    if (newBool != worldTrace)
                    {
                        worldTrace = newBool;
                        Main.NewText("Updated Map Trace: " + worldTrace, MESSAGE_COLOUR_YELLOW);
                    }

                    //death penalty
                    newDouble = reader.ReadDouble();
                    if (newDouble != worldDeathPenalty)
                    {
                        worldDeathPenalty = newDouble;
                        Main.NewText("Updated Death Penalty: " + (worldDeathPenalty * 100) + "%", MESSAGE_COLOUR_YELLOW);
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerUpdateMapSettings = " + worldClassDamageReduction + " " + (worldExpModifier*100) + " " + worldIgnoreCaps + " " + worldLevelCap + " " + worldRequireAuth + " " + worldTrace + " " + (worldDeathPenalty * 100));
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
                    if ((Main.netMode==2 && worldTrace) || (Main.netMode==1 && traceChar))
                    {
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), new Color(red, green, blue));
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved PlayerRequestAnnouncement from player #" + player.whoAmI + ":" + player.name + " = " + message);
                    break;

                //Server resyncs any recent exp changes
                case ExpModMessageType.ServerSyncExp:
                    if (Main.netMode != 1) break;

                    //trigger client sync when recieving full exp sync
                    newBool = reader.ReadBoolean(); //full sync
                    if (newBool)
                    {
                        TriggerLocalSyncs();
                    }

                    //read and set exp
                    newInt = reader.ReadInt32(); //number players
                    for (int ind = 0; ind < newInt; ind++)
                    {
                        pIndex = reader.ReadInt32();
                        newExp = reader.ReadDouble();
                        newInt2 = reader.ReadInt32(); //kill count

                        player = Main.player[pIndex];
                        if (player.active && (newExp >= 0))
                        {
                            myPlayer = player.GetModPlayer<MyPlayer>(this);
                            double expChange = newExp - myPlayer.experience;
                            myPlayer.experience = newExp;
                            myPlayer.ExpMsg(expChange);
                            myPlayer.kill_count = newInt2;

                            //if (Main.LocalPlayer.Equals(player))
                            //    uiExp.Update();
                        }
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ServerSyncExp");
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
                        if (worldAuthCode == code)
                        {
                            myPlayer.auth = true;
                            NetMessage.SendChatMessageToClient(NetworkText.FromLiteral("Auth: " + myPlayer.auth), ExperienceAndClasses.MESSAGE_COLOUR_YELLOW, player.whoAmI);
                            Console.WriteLine("Accepted auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code);
                        }
                        else
                        {
                            Console.WriteLine("Rejected auth attempt from player #" + player.whoAmI + ":" + player.name + " " + code + "\nExperience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                        }
                    }

                    if (worldTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientTryAuth from player #" + player.whoAmI + ":" + player.name + " " + code);
                    break;

                ////Player tells server that they are performing an ability
                //case ExpModMessageType.ClientAbility:
                //    //read
                //    player = Main.player[reader.ReadInt32()];

                //    //act
                //    Console.WriteLine("ABILITY_A");
                //    Main.NewText("ABILITY_B");

                //    if (mapTrace || traceChar) Methods.ChatCommands.Trace("TRACE:Recieved ClientAbility from player #" + player.whoAmI + ":" + player.name);
                //    break;

                //default:
                //    //ErrorLogger.Log("Unknown Message type: " + msgType);
                //    break;
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
                        myUserInterface.Update(Main._drawInterfaceGameTime);
                        uiExp.Draw(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ MISC ~~~~~~~~~~~~~~~~~~~~~ */

        //public static int CountActivePlayers()
        //{
        //    int count = 0;
        //    for (int i=0; i<Main.maxPlayers; i++)
        //    {
        //        if (Main.player[i].active)
        //        {
        //            count++;
        //        }
        //    }
        //    return count;
        //}

        public static void TriggerLocalSyncs()
        {
            sync_local_status = true;
            sync_local_proj = true;
        }
    }
}
