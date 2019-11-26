using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    public abstract class Unlock : MItem {
        private const int WIDTH = 32;
        private const int HEIGTH = 32;
        private const bool CONSUMABLE = false;

        protected Unlock(string texture, int rarity) : base(texture, CONSUMABLE, WIDTH, HEIGTH, rarity) {}
    }

    public class Unlock_Tier2 : Unlock {
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Tier2";
        private const int RARITY = 8;

        public Unlock_Tier2() : base(TEXTURE, RARITY) { }

        public override void AddRecipes() {
            QuckRecipe(mod, new int[,] { { ItemID.Gel, 50 } , { ItemID.FallenStar, 1 } , { ModContent.ItemType<Orb_Monster>(), 1 } }, this, 1);
        }
    }

    public class Unlock_Tier3 : Unlock {
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Tier3";
        private const int RARITY = 10;

        public Unlock_Tier3() : base(TEXTURE, RARITY) { }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(Shortcuts.MOD);
            recipe.AddRecipeGroup(Shortcuts.RECIPE_GROUP_MECHANICAL_SOUL, 50); //any mechanical boss soul
            QuckRecipe(mod, new int[,] { { ModContent.ItemType<Orb_Monster>(), 50 } , { ModContent.ItemType<Orb_Boss>(), 5 } }, this, 1, recipe);
        }
    }

    public class Unlock_Subclass : Unlock {
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Subclass";
        private const int RARITY = -12;

        public Unlock_Subclass() : base(TEXTURE, RARITY) { }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(Shortcuts.MOD);
            recipe.AddRecipeGroup(Shortcuts.RECIPE_GROUP_MECHANICAL_SOUL, 100); //any mechanical boss soul
            QuckRecipe(mod, new int[,] { { ItemID.SoulofLight, 100 }, { ItemID.SoulofNight, 100 } , { ModContent.ItemType<Orb_Boss>(), 20 } }, this, 1, recipe);
        }
    }
}
