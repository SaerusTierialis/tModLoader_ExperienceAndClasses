using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Recipes
{
    /* Recipes that take experience */
    class ExpRecipe : ModRecipe
    {
        public double experience_needed = 0;

        public ExpRecipe(Mod mod, double experience_needed) : base(mod)
        {
            this.experience_needed = experience_needed;
        }

        public override bool RecipeAvailable()
        {
            if (Main.LocalPlayer.GetModPlayer<MyPlayer>(mod).GetExp() < experience_needed)
                return false;
            else
                return base.RecipeAvailable();
        }

        public override void OnCraft(Item item)
        {
            if (Helpers.CraftWithExp(mod, experience_needed))
            {
                //success - do craft
                base.OnCraft(item);
            }
            else
            {
                //fail - remove the item if crafted
                base.OnCraft(item);
                Main.mouseItem.stack--;
            }
        }
    }

    /* Class Token recipe bases (take exp and standard item requirements, remove prefix, announce) */
    class ClassRecipes2 : ExpRecipe
    {
        public ClassRecipes2(Mod mod, double experience_required) : base(mod, experience_required)
        {
            //no changes here
        }
        public override void OnCraft(Item item)
        {
            //get exp prior to crafting
            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);
            double exp = myPlayer.GetExp();

            //do base (normally this can fail in ExpRecipe, but tokens don't stack so it shouldn't be possible here)
            base.OnCraft(item);

            //if there was enough exp to craft (this should always be the case because tokens cannot stack)
            if (exp >= experience_needed)
            {
                //remove prefix
                item.prefix = 0;
                item.rare = createItem.rare;

                //tell server to announce (no effect in single player)
                (mod as ExperienceAndClasses).PacketSend_ClientTellAnnouncement(Main.LocalPlayer.name + " has completed " + createItem.name + "!", 255, 255, 0);
            }
        }
    }

    /* OUTDATED
    // RecipeType: Token recipes that take experience
    class ClassRecipes : ModRecipe
    {

        public static int TIER_2_LEVEL = 10;
        public static int TIER_3_LEVEL = 25;

        public double experience_needed = 0;

        public ClassRecipes(Mod mod, int tier) : base(mod) //ClassRecipes(Mod mod, int ingredientID, ModItem result, int tier) : base(mod)
        {

            //if (ingredientID >= 0) AddIngredient(ingredientID);

            switch (tier)
            {
                case 2:
                    experience_needed = ExperienceAndClasses.GetExpReqForLevel(TIER_2_LEVEL, true);
                    AddIngredient(mod.ItemType("Monster_Orb"), 1);
                    //AddRecipeGroup("ExperienceAndClasses:Orb", 1);
                    break;
                case 3:
                    experience_needed = ExperienceAndClasses.GetExpReqForLevel(TIER_3_LEVEL, true);
                    AddIngredient(mod.ItemType("Boss_Orb"), 5);
                    AddIngredient(mod.ItemType("Monster_Orb"), 50);
                    //AddRecipeGroup("ExperienceAndClasses:Orb", 50);
                    break;
                default:
                    experience_needed = 0;
                    break;
            }

            //AddTile(TileID.WorkBenches);
            //SetResult(result);
        }

        public override bool RecipeAvailable()
        {
            if (Main.LocalPlayer.GetModPlayer<MyPlayer>(mod).GetExp() < experience_needed)
                return false;
            else
                return base.RecipeAvailable();
        }

        public override void OnCraft(Item item)
        {
            if (Methods.CraftWithExp(mod, experience_needed))
            {
                //success - do craft

                //tell server to announce
                (mod as ExperienceAndClasses).PacketSend_ClientTellAnnouncement(Main.LocalPlayer.name + " has completed " + createItem.name + "!", 255, 255, 0);

                //craft
                base.OnCraft(item);

                //remove prefix
                item.prefix = 0;
                item.rare = createItem.rare;
            }
            else
            {
                //fail - don't craft
                base.OnCraft(item);
                Main.mouseItem.stack--;
            }
        }
    }
    */

    /* Recipes that are available at specified class tiers (token must be equipped, else tier 1) */
    class TierRecipe : ModRecipe
    {
        int tier;
        bool include_lower_tier;
        bool include_higher_tier;
        int level_min;
        int level_max;
        public TierRecipe(Mod mod, int tier, bool include_lower_tier, bool include_higher_tier, int level_min, int level_max) : base(mod)
        {
            this.tier = tier;
            this.include_lower_tier = include_lower_tier;
            this.include_higher_tier = include_higher_tier;
            this.level_min = level_min;
            this.level_max = level_max;
        }

        public override bool RecipeAvailable()
        {
            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            int tier_current = ExperienceAndClasses.GetTier(player);
            int level_current = ExperienceAndClasses.GetLevel(myPlayer.GetExp());

            if (level_min >= 0 && level_current < level_min) return false;
            if (level_max >= 0 && level_current > level_max) return false;

            if (tier_current == tier || (tier_current > tier && include_higher_tier) || (tier_current < tier && include_lower_tier)) return true;
            else return false;
        }
    }
}
