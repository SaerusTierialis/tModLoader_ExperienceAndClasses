using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    public static class PacketHandler {
        public enum PACKET_TYPE : byte {
            UNKNOWN,
            BROADCAST_TRACE,
            FORCE_FULL,
            FORCE_CLASS,
            FORCE_ATTRIBUTE,
            HEAL,
            AFK,
            XP,
        };

        //base type
        public abstract class Base<T> where T : Base<T> {
            //singleton (need instance for abstract methods)
            private static readonly ThreadLocal<T> Lazy = new ThreadLocal<T>(() => Activator.CreateInstance(typeof(T), true) as T);
            protected static T Instance { get { return Lazy.Value; } }

            protected abstract PACKET_TYPE GetPacketType();
            protected static ModPacket GetPacket(int origin) {
                ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
                packet.Write((byte)Instance.GetPacketType());
                packet.Write(origin);
                return packet;
            }
            public static void Recieve(BinaryReader reader, int origin) {
                //do not read anything from reader here (called multiple times when processing full packet)

                if (ExperienceAndClasses.trace) {
                    Commons.Trace("Handling " + Instance.GetPacketType() + " originating from " + origin);
                }

                MPlayer origin_mplayer = null;
                if ((origin >= 0) && (origin <= Main.maxPlayers)) {
                    origin_mplayer = Main.player[origin].GetModPlayer<MPlayer>(ExperienceAndClasses.MOD);
                }

                Instance.RecieveBody(reader, origin, origin_mplayer);
            }
            protected abstract void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer);
        }

        /// <summary>
        /// Client request broadcast from server
        /// </summary>
        public sealed class Broadcast : Base<Broadcast> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.BROADCAST_TRACE; }
            public static void Send(int target, int origin, string message) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                packet.Write(message);

                //send
                packet.Send(target, origin);
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                string message = reader.ReadString();

                //broadcast
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_TRACE);

                //also write in console
                Console.WriteLine(message);
            }
        }

        public sealed class ForceFull : Base<ForceFull> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.FORCE_FULL; }
            public static void Send(int target, int origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level, short[] attributes, bool afk_status) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                ForceClass.WritePacketBody(packet, primary_id, primary_level, secondary_id, secondary_level);
                ForceAttribute.WritePacketBody(packet, attributes);
                AFK.WritePacketBody(packet, afk_status);

                //send
                packet.Send(target, origin);
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                ForceClass.Recieve(reader, origin);
                ForceAttribute.Recieve(reader, origin);
                AFK.Recieve(reader, origin);
            }
        }

        public sealed class ForceClass : Base<ForceClass> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.FORCE_CLASS; }
            public static void Send(int target, int origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                WritePacketBody(packet, primary_id, primary_level, secondary_id, secondary_level);

                //send
                packet.Send(target, origin);
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                byte[] bytes = reader.ReadBytes(4);

                //set
                origin_mplayer.ForceClass(bytes[0], bytes[1], bytes[2], bytes[3]);

                //relay
                if (ExperienceAndClasses.IS_SERVER) {
                    Send(-1, origin, bytes[0], bytes[1], bytes[2], bytes[3]);
                }
            }
            public static void WritePacketBody(ModPacket packet, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
                packet.Write(primary_id);
                packet.Write(primary_level);
                packet.Write(secondary_id);
                packet.Write(secondary_level);
            }
        }

        public sealed class ForceAttribute : Base<ForceAttribute> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.FORCE_ATTRIBUTE; }
            public static void Send(int target, int origin, short[] attributes) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                WritePacketBody(packet, attributes);

                //send
                packet.Send(target, origin);
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                short[] attributes = new short[(byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs];
                for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                    attributes[i] = reader.ReadInt16();
                }

                //set
                origin_mplayer.ForceAttribute(attributes);

                //relay
                if (ExperienceAndClasses.IS_SERVER) {
                    Send(-1, origin, attributes);
                }
            }
            public static void WritePacketBody(ModPacket packet, short[] attributes) {
                for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                    packet.Write(attributes[i]);
                }
            }
        }

        public sealed class AFK : Base<AFK> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.AFK; }
            public static void Send(int target, int origin, bool afk_status) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                WritePacketBody(packet, afk_status);

                //send
                packet.Send(target, origin);
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                bool afk_status = reader.ReadBoolean();

                //set
                origin_mplayer.SetAfk(afk_status);

                //relay
                if (ExperienceAndClasses.IS_SERVER) {
                    Send(-1, origin, afk_status);
                }
            }
            public static void WritePacketBody(ModPacket packet, bool afk_status) {
                packet.Write(afk_status);
            }
        }

        public sealed class HEAL : Base<HEAL> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.HEAL; }
            public static void Send(int target, int origin, int amount_life, int amount_mana) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                packet.Write(target);
                packet.Write(amount_life);
                packet.Write(amount_mana);

                //send
                if (ExperienceAndClasses.IS_SERVER) {
                    packet.Send(target, origin);
                }
                else {
                    packet.Send(-1, origin);
                }
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                byte target = reader.ReadByte();
                int amount_life = reader.ReadInt32();
                int amount_mana = reader.ReadInt32();

                //do or relay
                Main.player[target].GetModPlayer<MPlayer>(ExperienceAndClasses.MOD).Heal(amount_life, amount_mana);
            }
        }

        public sealed class XP : Base<XP> {
            protected override PACKET_TYPE GetPacketType() { return PACKET_TYPE.XP; }
            public static void Send(int target, int origin, double xp) {
                //get packet containing header
                ModPacket packet = GetPacket(origin);

                //specific content
                packet.Write(xp);

                //send
                packet.Send(target, origin);
            }
            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                double xp = reader.ReadDouble();

                //set
                ExperienceAndClasses.LOCAL_MPLAYER.LocalAddXP(xp);
            }
        }
    }
}
