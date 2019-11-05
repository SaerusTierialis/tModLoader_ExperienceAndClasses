using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class AttributeSheet : ContainerTemplate {
        public AttributeSheet(PSheet psheet) : base(psheet) { }

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
                Attribute.LOOKUP[i].ApplyEffect(PSHEET.eacplayer, Final[i]);
            }
        }

        public void UpdateFromClass() {
            From_Class = new int[Count];

            //TODO
            //use Level_Effective
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
                if (PSHEET.eacplayer.Fields.Is_Local)
                    Utilities.PacketHandler.Attributes.Send(-1, Shortcuts.WHO_AM_I, Allocated_Effective);
                else
                    Utilities.Logger.Error("SendPacketAttributesEffective called by non-local");
            }
        }

        public void UpdatePoints() {
            //calculate spent + update costs for next point
            Points_Spent = 0;
            for (byte i = 0; i < Count; i++) {
                Point_Costs[i] = Attribute.AllocationPointCost(Allocated[i]);
                Points_Spent += Attribute.AllocationPointCostTotal(Allocated[i]);
            }

            //calculate total points available
            Points_Total = Attribute.LocalAllocationPointTotal(PSHEET);

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
            for (byte i = 0; i < Count; i++) {
                Allocated_Effective[i] = Allocated[i] - Zero_Point;
            }
        }

        public void ForceAllocatedEffective(int[] attribute) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("ForceAllocatedEffective called by local");
            }
            else {
                Allocated_Effective = attribute;
            }
        }

        public TagCompound Save(TagCompound tag) {
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Attributes_Allocated, Allocated);
            return tag;
        }
        public void Load(TagCompound tag) {
            Allocated = Utilities.Commons.TagLoadListAsArray<int>(tag, TAG_NAMES.Attributes_Allocated, Count);

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
}
