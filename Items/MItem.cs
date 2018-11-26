using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    abstract class MItem : ModItem {
        private const int STACK_SIZE = 99999;

        public override void SetDefaults() {
            item.maxStack = STACK_SIZE;
        }
        public override void OnCraft(Recipe recipe) {
            item.maxStack = STACK_SIZE;
            base.OnCraft(recipe);
        }
        public override void UpdateInventory(Player player) {
            item.maxStack = STACK_SIZE;
            base.UpdateInventory(player);
        }
    }
}
