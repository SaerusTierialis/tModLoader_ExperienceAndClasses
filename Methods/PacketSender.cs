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
        /// Player's response to server's request for experience (also send hasLootedMonsterOrb, explvlcap, and expdmgred)
        /// </summary>
        public static void ClientTellExperience()
        {
            if (Main.netMode != 1) return;

            MyPlayer localMyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellExperience);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(localMyPlayer.GetExp());
            packet.Write(localMyPlayer.hasLootedMonsterOrb);
            packet.Write(localMyPlayer.explvlcap);
            packet.Write(localMyPlayer.expdmgred);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to add exp
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="expAdd"></param>
        /// <param name="text"></param>
        public static void ClientRequestAddExp(int playerIndex, double expAdd, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestAddExp);
            packet.Write(playerIndex);
            packet.Write(expAdd);
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
        /// <param name="playerIndex"></param>
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
        /// <param name="newCapBool"></param>
        /// <param name="text"></param>
        public static void ClientRequestToggleClassCap(bool newCapBool, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestToggleClassCap);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newCapBool);
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
        /// <param name="newLevelCap"></param>
        public static void ClientUpdateLvlCap(int newLevelCap)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientUpdateLvlCap);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newLevelCap);
            packet.Send();
        }

        /// <summary>
        /// Player tells server that they would like to change their damage reduction
        /// </summary>
        /// <param name="newDamageReductionPercent"></param>
        public static void ClientUpdateDmgRed(int newDamageReductionPercent)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientUpdateDmgRed);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newDamageReductionPercent);
            packet.Send();
        }

        /* ~~~~~~~~~~~~~~~~~~~~~ Packet Senders - Server ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// Server telling specific clients a player's new exp value
        /// </summary>
        /// <param name="player"></param>
        /// <param name="exp"></param>
        /// <param name="toWho"></param>
        /// <param name="toIgnore"></param>
        public static void ServerForceExperience(Player player, int toWho = -1, int toIgnore = -1)
        {
            if (Main.netMode != 2) return;

            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerForceExperience);
            packet.Write(player.whoAmI);
            packet.Write(myPlayer.GetExp());
            packet.Write(myPlayer.explvlcap);
            packet.Write(myPlayer.expdmgred);
            packet.Send(toWho, toIgnore);
        }

        /// <summary>
        /// Server's initial request for player experience (also send hasLootedMonsterOrb, explvlcap, and expdmgred)
        /// </summary>
        /// <param name="playerIndex"></param>
        public static void ServerRequestExperience(int playerIndex)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerRequestExperience);
            packet.Send(playerIndex);
        }

        /// <summary>
        /// Server setting class caps on/off
        /// </summary>
        /// <param name="newCapBool"></param>
        public static void ServerToggleClassCap(bool newCapBool)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerToggleClassCap);
            packet.Write(newCapBool);
            packet.Send();
        }

        /// <summary>
        /// Server telling player that they have now recieved their first Ascension Orb
        /// </summary>
        /// <param name="playerIndex"></param>
        public static void ServerFirstAscensionOrb(int playerIndex)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerFirstAscensionOrb);
            packet.Send(playerIndex);
        }

        /// <summary>
        /// Server sends full exp list to new player (also explvlcap and expdmgred)
        /// </summary>
        /// <param name="toWho"></param>
        /// <param name="toIgnore"></param>
        public static void ServerFullExpList(int toWho, int toIgnore)
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
            packet.Send(toWho, toIgnore);
        }
    }
}
