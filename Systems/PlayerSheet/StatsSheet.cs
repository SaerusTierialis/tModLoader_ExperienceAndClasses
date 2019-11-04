using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class StatsSheet : ContainerTemplate {
        public StatsSheet(PSheet psheet) : base(psheet) {
            Reset();
        }

        public bool Can_Use_Abilities; //TODO - unused
        public bool Channelling; //TODO - unused

        public float Healing_Mult; //TODO - unused

        public float Dodge; //TODO - unused

        public float Ability_Delay_Reduction; //TODO - unused

        public float SpeedAdjust_Melee; //TODO - unused
        public float SpeedAdjust_Ranged; //TODO - unused
        public float SpeedAdjust_Magic; //TODO - unused
        public float SpeedAdjust_Throwing; //TODO - unused
        public float SpeedAdjust_Minion; //TODO - unused
        public float SpeedAdjust_Weapon; //TODO - unused
        public float SpeedAdjust_Tool; //TODO - unused

        public DamageModifier Holy = new DamageModifier(); //TODO - unused
        public DamageModifier AllNearby = new DamageModifier(); //TODO - unused
        public DamageModifier NonMinionProjectile = new DamageModifier(); //TODO - unused
        public DamageModifier NonMinionAll = new DamageModifier(); //TODO - unused

        public void Reset() {
            Can_Use_Abilities = true;
            Channelling = false;

            Healing_Mult = 1f;
            Dodge = 0f;
            Ability_Delay_Reduction = 1f;

            SpeedAdjust_Melee = SpeedAdjust_Ranged = SpeedAdjust_Magic = SpeedAdjust_Throwing = SpeedAdjust_Minion = SpeedAdjust_Weapon = SpeedAdjust_Tool = 0f;

            Holy.Increase = AllNearby.Increase = NonMinionProjectile.Increase = NonMinionAll.Increase = 0f;
            Holy.FinalMultAdd = AllNearby.FinalMultAdd = NonMinionProjectile.FinalMultAdd = NonMinionAll.FinalMultAdd = 0f;
        }

        public class DamageModifier {
            public float Increase, FinalMultAdd;
        }
    }
}
