using System;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Buffs
{
    /* Assassin opener attack indicator */
    class Buff_OpenerAttack : ModBuff
    {
        public override void SetDefaults()
        {
            //Main.buffName[Type] = "Opener Attack";
            //Main.buffTip[Type] = "Bonus damage on the next melee attack\nBonus is half for yo-yo";
            DisplayName.SetDefault("Assassinate");
            Description.SetDefault("Bonus damage on the next melee attack\nBonus is half for yo-yo");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }

    /* Assassin opener attack phase (cannot be hit) */
    class Buff_OpenerPhase : ModBuff
    {
        public override void SetDefaults()
        {
            //Main.buffName[Type] = "Phase";
            //Main.buffTip[Type] = "Cannot be hit";
            DisplayName.SetDefault("Phase");
            Description.SetDefault("Cannot be hit");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.GetModPlayer<MyPlayer>(mod).openerImmuneEnd.CompareTo(DateTime.Now) <= 0)
            {
                //player.DelBuff(mod.BuffType<Buffs.Buff_OpenerPhase>());
            }
            else
            {
                player.immune = true;
                player.immuneTime = 1;
                player.AddBuff(mod.BuffType<Buffs.Buff_OpenerPhase>(), 1);
            }

            base.Update(player, ref buffIndex);
        }
    }
}