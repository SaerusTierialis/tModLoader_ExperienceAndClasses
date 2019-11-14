using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    public abstract class Potion : MItem {
        private const int WIDTH = 20;
        private const int HEIGTH = 30;
        private const bool CONSUMABLE = true;

        protected Potion(string texture, int rarity, bool auto_consume) : base(texture, CONSUMABLE, WIDTH, HEIGTH, rarity, auto_consume) {}

        public override void SetDefaults() {
            base.SetDefaults();

            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
        }
    }

    public class Potion_XP_Instant : Potion {
        public const uint XP_PER_CHARACTER_LEVEL = 100;

        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Potion_XP_Instant";
        private const int RARITY = 9;

        public Potion_XP_Instant() : base(TEXTURE, RARITY, true) { }

        public override void SetDefaults() {
            base.SetDefaults();

            item.useTime = 30;
        }

        public override void AddRecipes() {
            QuckRecipe(mod, new int[,] { { ModContent.ItemType<Orb_Monster>(), 3 } , { ItemID.BottledWater, 1 } }, this, 1, null, TileID.AlchemyTable);
            QuckRecipe(mod, new int[,] { { ModContent.ItemType<Orb_Monster>(), 3 } , { ItemID.BottledWater, 1 } }, this, 1, null, TileID.Bottles);
        }

        public override bool UseItem(Player player) {
            EACPlayer eacplayer = player.GetModPlayer<EACPlayer>();

            if (eacplayer.Fields.Is_Local) {
                Systems.XP.Adjustments.LocalAddXP(eacplayer.PSheet.Character.Level * XP_PER_CHARACTER_LEVEL, false, true);
                return true;
            }
            else {
                return base.UseItem(player);
            }
        }
    }

    public class Potion_XP_Buff : Potion {
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Potion_XP_Buff";
        private const int RARITY = 11;

        public override bool CanUseItem(Player player) {
            if (player.HasBuff(ModContent.BuffType<XP_Buff>()))
                return false;
            else
                return base.CanUseItem(player);
        }

        public Potion_XP_Buff() : base(TEXTURE, RARITY, false) { }

        public override void SetDefaults() {
            base.SetDefaults();

            item.buffType = ModContent.BuffType<XP_Buff>();
            item.buffTime = 18000;
        }

        public override void AddRecipes() {
            QuckRecipe(mod, new int[,] { { ModContent.ItemType<Orb_Monster>(), 5 } , { ItemID.BottledWater, 1 } }, this, 1, null, TileID.AlchemyTable);
            QuckRecipe(mod, new int[,] { { ModContent.ItemType<Orb_Monster>(), 5 } , { ItemID.BottledWater, 1 } }, this, 1, null, TileID.Bottles);
        }
    }

    public class XP_Buff : ModBuff {
        public const float MULTIPLIER = 1.5f;

        public override bool Autoload(ref string name, ref string texture) {
            texture = "ExperienceAndClasses/Textures/Status/XP_Buff";
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults() {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
            canBeCleared = true;
        }
    }
}
