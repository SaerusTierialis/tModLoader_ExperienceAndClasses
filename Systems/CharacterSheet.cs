using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems {
    class CharacterSheet {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes + Points ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Custom Stats ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// Reset on each update cycle
        /// </summary>
        public StatsContainer Stats { get; } = new StatsContainer();
        public class StatsContainer {
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

            public StatsContainer() {
                Clear();
            }

            public void Clear() {
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Character ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public CharacterMethods Character { get; } = new CharacterMethods();
        public class CharacterMethods {
            public byte Level { get; private set; } = 1;
            public uint XP { get; private set; } = 0;

            public uint XP_Level_Total { get; private set; } = 0; //TODO
            public uint XP_Level_Remaining { get; private set; } = 0; //TODO

            /// <summary>
            /// True while player is AFK
            /// | sync server
            /// </summary>
            public bool AFK { get; private set; } = false; //TODO

            /// <summary>
            /// True while in combat
            /// | sync ALL
            /// </summary>
            public bool In_Combat { get; private set; } = false; //TODO

            /// <summary>
            /// Track boss kill
            /// | local only
            /// </summary>
            public bool Defeated_WOF { get; private set; } = false; //TODO - not added

            /// <summary>
            /// Has unlocked subclass system
            /// | local only
            /// </summary>
            public bool Secondary_Unlocked { get; private set; } = false; //TODO - not used

            /// <summary>
            /// True when player is the local player
            /// </summary>
            public bool Is_Local { get; private set; } = false;

            public void ForceLevel(byte level) {
                if (Is_Local) {
                    Utilities.Logger.Error("ForceLevel called by local");
                }
                else {
                    Level = level;
                }
            }

            public void SetAsLocal() {
                Is_Local = true;
            }

            public void SetAFK(bool afk) {
                AFK = afk;
                if (Is_Local) {
                    if (AFK) {
                        Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.AFK_Start"), UI.Constants.COLOUR_MESSAGE_ERROR);
                    }
                    else {
                        Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.AFK_End"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    }
                }
            }

            public void SetInCombat(bool combat_state) {
                In_Combat = combat_state;
            }

            public void DefeatWOF() {
                if (Is_Local && !Defeated_WOF) {
                    Defeated_WOF = true;
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_WOF"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    //TODO
                    /*
                    if (Systems.PlayerClass.LocalCanUnlockTier3()) {
                        Main.NewText("You can now unlock tier III classes!", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    }
                    */
                }
            }

            public void LocalAddXP(uint xp) {
                //TODO
                LocalHandleXPChange();
            }

            public void LocalDeathPenalty() {
                //TODO
                LocalHandleXPChange();
            }

            private void LocalHandleXPChange() {
                //TODO levelup, xp needed for level, etc.
            }

            public TagCompound Save(TagCompound tag) {
                tag.Add(TAG_NAMES.Character_Level, Level);
                tag.Add(TAG_NAMES.Character_XP, XP);
                tag.Add(TAG_NAMES.WOF, Defeated_WOF);
                tag.Add(TAG_NAMES.UNLOCK_SUBCLASS, Secondary_Unlocked);
                return tag;
            }
            public void Load(TagCompound tag) {
                Level = Utilities.Commons.TryGet<byte>(tag, TAG_NAMES.Character_Level, 1);
                XP = Utilities.Commons.TryGet<uint>(tag, TAG_NAMES.Character_XP, 1);
                Defeated_WOF = Utilities.Commons.TryGet<bool>(tag, TAG_NAMES.WOF, false);
                Secondary_Unlocked = Utilities.Commons.TryGet<bool>(tag, TAG_NAMES.UNLOCK_SUBCLASS, false);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private static class TAG_NAMES {
            public static string PREFIX = "eac_";

            //Class
            //TODO

            //Attribute Allocations
            //TODO

            //Character
            public static string Character_Level = PREFIX + "character_level";
            public static string Character_XP = PREFIX + "character_xp";
            public static string WOF = PREFIX + "wof";
            public static string UNLOCK_SUBCLASS = PREFIX + "class_subclass_unlocked";
        }

        public void Load(TagCompound tag) {
            //Class
            //TODO

            //Attribute Allocations
            //TODO

            //Character
            Character.Load(tag);
        }

        public TagCompound Save(TagCompound tag) {
            //Class
            //TODO

            //Attribute Allocations
            //TODO

            //Character
            tag = Character.Save(tag);

            return tag;
        }
    }
}
