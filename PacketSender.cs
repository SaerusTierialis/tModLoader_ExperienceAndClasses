using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    class PacketSender {
        public static void SendForceClass(byte who, byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.MESSAGE_TYPE.FORCE_CLASS);
            packet.Write(who);
            packet.Write(primary_id);
            packet.Write(primary_level);
            packet.Write(secondary_id);
            packet.Write(secondary_level);
            packet.Send(-1, who);
        }

        public static void SendForceAttribute(byte who, short[] attributes) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.MESSAGE_TYPE.FORCE_ATTRIBUTE);
            packet.Write(who);
            for (byte i = 0; i < (byte)Systems.Attribute.ATTRIBUTE_IDS.NUMBER_OF_IDs; i++) {
                packet.Write(attributes[i]);
            }
            packet.Send(-1, who);
        }

        public static void SendBroadcastTrace(byte who, string message) {
            ModPacket packet = ExperienceAndClasses.MOD.GetPacket();
            packet.Write((byte)ExperienceAndClasses.MESSAGE_TYPE.BROADCAST_TRACE);
            packet.Write(who);
            packet.Write(message);
            packet.Send(-1);
        }

    }
}
