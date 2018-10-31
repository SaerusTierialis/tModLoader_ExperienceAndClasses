using Terraria.ModLoader;
using Terraria.UI;
using Terraria;
using System.Collections.Generic;
using System;
using System.IO;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System.Reflection;

//needed for compiling outside of Terraria
public class Application
{
    [STAThread]
    static void Main(string[] args) { }
}

namespace ExperienceAndClasses
{
    class ExperienceAndClasses : Mod {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly bool IS_SERVER = (Main.netMode == 2);
        public static readonly bool IS_CLIENT = (Main.netMode == 1);
        public static readonly bool IS_SINGLEPLAYER = (Main.netMode == 0);

        public enum MessageType : byte {
            SYNC_TEST,
        };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static MPlayer LOCAL_MPLAYER;
        public static Mod mod;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public ExperienceAndClasses() {
            Properties = new ModProperties() {
                Autoload = true,
            };
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Load/Unload ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load() {
            mod = this;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Packets ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            MessageType message_type = (MessageType)reader.ReadByte();
            byte player_ind;
            Player player;
            MPlayer mplayer;
            int int1;
            switch (message_type) {
                case MessageType.SYNC_TEST:
                    //read
                    player_ind = reader.ReadByte();
                    int1 = reader.ReadInt32();

                    //apply
                    player = Main.player[player_ind]; //sender
                    mplayer = player.GetModPlayer<MPlayer>(this);
                    mplayer.sync_test = int1;

                    //relay
                    if (IS_SERVER) {
                        ModPacket packet = mod.GetPacket();
                        packet.Write((byte)ExperienceAndClasses.MessageType.SYNC_TEST);
                        packet.Write((byte)player_ind);
                        packet.Write(int1);
                        packet.Send(-1, player_ind);
                    }
                    break;

                default:
                    //unknown type
                    break;
            }
        }

    }
}
