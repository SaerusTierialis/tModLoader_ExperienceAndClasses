using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items
{
    /* Experience Orb x1 */
	public class Experience : ModItem
	{
        static int orb_value = 1;
        static double exp_value = ExperienceAndClasses.EXP_ITEM_VALUE * orb_value;

        public override void SetDefaults()
		{
			item.name = "Experience Orb";
            Methods.SetDefaults_ExpOrbs(item, exp_value);
            item.toolTip += "\nEnter \"/expuse #\" to consume several at once.";
        }

		public override void AddRecipes()
		{
            Recipes.Methods.AddRecipes_ExpOrbConversion(mod, this, orb_value);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, exp_value), TileID.Campfire);
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(ExperienceAndClasses.EXP_ITEM_VALUE);
            return true;
        }
    }

    /* Experience Orb x100 */
    public class Experience100 : ModItem
    {
        static int orb_value = 100;
        static double exp_value = ExperienceAndClasses.EXP_ITEM_VALUE * orb_value;

        public override void SetDefaults()
        {
            item.name = "Experience Orb 100";
            Methods.SetDefaults_ExpOrbs(item, exp_value);
        }

        public override void AddRecipes()
        {
            Recipes.Methods.AddRecipes_ExpOrbConversion(mod, this, orb_value);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, exp_value), TileID.Campfire);

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier I Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 2, new Recipes.TierRecipe(mod, 1, true, false, -1, -1));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 1, new Recipes.TierRecipe(mod, 1, true, false, -1, -1));

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 2 Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 6, new Recipes.TierRecipe(mod, 2, false, false, -1, -1));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 3, new Recipes.TierRecipe(mod, 2, false, false, -1, -1));

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 3 Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 5, new Recipes.TierRecipe(mod, 3, false, false, -1, 49));
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(exp_value);
            return true;
        }
    }

    /* Experience Orb x1000 */
    public class Experience1000 : ModItem
    {
        static int orb_value = 1000;
        static double exp_value = ExperienceAndClasses.EXP_ITEM_VALUE * orb_value;

        public override void SetDefaults()
        {
            item.name = "Experience Orb 1000";
            Methods.SetDefaults_ExpOrbs(item, exp_value);
        }

        public override void AddRecipes()
        {
            Recipes.Methods.AddRecipes_ExpOrbConversion(mod, this, orb_value);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, exp_value), TileID.Campfire);

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 3 Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 1, new Recipes.TierRecipe(mod, 3, false, false, -1, 49));

            /*~~~~~~~~~~~~~~~~~~~~~~~~Tier 3 Level 50+ Exchange Rates~~~~~~~~~~~~~~~~~~~~~~~~*/
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Boss_Orb"), 1 } }, this, 2, new Recipes.TierRecipe(mod, 3, false, false, 50, -1));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Monster_Orb"), 1 } }, this, 1, new Recipes.TierRecipe(mod, 3, false, false, 50, -1));
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(exp_value);
            return true;
        }
    }

    /* Experience Orb x10,000 */
    public class Experience10000 : ModItem
    {
        static int orb_value = 10000;
        static double exp_value = ExperienceAndClasses.EXP_ITEM_VALUE * orb_value;

        public override void SetDefaults()
        {
            item.name = "Experience Orb 10,000";
            Methods.SetDefaults_ExpOrbs(item, exp_value);
        }

        public override void AddRecipes()
        {
            Recipes.Methods.AddRecipes_ExpOrbConversion(mod, this, orb_value);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, exp_value), TileID.Campfire);
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(exp_value);
            return true;
        }
    }

    /* Experience Orb x100,000 */
    public class Experience100000 : ModItem
    {
        static int orb_value = 100000;
        static double exp_value = ExperienceAndClasses.EXP_ITEM_VALUE * orb_value;

        public override void SetDefaults()
        {
            item.name = "Experience Orb 100,000";
            Methods.SetDefaults_ExpOrbs(item, exp_value);
        }

        public override void AddRecipes()
        {
            Recipes.Methods.AddRecipes_ExpOrbConversion(mod, this, orb_value);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, exp_value), TileID.Campfire);
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(exp_value);
            return true;
        }
    }

    /* Experience Orb x1,000,000 */
    public class Experience1000000 : ModItem
    {
        static int orb_value = 1000000;
        static double exp_value = ExperienceAndClasses.EXP_ITEM_VALUE * orb_value;

        public override void SetDefaults()
        {
            item.name = "Experience Orb 1,000,000";
            Methods.SetDefaults_ExpOrbs(item, exp_value);
        }

        public override void AddRecipes()
        {
            Recipes.Methods.AddRecipes_ExpOrbConversion(mod, this, orb_value);
            Commons.QuckRecipe(mod, new int[,] { { } }, this, 1, new Recipes.ExpRecipe(mod, exp_value), TileID.Campfire);
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<MyPlayer>(mod).AddExp(exp_value);
            return true;
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

    /* General Methods */
    class Methods
    {
        /// <summary>
        /// Fill item info for Exp Orbs
        /// </summary>
        /// <param name="item"></param>
        /// <param name="exp_value"></param>
        public static void SetDefaults_ExpOrbs(Item item, double exp_value)
        {
            item.toolTip = "Worth " + exp_value + " experience.";
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
    }
}
