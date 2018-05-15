using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Methods
{
    public static class PacketSender
    {
        /* ~~~~~~~~~~~~~~~~~~~~~ Packet Senders - Client ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// Player telling server to give another player debuff immunities
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="immunity_indecies"></param>
        /// <param name="duration_seconds"></param>
        public static void ClientSendDebuffImmunity(int player_index, List<int> immunity_indecies, double duration_seconds)
        {
            if ((Main.netMode != 1) || (immunity_indecies.Count == 0))
            {
                return;
            }

            ModPacket packet = ExperienceAndClasses.mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientSendDebuffImmunity);
            packet.Write(Main.LocalPlayer.whoAmI); //sender (int)
            packet.Write(player_index); //target (int)
            packet.Write(duration_seconds); //duration (double)
            packet.Write(immunity_indecies.Count); //number of immunities (int)
            foreach (int i in immunity_indecies)
            {
                packet.Write(i); //immunity indicies (int)
            }
            packet.Send();
        }

        /// <summary>
        /// Player telling server to make an announcement.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="message"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        public static void ClientTellAnnouncement(Mod mod, string message, int red, int green, int blue)
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
        /// Player telling the server to adjust experience (e.g., craft token) NO AUTH REQUIRED.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="exp"></param>
        public static void ClientTellAddExp(Mod mod, double exp)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellAddExp);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(exp);
            packet.Send();
        }

        /// <summary>
        /// Player's response to server's request for experience
        /// </summary>
        /// <param name="mod"></param>
        public static void ClientTellExperience(Mod mod)
        {
            if (Main.netMode != 1) return;

            MyPlayer localMyPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTellExperience);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(localMyPlayer.GetExp());
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to add exp.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="playerIndex"></param>
        /// <param name="expAdd"></param>
        /// <param name="text"></param>
        public static void ClientRequestAddExp(Mod mod, int playerIndex, double expAdd, string text)
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
        /// Player requests (always needs auth) to set expauth code
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="newCode"></param>
        public static void ClientRequestSetmapAuthCode(Mod mod, double newCode, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestSetmapAuthCode);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newCode);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requests (needs auth) to set experience race
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="playerIndex"></param>
        /// <param name="rate"></param>
        /// <param name="text"></param>
        public static void ClientRequestExpRate(Mod mod, double rate, string text)
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
        /// Player requests (needs auth) to set level cap
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="newCap"></param>
        /// <param name="text"></param>
        public static void ClientRequestLevelCap(Mod mod, int newCap, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestLevelCap);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newCap);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requests (needs auth) to set class damage reduction
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="newReduction"></param>
        /// <param name="text"></param>
        public static void ClientRequestDamageReduction(Mod mod, int newReduction, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestDamageReduction);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newReduction);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to toggle class caps
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="newCapBool"></param>
        /// <param name="text"></param>
        public static void ClientRequestIgnoreCaps(Mod mod, bool newCapBool, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestIgnoreCaps);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(newCapBool);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to toggle noauth
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="text"></param>
        public static void ClientRequestNoAuth(Mod mod, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestNoAuth);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to toggle map trace
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="text"></param>
        public static void ClientRequestMapTrace(Mod mod, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestMapTrace);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player requesting (needs auth) to set death penalty
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="rate"></param>
        /// <param name="text"></param>
        public static void ClientRequestDeathPenalty(Mod mod, double rate, string text)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientRequestDeathPenalty);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(rate);
            packet.Write(text);
            packet.Send();
        }

        /// <summary>
        /// Player attempt auth.
        /// </summary>
        /// <param name="code"></param>
        public static void ClientTryAuth(Mod mod, double code)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientTryAuth);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Write(code);
            packet.Send();
        }

        /// <summary>
        /// Player telling server that they are away
        /// </summary>
        /// <param name="mod"></param>
        public static void ClientAFK(Mod mod)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientAFK);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Send();
        }

        /// <summary>
        /// Player telling server that they are back
        /// </summary>
        /// <param name="mod"></param>
        public static void ClientUnAFK(Mod mod)
        {
            if (Main.netMode != 1) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ClientUnAFK);
            packet.Write(Main.LocalPlayer.whoAmI);
            packet.Send();
        }

        ///// <summary>
        ///// Player tells server that they are performing an ability
        ///// </summary>
        ///// <param name="mod"></param>
        ///// <param name="abilityID"></param>
        //public static void ClientAbility(Mod mod, int abilityID, int level = 1, double rand = 0)
        //{
        //    if (Main.netMode != 1) return;

        //    ModPacket packet = mod.GetPacket();
        //    packet.Write((byte)ExpModMessageType.ClientAbility);
        //    packet.Write(Main.LocalPlayer.whoAmI);
        //    packet.Write(abilityID);
        //    packet.Write(level);
        //    packet.Write(rand);
        //    packet.Send();
        //}

        /* ~~~~~~~~~~~~~~~~~~~~~ Packet Senders - Server ~~~~~~~~~~~~~~~~~~~~~ */

        /// <summary>
        /// Server giving a player debuff immunities
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="immunity_indecies"></param>
        /// <param name="duration_seconds"></param>
        public static void ServerDebuffImmunity(int player_index_source, int player_index_target, List<int> immunity_indecies, double duration_seconds)
        {
            if ((Main.netMode != 2) || (immunity_indecies.Count == 0))
            {
                return;
            }

            ModPacket packet = ExperienceAndClasses.mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerDebuffImmunity);
            packet.Write(player_index_source); //sender (int)
            packet.Write(duration_seconds); //duration (double)
            packet.Write(immunity_indecies.Count); //number of immunities (int)
            foreach (int i in immunity_indecies)
            {
                packet.Write(i); //immunity indicies (int)
            }
            packet.Send(player_index_target);
        }

        /// <summary>
        /// Server's request to new player (includes map settings)
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="playerIndex"></param>
        public static void ServerNewPlayerSync(Mod mod, int playerIndex)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();

            packet.Write((byte)ExpModMessageType.ServerNewPlayerSync);
            packet.Write(ExperienceAndClasses.worldClassDamageReduction);
            packet.Write(ExperienceAndClasses.worldExpModifier);
            packet.Write(ExperienceAndClasses.worldIgnoreCaps);
            packet.Write(ExperienceAndClasses.worldLevelCap);
            packet.Write(ExperienceAndClasses.worldRequireAuth);
            packet.Write(ExperienceAndClasses.worldTrace);
            packet.Write(ExperienceAndClasses.worldDeathPenalty);
            packet.Send(playerIndex);
        }

        /// <summary>
        /// Server telling specific clients a player's new exp value.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="player"></param>
        /// <param name="exp"></param>
        /// <param name="toWho"></param>
        /// <param name="toIgnore"></param>
        public static void ServerForceExperience(Mod mod, Player player, int toWho = -1, int toIgnore = -1)
        {
            if (Main.netMode != 2) return;

            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);

            ModPacket packet = mod.GetPacket();

            packet.Write((byte)ExpModMessageType.ServerForceExperience);
            packet.Write(player.whoAmI);
            packet.Write(myPlayer.GetExp());
            packet.Send(toWho, toIgnore);
        }

        /// <summary>
        /// Server updating map settings
        /// </summary>
        /// <param name="mod"></param>
        public static void ServerUpdateMapSettings(Mod mod)
        {
            if (Main.netMode != 2) return;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerUpdateMapSettings);
            packet.Write(ExperienceAndClasses.worldClassDamageReduction);
            packet.Write(ExperienceAndClasses.worldExpModifier);
            packet.Write(ExperienceAndClasses.worldIgnoreCaps);
            packet.Write(ExperienceAndClasses.worldLevelCap);
            packet.Write(ExperienceAndClasses.worldRequireAuth);
            packet.Write(ExperienceAndClasses.worldTrace);
            packet.Write(ExperienceAndClasses.worldDeathPenalty);
            packet.Send();
        }

        /// <summary>
        /// Server resyncs any recent exp changes
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="toWho"></param>
        /// <param name="toIgnore"></param>
        public static void ServerSyncExp(Mod mod, bool full = false)
        {
            if (Main.netMode != 2) return;

            if (full) MyWorld.FlagAllForSyncExp();

            if (MyWorld.clientNeedsExpUpdate_counter <= 0) return;

            Player player;
            MyPlayer myPlayer;

            ModPacket packet = mod.GetPacket();
            packet.Write((byte)ExpModMessageType.ServerSyncExp);
            packet.Write(MyWorld.clientNeedsExpUpdate_counter); //number of clients updated
            for (int ind = 0; ind < MyWorld.clientNeedsExpUpdate_counter; ind++)
            {
                player = Main.player[MyWorld.clientNeedsExpUpdate_who[ind]];
                packet.Write(player.whoAmI); //who dis
                if (player.active)
                {
                    myPlayer = player.GetModPlayer<MyPlayer>(mod);
                    packet.Write(myPlayer.GetExp()); //xp

                    int kill_id = myPlayer.kill_count_track_id;
                    if (NPCs.MyGlobalNPC.kill_counts.ContainsKey(kill_id))
                    {
                        packet.Write((int)NPCs.MyGlobalNPC.kill_counts.GetByIndex(NPCs.MyGlobalNPC.kill_counts.IndexOfKey(kill_id))); //kill count (int32)
                    }
                    else
                    {
                        packet.Write(0); //kill count
                    }
                }
                else
                {
                    packet.Write(-1); //xp
                    packet.Write(-1); //kill count
                }

                //reset
                MyWorld.clientNeedsExpUpdate_who[ind] = 0;
                MyWorld.clientNeedsExpUpdate[player.whoAmI] = false;
            }
            packet.Send();

            //reset
            MyWorld.clientNeedsExpUpdate_counter = 0;
        }
    }
}
