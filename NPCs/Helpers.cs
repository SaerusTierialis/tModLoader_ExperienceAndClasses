using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.NPCs
{
    static class Helpers
    {
        /// <summary>
        /// Returns the unrounded base experience for NPC. Returns 0 for invalid NPC.
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static double CalcBaseExp(NPC npc)
        {
            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax <= 5 || npc.friendly) return 0f;

            float experience = 0; ;
            if (npc.defDefense == 1000)
                experience = (npc.lifeMax / 100f) * (1f + (npc.defDamage / 25f));
            else
                experience = (npc.lifeMax / 100f) * (1f + (npc.defDefense / 10f)) * (1f + (npc.defDamage / 25f));

            return experience;
        }
    }
}
