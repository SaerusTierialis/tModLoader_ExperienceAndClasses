namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class StatsSheet : ContainerTemplate {
        public StatsSheet(PSheet psheet) : base(psheet) {
            Reset();
        }

        public bool Can_Use_Abilities; //TODO - unused
        public bool Channelling; //TODO - unused

        public float Healing_Mult; //TODO - unused

        /// <summary>
        /// 0 to 100
        /// </summary>
        public float Dodge;

        public float Ability_Delay_Reduction; //TODO - unused

        public float Item_Speed_Weapon; //TODO - unused
        public float Item_Speed_Tool; //TODO - unused

        public float Damage_Light; //TODO - unused
        public float Damage_Harmonic; //TODO - unused
        public float Damage_Other; //TODO - unused

        /// <summary>
        /// 0 to 100
        /// </summary>
        public float Crit_All;

        public class DamageModifier {
            public float Increase, FinalMultAdd;
        }

        public void Reset() {
            Can_Use_Abilities = true;
            Channelling = false;

            Healing_Mult = 1f;
            Dodge = 0f;
            Ability_Delay_Reduction = 1f;

            Damage_Light = Damage_Harmonic = Damage_Other = 0f;

            Crit_All = 0f;

            Item_Speed_Weapon = Item_Speed_Tool = 0f;
        }

        public void Limit() {
            Dodge = (float)Utilities.Commons.Clamp(Dodge, 0, 1);
        }
    }
}
