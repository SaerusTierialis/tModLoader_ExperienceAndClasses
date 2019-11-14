using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    public abstract class Orb : MItem {
        private readonly short DUST;

        private const int WIDTH = 20;
        private const int HEIGTH = 20;
        private const bool CONSUMABLE = false;
        private const bool CONSUMABLE_AUTO = false;

        protected Orb(string texture, int rarity, short dust) : base(texture, CONSUMABLE, WIDTH, HEIGTH, rarity, CONSUMABLE_AUTO) {
            DUST = dust;
        }

        public override void GrabRange(Player player, ref int grabRange) {
            grabRange *= 20;
            base.GrabRange(player, ref grabRange);
        }

        public override bool GrabStyle(Player player) {
            item.velocity = item.DirectionTo(player.Center) * (float)Utilities.Commons.Clamp(1000f / item.Distance(player.Center), 1f, 20f);
            Dust.NewDust(item.Center, 0, 0, DUST);
            Lighting.AddLight(item.Center, Color.White.ToVector3());
            return true;
        }
    }

    public class Orb_Monster : Orb {
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Monster";
        private const int RARITY = 9;
        private const short DUST = DustID.ApprenticeStorm;

        public const int CONVERT_BOSS_ORB = 5;

        public Orb_Monster() : base(TEXTURE, RARITY, DUST) {}

        public override void AddRecipes() {
            //convert boss orb to ascension orb
            QuckRecipe(mod, new int[,] { { ModContent.ItemType<Orb_Boss>(), 1 } }, this, CONVERT_BOSS_ORB);
        }
    }

    class Orb_Boss : Orb {
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Boss";
        private const int RARITY = 11;
        private const short DUST = DustID.PurpleCrystalShard;

        public Orb_Boss() : base(TEXTURE, RARITY, DUST) {}

    }
}
