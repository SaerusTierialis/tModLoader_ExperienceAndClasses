using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExperienceAndClasses
{
	public class ExperienceAndClasses : Mod
	{
        public ExperienceAndClasses(){}

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Load/Unload ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load() {
            Shortcuts.DoModLoad(this);
        }

        public override void Unload() {
            Shortcuts.DoModUnload();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void UpdateUI(GameTime gameTime) {
            base.UpdateUI(gameTime);

            Shortcuts.UpdateUIs(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            Shortcuts.SetUILayers(layers);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Packets ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            //first 2 bytes are always type and origin
            byte packet_type = reader.ReadByte();
            int origin = reader.ReadInt32();

            if (packet_type >= 0 && packet_type < (byte)Utilities.PacketHandler.PACKET_TYPE.NUMBER_OF_TYPES) {
                Utilities.PacketHandler.LOOKUP[packet_type].Recieve(reader, origin);
            }
            else {
                Utilities.Logger.Error("Cannot handle packet type id " + packet_type + " originating from " + origin);
            }
        }

    }
}