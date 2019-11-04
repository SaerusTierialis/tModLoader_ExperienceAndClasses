using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ RecipeGroup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void AddRecipeGroups() {
            base.AddRecipeGroups();

            // Creates a new recipe group
            RecipeGroup mechanical_soul = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " Mechanical Boss Soul", new int[]
            {
                ItemID.SoulofFright,
                ItemID.SoulofMight,
                ItemID.SoulofSight
            });
            // Registers the new recipe group with the specified name
            RecipeGroup.RegisterGroup(Shortcuts.RECIPE_GROUP_MECHANICAL_SOUL, mechanical_soul);
        }

    }
}