using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Systems {
    class Combat {
        public const double SECONDS_IN_COMBAT = 10;

        public class DamageSource {
            public readonly bool Is_Item;
            public readonly bool Is_Projectile;
            public readonly Item Item;
            public readonly Projectile Projectile;

            public DamageSource(Item item, bool is_light = false, bool is_harmonic = false) {
                Is_Item = true;
                Is_Projectile = false;
                Item = item;

                Melee = item.melee;
                Ranged = item.ranged;
                Throwing = item.thrown;
                Magic = item.magic;
                Minion = ItemIsMinionWeapon(item);
                Light = is_light;
                Harmonic = is_harmonic;
                Tool = ItemIsTool(item);
                Other = !(Melee || Ranged || Throwing || Magic || Minion || Light || Harmonic || Tool);
            }
            public DamageSource(Projectile proj, bool is_light = false, bool is_harmonic = false) {
                Is_Item = false;
                Is_Projectile = true;
                Projectile = proj;

                Melee = proj.melee;
                Ranged = proj.ranged;
                Throwing = proj.thrown;
                Magic = proj.magic;
                Minion = proj.minion || proj.sentry;
                Light = is_light;
                Harmonic = is_harmonic;
                Tool = false;
                Other = !(Melee || Ranged || Throwing || Magic || Minion || Light || Harmonic || Tool);
            }

            public readonly bool Melee;
            public readonly bool Ranged;
            public readonly bool Throwing;
            public readonly bool Magic;
            public readonly bool Minion;
            public readonly bool Tool;
            public readonly bool Light;
            public readonly bool Harmonic;
            public readonly bool Other;

        }

        public static bool ItemIsTool(Item item) {
            return (item.hammer > 0 || item.axe > 0 || item.pick > 0 || item.fishingPole > 0);
        }

        public static bool ItemIsWeapon(Item item) {
            return (item.damage > 0) && !ItemIsTool(item);
        }

        public static bool ItemIsMinionWeapon(Item item) {
            return item.summon || item.DD2Summon || item.sentry;
        }

        public static void LocalModifyDamageDealt(EACPlayer eacplayer, DamageSource dsource, ref int damage, ref bool crit, bool is_projectile = false, float distance = 0f) {
            //apply all crit chance
            if (eacplayer.Fields.Is_Local && !crit && (eacplayer.PSheet.Stats.Crit_All > 0)) {
                //adjust chance to prevent diminishing returns
                int adjust = 0;

                if (dsource.Melee)
                    adjust += eacplayer.player.meleeCrit;

                if (dsource.Ranged)
                    adjust += eacplayer.player.rangedCrit;

                if (dsource.Throwing)
                    adjust += eacplayer.player.thrownCrit;

                if (dsource.Magic)
                    adjust += eacplayer.player.magicCrit;

                //roll crit
                if (RollCrit(eacplayer, adjust)) {
                    crit = true;
                }
            }
        }

        public static bool RollCrit(EACPlayer eacplayer, float adjust = 0) {
            return Main.rand.NextFloat(0, 100 - adjust) < eacplayer.PSheet.Stats.Crit_All;
        }

    }
}
