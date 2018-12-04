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

        public float xp_multiplier = 0f;

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
            string str = "";
            uint value;
            bool can_use = false;

            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID != (byte)Systems.Class.CLASS_IDS.None) {
                str += ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Name;
                value = GetPrimaryValue();
                if (value > 0) {
                    str += " XP: " + value;
                    can_use = true;
                }
                else {
                    str += " is MAXED";
                }
            }

            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID != (byte)Systems.Class.CLASS_IDS.None) {
                if (str.Length > 0) {
                    str += "\n";
                }
                str += ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Name;
                value = GetSecondaryValue();
                if (value > 0) {
                    str += " XP: " + value;
                    can_use = true;
                }
                else {
                    str += " is MAXED";
                }
            }

            if (str.Length > 0) {
                TooltipLine line = new TooltipLine(mod, "desc", str);
                line.overrideColor = UI.Constants.COLOUR_XP_BRIGHT;
                tooltips.Add(line);
            }

            if (!can_use) {
                TooltipLine line = new TooltipLine(mod, "desc2", "Cannot be consumed because you cannot currently gain XP!");
                line.overrideColor = UI.Constants.COLOUR_MESSAGE_ERROR;
                tooltips.Add(line);
            }
        }

        public override bool CanUseItem(Player player) {
            return ExperienceAndClasses.LOCAL_MPLAYER.CanGainXP();
        }

        public override bool UseItem(Player player) {
            uint xp_primary = GetPrimaryValue();
            uint xp_secondary = GetSecondaryValue();

            if ((xp_primary + xp_secondary) <= 0) {
                //cannot gain xp!
                return false;
            }
            else {
                ExperienceAndClasses.LOCAL_MPLAYER.ForceAddXP(xp_primary, xp_secondary);
                return true;
            }
        }

        private uint GetPrimaryValue() {
            if (ExperienceAndClasses.LOCAL_MPLAYER.CanGainXPPrimary()) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.CanGainXPSecondary()) {
                    return (uint)Math.Ceiling(0.5 * xp_multiplier * Systems.XP.GetBossOrbXP(ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID]));
                }
                else {
                    return (uint)Math.Ceiling(xp_multiplier * Systems.XP.GetBossOrbXP(ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID]));
                }
            }
            else {
                return 0;
            }
        }

        private uint GetSecondaryValue() {
            if (ExperienceAndClasses.LOCAL_MPLAYER.CanGainXPSecondary()) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.CanGainXPPrimary()) {
                    return (uint)Math.Ceiling(0.5 * xp_multiplier * Systems.XP.GetBossOrbXP(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID]));
                }
                else {
                    return (uint)Math.Ceiling(xp_multiplier * Systems.XP.GetBossOrbXP(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID]));
                }
            }
            else {
                return 0;
            }
        }
    }

    public class Orb_Monster : Orb {
        public const string NAME = "Ascension Orb";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Monster";
        private const int RARITY = 9;
        private const short DUST = DustID.ApprenticeStorm;

        public const int CONVERT_BOSS_ORB = 5;

        public Orb_Monster() : base(NAME, TOOLTIP, TEXTURE, RARITY, DUST) {
            xp_multiplier = 1f / CONVERT_BOSS_ORB;
        }

        public override void AddRecipes() {
            //convert boss orb to ascension orb
            recipe = Commons.QuckRecipe(mod, new int[,] { { mod.ItemType<Orb_Boss>(), 1 } }, this, CONVERT_BOSS_ORB);
        }
    }

    class Orb_Boss : Orb {
        public const string NAME = "Transcendence Orb";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Orb_Boss";
        private const int RARITY = 11;
        private const short DUST = DustID.PurpleCrystalShard;

        public Orb_Boss() : base(NAME, TOOLTIP, TEXTURE, RARITY, DUST) {
            xp_multiplier = 1f;
        }

    }
}
