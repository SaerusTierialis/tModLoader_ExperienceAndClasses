using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    class MNPC : GlobalNPC {

        public override void NPCLoot(NPC npc) {
            base.NPCLoot(npc);
            double xp = Systems.XP.CalcBaseExp(npc);
        }

    }
}
