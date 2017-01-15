using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Recipes
{
    /* Recipes that take experience */
    class ExpRecipe : ModRecipe
    {
        public double experienceNeeded = 0;

        public ExpRecipe(Mod mod, double experienceNeeded) : base(mod)
        {
            this.experienceNeeded = experienceNeeded;
        }

        public override bool RecipeAvailable()
        {
            if (Main.LocalPlayer.GetModPlayer<MyPlayer>(mod).GetExp() < experienceNeeded)
                return false;
            else
                return base.RecipeAvailable();
        }

        public override void OnCraft(Item item)
        {
            if (Helpers.CraftWithExp(experienceNeeded))
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
        public ClassRecipes2(Mod mod, double experienceNeeded) : base(mod, experienceNeeded)
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
            if (exp >= experienceNeeded)
            {
                //remove prefix
                item.prefix = 0;
                item.rare = createItem.rare;

                //tell server to announce (no effect in single player)
                Methods.PacketSender.ClientTellAnnouncement(Main.LocalPlayer.name + " has completed " + createItem.name + "!", 255, 255, 0);
            }
        }
    }

    /* Recipes that are available at specified class tiers (token must be equipped, else tier 1) */
    class TierRecipe : ModRecipe
    {
        int tier;
        bool includeLowerTier;
        bool includeHigherTier;
        int levelMin;
        int levelMax;
        public TierRecipe(Mod mod, int tier, bool includeLowerTier, bool includeHigherTier, int levelMin, int levelMax) : base(mod)
        {
            this.tier = tier;
            this.includeLowerTier = includeLowerTier;
            this.includeHigherTier = includeHigherTier;
            this.levelMin = levelMin;
            this.levelMax = levelMax;
        }

        public override bool RecipeAvailable()
        {
            Player player = Main.LocalPlayer;
            MyPlayer myPlayer = player.GetModPlayer<MyPlayer>(mod);
            int tierCurrent = Methods.Experience.GetTier(player);
            int levelCurrent = Methods.Experience.GetLevel(myPlayer.GetExp());

            if (levelMin >= 0 && levelCurrent < levelMin) return false;
            if (levelMax >= 0 && levelCurrent > levelMax) return false;

            if (tierCurrent == tier || (tierCurrent > tier && includeHigherTier) || (tierCurrent < tier && includeLowerTier)) return true;
            else return false;
        }
    }
}
