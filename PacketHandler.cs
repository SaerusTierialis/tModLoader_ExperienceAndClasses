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
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Packet Types ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public enum PACKET_TYPE : byte {
            BROADCAST_TRACE,
            FORCE_FULL,
            FORCE_CLASS,
            FORCE_ATTRIBUTE,
            HEAL,
            AFK,
            XP,
        };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sending ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void SendForceClass(byte origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PacketHandler.PACKET_TYPE.FORCE_CLASS);
            packet.Write(origin);
            packet = SendForceClass_Body(packet, primary_id, primary_level, secondary_id, secondary_level);
            packet.Send(-1, origin);
        }
        private static ModPacket SendForceClass_Body(ModPacket packet, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            packet.Write(primary_id);
            packet.Write(primary_level);
            packet.Write(secondary_id);
            packet.Write(secondary_level);
            return packet;
        }

        public static void SendForceAttribute(byte origin, short[] attributes) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PACKET_TYPE.FORCE_ATTRIBUTE);
            packet.Write(origin);
            packet = SendForceAttribute_Body(packet, attributes);
            packet.Send(-1, origin);
        }
        private static ModPacket SendForceAttribute_Body(ModPacket packet, short[] attributes) {
            for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                packet.Write(attributes[i]);
            }
            return packet;
        }

        public static void SendForceFull(byte origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level, short[] attributes, bool afk) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PACKET_TYPE.FORCE_FULL);
            packet.Write(origin);
            packet = SendForceClass_Body(packet, primary_id, primary_level, secondary_id, secondary_level);
            packet = SendForceAttribute_Body(packet, attributes);
            packet.Write(afk);
            packet.Send(-1, origin);
        }

        public static void SendBroadcastTrace(byte origin, string message) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PACKET_TYPE.BROADCAST_TRACE);
            packet.Write(origin);
            packet.Write(message);
            packet.Send(-1);
        }

        public static void SendHeal(byte origin, byte target, int amount_life, int amount_mana) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PACKET_TYPE.HEAL);
            packet.Write(origin);
            packet.Write(target);
            packet.Write(amount_life);
            packet.Write(amount_mana);
            if (ExperienceAndClasses.IS_SERVER) {
                packet.Send(target);
            }
            else {
                packet.Send(-1);
            }
        }

        public static void SendAFK(byte origin, bool afk) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PACKET_TYPE.AFK);
            packet.Write(origin);
            packet.Write(afk);
            packet.Send(-1, origin);
        }

        public static void SendXP(byte target, double xp) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)PACKET_TYPE.XP);
            packet.Write(-1); //from server
            packet.Write(xp);
            packet.Send(target);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Recieving ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static void HandlePacketContents(byte origin_id, Player origin_player, MPlayer origin_mplayer, PACKET_TYPE packet_type, BinaryReader reader) {
            if (ExperienceAndClasses.trace) {
                Commons.Trace("Handling " + packet_type + " originating from " + origin_id);
            }

            switch (packet_type) {
                case PACKET_TYPE.BROADCAST_TRACE: //sent by client to server
                    //read
                    string message = reader.ReadString();

                    //broadcast
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), Shared.COLOUR_MESSAGE_TRACE);

                    //also write in console
                    Console.WriteLine(message);

                    break;

                case PACKET_TYPE.FORCE_FULL:
                    HandlePacketContents(origin_id, origin_player, origin_mplayer, PACKET_TYPE.FORCE_CLASS, reader);
                    HandlePacketContents(origin_id, origin_player, origin_mplayer, PACKET_TYPE.FORCE_ATTRIBUTE, reader);
                    HandlePacketContents(origin_id, origin_player, origin_mplayer, PACKET_TYPE.AFK, reader);
                    break;

                case PACKET_TYPE.FORCE_CLASS:
                    //read
                    byte[] bytes = reader.ReadBytes(4);

                    //set
                    origin_mplayer.ForceClass(bytes[0], bytes[1], bytes[2], bytes[3]);

                    //relay
                    if (ExperienceAndClasses.IS_SERVER) {
                        SendForceClass(origin_id, bytes[0], bytes[1], bytes[2], bytes[3]);
                    }

                    break;

                case PACKET_TYPE.FORCE_ATTRIBUTE:
                    //read
                    short[] attributes = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
                    for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                        attributes[i] = reader.ReadInt16();
                    }

                    //set
                    origin_mplayer.ForceAttribute(attributes);

                    //relay
                    if (ExperienceAndClasses.IS_SERVER) {
                        SendForceAttribute(origin_id, attributes);
                    }

                    break;

                case PACKET_TYPE.HEAL:
                    //read
                    byte target = reader.ReadByte();
                    int amount_life = reader.ReadInt32();
                    int amount_mana = reader.ReadInt32();

                    //do or relay
                    Main.player[target].GetModPlayer<MPlayer>(ExperienceAndClasses.MOD).Heal(amount_life, amount_mana);

                    break;

                case PACKET_TYPE.AFK:
                    //read
                    bool afk = reader.ReadBoolean();

                    //set
                    origin_mplayer.SetAfk(afk);

                    //relay
                    if (ExperienceAndClasses.IS_SERVER) {
                        SendAFK(origin_id, afk);
                    }

                    break;

                case PACKET_TYPE.XP:
                    //read
                    double xp = reader.ReadDouble();

                    //set
                    ExperienceAndClasses.LOCAL_MPLAYER.LocalAddXP(xp);

                    break;

                default:
                    //unknown type
                    break;
            }
        }

    }
}
