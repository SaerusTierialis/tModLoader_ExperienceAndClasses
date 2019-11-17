using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Systems {
    class Battle {
        public const float DISTANCE_CLOSE_RANGE = 250f;
        public const double SECONDS_IN_COMBAT = 10;

        public class DamageSource {
            public readonly bool Is_Item;
            public readonly bool Is_Projectile;
            public readonly Item Item;
            public readonly Projectile Projectile;

            public DamageSource(Item item) {
                Is_Item = true;
                Is_Projectile = false;
                Item = item;
            }
            public DamageSource(Projectile proj) {
                Is_Item = false;
                Is_Projectile = true;
                Projectile = proj;
            }
        }

    }
}
