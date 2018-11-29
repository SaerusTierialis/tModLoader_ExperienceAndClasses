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
        private const bool CONSUMABLE = true;

        protected Orb(string name, string tooltip, string texture, int rarity, short dust) : base(name, tooltip, texture, CONSUMABLE, WIDTH, HEIGTH, rarity) {
            DUST = dust;
        }

        public override void GrabRange(Player player, ref int grabRange) {
            grabRange *= 20;
            base.GrabRange(player, ref grabRange);
        }

        public override bool GrabStyle(Player player) {
            item.velocity = item.DirectionTo(player.Center) * Math.Min(Math.Max(3000f / item.Distance(player.Center), 1f), 50f);
            Dust.NewDust(item.Center, 0, 0, DUST);
            Lighting.AddLight(item.Center, Color.White.ToVector3());
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            TooltipLine line = new TooltipLine(mod, "desc", "Current XP Value: " + GetValue());
            line.overrideColor = UI.Constants.COLOUR_XP_BRIGHT;
            tooltips.Add(line);
        }

        public override void OnConsumeItem(Player player) {
            //TODO - grant xp, prevent use if can't gain xp
            base.OnConsumeItem(player);
        }

        abstract protected uint GetValue();
    }

    public class Monster_Orb : Orb {
        public const string NAME = "Ascension Orb";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Monster";
        private const int RARITY = 9;
        private const short DUST = DustID.ApprenticeStorm;

        public Monster_Orb() : base(NAME, TOOLTIP, TEXTURE, RARITY, DUST) {}

        protected override uint GetValue() {
            return 0; //TODO
        }

        public override void AddRecipes() {
            //convert boss orb to ascension orb
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType<Boss_Orb>(), 1 } }, this, 5);

            //alt recipe: gold
            Commons.QuckRecipe(mod, new int[,] { { ItemID.LifeCrystal, 1 }, { ItemID.ManaCrystal, 1 }, { ItemID.GoldBar, 5 } }, this, 1);

            //alt recipe: plat
            Commons.QuckRecipe(mod, new int[,] { { ItemID.LifeCrystal, 1 }, { ItemID.ManaCrystal, 1 }, { ItemID.PlatinumBar, 5 } }, this, 1);
        }
    }

    class Boss_Orb : Orb {
        public const string NAME = "Transcendence Orb";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Boss";
        private const int RARITY = 11;
        private const short DUST = DustID.PurpleCrystalShard;

        public Boss_Orb() : base(NAME, TOOLTIP, TEXTURE, RARITY, DUST) { }

        protected override uint GetValue() {
            return 0; //TODO
        }

    }
}
