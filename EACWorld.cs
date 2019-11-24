using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.World.Generation;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    class EACWorld : ModWorld {
        public static string password = "";

        public override void PostUpdate() {
            Systems.NPCRewards.ServerProcessXPBuffer();

            if (Shortcuts.IS_SERVER) {
                //update time if server
                Shortcuts.UpdateTime();
            }
        }

        public override TagCompound Save() {
            List<int> tiles_placed = new List<int>();
            foreach (Tuple<int,int> tile in Systems.LifeSkillTile.Tiles_Placed) {
                tiles_placed.Add(tile.Item1);
                tiles_placed.Add(tile.Item2);
            }

            return new TagCompound {
                ["eac_password"] = Systems.Password.world_password,
                ["eac_tiles_placed"] = tiles_placed
            };
        }

        public override void Load(TagCompound tag) {
            Systems.Password.world_password = Utilities.Commons.TagTryGet<string>(tag, "eac_password", "");
            List<int> tiles_placed = Utilities.Commons.TagTryGet<List<int>>(tag, "eac_tiles_placed", new List<int>());

            int count = 0;
            for (int i = 0; i < (tiles_placed.Count / 2.0); i++) {
                Systems.LifeSkillTile.PlaceTile(tiles_placed[count++], tiles_placed[count++]);
            }
        }

    }
}
