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

    /* Template & Sage's defense aura tiers */
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
            if (Helpers.AllowEffect<Player>(mod, player, tierNumber)) player.statDefense += bonus;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            if (Helpers.AllowEffect<NPC>(mod, npc, tierNumber)) npc.defense += bonus;
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
