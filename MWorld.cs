using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    class MWorld : ModWorld {
        public override void PostUpdate() {
            Systems.XP.Rewards.ProcessXPBuffer();
        }
    }
}
