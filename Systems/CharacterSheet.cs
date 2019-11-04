using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems {
    public class CharacterSheet {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public readonly EACPlayer eacplayer;
        public CharacterSheet(EACPlayer owner) {
            eacplayer = owner;
            Attributes = new AttributesContainer(this);
            Stats = new StatsContainer(this);
            Character = new CharacterMethods(this);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Classes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes + Points ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public readonly AttributesContainer Attributes;
        public class AttributesContainer : ContainerTemplate {
            public AttributesContainer(CharacterSheet csheet) : base(csheet) { }

            public static byte Count { get { return (byte)Attribute.IDs.NUMBER_OF_IDs; } }

            /// <summary>
            /// From allocated points
            /// | not synced
            /// </summary>
            public int[] Allocated { get; protected set; } = new int[Count];

            /// <summary>
            /// Dynamic zero point
            /// | not synced
            /// </summary>
            public int Zero_Point { get; protected set; } = 0;

            /// <summary>
            /// From allocated points after applying zero point
            /// | synced
            /// </summary>
            public int[] Allocated_Effective { get; protected set; } = new int[Count];

            /// <summary>
            /// From active class bonuses
            /// | not synced (deterministic)
            /// </summary>
            public int[] From_Class { get; protected set; } = new int[Count];


            /// <summary>
            /// Calculated on each update. Attributes from status, passive, etc.
            /// | not synced (deterministic)
            /// </summary>
            public int[] Bonuses = new int[Count];

            /// <summary>
            /// Calculated on each update. Equals Allocated_Effective + From_Class + Bonuses
            /// | not synced (deterministic)
            /// </summary>
            public int[] Final { get; protected set; } = new int[Count];

            public int[] Point_Costs { get; protected set; } = new int[Count];
            public int Points_Available { get; protected set; } = 0;
            public int Points_Spent { get; protected set; } = 0;
            public int Points_Total { get; protected set; } = 0;

            /// <summary>
            /// Reset bonus attributes (to be called before each update)
            /// </summary>
            public void ResetBonuses() {
                Bonuses = new int[Count];
            }

            /// <summary>
            /// Apply attribute effects (to be called on each update)
            /// </summary>
            public void Apply() {
                for (byte i = 0; i < Count; i++) {
                    //calculate
                    Final[i] = Allocated_Effective[i] + From_Class[i] + Bonuses[i];
                    //apply
                    Attribute.LOOKUP[i].ApplyEffect(CSheet.eacplayer, Final[i]);
                }
            }

            public void UpdateFromClass() {
                //TODO
                From_Class = new int[Count];
            }

            /// <summary>
            /// Attempt to allocate attribute point
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool AllocatePoint(byte id) {
                //can allocate?
                bool success = Point_Costs[id] <= Points_Available;

                if (success) {
                    //add point
                    Allocated[id]++;

                    //update effective values
                    UpdateAllocatedEffective();

                    //recalc points
                    UpdatePoints();

                    //sync
                    SyncAttributesEffective();
                }

                return success;
            }

            private void SyncAttributesEffective() {
                if (Shortcuts.IS_CLIENT) {
                    if (CSheet.eacplayer.Fields.Is_Local)
                        Utilities.PacketHandler.Attributes.Send(-1, Shortcuts.WHO_AM_I, Allocated_Effective);
                    else
                        Utilities.Logger.Error("SendPacketAttributesEffective called by non-local");
                }
            }

            private void UpdatePoints() {
                //calculate spent + update costs for next point
                Points_Spent = 0;
                for (byte i = 0; i < Count; i++) {
                    Point_Costs[i] = Attribute.AllocationPointCost(Allocated[i]);
                    Points_Spent += Attribute.AllocationPointCostTotal(Allocated[i]);
                }

                //calculate total points available
                Points_Total = Attribute.LocalAllocationPointTotal(CSheet);

                //calculte remaining points
                Points_Available = Points_Total - Points_Spent;
            }

            private void Reset(bool allow_sync = true) {
                //clear allocated
                Allocated = new int[Count];

                //update
                UpdateAllocatedEffective();
                UpdatePoints();

                //sync?
                if (allow_sync)
                    SyncAttributesEffective();
            }

            private void UpdateAllocatedEffective() {
                //calculate average allocated
                float sum = 0;
                float count = 0;
                for (byte i = 0; i < Count; i++) {
                    if (Attribute.LOOKUP[i].Active) {
                        sum += Allocated[i];
                        count++;
                    }
                }
                float average = sum / count;

                //recalc zero point
                Zero_Point = Attribute.CalculateZeroPoint(average);

                //apply zero point
                for (byte i = 0; i < Count; i ++) {
                    Allocated_Effective[i] = Allocated[i] - Zero_Point;
                }
            }

            public void ForceAllocatedEffective(int[] attribute) {
                if (CSheet.eacplayer.Fields.Is_Local) {
                    Utilities.Logger.Error("ForceAllocatedEffective called by local");
                }
                else {
                    Allocated_Effective = attribute;
                }
            }

            public TagCompound Save(TagCompound tag) {
                tag = TagAddArrayAsList<int>(tag, TAG_NAMES.Attributes_Allocated, Allocated);
                return tag;
            }
            public void Load(TagCompound tag) {
                Allocated = TagLoadListAsArray<int>(tag, TAG_NAMES.Attributes_Allocated, Count);

                //unallocate any attribute that is no longer active
                for (byte i = 0; i < Count; i++) {
                    if (!Attribute.LOOKUP[i].Active) {
                        Allocated[i] = 0;
                    }
                }

                //calculate effective allocated
                UpdateAllocatedEffective();

                //calculate points
                UpdatePoints();

                //if too few points, reset allocations
                if (Points_Available < 0)
                    Reset(false);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Custom Stats ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// Reset on each update cycle
        /// </summary>
        public readonly StatsContainer Stats;
        public class StatsContainer : ContainerTemplate {
            public StatsContainer(CharacterSheet csheet) : base(csheet) {
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Character ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public readonly CharacterMethods Character;
        public class CharacterMethods : ContainerTemplate {
            public CharacterMethods(CharacterSheet csheet) : base(csheet) { }

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

            public void ForceLevel(byte level) {
                if (CSheet.eacplayer.Fields.Is_Local) {
                    Utilities.Logger.Error("ForceLevel called by local");
                }
                else {
                    Level = level;
                    OnLevelChange();
                }
            }

            public void SetAFK(bool afk) {
                AFK = afk;
                if (CSheet.eacplayer.Fields.Is_Local) {
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
                if (CSheet.eacplayer.Fields.Is_Local && !Defeated_WOF) {
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
                XP = Utilities.Commons.SafeAdd(XP, xp);
                LocalHandleXPChange();
            }

            public void LocalSubtractXP(uint xp) {
                XP = Utilities.Commons.SafeSubtract(XP, xp);
                LocalHandleXPChange();
            }

            private void LocalHandleXPChange() {
                UpdateXPLevel();
                bool leveled = false;

                while (XP_Level_Remaining == 0 && (Level < Systems.XP.MAX_LEVEL)) {
                    Level = Utilities.Commons.SafeAdd(Level, 1);
                    XP = Utilities.Commons.SafeSubtract(XP, XP_Level_Total);
                    UpdateXPLevel();
                    leveled = true;
                }

                if (leveled) {
                    if (Shortcuts.IS_CLIENT)
                        Utilities.PacketHandler.CharLevel.Send(-1, Shortcuts.WHO_AM_I, Level, true);
                    else
                        Main.NewText(GetLevelupMessage(Main.LocalPlayer.name), UI.Constants.COLOUR_MESSAGE_SUCCESS);

                    OnLevelChange();
                }

                Main.NewText(Level + " | " + XP + " | " + XP_Level_Remaining + " / " + XP_Level_Total);

            }

            private void OnLevelChange() {
                //TODO
            }

            private void UpdateXPLevel() {
                XP_Level_Total = Systems.XP.Requirements.GetXPReqCharacter(Level);
                XP_Level_Remaining = Utilities.Commons.SafeSubtract(XP_Level_Total, XP);
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

                UpdateXPLevel();
            }

            public string GetLevelupMessage(string name) {
                return name + " " + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Levelup_Character") + " " + Level + "!";
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void PreUpdate() {
            Attributes.ResetBonuses();
            Stats.Reset();
        }

        public void PostUpdate() {
            Attributes.Apply();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private static class TAG_NAMES {
            public static string PREFIX = "eac_";

            //Class
            //TODO

            //Attribute Allocations
            public static string Attributes_Allocated = PREFIX + "attribute_allocation";

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
            Attributes.Load(tag);

            //Character
            Character.Load(tag);
        }

        public TagCompound Save(TagCompound tag) {
            //Class
            //TODO

            //Attribute Allocations
            tag = Attributes.Save(tag);

            //Character
            tag = Character.Save(tag);

            return tag;
        }


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Container Template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public abstract class ContainerTemplate {
            protected readonly CharacterSheet CSheet;
            public ContainerTemplate(CharacterSheet csheet) {
                CSheet = csheet;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Helper Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected static TagCompound TagAddArrayAsList<T>(TagCompound tag, string name, T[] array) {

            //convert to list
            List<T> list = new List<T>();
            foreach (T value in array) {
                list.Add(value);
            }

            //add list
            tag.Add(name, list);

            //return
            return tag;
        }

        protected static T[] TagLoadListAsArray<T>(TagCompound tag, string name, int length) {
            //load list
            List<T> list = Utilities.Commons.TryGet<List<T>>(tag, name, new List<T>());

            //warn if list is too long
            if (length < list.Count) {
                Utilities.Logger.Error("Error loading " + name + ". Loaded array is too long. Excess entries will be lost.");
            }

            //create array (if list was too long, produce a larger array in case this helps prevent data loss)
            T[] array = new T[Math.Max(length, list.Count)];

            //populate array
            for (int i = 0; i < list.Count; i++) {
                array[i] = list[i];
            }

            //return
            return array;
        }

    }
}
