using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses.Utilities {
    class PacketHandler {
        //IMPORTANT: each type MUST have a class with the exact same name
        public enum PACKET_TYPE : byte {
            ClientBroadcast,
            ClientPassword,
            WOF,
            XP,
            CharLevel,
            FullSync,
            Attributes,
            Class,
            AFK,


            NUMBER_OF_TYPES, //must be last
        };

        //populate lookup for automated receiving of packets (possible because receiving is standardized)
        public static Handler[] LOOKUP { get; private set; }
        static PacketHandler() {
            string str;
            LOOKUP = new Handler[(byte)PACKET_TYPE.NUMBER_OF_TYPES];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                str = Enum.GetName(typeof(PACKET_TYPE), i);
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Handler>(str, typeof(PacketHandler));
            }
        }

        //base type
        public abstract class Handler {
            public PACKET_TYPE ID { get; private set; }
            public byte ID_Num { get; private set; }

            public Handler(PACKET_TYPE id) {
                ID = id;
                ID_Num = (byte)ID;
            }

            public ModPacket GetPacket(int origin) {
                ModPacket packet = Shortcuts.MOD.GetPacket();
                packet.Write(ID_Num);
                packet.Write(origin);
                return packet;
            }

            public void Recieve(BinaryReader reader, int origin) {
                //do not read anything from reader here (called multiple times when processing full sync packet)

                bool do_trace = Shortcuts.GetConfigServer.PacketTrace && (ID != PACKET_TYPE.ClientBroadcast);

                if (do_trace) {
                    Logger.Trace("Handling " + ID + " originating from " + origin);
                }

                EACPlayer origin_eacplayer = null;
                if ((origin >= 0) && (origin <= Main.maxPlayers)) {
                    origin_eacplayer = Main.player[origin].GetModPlayer<EACPlayer>();
                }

                RecieveBody(reader, origin, origin_eacplayer);

                if (do_trace) {
                    Logger.Trace("Done handling " + ID + " originating from " + origin);
                }
            }

            protected abstract void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer);
        }

        /// <summary>
        /// Client request broadcast from server
        /// </summary>
        public sealed class ClientBroadcast : Handler {
            public enum BROADCAST_TYPE : byte {
                MESSAGE,
                TRACE,
                ERROR
            }

            public ClientBroadcast() : base(PACKET_TYPE.ClientBroadcast) { }

            public static void Send(int target, int origin, string message, BROADCAST_TYPE type) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.ClientBroadcast].GetPacket(origin);

                //type
                packet.Write((byte)type);

                //message
                packet.Write(message);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //type
                BROADCAST_TYPE type = (BROADCAST_TYPE)reader.ReadByte();

                //message
                string message = reader.ReadString();

                //colour
                Color colour = Color.White;
                switch (type) {
                    case BROADCAST_TYPE.ERROR:
                        colour = UI.Constants.COLOUR_MESSAGE_ERROR;
                        break;
                    case BROADCAST_TYPE.TRACE:
                        colour = UI.Constants.COLOUR_MESSAGE_TRACE;
                        break;
                    case BROADCAST_TYPE.MESSAGE:
                        colour = UI.Constants.COLOUR_MESSAGE_BROADCAST;
                        break;
                }

                //broadcast
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), colour);

                //also write in console
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Client tells server its local password
        /// </summary>
        public sealed class ClientPassword : Handler {
            public ClientPassword() : base(PACKET_TYPE.ClientPassword) { }

            public static void Send(int target, int origin, string password) {
                if (Shortcuts.IS_CLIENT) {
                    //get packet containing header
                    ModPacket packet = LOOKUP[(byte)PACKET_TYPE.ClientPassword].GetPacket(origin);

                    //specific content
                    packet.Write(password);

                    //send
                    packet.Send(target, origin);
                }
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read and set
                origin_eacplayer.Fields.password = reader.ReadString();
            }
        }

        /// <summary>
        /// When a client recieves this packet, WOF is marked as defeated
        /// </summary>
        public sealed class WOF : Handler {
            public WOF() : base(PACKET_TYPE.WOF) { }

            public static void Send(int target, int origin) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.WOF].GetPacket(origin);

                //no contents

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                Shortcuts.LOCAL_PLAYER.PSheet.Character.DefeatWOF();
            }
        }

        public sealed class XP : Handler {
            public XP() : base(PACKET_TYPE.XP) { }

            public static void Send(int target, int origin, uint xp, bool is_combat = true) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.XP].GetPacket(origin);

                //specific content
                packet.Write(xp);
                packet.Write(is_combat);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                uint xp = reader.ReadUInt32();
                bool is_combat = reader.ReadBoolean();

                //set
                Systems.XP.Adjustments.LocalAddXP(xp, is_combat);
            }
        }

        public sealed class CharLevel : Handler {
            public CharLevel() : base (PACKET_TYPE.CharLevel) { }

            public static void Send(int target, int origin, byte level, bool levelup = false) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.CharLevel].GetPacket(origin);

                //specific content
                WritePacketBody(packet, level, levelup);

                //send
                packet.Send(target, origin);
            }

            public static void WritePacketBody(ModPacket packet, byte level, bool levelup = false) {
                packet.Write(level);
                packet.Write(levelup);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                byte level = reader.ReadByte();
                bool levelup = reader.ReadBoolean();

                //set level
                origin_eacplayer.PSheet.Character.ForceLevel(level);

                //levelup?
                if (levelup && Shortcuts.IS_SERVER) {
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(origin_eacplayer.PSheet.Character.GetLevelupMessage()), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }

                //relay
                if (Shortcuts.IS_SERVER) {
                    Send(-1, origin, level, levelup);
                }
            }
        }

        public sealed class FullSync : Handler {
            public FullSync() : base (PACKET_TYPE.FullSync) { }

            public static void Send(EACPlayer eacplayer) {
                //get packet containing header
                int origin = eacplayer.player.whoAmI;
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.FullSync].GetPacket(origin);

                //specific content
                CharLevel.WritePacketBody(packet, eacplayer.PSheet.Character.Level, false);
                Attributes.WritePacketBody(packet, eacplayer.PSheet.Attributes.Allocated_Effective);
                Class.WritePacketBody(packet, eacplayer.PSheet.Classes.Primary.ID, eacplayer.PSheet.Classes.Primary.Level, eacplayer.PSheet.Classes.Secondary.ID, eacplayer.PSheet.Classes.Secondary.Level);
                AFK.WritePacketBody(packet, eacplayer.PSheet.Character.AFK);
                //TODO - other sync data

                //send
                packet.Send(-1, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //handle packet
                LOOKUP[(byte)PACKET_TYPE.CharLevel].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.Attributes].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.Class].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.AFK].Recieve(reader, origin);
                //TODO - other sync data

                //is init
                if (!origin_eacplayer.Fields.initialized) {
                    origin_eacplayer.Fields.initialized = true;
                }

                //relay
                if (Shortcuts.IS_SERVER) {
                    Send(origin_eacplayer);
                }
            }
        }

        public sealed class Attributes : Handler {
            public Attributes() : base(PACKET_TYPE.Attributes) { }

            public static void Send(int target, int origin, int[] attributes) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.Attributes].GetPacket(origin);

                //specific content
                WritePacketBody(packet, attributes);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                int[] attributes = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
                for (byte i = 0; i < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    attributes[i] = reader.ReadInt32();
                }

                //set
                origin_eacplayer.PSheet.Attributes.ForceAllocatedEffective(attributes);

                //relay
                if (Shortcuts.IS_SERVER) {
                    Send(-1, origin, attributes);
                }
            }

            public static void WritePacketBody(ModPacket packet, int[] attributes) {
                for (byte i = 0; i < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    packet.Write(attributes[i]);
                }
            }
        }

        public sealed class Class : Handler {
            public Class() : base(PACKET_TYPE.Class) { }

            public static void Send(int target, int origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level, byte levelup_id = 0) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.Class].GetPacket(origin);

                //specific content
                WritePacketBody(packet, primary_id, primary_level, secondary_id, secondary_level, levelup_id);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                byte[] bytes = reader.ReadBytes(5);

                //set
                origin_eacplayer.PSheet.Classes.ForceActive(bytes[0], bytes[1], bytes[2], bytes[3]);

                //relay + levelup message
                if (Shortcuts.IS_SERVER) {
                    byte levelup = bytes[4];
                    if (levelup > 0) {
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(origin_eacplayer.PSheet.Classes.GetLevelupMessage(levelup)), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    }

                    Send(-1, origin, bytes[0], bytes[1], bytes[2], bytes[3], levelup);
                }
            }

            public static void WritePacketBody(ModPacket packet, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level, byte levelup_id = 0) {
                packet.Write(primary_id);
                packet.Write(primary_level);
                packet.Write(secondary_id);
                packet.Write(secondary_level);
                packet.Write(levelup_id);
            }
        }

        /// <summary>
        /// Client tells server its local password
        /// </summary>
        public sealed class AFK : Handler {
            public AFK() : base(PACKET_TYPE.AFK) { }

            public static void Send(int target, int origin, bool status) {
                if (Shortcuts.IS_CLIENT) {
                    //get packet containing header
                    ModPacket packet = LOOKUP[(byte)PACKET_TYPE.AFK].GetPacket(origin);

                    //specific content
                    WritePacketBody(packet, status);

                    //send
                    packet.Send(target, origin);
                }
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read and set
                origin_eacplayer.PSheet.Character.SetAFK(reader.ReadBoolean());
            }

            public static void WritePacketBody(ModPacket packet, bool status) {
                packet.Write(status);
            }
        }

    }
}
