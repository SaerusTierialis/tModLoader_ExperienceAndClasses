using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Buffs
{
    /* Indicator for Saint life aura (no effect) */
    public class Aura_Life : ModBuff
    {
        public override void SetDefaults()
        {
            Main.buffName[Type] = "Life Aura";
            Main.buffTip[Type] = "Periodically restores life";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }

    /* Sage's defense aura tiers */
    public class Aura_Defense1 : ModBuff
    {
        public static int bonus_defense = 5;

        public int bonus = bonus_defense;
        public string tier = "I";
        public int tierNumber = 1;

        public override void SetDefaults()
        {
            Main.buffName[Type] = "Defense Aura " + bonus;
            Main.buffTip[Type] = "Adds " + bonus + " defense";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (AllowEffect<Player>(player)) player.statDefense += bonus;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            if (AllowEffect<NPC>(npc)) npc.defense += bonus;
        }

        /// <summary>
        /// Returns false if a higher tier def aura is active on target.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool AllowEffect<T>(T target)
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


    public class Aura_Defense2 : Aura_Defense1
    {
        public static int bonus_defense = 10;

        public Aura_Defense2()
        {
            base.bonus = bonus_defense;
            tierNumber = 2;
            tier = "II";
        }
    }
    public class Aura_Defense3 : Aura_Defense1
    {
        public static int bonus_defense = 15;

        public Aura_Defense3()
        {
            base.bonus = bonus_defense;
            tierNumber = 3;
            tier = "III";
        }
    }
}
