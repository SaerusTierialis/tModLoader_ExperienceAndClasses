using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public class LifeSkillTile : GlobalTile {

        public static List<Tuple<int, int>> Tiles_Placed = new List<Tuple<int, int>>();

        private static bool TileIsPlaced(int x, int y) {
            return TileIsPlaced(new Tuple<int, int>(x, y));
        }
        private static bool TileIsPlaced(Tuple<int, int> position) {
            return Tiles_Placed.Contains(position);
        }
        public static void PlaceTile(int x, int y) {
            Tuple<int, int> position = new Tuple<int, int>(x, y);
            if (!TileIsPlaced(position)) {
                Tiles_Placed.Add(position);
            }
        }
        private static void UnplaceTile(int x, int y) {
            Tuple<int, int> position = new Tuple<int, int>(x, y);
            Tiles_Placed.Remove(position);
        }


        public override void PlaceInWorld(int i, int j, Item item) {
            base.PlaceInWorld(i, j, item);
            if (Shortcuts.IS_EFFECTIVELY_SERVER) PlaceTile(i, j);
        }

        public override bool Drop(int i, int j, int type) {
            bool drop = base.Drop(i, j, type);

            if (Shortcuts.IS_EFFECTIVELY_SERVER) {
                //life skill xp
                if (drop && !TileIsPlaced(i, j)) {
                    if (Main.tileAxe[type]) {
                        XP.Adjustments.AddTreeXP(i, j);
                    }
                    else if (Main.tileSpelunker[type]) {
                        XP.Adjustments.AddOreXP(i, j, type);
                    }
                }

                //keep list as short as possible
                UnplaceTile(i, j);
            }

            return drop;
        }

    }

    public class Fish : GlobalItem {

        public override bool InstancePerEntity => true;
        public override bool CloneNewInstances => true;

        public Item item;
        public override void SetDefaults(Item item) {
            base.SetDefaults(item);
            this.item = item;
        }

        /// <summary>
        /// Called locally only
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stack"></param>
        public override void CaughtFishStack(int type, ref int stack) {
            base.CaughtFishStack(type, ref stack);
            Systems.XP.Adjustments.LocalAddFishXP(item);
        }
    }
}
