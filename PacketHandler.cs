using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    class PacketHandler {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sending ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void SendForceClass(byte who, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.PACKET_TYPE.FORCE_CLASS);
            packet.Write(who);
            packet = SendForceClass_Body(packet, primary_id, primary_level, secondary_id, secondary_level);
            packet.Send(-1, who);
        }
        private static ModPacket SendForceClass_Body(ModPacket packet, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            packet.Write(primary_id);
            packet.Write(primary_level);
            packet.Write(secondary_id);
            packet.Write(secondary_level);
            return packet;
        }

        public static void SendForceAttribute(byte who, short[] attributes) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.PACKET_TYPE.FORCE_ATTRIBUTE);
            packet.Write(who);
            packet = SendForceAttribute_Body(packet, attributes);
            packet.Send(-1, who);
        }
        private static ModPacket SendForceAttribute_Body(ModPacket packet, short[] attributes) {
            for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                packet.Write(attributes[i]);
            }
            return packet;
        }

        public static void SendForceFull(byte who, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level, short[] attributes) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.PACKET_TYPE.FORCE_FULL);
            packet.Write(who);
            packet = SendForceClass_Body(packet, primary_id, primary_level, secondary_id, secondary_level);
            packet = SendForceAttribute_Body(packet, attributes);
            packet.Send(-1, who);
        }

        public static void SendBroadcastTrace(byte who, string message) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.PACKET_TYPE.BROADCAST_TRACE);
            packet.Write(who);
            packet.Write(message);
            packet.Send(-1);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Recieving ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void HandlePacketContents(byte origin_id, Player origin_player, MPlayer origin_mplayer, ExperienceAndClasses.PACKET_TYPE packet_type, BinaryReader reader) {
            if (ExperienceAndClasses.trace) {
                Commons.Trace("Handling " + packet_type + " originating from player " + origin_id);
            }

            switch (packet_type) {
                case ExperienceAndClasses.PACKET_TYPE.BROADCAST_TRACE:
                    //read
                    string message = reader.ReadString();

                    //broadcast
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), ExperienceAndClasses.COLOUR_MESSAGE_TRACE);

                    break;

                case ExperienceAndClasses.PACKET_TYPE.FORCE_FULL:
                    HandlePacketContents(origin_id, origin_player, origin_mplayer, ExperienceAndClasses.PACKET_TYPE.FORCE_CLASS, reader);
                    HandlePacketContents(origin_id, origin_player, origin_mplayer, ExperienceAndClasses.PACKET_TYPE.FORCE_ATTRIBUTE, reader);
                    break;

                case ExperienceAndClasses.PACKET_TYPE.FORCE_CLASS:
                    //read
                    byte[] bytes = reader.ReadBytes(4);

                    //set
                    origin_mplayer.ForceClass(bytes[0], bytes[1], bytes[2], bytes[3]);

                    //relay
                    if (ExperienceAndClasses.IS_SERVER) {
                        PacketHandler.SendForceClass(origin_id, bytes[0], bytes[1], bytes[2], bytes[3]);
                    }

                    break;

                case ExperienceAndClasses.PACKET_TYPE.FORCE_ATTRIBUTE:
                    //read
                    short[] attributes = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
                    for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                        attributes[i] = reader.ReadInt16();
                    }

                    //set
                    origin_mplayer.ForceAttribute(attributes);

                    //relay
                    if (ExperienceAndClasses.IS_SERVER) {
                        PacketHandler.SendForceAttribute(origin_id, attributes);
                    }

                    break;

                default:
                    //unknown type
                    break;
            }
        }

    }
}
