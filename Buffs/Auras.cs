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
    public class Aura_Defense1 : Aura_Defense
    {
        public static int bonus = 5;
        public Aura_Defense1()
        {
            base.bonus = bonus;
            tier = "I";
        }
    }
    public class Aura_Defense2 : Aura_Defense
    {
        public static int bonus = 10;
        public Aura_Defense2()
        {
            base.bonus = bonus;
            tier = "II";
        }
    }
    public class Aura_Defense3 : Aura_Defense
    {
        public static int bonus = 15;
        public Aura_Defense3()
        {
            base.bonus = bonus;
            tier = "III";
        }
    }

    /* TEMPLATES */
    public abstract class Aura_Defense : ModBuff
    {
        public int bonus = 0;
        public string tier = "?";

        public override void SetDefaults()
        {
            Main.buffName[Type] = "Defense Aura " + bonus;
            Main.buffTip[Type] = "Adds " + bonus + " defense";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += bonus;
        }

    }
}
