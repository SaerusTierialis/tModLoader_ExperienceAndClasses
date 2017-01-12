using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Buffs
{
    class Aura_Defense1 : ModBuff
    {
        public const int bonus = 5;

        public override void SetDefaults()
        {
            Main.buffName[Type] = "Defense Aura I";
            Main.buffTip[Type] = "Adds " + bonus + " defense";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += bonus;
        }

    }

    class Aura_Defense2 : ModBuff
    {
        public const int bonus = 10;

        public override void SetDefaults()
        {
            Main.buffName[Type] = "Defense Aura II";
            Main.buffTip[Type] = "Adds " + bonus + " defense";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += bonus;
        }

    }

    class Aura_Defense3 : ModBuff
    {
        public const int bonus = 15;

        public override void SetDefaults()
        {
            Main.buffName[Type] = "Defense Aura III";
            Main.buffTip[Type] = "Adds " + bonus + " defense";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += bonus;
        }

    }

    //NOTE: does not do the actual healing
    class Aura_Life : ModBuff
    {
        public override void SetDefaults()
        {
            Main.buffName[Type] = "Life Aura";
            Main.buffTip[Type] = "Periodically restores life";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

    }
}
