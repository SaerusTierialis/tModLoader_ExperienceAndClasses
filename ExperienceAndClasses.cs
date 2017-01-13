using Terraria.ModLoader;
using ExperienceAndClasses.UI;
using Terraria.UI;
using Terraria.DataStructures;
using Terraria;
using System.Collections.Generic;
using System;
using System.IO;

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
            CalcExpReqs();
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

        /* ~~~~~~~~~~~~~~~~~~~~~ Experience ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// calculate experience requirements, call only once
        /// </summary>
        public static void CalcExpReqs()
        {
            double adjust = 0;
            double total = 0;
            for (int lvl = 0; lvl <= MAX_LEVEL; lvl++)
            {
                if (lvl < EARLY_EXP_REQ.Length)
                {
                    EXP_REQ[lvl] = EARLY_EXP_REQ[lvl];
                }
                else
                {
                    adjust = ((double)lvl - (EARLY_EXP_REQ.Length)) / 100;
                    if (adjust > 0.32) adjust = 0.32;
                    EXP_REQ[lvl] = Math.Round(EXP_REQ[lvl - 1] * (1.35 - adjust), 0);
                }
                total += EXP_REQ[lvl];
                EXP_REQ_TOTAL[lvl] = total;
            }
        }

        /// <summary>
        /// return the amount of exp required for given level (optionally returns the total exp required instead)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public static double GetExpReqForLevel(int level, bool total)
        {
            if (level > MAX_LEVEL) level = MAX_LEVEL;
            if (!total)
                return EXP_REQ[level];
            else
                return EXP_REQ_TOTAL[level];
        }

        /// <summary>
        /// get current level given experience
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public static int GetLevel(double experience)
        {
            int level = 0;
            while (experience >= GetExpReqForLevel(level + 1, true) && level < MAX_LEVEL) level++;
            return level;
        }

        /// <summary>
        /// get exp needed to reach next level
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public static double GetExpUntilNextLevel(double experience)
        {
            int level = GetLevel(experience);
            return GetExpReqForLevel(GetLevel(experience) + 1, true) - experience;
        }


        /// <summary>
        /// get exp needed to reach next level
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public static double GetExpTowardsNextLevel(double experience)
        {
            int level = GetLevel(experience);
            return experience - GetExpReqForLevel(GetLevel(experience), true);
        }

        /// <summary>
        /// Get player's class(es)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetClass(Player player)
        {
            string job = "";
            Item[] equip = player.armor;
            for (int i = 0; i < equip.Length; i++)
            {
                if (equip[i].name.Contains("Class Token"))
                {
                    if (job.Length > 0) job += " & ";
                    job += equip[i].name.Substring(equip[i].name.IndexOf(":") + 2);
                }
            }
            if (job.Length == 0) job = "No Class";
            return job;
        }

        /// <summary>
        /// Get max tier of player's class(es)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetTier(Player player)
        {
            string str;

            Item[] equip = player.armor;
            int tier = -1;
            for (int i = 0; i < equip.Length; i++)
            {
                if (equip[i].name.Contains("Class Token"))
                {
                    str = equip[i].name.Substring(equip[i].name.IndexOf("Tier") + 5);
                    str = str.Substring(0, str.Length - 1);
                    switch (str)
                    {
                        case "I":
                            if (tier < 1) tier = 1;
                            break;
                        case "II":
                            if (tier < 2) tier = 2;
                            break;
                        case "III":
                            if (tier < 3) tier = 3;
                            break;
                        default:
                            break;
                    }
                }
            }
            return tier;
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ Packet Senders - Player ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// Player telling server to make an announcement
        /// </summary>
        /// <param name="message"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        public void PacketSend_ClientTellAnnouncement(string message, int red, int green, int blue)
        {
            if (Main.netMode != 1) return;

            if (red < 0) red = 0;
            if (red > 255) red = 255;
            if (green < 0) green = 0;
            if (green > 255) green = 255;
            if (blue < 0) blue = 0;
            if (blue > 255) blue = 255;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellAnnouncement);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(message);
            packet.Write(red); //red
            packet.Write(green); //green
            packet.Write(blue);   //blue
            packet.Send();
        }

        /// <summary>
        /// Player telling the server to adjust experience (e.g., craft token) NO AUTH REQUIRED
        /// </summary>
        /// <param name="exp"></param>
        public void PacketSend_ClientTellAddExp(double exp)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellAddExp);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(exp);
            packet.Send();
        }

        /// <summary>
        /// Player's response to server's request for experience (also send has_looted_monster_orb, explvlcap, and expdmgred)
        /// </summary>
        public void PacketSend_ClientTellExperience()
        {
            if (Main.netMode != 1) return;

            MyPlayer local_MyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(this);

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellExperience);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(local_MyPlayer.GetExp());
            packet.Write(local_MyPlayer.has_looted_monster_orb);
            packet.Write(local_MyPlayer.explvlcap);
            packet.Write(local_MyPlayer.expdmgred);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to add exp
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="exp_add"></param>
        /// <param name="text"></param>
        public void PacketSend_ClientRequestAddExp(int player_index, double exp_add, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestAddExp);
            packet.Write(player_index);
            packet.Write(exp_add);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player asks server what the exprate is
        /// </summary>
        public void PacketSend_ClientAsksExpRate()
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientAsksExpRate);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Send();
        }

        /// <summary>
        /// Player requests (needs auth) to set exprate
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="rate"></param>
        /// <param name="text"></param>
        public void PacketSend_ClientRequestExpRate(double rate, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestExpRate);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(rate);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to toggle class caps
        /// </summary>
        /// <param name="new_cap_bool"></param>
        /// <param name="text"></param>
        public void PacketSend_ClientRequestToggleCap(bool new_cap_bool, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestToggleCap);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(new_cap_bool);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player attempt auth
        /// </summary>
        /// <param name="code"></param>
        public void PacketSend_ClientTryAuth(double code)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTryAuth);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(code);
            packet.Send();
        }

        /// <summary>
        /// Player tells server that they would like to change their level cap
        /// </summary>
        /// <param name="new_level_cap"></param>
        public void PacketSend_ClientUpdateLvlCap(int new_level_cap)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientUpdateLvlCap);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(new_level_cap);
            packet.Send();
        }

        /// <summary>
        /// Player tells server that they would like to change their damage reduction
        /// </summary>
        /// <param name="new_damage_reduction_percent"></param>
        public void PacketSend_ClientUpdateDmgRed(int new_damage_reduction_percent)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ClientUpdateDmgRed);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(new_damage_reduction_percent);
            packet.Send();
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ Packet Senders - Server ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// Server telling specific clients a player's new exp value
        /// </summary>
        /// <param name="player"></param>
        /// <param name="exp"></param>
        /// <param name="to_who"></param>
        /// <param name="to_ignore"></param>
        public void PacketSend_ServerForceExperience(Player player, int to_who=-1, int to_ignore=-1)
        {
            if (Main.netMode != 2) return;

            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(this);

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ServerForceExperience);
            packet.Write(player.whoAmI);
            packet.Write(myPlayer.GetExp());
            packet.Write(myPlayer.explvlcap);
            packet.Write(myPlayer.expdmgred);
            packet.Send(to_who,to_ignore);
        }

        /// <summary>
        /// Server's initial request for player experience (also send has_looted_monster_orb, explvlcap, and expdmgred)
        /// </summary>
        /// <param name="player_index"></param>
        public void PacketSend_ServerRequestExperience(int player_index)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ServerRequestExperience);
            packet.Send(player_index);
        }

        /// <summary>
        /// Server setting class caps on/off
        /// </summary>
        /// <param name="new_cap_bool"></param>
        public void PacketSend_ServerToggleCap(bool new_cap_bool)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ServerToggleCap);
            packet.Write(new_cap_bool);
            packet.Send();
        }

        /// <summary>
        /// Server telling player that they have now recieved their first Ascension Orb
        /// </summary>
        /// <param name="player_index"></param>
        public void PacketSend_ServerFirstAscensionOrb(int player_index)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ServerFirstAscensionOrb);
            packet.Send(player_index);
        }

        /// <summary>
        /// Server sends full exp list to new player (also explvlcap and expdmgred)
        /// </summary>
        /// <param name="to_who"></param>
        /// <param name="to_ignore"></param>
        public void PacketSend_ServerFullExpList(int to_who, int to_ignore)
        {
            if (Main.netMode != 2) return;

            Player player;
            MyPlayer myPlayer;
            ModPacket packet = GetPacket();
            packet.Write((byte)ExpModMessageType.ServerFullExpList);
            for (int i = 0; i <= 255; i++)
            {
                player = Main.player[i];
                if (Main.player[i].active)
                {
                    myPlayer = player.GetModPlayer<MyPlayer>(this);
                    packet.Write(myPlayer.GetExp());
                    packet.Write(myPlayer.explvlcap);
                    packet.Write(myPlayer.expdmgred);
                }
                else
                {
                    packet.Write(-1);
                    packet.Write(-1);
                    packet.Write(-1);
                }
            }
            packet.Send(to_who,to_ignore);
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
                    PacketSend_ClientTellExperience();

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
                    PacketSend_ServerForceExperience(player, -1, ind_new_player);

                    //give new player full exp list
                    if (Main.netMode == 2)
                    {
                        PacketSend_ServerFullExpList(ind_new_player, -1);
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
                        PacketSend_ServerToggleCap(global_ignore_caps);

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

        /* ~~~~~~~~~~~~~~~~~~~~~ COMMAND FUNCTIONS ~~~~~~~~~~~~~~~~~~~~~ */

        public void CommandSetExp(double exp, string text)
        {
            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(this);
            if (Main.netMode==0)
            {
                myPlayer.SetExp(exp);
                Main.NewText("Set experience to " + exp+".");
            }
            else if (Main.netMode==1)
            {
                double exp_add = exp - myPlayer.GetExp();
                PacketSend_ClientRequestAddExp(player.whoAmI, exp_add, text);
                Main.NewTextMultiline("Request that experience be set to " + exp + " has been sent to the server."+
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]"+
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public void CommandExpRate()
        {
            if (Main.netMode == 0)
            {
                Main.NewText("Your current exprate is " + (Main.LocalPlayer.GetModPlayer<MyPlayer>(this).experience_modifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                PacketSend_ClientAsksExpRate();
                Main.NewText("Request for exprate has been sent to the server.");
            }
        }

        public void CommandSetExpRate(double rate, string text)
        {
            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(this);
            if (Main.netMode == 0)
            {
                myPlayer.experience_modifier = rate;
                Main.NewText("The new exprate is " + (myPlayer.experience_modifier * 100) + "%.");
            }
            else if (Main.netMode == 1)
            {
                PacketSend_ClientRequestExpRate(rate, text);
                Main.NewTextMultiline("Request that exprate be set to " + (rate * 100) + "% has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public void CommandToggleCaps(string text)
        {
            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(this);
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
            else if (Main.netMode==1)
            {
                PacketSend_ClientRequestToggleCap(!global_ignore_caps, text);
                Main.NewTextMultiline("Request to toggle the class caps feature has been sent to the server." +
                                    "\nIf you are authorized, the change should occur shortly. Use /auth [code]" +
                                    "\nto become authorized. The code is displayed in the server console.");
            }
        }

        public void CommandAuth(double code)//code -1 to check auth
        {
            if (Main.netMode != 1) return;
            PacketSend_ClientTryAuth(code);
            Main.NewTextMultiline("Request to authenticate has been sent to the server." +
                                "\nIf successful, you will receive a response shortly.");
        }

        public void CommandLvlCap(int level)
        {
            if (level < -1 || level==0 || level > MAX_LEVEL) return;

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(this);
            if (Main.netMode == 0)
            {
                int prior_level = GetLevel(myPlayer.GetExp());
                myPlayer.explvlcap = level;
                myPlayer.LimitExp();
                myPlayer.LevelUp(prior_level);
                myUI.updateValue(myPlayer.GetExp());
                if (level == -1) Main.NewText("Level cap disabled.");
                    else Main.NewText("Level cap set to " + myPlayer.explvlcap + ".");
            }
            else if (Main.netMode == 1)
            {
                PacketSend_ClientUpdateLvlCap(level);
                Main.NewText("Request to change level cap to "+level+" has been sent to the server.");
            }
        }

        public void CommandDmgRed(int damage_reduction_percent)
        {
            if (damage_reduction_percent < -1 || damage_reduction_percent > 100) return;

            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(this);
            if (Main.netMode == 0)
            {
                myPlayer.expdmgred = damage_reduction_percent;
                if (damage_reduction_percent == -1) Main.NewText("Damage reduction disabled.");
                    else Main.NewText("Damage reduction set to " + myPlayer.expdmgred + ".");
            }
            else if (Main.netMode == 1)
            {
                PacketSend_ClientUpdateDmgRed(damage_reduction_percent);
                Main.NewText("Request to change damage reduction to " + damage_reduction_percent + "% has been sent to the server.");
            }
        }

        public void CommandRequireAuth()
        {
            if (Main.netMode != 0)
            {
                Main.NewText("This command functions only in singleplayer mode.");
            }
            else
            {
                require_auth = !require_auth;
                if (require_auth) Main.NewText("Require Auth has been enabled. This map will now require auth in multiplayer mode.");
                else Main.NewText("Require Auth has been disabled. This map will no longer require auth in multiplayer mode.");
            }
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ OVERRIDES ~~~~~~~~~~~~~~~~~~~~~ */

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
                    Item[] equip; //inv
                    string job, message = "Current Players:";
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        player = Main.player[playerIndex];
                        if (player.active)
                        {
                            //temp
                            exp = player.GetModPlayer<MyPlayer>(this).GetExp();
                            //Main.NewText(player.name + "=" + exp);

                            job = GetClass(player);
                            level = GetLevel(exp);

                            exp_have = ExperienceAndClasses.GetExpTowardsNextLevel(exp);
                            exp_need = ExperienceAndClasses.GetExpReqForLevel(level + 1, false);

                            message += "\n" + player.name + ", Level " + level + "(" + Math.Round((double)exp_have / (double)exp_need * 100, 2) + "%), " + job;
                        }
                    }
                    Main.NewTextMultiline(message);
                }
                else if (command == "expadd" && args.Length > 0)
                {
                    double exp = Double.Parse(args[0]);
                    CommandSetExp(localMyPlayer.GetExp() + exp, text);
                }
                else if (command == "expsub" && args.Length > 0)
                {
                    double exp = Double.Parse(args[0]);
                    CommandSetExp(localMyPlayer.GetExp() - exp, text);
                }
                else if (command == "expset" && args.Length > 0)
                {
                    double exp = Double.Parse(args[0]);
                    CommandSetExp(exp, text);
                }
                else if (command == "exprate" && args.Length == 0)
                {
                    CommandExpRate();
                }
                else if (command == "exprate" && args.Length > 0)
                {
                    double rate = Double.Parse(args[0]);
                    CommandSetExpRate(rate / 100, text);
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
                        PacketSend_ClientTellAddExp(numUsed * EXP_ITEM_VALUE);
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
                        PacketSend_ClientTellAddExp(-1 * numCraft * EXP_ITEM_VALUE);
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
                    int level = GetLevel(exp) + amt;
                    exp = GetExpReqForLevel(level, true);

                    CommandSetExp(exp, text);
                }
                else if (command == "explvlsub")
                {
                    int amt = 0;
                    if (args.Length == 0) amt = 1;
                    else amt = Int32.Parse(args[0]);

                    double exp = localMyPlayer.GetExp();
                    int level = GetLevel(exp) - amt;
                    exp = GetExpReqForLevel(level, true);
                    if (exp < 0) exp = 0;

                    CommandSetExp(exp, text);
                }
                else if (command == "explvlset" && args.Length > 0)
                {
                    int level = Int32.Parse(args[0]);
                    CommandSetExp(GetExpReqForLevel(level, true), text);
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
                    CommandToggleCaps(text);
                }
                else if (command == "expauth" && args.Length==0)
                {
                    if (Main.netMode == 0) Main.NewText("Auth is only for multiplayer use.");
                        else CommandAuth(-1);
                }
                else if (command == "expauth" && args.Length>0)
                {
                    if (Main.netMode == 0) Main.NewText("Auth is only for multiplayer use.");
                    else
                    {
                        double code = Double.Parse(args[0]);
                        CommandAuth(code);
                    }
                }
                else if (command == "explvlcap" && args.Length > 0)
                {
                    int lvl = Int32.Parse(args[0]);
                    CommandLvlCap(lvl);
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
                    CommandDmgRed(dmgred);
                }
                else if (command == "expdmgred" && args.Length == 0)
                {
                    if (localMyPlayer.expdmgred == -1) Main.NewText("Damage reduction is disabled.");
                    else Main.NewText("Damage reduction is " + localMyPlayer.expdmgred + "%.");
                }
                else if (command == "expnoauth")
                {
                    CommandRequireAuth();
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
    }
}
