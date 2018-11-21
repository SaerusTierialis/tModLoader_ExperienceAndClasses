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
    abstract class Orb : ModItem {
        protected short dust_type = DustID.AmberBolt;

        public override void SetDefaults() {
            item.width = 29;
            item.height = 30;
            item.maxStack = 9999999;
            item.consumable = true;
            item.useAnimation = 10;
            item.useTime = 10;
            item.useStyle = 4;
        }

        public override void GrabRange(Player player, ref int grabRange) {
            grabRange *= 20;
            base.GrabRange(player, ref grabRange);
        }

        public override bool GrabStyle(Player player) {
            item.velocity = item.DirectionTo(player.Center) * Math.Min(Math.Max(3000f / item.Distance(player.Center), 1f), 50f);
            Dust.NewDust(item.Center, 0, 0, dust_type);
            Lighting.AddLight(item.Center, Color.White.ToVector3());
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            TooltipLine line = new TooltipLine(mod, "desc", "Current XP Value: " + GetValue());
            line.overrideColor = UI.Constants.COLOUR_XP;
            tooltips.Add(line);
        }

        abstract protected uint GetValue();

        /* Workaround for MaxStacks mod */
        public override void OnCraft(Recipe recipe) {
            item.maxStack = 9999999;
            base.OnCraft(recipe);
        }
        public override void UpdateInventory(Player player) {
            item.maxStack = 9999999;
            base.UpdateInventory(player);
        }
    }

    class Monster_Orb : Orb {
        public Monster_Orb() {
            dust_type = DustID.GreenBlood;
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ascension Orb");
            Tooltip.SetDefault("TODO");
        }

        public override void SetDefaults() {
            base.SetDefaults();
            item.rare = 9;
        }

        public override string Texture {
            get {
                return "ExperienceAndClasses/Textures/Item/Ascension_Orb";
            }
        }

        protected override uint GetValue() {
            return 0; //TODO
        }

        public override void AddRecipes() {
            //convert boss orb to ascension orb
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 3);

            //alt recipe: gold
            Commons.QuckRecipe(mod, new int[,] { { ItemID.LifeCrystal, 1 }, { ItemID.ManaCrystal, 1 }, { ItemID.GoldBar, 5 } }, this, 1);

            //alt recipe: plat
            Commons.QuckRecipe(mod, new int[,] { { ItemID.LifeCrystal, 1 }, { ItemID.ManaCrystal, 1 }, { ItemID.PlatinumBar, 5 } }, this, 1);
        }
    }

    class Boss_Orb : Orb {
        public Boss_Orb() {
            dust_type = DustID.SomethingRed;
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Boss Orb");
            Tooltip.SetDefault("TODO");
        }

        public override void SetDefaults() {
            base.SetDefaults();
            item.rare = 10;
        }

        public override string Texture {
            get {
                return "ExperienceAndClasses/Textures/Item/Boss_Orb";
            }
        }

        protected override uint GetValue() {
            return 0; //TODO
        }

    }
}
