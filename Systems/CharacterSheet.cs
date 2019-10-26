using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Systems {
    public class CharacterSheet {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Init ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public CharacterSheet() {
            //class list
            Classes = new ClassContainer[Class_Count];
            for (byte id = 0; id < Class_Count; id++) {
                Classes[id] = new ClassContainer(id);
            }
            Class_Primary   = Classes[(byte)PlayerClass.IDs.None];
            Class_Secondary = Classes[(byte)PlayerClass.IDs.None];
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Active Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public ClassContainer Class_Primary { get; protected set; }
        public ClassContainer Class_Secondary { get; protected set; }

        public ClassContainer[] Classes_Active { get { return new ClassContainer[] { Class_Primary,Class_Secondary}; } }

        public readonly ClassContainer[] Classes;

        public byte Class_Count { get { return (byte)PlayerClass.IDs.NUMBER_OF_IDs; } }

        public class ClassContainer {
            public ClassContainer(byte id = (byte)PlayerClass.IDs.None) {
                Class = PlayerClass.LOOKUP[id];
            }

            public PlayerClass Class { get; protected set; }

            public byte Level { get; protected set; } = 0;
            public uint XP { get; protected set; } = 0;
            public bool Unlocked { get; protected set; } = false;

            public byte Level_Active { get; protected set; } = 0;

            public bool CanGainXP() {
                return ((Class.Tier > 0) && (Level < Class.Max_Level));
            }

            public float GetAllocationPoints() {
                if (Unlocked && (Level > 0) && (Class.Tier > 0) && (Class.Gives_Allocation_Attributes) && (Class.Enabled)) {
                    return Level * Attribute.ALLOCATION_POINTS_PER_LEVEL_TIERS[Class.Tier];
                }
                else {
                    return 0f;
                }
            }
        }

        public void ForceClass(byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            //TODO
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public AttributesContainer Attributes { get; private set; }
        public class AttributesContainer {
            public byte Count { get { return (byte)Attribute.IDs.NUMBER_OF_IDs; } }

            /// <summary>
            /// From allocated points (not synced)
            /// </summary>
            public int[] Allocated { get; protected set; } = new int[(byte)Attribute.IDs.NUMBER_OF_IDs];

            /// <summary>
            /// Dynamic zero point (not synced)
            /// </summary>
            public int Zero_Point { get; protected set; } = 0;

            /// <summary>
            /// From allocated points after applying zero point (not synced)
            /// </summary>
            public int[] Allocated_Effective { get; protected set; } = new int[(byte)Attribute.IDs.NUMBER_OF_IDs];

            /// <summary>
            /// From active class bonuses (not synced)
            /// </summary>
            public int[] From_Class { get; protected set; } = new int[(byte)Attribute.IDs.NUMBER_OF_IDs];

            /// <summary>
            /// Allocated_Effective + From_Class (synced)
            /// </summary>
            public int[] To_Sync { get; protected set; } = new int[(byte)Attribute.IDs.NUMBER_OF_IDs];

            /// <summary>
            /// Calculated on each update. Attributes from status, passive, etc. (deterministic)
            /// </summary>
            public int[] From_Other { get; protected set; } = new int[(byte)Attribute.IDs.NUMBER_OF_IDs];

            /// <summary>
            /// Calculated on each update. Equals To_Sync + From_Other (deterministic)
            /// </summary>
            public int[] Final { get; protected set; } = new int[(byte)Attribute.IDs.NUMBER_OF_IDs];

            public int Points_Available { get; protected set; } = 0;
            public int Points_Spent { get; protected set; } = 0;
            public int Points_Total { get; protected set; } = 0;

            /// <summary>
            /// Attempt to allocate attribute point
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool AllocatePoint(byte id) {
                //TODO
                return false;
            }

            public void Force(int[] attribute) {
                To_Sync = attribute;
                //TODO - anything needed?
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Custom Stats ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// Reset on each update cycle
        /// </summary>
        public StatsContainer Stats { get; private set; }
        public class StatsContainer {
            public bool Can_Use_Abilities = true;
            public bool Channelling = false;

            public float Healing = 1f; //TODO - unused

            public float Dodge = 0f; //TODO - unused

            public float Ability_Delay_Reduction = 1f; //TODO - unused

            public float SpeedAdjust_Melee = 0f; //TODO - unused
            public float SpeedAdjust_Ranged = 0f; //TODO - unused
            public float SpeedAdjust_Magic = 0f; //TODO - unused
            public float SpeedAdjust_Throwing = 0f; //TODO - unused
            public float SpeedAdjust_Minion = 0f; //TODO - unused
            public float SpeedAdjust_Weapon = 0f; //TODO - unused
            public float SpeedAdjust_Tool = 0f; //TODO - unused

            public float Damage_Holy = 0f; //TODO - unused
            public float Damage_Close = 0f; //TODO - unused
            public float Damage_NonMinionProjectile = 0f; //TODO - unused
            public float Damage_NonMinionAll = 0f; //TODO - unused

            public float AddDamageMult_Holy = 0f; //TODO - unused
            public float AddDamageMult_Close = 0f; //TODO - unused
            public float AddDamageMult_NonMinionProjectile = 0f; //TODO - unused
            public float AddDamageMult_NonMinionAll = 0f; //TODO - unused
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Character Level ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public CharacterContainer Character { get; private set; }
        public class CharacterContainer {
            public uint Level { get; protected set; } = 1;
            public uint XP { get; protected set; } = 0;

            /// <summary>
            /// True while player is AFK
            /// | Not synced between clients
            /// </summary>
            public bool AFK { get; protected set; } = false;

            /// <summary>
            /// True while in combat
            /// | Sync
            /// </summary>
            public bool In_Combat { get; protected set; } = false;

            /// <summary>
            /// Track boss kill
            /// </summary>
            public bool Defeated_WOF { get; protected set; } = false;

            /// <summary>
            /// Has unlocked subclass system
            /// </summary>
            public bool Secondary_Unlocked { get; protected set; } = false;

            /// <summary>
            /// True when player is the local player
            /// </summary>
            public bool Is_Local { get; protected set; } = false;

            public void ForceLevel(uint level) {
                if (Is_Local) {
                    Utilities.Logger.Error("ForceLevel called by local");
                }
                else {
                    Level = level;
                }
            }

            public void SetLocal() {
                Is_Local = true;
            }

            public void SetAFK(bool afk) {
                AFK = afk;
                if (Is_Local) {
                    if (AFK) {
                        Main.NewText("You are now AFK. You will not gain or lose XP.", UI.Constants.COLOUR_MESSAGE_ERROR);
                    }
                    else {
                        Main.NewText("You are no longer AFK. You can gain and lose XP again.", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    }
                }
            }

            public void SetInCombat(bool combat_state) {
                In_Combat = combat_state;
            }

            public void DefeatWOF() {
                if (Is_Local && !Defeated_WOF) {
                    Defeated_WOF = true;
                    Main.NewText("Tier III Requirement Met: Defeat Wall of Flesh", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    if (Systems.PlayerClass.LocalCanUnlockTier3()) {
                        Main.NewText("You can now unlock tier III classes!", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    }
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        //TODO

    }
}
