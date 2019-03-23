using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    public abstract class MItem : ModItem {
        private const int STACK_SIZE = 99999;

        private readonly string NAME, TOOLTIP, TEXTURE;
        private readonly bool CONSUMABLE, CONSUMABLE_AUTO;
        private readonly int WIDTH, HEIGHT, RARITY;

        protected Recipe recipe = null;
        private bool recipe_searched = false;

        protected MItem(string name, string tooltip, string texture, bool consumable, int width, int height, int rarity, bool consumable_auto=false) {
            NAME = name;
            TOOLTIP = tooltip;
            TEXTURE = texture;
            CONSUMABLE = consumable;
            CONSUMABLE_AUTO = consumable_auto;
            WIDTH = width;
            HEIGHT = height;
            RARITY = rarity;
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault(NAME);
            Tooltip.SetDefault(TOOLTIP);
        }
        public override void SetDefaults() {
            item.maxStack = STACK_SIZE;

            item.width = WIDTH;
            item.height = HEIGHT;

            item.rare = RARITY;

            if (CONSUMABLE) {
                item.consumable = true;
                item.useAnimation = 10;
                item.useTime = 10;
                item.useStyle = 4;
                item.autoReuse = CONSUMABLE_AUTO;
                item.UseSound = SoundID.Item4;
                item.useTurn = true;
            }
        }
        public override void OnCraft(Recipe recipe) {
            item.maxStack = STACK_SIZE;
            base.OnCraft(recipe);
        }
        public override void UpdateInventory(Player player) {
            item.maxStack = STACK_SIZE;
            base.UpdateInventory(player);
        }
        public override string Texture {
            get {
                return TEXTURE;
            }
        }

        public string GetRecipeString(bool multiline=false) {
            if (recipe != null) {
                return Utilities.Commons.GetRecipeString(recipe, multiline);
            }
            else {
                //one-time runtime search for recipe if not set manually
                if (!recipe_searched) {
                    recipe_searched = true;
                    foreach (Recipe r in Main.recipe) {
                        if (r.createItem.type == item.type) {
                            recipe = r;
                            return Utilities.Commons.GetRecipeString(recipe, multiline);
                        }
                    }
                }

                //no recipe found
                return "no recipe";
            }
        }
    }
}
