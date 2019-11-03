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

        private readonly string TEXTURE;
        private readonly bool CONSUMABLE, CONSUMABLE_AUTO;
        private readonly int WIDTH, HEIGHT, RARITY;

        private string recipe_string = "";
        private string recipe_string_multiline = "";

        protected MItem(string texture, bool consumable, int width, int height, int rarity, bool consumable_auto=false) {
            TEXTURE = texture;
            CONSUMABLE = consumable;
            CONSUMABLE_AUTO = consumable_auto;
            WIDTH = width;
            HEIGHT = height;
            RARITY = rarity;
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
            string value = multiline ? recipe_string_multiline : recipe_string;

            if (value.Length == 0) {
                //default to no recipe
                value = "no recipe";

                //look for recipe, set value if found
                foreach (Recipe r in Main.recipe) {
                    if (r.createItem.type == item.type) {
                        value = CreateRecipeString(r, multiline);
                        break;
                    }
                }

                //set for future calls
                if (multiline)
                    recipe_string_multiline = value;
                else
                    recipe_string = value;
            }

            return value;
        }

        /// <summary>
        /// Returns a string representation of an item's recipe for UI or any other purpose
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="multiline"></param>
        /// <returns></returns>
        private static string CreateRecipeString(Recipe recipe, bool multiline = false) {
            string str = "";

            RecipeGroup group;
            bool first = true;
            string name;

            for (uint i = 0; i < recipe.requiredItem.Length; i++) {
                if (recipe.requiredItem[i].type > 0) {
                    if (!first) {
                        if (multiline) {
                            str += "\n";
                        }
                        else {
                            str += " + ";
                        }
                    }
                    else {
                        first = false;
                    }

                    //get group name if applicable
                    name = recipe.requiredItem[i].Name;
                    foreach (int group_id in recipe.acceptedGroups) {
                        if (RecipeGroup.recipeGroups.TryGetValue(group_id, out group)) {
                            if (group.ContainsItem(recipe.requiredItem[i].type)) {
                                name = group.GetText.Invoke();
                            }
                        }
                    }

                    str += "x" + recipe.requiredItem[i].stack + " " + name;
                }
            }

            return str;
        }

        /// <summary>
        /// Creates and finalizes a recipe. Ingredients must be formatted new int[,] { {id1, num1}, {id2, num2}, ... }. Can build on an existing recipe.
        /// NOTE: Duplicate items in a recipe cause a bug where only one stack is checked/needed. The method below can be used to solve this.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="ingredients"></param>
        /// <param name="result"></param>
        /// <param name="numResult"></param>
        /// <param name="where"></param>
        protected static Recipe QuckRecipe(Mod mod, int[,] ingredients, ModItem result, int numResult = 1, ModRecipe buildOn = null, ushort where = TileID.WorkBenches) {
            //recipe
            ModRecipe recipe;
            if (buildOn == null) {
                recipe = new ModRecipe(mod);
            }
            else {
                recipe = buildOn;
            }

            //where to craft (use MaxValue to skip)
            if (where != ushort.MaxValue) {
                recipe.AddTile(where);
            }

            //add ingredients
            if (ingredients.GetLength(1) == 2) {
                for (int i = 0; i < ingredients.GetLength(0); i++) {
                    recipe.AddIngredient(ingredients[i, 0], ingredients[i, 1]);
                }
            }

            //result
            recipe.SetResult(result, numResult);

            //complete
            recipe.AddRecipe();
            return recipe;
        }

    }
}
