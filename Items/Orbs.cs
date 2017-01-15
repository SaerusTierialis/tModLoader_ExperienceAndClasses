using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items
{
    /* Experience Orb x1 */
    public class Experience : ExperienceOrb
    {
        public Experience() : base()
        {
            orbValue = 1;
        }
    }

    /* Experience Orb x100 */
    public class Experience100 : ExperienceOrb
    {
        public Experience100() : base()
        {
            orbValue = 100;
        }
        public override void AddRecipes()
        {
            //include basic recipes
            base.AddRecipes();

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier I Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 2, new Recipes.TierRecipe(mod, 1, true, false, -1, -1));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 1, new Recipes.TierRecipe(mod, 1, true, false, -1, -1));

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 2 Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 6, new Recipes.TierRecipe(mod, 2, false, false, -1, -1));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 3, new Recipes.TierRecipe(mod, 2, false, false, -1, -1));

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 3 Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 5, new Recipes.TierRecipe(mod, 3, false, false, -1, 49));
        }
    }

    /* Experience Orb x1000 */
    public class Experience1000 : ExperienceOrb
    {
        public Experience1000() : base()
        {
            orbValue = 1000;
        }
        public override void AddRecipes()
        {
            //include basic recipes
            base.AddRecipes();

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 3 Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 1, new Recipes.TierRecipe(mod, 3, false, false, -1, 49));

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 3 Level 50+ Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 2, new Recipes.TierRecipe(mod, 3, false, false, 50, -1));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 1, new Recipes.TierRecipe(mod, 3, false, false, 50, -1));
        }
    }

    /* Experience Orb x10,000 */
    public class Experience10000 : ExperienceOrb
    {
        public Experience10000() : base()
        {
            orbValue = 10000;
        }
    }

    /* Experience Orb x100,000 */
    public class Experience100000 : ExperienceOrb
    {
        public Experience100000() : base()
        {
            orbValue = 100000;
        }
    }

    /* Experience Orb x1,000,000 */
    public class Experience1000000 : ExperienceOrb
    {
        public Experience1000000() : base()
        {
            orbValue = 1000000;
        }
    }

    /* Boss Orb */
    public class Boss_Orb : ModItem
    {
        public override void SetDefaults()
        {
            item.name = "Boss Orb";
            item.width = 29;
            item.height = 30;
            item.maxStack = 9999999;
            item.value = 50000;
            item.rare = 10;
            item.toolTip = "Can be converted to Ascension Orbs, broken down into"+
                         "\nExperience Orbs, or sold."+
                       "\n\nNote: rate of experience exchange improves with class tier";
        }
    }

    /* Ascension Orb */
    public class Monster_Orb : ModItem
    {
        public override void SetDefaults()
        {
            item.name = "Ascension Orb";
            item.width = 29;
            item.height = 30;
            item.maxStack = 9999999;
            item.value = 25000;
            item.rare = 9;
            item.toolTip = "Used in Tier II and III class advancements. Can also" +
                         "\nbe broken down into Experience Orbs or sold."+
                       "\n\nNote: rate of experience exchange improves with class tier";
        }

        public override void AddRecipes()
        {
            //convert boss orb to ascension orb
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 2);

            //alt recipe: gold
            Commons.QuckRecipe(mod, new int[,] { { ItemID.LifeCrystal, 1 }, { ItemID.ManaCrystal, 1 }, { ItemID.GoldBar, 20 } }, this, 1);

            //alt recipe: plat
            Commons.QuckRecipe(mod, new int[,] { { ItemID.LifeCrystal, 1 }, { ItemID.ManaCrystal, 1 }, { ItemID.PlatinumBar, 20 } }, this, 1);
        }
    }

    /* TEMPLATES */
    public abstract class ExperienceOrb : ModItem
    {
        public int orbValue = 1;

        public override void SetDefaults()
        {
            //name
            item.name = "Experience Orb";
            if (orbValue > 1) item.name += " " + orbValue;

            //info
            item.toolTip = "Worth " + (ExperienceAndClasses.EXP_ITEM_VALUE * orbValue) + " experience.";
            item.width = 29;
            item.height = 30;
            item.maxStack = 9999999;
            item.value = 0;
            item.rare = 7;
            item.consumable = true;
            item.useAnimation = 10;
            item.useTime = 10;
            item.useStyle = 4;
        }

        public override void AddRecipes()
        {
            Recipes.Helpers.AddRecipes_ExpOrbConversion(this, orbValue);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, ExperienceAndClasses.EXP_ITEM_VALUE * orbValue), TileID.Campfire);
        }
        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(ExperienceAndClasses.EXP_ITEM_VALUE * orbValue);
            return true;
        }

        /* Bypass MaxStacks */
        public override void OnCraft(Recipe recipe)
        {
            item.maxStack = 9999999;
            base.OnCraft(recipe);
        }
        public override void UpdateInventory(Player player)
        {
            item.maxStack = 9999999;
            base.UpdateInventory(player);
        }
    }
}
