using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Utilities {
    /// <summary>
    /// Contains handlers for all mod packet types.
    /// 
    /// Packet sending uses static methods for ease of use.
    /// Packet receiving is standardized and automatic aside from RecieveBody.
    /// 
    /// The implementation is not very elegant, but it should be pretty fast and
    /// new packet types can be added without making changes elsewhere.
    /// 
    /// To add new packets: add to the enum and define a class with the same name
    /// </summary>
    public static class PacketHandler {

        //IMPORTANT: each type MUST have a class with the exact same name
        public enum PACKET_TYPE : byte {
            Broadcast,
            ForceFull,
            ForceClass,
            ForceAttribute,
            //Heal,
            AFK, //TODO convert to status
            InCombat, //TODO convert to status
            XP,
            AddStatus,
            RemoveStatus,
            SetStatuses,

            NUMBER_OF_TYPES, //must be last
        };

        //populate lookup for automated receiving of packets (possible because receiving is standardized)
        public static Handler[] LOOKUP { get; private set; }
        static PacketHandler() {
            LOOKUP = new Handler[(byte)PACKET_TYPE.NUMBER_OF_TYPES];

            string[] packet_names = Enum.GetNames(typeof(PACKET_TYPE));

            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Handler)Assembly.GetExecutingAssembly().CreateInstance(typeof(PacketHandler).FullName + "+" + packet_names[i]);

                //set instance vars for inherited methods
                LOOKUP[i].SetFields(i, packet_names[i]);
            }
        }

        //base type
        public abstract class Handler {
            public byte packet_byte { get; private set; }
            public string packet_string { get; private set; }
            private bool fields_set = false;

            public bool SetFields(byte packet_byte, string packet_string) {
                if (!fields_set) {
                    this.packet_byte = packet_byte;
                    this.packet_string = packet_string;
                    fields_set = true;
                    return true;
                }
                else {
                    return false;
                }
            }

            public ModPacket GetPacket(int origin) {
                ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
                packet.Write(packet_byte);
                packet.Write(origin);
                return packet;
            }

            public void Recieve(BinaryReader reader, int origin) {
                //do not read anything from reader here (called multiple times when processing full sync packet)

                if (ExperienceAndClasses.trace) {
                    Commons.Trace("Handling " + packet_string + " originating from " + origin);
                }

                MPlayer origin_mplayer = null;
                if ((origin >= 0) && (origin <= Main.maxPlayers)) {
                    origin_mplayer = Main.player[origin].GetModPlayer<MPlayer>(ExperienceAndClasses.MOD);
                }

                RecieveBody(reader, origin, origin_mplayer);
            }

            protected abstract void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer);
        }

        /// <summary>
        /// Client request broadcast from server
        /// </summary>
        public sealed class Broadcast : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, string message) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);
                
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

        public sealed class ForceFull : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level, int[] attributes, bool afk_status) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

                //specific content
                ForceClass.WritePacketBody(packet, primary_id, primary_level, secondary_id, secondary_level);
                ForceAttribute.WritePacketBody(packet, attributes);
                AFK.WritePacketBody(packet, afk_status);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                LOOKUP[(byte)PACKET_TYPE.ForceClass].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.ForceAttribute].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.AFK].Recieve(reader, origin);
            }
        }

        public sealed class ForceClass : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

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
                if (Utilities.Netmode.IS_SERVER) {
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

        public sealed class ForceAttribute : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, int[] attributes) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

                //specific content
                WritePacketBody(packet, attributes);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                int[] attributes = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
                for (byte i = 0; i < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    attributes[i] = reader.ReadInt16();
                }

                //set
                origin_mplayer.ForceAttribute(attributes);

                //relay
                if (Utilities.Netmode.IS_SERVER) {
                    Send(-1, origin, attributes);
                }
            }

            public static void WritePacketBody(ModPacket packet, int[] attributes) {
                for (byte i = 0; i < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    packet.Write(attributes[i]);
                }
            }
        }

        public sealed class AFK : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, bool afk_status) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

                //specific content
                WritePacketBody(packet, afk_status);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                bool afk_status = reader.ReadBoolean();

                //set
                origin_mplayer.SetInCombat(afk_status);

                //relay
                if (Utilities.Netmode.IS_SERVER) {
                    Send(-1, origin, afk_status);
                }
            }

            public static void WritePacketBody(ModPacket packet, bool afk_status) {
                packet.Write(afk_status);
            }
        }

        public sealed class InCombat : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, bool combat_status) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

                //specific content
                WritePacketBody(packet, combat_status);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                bool combat_status = reader.ReadBoolean();

                //set
                origin_mplayer.SetInCombat(combat_status);

                //relay
                if (Utilities.Netmode.IS_SERVER) {
                    Send(-1, origin, combat_status);
                }
            }

            public static void WritePacketBody(ModPacket packet, bool afk_status) {
                packet.Write(afk_status);
            }
        }

        /*
        public sealed class Heal : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, int amount_life, int amount_mana) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

                //specific content
                packet.Write(target);
                packet.Write(amount_life);
                packet.Write(amount_mana);

                //send
                if (Utilities.Netmode.IS_SERVER) {
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
        */

        public sealed class XP : Handler {
            private static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            public static void Send(int target, int origin, uint xp) {
                //get packet containing header
                ModPacket packet = Instance.GetPacket(origin);

                //specific content
                packet.Write(xp);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //read
                uint xp = reader.ReadUInt32();

                //set
                ExperienceAndClasses.LOCAL_MPLAYER.AddXP(xp);
            }
        }

        public sealed class AddStatus : Handler {
            public static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //TODO
                
            }
        }

        public sealed class RemoveStatus : Handler {
            public static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //TODO

            }
        }

        public sealed class SetStatuses : Handler {
            public static readonly Handler Instance = LOOKUP[(byte)Enum.Parse(typeof(PACKET_TYPE), MethodBase.GetCurrentMethod().DeclaringType.Name)];

            protected override void RecieveBody(BinaryReader reader, int origin, MPlayer origin_mplayer) {
                //TODO

            }
        }
    }
}
