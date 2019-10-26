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
            Broadcast,
            Password,
            ForceFull,
            ForceClass,
            SyncAttribute,
            AFK,
            InCombat,
            CharacterLevel,
            XP,
            AddStatus,
            RemoveStatus,
            SetStatuses,
            WOF,

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

                EACPlayer origin_EACPlayer = null;
                if ((origin >= 0) && (origin <= Main.maxPlayers)) {
                    origin_EACPlayer = Main.player[origin].GetModPlayer<EACPlayer>();
                }

                RecieveBody(reader, origin, origin_EACPlayer);

                if (do_trace) {
                    Logger.Trace("Done handling " + ID + " originating from " + origin);
                }
            }

            protected abstract void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_EACPlayer);
        }

        /// <summary>
        /// Client request broadcast from server
        /// </summary>
        public sealed class Broadcast : Handler {
            public enum BROADCAST_TYPE : byte {
                MESSAGE,
                TRACE,
                ERROR
            }

            public Broadcast() : base(PACKET_TYPE.Broadcast) { }

            public static void Send(int target, int origin, string message, BROADCAST_TYPE type) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.Broadcast].GetPacket(origin);

                //type
                packet.Write((byte)type);

                //message
                packet.Write(message);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_EACPlayer) {
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
                        colour = UI.Constants.COLOUR_MESSAGE_ANNOUNCE;
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
        public sealed class Password : Handler {
            public Password() : base(PACKET_TYPE.Password) { }

            public static void Send(int target, int origin, string password) {
                if (Shortcuts.IS_CLIENT) {
                    //get packet containing header
                    ModPacket packet = LOOKUP[(byte)PACKET_TYPE.Password].GetPacket(origin);

                    //specific content
                    packet.Write(password);

                    //send
                    packet.Send(target, origin);
                }
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_EACPlayer) {
                //read and set
                origin_EACPlayer.Fields.password = reader.ReadString();
            }
        }

        public sealed class ForceFull : Handler {
            public ForceFull() : base(PACKET_TYPE.ForceFull) { }

            public static void Send(EACPlayer eacplayer) {
                //get packet containing header
                int origin = eacplayer.player.whoAmI;
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.ForceFull].GetPacket(origin);

                //specific content
                ForceClass.WritePacketBody(packet, eacplayer.CSheet.Class_Primary.Class.ID_num, eacplayer.CSheet.Class_Primary.Level_Active, eacplayer.CSheet.Class_Secondary.Class.ID_num, eacplayer.CSheet.Class_Secondary.Level_Active);
                SyncAttribute.WritePacketBody(packet, eacplayer.CSheet.Attributes.To_Sync);
                AFK.WritePacketBody(packet, eacplayer.CSheet.Character.AFK);
                InCombat.WritePacketBody(packet, eacplayer.CSheet.Character.In_Combat);
                CharacterLevel.WritePacketBody(packet, eacplayer.CSheet.Character.Level);

                //send
                packet.Send(-1, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                LOOKUP[(byte)PACKET_TYPE.ForceClass].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.SyncAttribute].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.AFK].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.InCombat].Recieve(reader, origin);
                LOOKUP[(byte)PACKET_TYPE.CharacterLevel].Recieve(reader, origin);

                if (!origin_eacplayer.Fields.initialized) {
                    origin_eacplayer.Fields.initialized = true;
                }

                if (Shortcuts.IS_SERVER) {
                    Send(origin_eacplayer);
                }
            }
        }

        public sealed class ForceClass : Handler {
            public ForceClass() : base(PACKET_TYPE.ForceClass) { }

            public static void Send(int target, int origin, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.ForceClass].GetPacket(origin);

                //specific content
                WritePacketBody(packet, primary_id, primary_level, secondary_id, secondary_level);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                byte[] bytes = reader.ReadBytes(4);

                //set
                origin_eacplayer.CSheet.Classes.ForceClass(bytes[0], bytes[1], bytes[2], bytes[3]);

                //relay
                if (Shortcuts.IS_SERVER) {
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

        public sealed class SyncAttribute : Handler {
            public SyncAttribute() : base(PACKET_TYPE.SyncAttribute) { }

            public static void Send(int target, int origin, int[] attributes) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.SyncAttribute].GetPacket(origin);

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
                origin_eacplayer.CSheet.Attributes.Force(attributes);

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

        public sealed class AFK : Handler {
            public AFK() : base(PACKET_TYPE.AFK) { }

            public static void Send(int target, int origin, bool afk_status) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.AFK].GetPacket(origin);

                //specific content
                WritePacketBody(packet, afk_status);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                bool afk_status = reader.ReadBoolean();

                //set
                origin_eacplayer.CSheet.Character.SetAFK(afk_status);

                //relay
                if (Shortcuts.IS_SERVER) {
                    Send(-1, origin, afk_status);
                }
            }

            public static void WritePacketBody(ModPacket packet, bool afk_status) {
                packet.Write(afk_status);
            }
        }

        public sealed class InCombat : Handler {
            public InCombat() : base(PACKET_TYPE.InCombat) { }

            public static void Send(int target, int origin, bool combat_status) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.InCombat].GetPacket(origin);

                //specific content
                WritePacketBody(packet, combat_status);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                bool combat_status = reader.ReadBoolean();

                //set
                origin_eacplayer.CSheet.Character.SetInCombat(combat_status);

                //relay
                if (Shortcuts.IS_SERVER) {
                    Send(-1, origin, combat_status);
                }
            }

            public static void WritePacketBody(ModPacket packet, bool afk_status) {
                packet.Write(afk_status);
            }
        }

        public sealed class CharacterLevel : Handler {
            public CharacterLevel() : base(PACKET_TYPE.CharacterLevel) { }

            public static void Send(int target, int origin, uint level) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.CharacterLevel].GetPacket(origin);

                //specific content
                WritePacketBody(packet, level);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                uint level = reader.ReadUInt32();

                //set
                origin_eacplayer.CSheet.Character.ForceLevel(level);

                /*
                //relay
                if (Shortcuts.IS_SERVER) {
                    Send(-1, origin, player_progression);
                }
                */
            }

            public static void WritePacketBody(ModPacket packet, uint level) {
                packet.Write(level);
            }
        }

        public sealed class XP : Handler {
            public XP() : base(PACKET_TYPE.XP) { }

            public static void Send(int target, int origin, uint xp) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.XP].GetPacket(origin);

                //specific content
                packet.Write(xp);

                //send
                packet.Send(target, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //read
                uint xp = reader.ReadUInt32();

                //set
                Systems.XP.Adjusting.LocalAddXP(xp);
            }
        }

        public sealed class AddStatus : Handler {
            public AddStatus() : base(PACKET_TYPE.AddStatus) { }

            public static void Send(Systems.Status status, int origin) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.AddStatus].GetPacket(origin);

                //write status
                StatusWrite(packet, status);

                //send
                packet.Send(-1, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                StatusRead(reader, false);
            }

            /// <summary>
            /// set_statuses prevents target from being written
            /// </summary>
            /// <param name="packet"></param>
            /// <param name="status"></param>
            /// <param name="set_statuses"></param>
            public static void StatusWrite(ModPacket packet, Systems.Status status, bool set_statuses = false) {
                //1:  ID (ushort = uint16)
                packet.Write(status.ID_num);

                //2:  Instance ID (byte)
                packet.Write(status.Instance_ID);

                //3:  Owner (ushort)
                packet.Write(status.Owner.Index);

                if (!set_statuses) {
                    //4:  Target (ushort)
                    packet.Write(status.Target.Index);
                }

                //5?:  time remaining if duration-type (float seconds)
                if (status.Specific_Duration_Type == Systems.Status.DURATION_TYPES.TIMED) {
                    packet.Write((float)status.Time_End.Subtract(Shortcuts.Now).TotalSeconds);
                }

                //6?:  time until next effect if timed (float seconds)
                if ((status.Specific_Effect_Type == Systems.Status.EFFECT_TYPES.TIMED) || (status.Specific_Effect_Type == Systems.Status.EFFECT_TYPES.CONSTANT_AND_TIMED)) {
                    packet.Write(Systems.Status.Times_Next_Timed_Effect[status.ID].Subtract(Shortcuts.Now).TotalSeconds);
                }

                //7+?: extra data values in enum order  (float[])
                foreach (Systems.Status.AUTOSYNC_DATA_TYPES type in status.Specific_Autosync_Data_Types) {
                    packet.Write(status.GetData(type));
                }

                //write any extra stuff
                status.PacketAddWrite(packet);
            }

            /// <summary>
            /// set_statuses prevents OnStart and creation sync, also prevents writing target
            /// must provide target if set_statuses
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="set_statuses"></param>
            public static void StatusRead(BinaryReader reader, bool set_statuses = false, Utilities.Containers.Thing target = null) {
                //1:  ID (ushort = uint16)
                ushort id_num = reader.ReadUInt16();
                Systems.Status reference = Systems.Status.LOOKUP[id_num];
                Systems.Status.IDs id = reference.ID;

                //2:  Instance ID (byte)
                byte instance_id = reader.ReadByte();

                //3:  Owner (ushort)
                ushort owner_index = reader.ReadUInt16();
                Utilities.Containers.Thing owner = Utilities.Containers.Thing.Things[owner_index];

                //don't write target in set_statuses (written once at start of large packet)
                if (!set_statuses) {
                    //4:  Target (ushort)
                    ushort target_index = reader.ReadUInt16();
                    target = Utilities.Containers.Thing.Things[target_index];
                }

                //5?:  time remaining if duration-type (float seconds)
                float duration_seconds = 0f;
                if (reference.Specific_Duration_Type == Systems.Status.DURATION_TYPES.TIMED) {
                    duration_seconds = reader.ReadSingle();
                }

                //6?:  time until next effect if timed (float seconds)
                float effect_seconds = 0f;
                if ((reference.Specific_Effect_Type == Systems.Status.EFFECT_TYPES.TIMED) || (reference.Specific_Effect_Type == Systems.Status.EFFECT_TYPES.CONSTANT_AND_TIMED)) {
                    effect_seconds = reader.ReadSingle();
                }

                //7+?: extra data values in enum order  (float[])
                Dictionary<Systems.Status.AUTOSYNC_DATA_TYPES, float> data = new Dictionary<Systems.Status.AUTOSYNC_DATA_TYPES, float>();
                foreach (Systems.Status.AUTOSYNC_DATA_TYPES type in reference.Specific_Autosync_Data_Types) {
                    data.Add(type, reader.ReadSingle());
                }

                //add (PacketAddRead is in there, will relay to clients if needed)
                //does not sync if set_statuses
                Systems.Status.Add(id, target, owner, data, duration_seconds, effect_seconds, instance_id, reader, set_statuses);
            }
        }

        public sealed class RemoveStatus : Handler {
            public RemoveStatus() : base(PACKET_TYPE.RemoveStatus) { }

            public static void Send(Systems.Status status, int origin) {
                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.RemoveStatus].GetPacket(origin);

                //1:  ID (ushort = uint16)
                packet.Write(status.ID_num);

                //2:  Instance ID (byte)
                packet.Write(status.Instance_ID);

                //3:  Target (ushort)
                packet.Write(status.Target.Index);

                //write any extra stuff
                status.PacketRemoveWrite(packet);

                //send
                packet.Send(-1, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //1:  ID (ushort = uint16)
                ushort id_num = reader.ReadUInt16();
                Systems.Status reference = Systems.Status.LOOKUP[id_num];
                Systems.Status.IDs id = reference.ID;

                //2:  Instance ID (byte)
                byte instance_id = reader.ReadByte();

                //3:  Target (ushort)
                ushort target_index = reader.ReadUInt16();
                Utilities.Containers.Thing target = Utilities.Containers.Thing.Things[target_index];

                //get status
                Systems.Status status = target.Statuses.Get(id, instance_id);

                //read any extra stuff
                status.PacketRemoveRead(reader);

                //remove (and relay to clients)
                status.RemoveEverywhere();
            }
        }

        public sealed class SetStatuses : Handler {
            public SetStatuses() : base(PACKET_TYPE.SetStatuses) { }

            public static void Send(Utilities.Containers.Thing target) {
                //origin
                int origin;
                if (target.Is_Player) {
                    origin = target.whoAmI;
                }
                else {
                    origin = -1;
                }

                //get packet containing header
                ModPacket packet = LOOKUP[(byte)PACKET_TYPE.SetStatuses].GetPacket(origin);

                //1: write target (ushort)
                packet.Write(target.Index);

                //get all sync statuses
                List<Systems.Status> statuses = target.GetAllSyncStatuses();

                //2: write number of sync statuses (ushort)
                packet.Write((ushort)statuses.Count);

                //3+: write each status
                foreach (Systems.Status status in statuses) {
                    AddStatus.StatusWrite(packet, status, true);
                }

                //send
                packet.Send(-1, origin);
            }

            protected override void RecieveBody(BinaryReader reader, int origin, EACPlayer origin_eacplayer) {
                //1: read target
                ushort target_index = reader.ReadUInt16();
                Utilities.Containers.Thing target = Utilities.Containers.Thing.Things[target_index];

                //2: read number of statuses
                ushort number_statuses = reader.ReadUInt16();

                //clear all sync statuses
                target.RemoveAllSyncStatuses();

                //3+: reach each status (readds sync statuses)
                for (int i = 0; i < number_statuses; i++) {
                    AddStatus.StatusRead(reader, true, target);
                }

                //relay to clients if this is server
                if (Shortcuts.IS_SERVER) {
                    Send(target);
                }
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
                Shortcuts.LOCAL_PLAYER.CSheet.Character.DefeatWOF();
            }
        }

    }
}
