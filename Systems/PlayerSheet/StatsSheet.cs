using System;

namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class StatsSheet : ContainerTemplate {
        public StatsSheet(PSheet psheet) : base(psheet) {
            Reset();
        }

        public bool Can_Use_Abilities; //TODO - unused
        public bool Channelling; //TODO - unused

        public float Healing_Mult; //TODO - unused

        public float Mana_Regen_Delay_Reduction;

        /// <summary>
        /// 0 to 100
        /// </summary>
        public float Dodge;

        public float Ability_Delay_Reduction; //TODO - unused

        public float Item_Speed_Weapon;
        public float Item_Speed_Tool;

        //1 = 100%
        public float Damage_Light;
        public float Damage_Harmonic;
        public float Damage_Other_Add;

        /// <summary>
        /// 0 to 1
        /// </summary>
        public float Crit_All;

        /// <summary>
        /// 1 = 100%
        /// </summary>
        public float Crit_Damage_Mult;

        public class DamageModifier {
            public float Increase, FinalMultAdd;
        }

        public void Reset() {
            Can_Use_Abilities = true;
            Channelling = false;

            Mana_Regen_Delay_Reduction = 0f;

            Healing_Mult = 1f;
            Dodge = 0f;
            Ability_Delay_Reduction = 0f;

            Damage_Light = Damage_Harmonic = 1f;
            Damage_Other_Add = 0f;

            Crit_All = 0f;
            Crit_Damage_Mult = 1f;

            Item_Speed_Weapon = Item_Speed_Tool = 1f;
        }

        public void Limit() {
            Dodge = (float)Utilities.Commons.Clamp(Dodge, 0f, 1f);
            Crit_All = (float)Utilities.Commons.Clamp(Crit_All, 0f, 1f);

            //prevent minion cap from dropping below 1
            PSHEET.eacplayer.player.maxMinions = Math.Max(1, PSHEET.eacplayer.player.maxMinions);
        }

        public void Apply() {
            if (PSHEET.eacplayer.player.manaRegenDelay > 50) {
                int new_delay = (int)Math.Max(Math.Round(PSHEET.eacplayer.player.manaRegenDelay * (1f / (1f + Mana_Regen_Delay_Reduction))), 50);
                PSHEET.eacplayer.player.manaRegenDelayBonus += PSHEET.eacplayer.player.manaRegenDelay - new_delay;
            }
        }
    }
}
