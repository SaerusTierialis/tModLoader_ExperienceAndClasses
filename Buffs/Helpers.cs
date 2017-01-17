using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Buffs
{
    public static class Helpers
    {
        /// <summary>
        /// Returns false if a higher tier def aura is active on target.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool AllowEffect<T>(Mod mod, T target, int tierNumber)
        {
            if (tierNumber == 3) return true;

            bool buff2 = false, buff3 = false;
            if (typeof(T) == typeof(Player))
            {
                Player player = (target as Player);
                buff2 = player.FindBuffIndex(mod.BuffType("Aura_Defense2")) != -1;
                buff3 = player.FindBuffIndex(mod.BuffType("Aura_Defense3")) != -1;
            }
            else if (typeof(T) == typeof(NPC))
            {
                NPC npc = (target as NPC);
                buff2 = npc.FindBuffIndex(mod.BuffType("Aura_Defense2")) != -1;
                buff3 = npc.FindBuffIndex(mod.BuffType("Aura_Defense3")) != -1;
            }

            if ((tierNumber == 1 && (buff2 || buff3)) || (tierNumber == 2 && buff3))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
