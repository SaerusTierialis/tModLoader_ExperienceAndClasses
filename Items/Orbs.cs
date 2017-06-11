using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items
{
    /* Template & Experience Orb x1 */
    //note that abstract ModItem cause issues
    public class Experience : ModItem
    {
        static int[] values = { 1, 100, 1000, 10000, 100000, 1000000 };

        public int orbValue = 1;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Worth " + (ExperienceAndClasses.EXP_ITEM_VALUE * orbValue) + " experience.");
            if (orbValue > 1)
            {
                DisplayName.SetDefault("Experience Orb " + orbValue);
            }
            else
            {
                DisplayName.SetDefault("Experience Orb");
            }
        }

        public override void SetDefaults()
        {
            //name
            //item.name = "Experience Orb";
            //if (orbValue > 1) item.name += " " + orbValue;

            //info
            //item.toolTip = "Worth " + (ExperienceAndClasses.EXP_ITEM_VALUE * orbValue) + " experience.";
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
            //orb-to-orb converstion
            if (orbValue == 1)
            {
                //convert down to 1's
                foreach (int i in values)
                {
                    if (i>1)
                    {
                        Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Experience"+i), 1 } }, this, i);
                    }
                }
            }
            else
            {
                //convert up from 1's
                Commons.QuckRecipe(mod, new int[,] { {mod.ItemType<Experience>(), orbValue } }, this, 1);
            }
            
            //exp-to-orb conversion
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

    /* Experience Orb x100 */
    public class Experience100 : Experience
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
    public class Experience1000 : Experience
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
    public class Experience10000 : Experience
    {
        public Experience10000() : base()
        {
            orbValue = 10000;
        }
    }

    /* Experience Orb x100,000 */
    public class Experience100000 : Experience
    {
        public Experience100000() : base()
        {
            orbValue = 100000;
        }
    }

    /* Experience Orb x1,000,000 */
    public class Experience1000000 : Experience
    {
        public Experience1000000() : base()
        {
            orbValue = 1000000;
        }
    }

    /* Boss Orb */
    public class Boss_Orb : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Boss Orb");
            Tooltip.SetDefault("Can be converted to Ascension Orbs, broken down into" +
                         "\nExperience Orbs, or sold." +
                       "\n\nNote: rate of experience exchange improves with class tier");
        }

        public override void SetDefaults()
        {
            //item.name = "Boss Orb";
            item.width = 29;
            item.height = 30;
            item.maxStack = 9999999;
            item.value = 50000;
            item.rare = 10;
            //item.toolTip = "Can be converted to Ascension Orbs, broken down into"+
            //             "\nExperience Orbs, or sold."+
            //           "\n\nNote: rate of experience exchange improves with class tier";
        }
    }

    /* Ascension Orb */
    public class Monster_Orb : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ascension Orb");
            Tooltip.SetDefault("Used in Tier II and III class advancements. Can also" +
                         "\nbe broken down into Experience Orbs or sold." +
                       "\n\nNote: rate of experience exchange improves with class tier");
        }
        public override void SetDefaults()
        {
            //item.name = "Ascension Orb";
            item.width = 29;
            item.height = 30;
            item.maxStack = 9999999;
            item.value = 25000;
            item.rare = 9;
            //item.toolTip = "Used in Tier II and III class advancements. Can also" +
            //             "\nbe broken down into Experience Orbs or sold."+
            //           "\n\nNote: rate of experience exchange improves with class tier";
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
}
