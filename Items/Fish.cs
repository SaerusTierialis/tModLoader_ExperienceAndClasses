using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    class Fish : GlobalItem {

        public override bool InstancePerEntity => true;
        public override bool CloneNewInstances => true;

        public Item item;
        public override void SetDefaults(Item item) {
            base.SetDefaults(item);
            this.item = item;
        }

        /// <summary>
        /// Called locally only
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stack"></param>
        public override void CaughtFishStack(int type, ref int stack) {
            base.CaughtFishStack(type, ref stack);
            Systems.XP.Adjustments.LocalAddFishXP(item);
        }
    }
}
