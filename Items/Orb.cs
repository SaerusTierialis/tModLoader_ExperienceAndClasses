using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    public abstract class Orb : MItem {
        private readonly short DUST;

        private const int WIDTH = 20;
        private const int HEIGTH = 20;
        private const bool CONSUMABLE = true;
        private const bool CONSUMABLE_AUTO = true;

        public double xp_multiplier = 0.0;

        protected Orb(string name, string tooltip, string texture, int rarity, short dust) : base(name, tooltip, texture, CONSUMABLE, WIDTH, HEIGTH, rarity, CONSUMABLE_AUTO) {
            DUST = dust;
        }

        public override void GrabRange(Player player, ref int grabRange) {
            grabRange *= 20;
            base.GrabRange(player, ref grabRange);
        }

        public override bool GrabStyle(Player player) {
            item.velocity = item.DirectionTo(player.Center) * Math.Min(Math.Max(3000f / item.Distance(player.Center), 1f), 50f);
            Dust.NewDust(item.Center, 0, 0, DUST);
            Lighting.AddLight(item.Center, Color.White.ToVector3());
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            TooltipLine line = new TooltipLine(mod, "desc", "Current XP Value: " + GetXPValue());
            line.overrideColor = UI.Constants.COLOUR_XP_BRIGHT;
            tooltips.Add(line);

            if (!Systems.XP.Adjusting.LocalCanGainXP) {
                line = new TooltipLine(mod, "desc2", "Use is currently prevented because you cannot gain XP!");
                line.overrideColor = UI.Constants.COLOUR_MESSAGE_ERROR;
                tooltips.Add(line);
            }
        }

        public override bool CanUseItem(Player player) {
            return Systems.XP.Adjusting.LocalCanGainXP;
        }

        public override bool UseItem(Player player) {
            if (Systems.XP.Adjusting.LocalCanGainXP) {
                Systems.XP.Adjusting.LocalAddXP(GetXPValue(), false);
                return true;
            }
            else {
                //cannot gain xp!
                return false;
            }
        }

        public uint GetXPValue() {
            return (uint)(Systems.NPCRewards.GetBossOrbXP() * xp_multiplier);
        }
    }

    public class Orb_Monster : Orb {
        public const string NAME = "Ascension Orb";
        private const string TOOLTIP = "TODO_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Monster";
        private const int RARITY = 9;
        private const short DUST = DustID.ApprenticeStorm;

        public const int CONVERT_BOSS_ORB = 5;

        public Orb_Monster() : base(NAME, TOOLTIP, TEXTURE, RARITY, DUST) {
            xp_multiplier = 1.0 / CONVERT_BOSS_ORB;
        }

        public override void AddRecipes() {
            //convert boss orb to ascension orb
            recipe = Utilities.Commons.QuckRecipe(mod, new int[,] { { mod.ItemType<Orb_Boss>(), 1 } }, this, CONVERT_BOSS_ORB);
        }
    }

    class Orb_Boss : Orb {
        public const string NAME = "Transcendence Orb";
        private const string TOOLTIP = "TODO_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Boss";
        private const int RARITY = 11;
        private const short DUST = DustID.PurpleCrystalShard;

        public Orb_Boss() : base(NAME, TOOLTIP, TEXTURE, RARITY, DUST) {
            xp_multiplier = 1.0;
        }

    }
}
