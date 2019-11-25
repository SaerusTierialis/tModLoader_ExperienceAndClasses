using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class AttributeSheet : ContainerTemplate {
        public AttributeSheet(PSheet psheet) : base(psheet) { }

        /// <summary>
        /// From allocated points
        /// | not synced
        /// </summary>
        public int[] Allocated { get; protected set; } = new int[Attribute.Count];

        /// <summary>
        /// Dynamic zero point
        /// | not synced
        /// </summary>
        public int Zero_Point { get; protected set; } = 0;

        /// <summary>
        /// From allocated points after applying zero point
        /// | synced
        /// </summary>
        public int[] Allocated_Effective { get; protected set; } = new int[Attribute.Count];

        /// <summary>
        /// From active class bonuses
        /// | not synced (deterministic)
        /// </summary>
        public int[] From_Class { get; protected set; } = new int[Attribute.Count];


        /// <summary>
        /// Calculated on each update. Attributes from status, passive, etc.
        /// | not synced (deterministic)
        /// </summary>
        public int[] Bonuses = new int[Attribute.Count];

        /// <summary>
        /// Calculated on each update. Equals Allocated_Effective + From_Class + Bonuses
        /// | not synced (deterministic)
        /// </summary>
        public int[] Final { get; protected set; } = new int[Attribute.Count];

        public int[] Point_Costs { get; protected set; } = new int[Attribute.Count];
        public int Points_Available { get; protected set; } = 0;
        public int Points_Spent { get; protected set; } = 0;
        public int Points_Total { get; protected set; } = 0;

        public PowerScaling Power_Scaling { get; protected set; } = PowerScaling.LOOKUP[PowerScaling.ID_NUM_DEFAULT];

        /// <summary>
        /// Reset bonus attributes (to be called before each update)
        /// </summary>
        public void ResetBonuses() {
            Bonuses = new int[Attribute.Count];
        }

        /// <summary>
        /// Apply attribute effects (to be called on each update)
        /// </summary>
        public void Apply(bool do_effects = true) {
            CalculateFinal();
            for (byte i = 0; i < Attribute.Count; i++) {
                //apply
                Attribute.LOOKUP[i].ApplyEffect(PSHEET.eacplayer, Final[i], do_effects);
            }
        }

        private void CalculateFinal() {
            for (byte i = 0; i < Attribute.Count; i++) {
                Final[i] = Allocated_Effective[i] + From_Class[i] + Bonuses[i];
            }
        }

        public void UpdateFromClass() {
            From_Class = new int[Attribute.Count];

            for (byte i = 0; i < Attribute.Count; i++) {
                if (Attribute.LOOKUP[i].Active) {
                    From_Class[i] = Attribute.GetClassBonus(PSHEET, i);
                }
            }

            CalculateFinal();
        }

        /// <summary>
        /// Attempt to allocate attribute points. Returns true if any points where allocated.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool AllocatePoint(byte id, int points = 1) {
            //default
            bool any_allocated = false;

            //allow allocate
            if (PSHEET.Character.In_Combat) {
                Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Attribute_Allocate_InCombat"), UI.Constants.COLOUR_MESSAGE_ERROR);
                return false;
            }

            while ((points > 0) && (Point_Costs[id] <= Points_Available)) {
                //add point
                Allocated[id]++;

                //one less point to add
                points--;

                //recalc points
                LocalUpdatePointsAndEffective();

                //mark success
                any_allocated = true;
            }

            if (any_allocated) {
                //ui
                CalculateFinal();
                Shortcuts.UpdateUIPSheet(PSHEET);

                //sync
                SyncAttributesEffective();

                //destroy minions
                PSHEET.eacplayer.LocalDestroyMinions();
            }

            return any_allocated;
        }

        private void SyncAttributesEffective() {
            if (Shortcuts.IS_CLIENT) {
                if (PSHEET.eacplayer.Fields.Is_Local)
                    Utilities.PacketHandler.Attributes.Send(-1, Shortcuts.WHO_AM_I, Allocated_Effective);
                else
                    Utilities.Logger.Error("SendPacketAttributesEffective called by non-local");
            }
        }

        public void LocalUpdatePointsAndEffective() {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                //calculate spent + update costs for next point
                Points_Spent = 0;
                for (byte i = 0; i < Attribute.Count; i++) {
                    Point_Costs[i] = Attribute.AllocationPointCost(Allocated[i]);
                    Points_Spent += Attribute.AllocationPointCostTotal(Allocated[i]);
                }

                //calculate total points available
                Points_Total = Attribute.LocalAllocationPointTotal(PSHEET);

                //calculte remaining points
                Points_Available = Points_Total - Points_Spent;

                //recalc zero point
                Zero_Point = Attribute.CalculateZeroPoint(PSHEET);

                //apply zero point
                for (byte i = 0; i < Attribute.Count; i++) {
                    Allocated_Effective[i] = Allocated[i] - Zero_Point;
                }
            }
            else {
                Utilities.Logger.Error("LocalUpdatePointsAndEffective called by non-local");
            }
        }

        public void LocalPowerScalingNext() {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                if (PSHEET.Character.In_Combat) {
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.PowerScaling_SetFail_InCombat"), UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Power_Scaling = Power_Scaling.GetNext();
                    OnLocalPowerScalingChange();
                }
            }
            else {
                Utilities.Logger.Error("LocalPowerScalingNext called by non local");
            }
        }

        public void LocalPowerScalingPrior() {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                if (PSHEET.Character.In_Combat) {
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.PowerScaling_SetFail_InCombat"), UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Power_Scaling = Power_Scaling.GetPrior();
                    OnLocalPowerScalingChange();
                }
            }
            else {
                Utilities.Logger.Error("LocalPowerScalingPrior called by non local");
            }
        }

        private void OnLocalPowerScalingChange() {
            if (Shortcuts.IS_CLIENT) Utilities.PacketHandler.PowerScaling.Send(-1, Shortcuts.WHO_AM_I, Power_Scaling.ID_num);

            //destroy minions
            PSHEET.eacplayer.LocalDestroyMinions();

            //update ui
            Shortcuts.UpdateUIPSheet(PSHEET);
        }

        public void Reset(bool allow_sync = true) {
            //clear allocated
            Allocated = new int[Attribute.Count];

            //update
            LocalUpdatePointsAndEffective();

            //ui
            CalculateFinal();
            Shortcuts.UpdateUIPSheet(PSHEET);

            //sync?
            if (allow_sync)
                SyncAttributesEffective();

            //destroy minions
            PSHEET.eacplayer.LocalDestroyMinions();
        }

        public void ForceAllocatedEffective(int[] attribute) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("ForceAllocatedEffective called by local");
            }
            else {
                Allocated_Effective = attribute;
            }
        }

        public void ForcePowerScaling(byte id_num) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("ForcePowerScaling called by local");
            }
            else {
                Power_Scaling = PowerScaling.LOOKUP[id_num];
            }
        }

        public TagCompound Save(TagCompound tag) {
            tag.Add(TAG_NAMES.Attributes_PowerScaling, Power_Scaling.ID_num);
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Attributes_Allocated, Allocated);
            return tag;
        }
        public void Load(TagCompound tag) {
            Power_Scaling = PowerScaling.LOOKUP[Utilities.Commons.TagTryGet(tag, TAG_NAMES.Attributes_PowerScaling, PowerScaling.ID_NUM_DEFAULT)];

            Allocated = Utilities.Commons.TagLoadListAsArray<int>(tag, TAG_NAMES.Attributes_Allocated, Attribute.Count);

            //unallocate any attribute that is no longer active
            for (byte i = 0; i < Attribute.Count; i++) {
                if (!Attribute.LOOKUP[i].Active) {
                    Allocated[i] = 0;
                }
            }

            //calculate points
            LocalUpdatePointsAndEffective();

            //calculate final
            CalculateFinal();

            //if too few points, reset allocations
            if (Points_Available < 0)
                Reset(false);
        }
    }
}
