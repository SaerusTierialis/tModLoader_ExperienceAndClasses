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

                bool do_trace = Shortcuts.GetConfigServer.PacketTrace;

                if (do_trace) {
                    Logger.Trace("Handling " + ID + " originating from " + origin);
                }

                EACPlayer origin_mplayer = null;
                if ((origin >= 0) && (origin <= Main.maxPlayers)) {
                    origin_mplayer = Main.player[origin].GetModPlayer<EACPlayer>();
                }

                RecieveBody(reader, origin, origin_mplayer);

                if (do_trace) {
                    Logger.Trace("Done handling " + ID + " originating from " + origin);
                }
            }

            protected abstract void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_mplayer);
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

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_mplayer) {
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

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_mplayer) {
                //read and set
                origin_mplayer.FieldsSyncServer.password = reader.ReadString();
            }
        }

    }
}
