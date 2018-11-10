using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Systems {
    class XP {

        public static double CalcBaseExp(NPC npc) {
            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            float experience = 0; ;
            if (npc.defDefense >= 1000)
                experience = (npc.lifeMax / 100f) * (1f + (npc.defDamage / 25f));
            else
                experience = (npc.lifeMax / 100f) * (1f + (npc.defDefense / 10f)) * (1f + (npc.defDamage / 25f));

            return experience;
        }

    }
}
