using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Buffs
{
    /* Assassin opener attack indicator */
    class Buff_OpenerAttack : ModBuff
    {
        public override void SetDefaults()
        {
            Main.buffName[Type] = "Opener Attack";
            Main.buffTip[Type] = "Bonus damage on the next melee attack\nBonus is half for yo-yo";
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}