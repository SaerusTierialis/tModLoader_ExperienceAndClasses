using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Methods
{
    public static class PacketSender
    {
        static Mod mod = ModLoader.GetMod("ExperienceAndClasses");

        /* ~~~~~~~~~~~~~~~~~~~~~ Packet Senders - Client ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// Player telling server to make an announcement
        /// </summary>
        /// <param name="message"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        public static void ClientTellAnnouncement(string message, int red, int green, int blue)
        {
            if (Main.netMode != 1) return;

            if (red < 0) red = 0;
            if (red > 255) red = 255;
            if (green < 0) green = 0;
            if (green > 255) green = 255;
            if (blue < 0) blue = 0;
            if (blue > 255) blue = 255;

            ModPacket packet = mod.GetPacket();
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
        public static void ClientTellAddExp(double exp)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellAddExp);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(exp);
            packet.Send();
        }

        /// <summary>
        /// Player's response to server's request for experience (also send has_looted_monster_orb, explvlcap, and expdmgred)
        /// </summary>
        public static void ClientTellExperience()
        {
            if (Main.netMode != 1) return;

            MyPlayer local_MyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);

            ModPacket packet = mod.GetPacket();
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
        public static void ClientRequestAddExp(int player_index, double exp_add, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestAddExp);
            packet.Write(player_index);
            packet.Write(exp_add);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player asks server what the exprate is
        /// </summary>
        public static void ClientAsksExpRate()
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
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
        public static void ClientRequestExpRate(double rate, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
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
        public static void ClientRequestToggleCap(bool new_cap_bool, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
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
        public static void ClientTryAuth(double code)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTryAuth);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(code);
            packet.Send();
        }

        /// <summary>
        /// Player tells server that they would like to change their level cap
        /// </summary>
        /// <param name="new_level_cap"></param>
        public static void ClientUpdateLvlCap(int new_level_cap)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientUpdateLvlCap);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(new_level_cap);
            packet.Send();
        }

        /// <summary>
        /// Player tells server that they would like to change their damage reduction
        /// </summary>
        /// <param name="new_damage_reduction_percent"></param>
        public static void ClientUpdateDmgRed(int new_damage_reduction_percent)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
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
        public static void ServerForceExperience(Player player, int to_who = -1, int to_ignore = -1)
        {
            if (Main.netMode != 2) return;

            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerForceExperience);
            packet.Write(player.whoAmI);
            packet.Write(myPlayer.GetExp());
            packet.Write(myPlayer.explvlcap);
            packet.Write(myPlayer.expdmgred);
            packet.Send(to_who, to_ignore);
        }

        /// <summary>
        /// Server's initial request for player experience (also send has_looted_monster_orb, explvlcap, and expdmgred)
        /// </summary>
        /// <param name="player_index"></param>
        public static void ServerRequestExperience(int player_index)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerRequestExperience);
            packet.Send(player_index);
        }

        /// <summary>
        /// Server setting class caps on/off
        /// </summary>
        /// <param name="new_cap_bool"></param>
        public static void ServerToggleCap(bool new_cap_bool)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerToggleCap);
            packet.Write(new_cap_bool);
            packet.Send();
        }

        /// <summary>
        /// Server telling player that they have now recieved their first Ascension Orb
        /// </summary>
        /// <param name="player_index"></param>
        public static void ServerFirstAscensionOrb(int player_index)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerFirstAscensionOrb);
            packet.Send(player_index);
        }

        /// <summary>
        /// Server sends full exp list to new player (also explvlcap and expdmgred)
        /// </summary>
        /// <param name="to_who"></param>
        /// <param name="to_ignore"></param>
        public static void ServerFullExpList(int to_who, int to_ignore)
        {
            if (Main.netMode != 2) return;

            Player player;
            MyPlayer myPlayer;
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerFullExpList);
            for (int i = 0; i <= 255; i++)
            {
                player = Main.player[i];
                if (Main.player[i].active)
                {
                    myPlayer = player.GetModPlayer<MyPlayer>(mod);
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
            packet.Send(to_who, to_ignore);
        }
    }
}
