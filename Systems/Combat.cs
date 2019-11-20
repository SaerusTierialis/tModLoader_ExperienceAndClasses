using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Systems {
    class Combat {
        public const double SECONDS_IN_COMBAT = 10;
        public const float BASE_CRIT_MINION = 0.04f;
        public const float BASE_CRIT_LIGHT = 0.04f;
        public const float BASE_CRIT_HARMONIC = 0.04f;

        public class DamageSource {
            public readonly bool Is_Item;
            public readonly bool Is_Projectile;
            public readonly Item Item;
            public readonly Projectile Projectile;

            public DamageSource(Item item, bool is_light = false, bool is_harmonic = false) {
                Is_Item = true;
                Is_Projectile = false;
                Item = item;

                Tool = ItemIsTool(item);
                Weapon = ItemIsWeapon(item);

                Melee = item.melee;
                Ranged = item.ranged;
                Throwing = item.thrown;
                Magic = item.magic;
                Minion = ItemIsMinionWeapon(item);
                Light = is_light;
                Harmonic = is_harmonic;
                Other = Weapon && !(Melee || Ranged || Throwing || Magic || Minion || Light || Harmonic);
            }
            public DamageSource(Projectile proj, bool is_light = false, bool is_harmonic = false) {
                Is_Item = false;
                Is_Projectile = true;
                Projectile = proj;

                Tool = false;
                Weapon = true;

                Melee = Weapon && proj.melee;
                Ranged = proj.ranged;
                Throwing = proj.thrown;
                Magic = proj.magic;
                Minion = proj.minion || proj.sentry;
                Light = is_light;
                Harmonic = is_harmonic;
                Other = Weapon && !(Melee || Ranged || Throwing || Magic || Minion || Light || Harmonic || Tool);
            }

            public readonly bool Tool;
            public readonly bool Weapon;
            public readonly bool Melee;
            public readonly bool Ranged;
            public readonly bool Throwing;
            public readonly bool Magic;
            public readonly bool Minion;
            public readonly bool Light;
            public readonly bool Harmonic;
            public readonly bool Other;

        }

        public static bool ItemIsTool(Item item) {
            return (item.hammer > 0 || item.axe > 0 || item.pick > 0 || item.fishingPole > 0);
        }

        public static bool ItemIsWeapon(Item item) {
            return ((item.damage > 0) || (item.mana > 0)) && !ItemIsTool(item);
        }

        public static bool ItemIsMinionWeapon(Item item) {
            return item.summon || item.DD2Summon || item.sentry;
        }

        public static void ModifyItemDamge(EACPlayer eacplayer, Item item, ref float add, ref float mult, ref float flat) {
            DamageSource dsource = new DamageSource(item);

            //added workaround for radiant damage
            if (dsource.Other || (dsource.Magic && (add != (eacplayer.player.allDamage + eacplayer.player.magicDamage - 1f)))) {
                add += eacplayer.PSheet.Stats.Damage_Other_Add;
            }

            if (dsource.Light) add += (eacplayer.PSheet.Stats.Damage_Light - 1f);
            if (dsource.Harmonic) add += (eacplayer.PSheet.Stats.Damage_Harmonic - 1f);
        }

        public static float ModifyItemUseTime(EACPlayer eacplayer, Item item, float use_time) {
            DamageSource dsource = new DamageSource(item);

            if (dsource.Tool) {
                use_time *= eacplayer.PSheet.Stats.Item_Speed_Tool;
            }
            
            if (dsource.Weapon) {
                use_time *= eacplayer.PSheet.Stats.Item_Speed_Weapon;
            }

            return use_time;
        }

        public static void LocalModifyDamageDealt(EACPlayer eacplayer, DamageSource dsource, ref int damage, ref bool crit, bool is_projectile = false, float distance = 0f) {
            if (eacplayer.Fields.Is_Local) {
                //is a crit?
                if (!crit && (eacplayer.PSheet.Stats.Crit_All > 0)) {
                    //adjust chance to prevent diminishing returns
                    float adjust = 0;

                    if (dsource.Melee)
                        adjust += eacplayer.player.meleeCrit / 100f;

                    if (dsource.Ranged)
                        adjust += eacplayer.player.rangedCrit / 100f;

                    if (dsource.Throwing)
                        adjust += eacplayer.player.thrownCrit / 100f;

                    if (dsource.Magic)
                        adjust += eacplayer.player.magicCrit / 100f;

                    //roll crit
                    if (RollCrit(eacplayer, dsource, adjust)) {
                        crit = true;
                    }
                }

                //modifify crit damage
                if (crit) {
                    damage = (int)Math.Ceiling(damage * eacplayer.PSheet.Stats.Crit_Damage_Mult);
                }
            }
            else {
                Utilities.Logger.Error("LocalModifyDamageDealt called by non-local");
            }
        }

        public static bool RollCrit(EACPlayer eacplayer, DamageSource dsource, float adjust = 0) {
            float crit_chance = eacplayer.PSheet.Stats.Crit_All;

            if (dsource.Minion) crit_chance += BASE_CRIT_MINION;
            if (dsource.Light) crit_chance += BASE_CRIT_LIGHT;
            if (dsource.Harmonic) crit_chance += BASE_CRIT_HARMONIC;

            return Main.rand.NextFloat(0f, 1f - adjust) < crit_chance;
        }

    }
}
