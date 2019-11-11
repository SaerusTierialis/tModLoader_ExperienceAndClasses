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
            CalculateFinal();
            for (byte i = 0; i < Count; i++) {
                //apply
                Attribute.LOOKUP[i].ApplyEffect(PSHEET.eacplayer, Final[i]);
            }
        }

        private void CalculateFinal() {
            for (byte i = 0; i < Count; i++) {
                Final[i] = Allocated_Effective[i] + From_Class[i] + Bonuses[i];
            }
        }

        public void UpdateFromClass() {
            From_Class = new int[Count];

            for (byte i = 0; i < Count; i++) {
                if (Attribute.LOOKUP[i].Active) {
                    From_Class[i] = Attribute.GetClassBonus(PSHEET, i);
                }
            }
        }

        /// <summary>
        /// Attempt to allocate attribute points. Returns true if any points where allocated.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool AllocatePoint(byte id, int points = 1) {
            //default
            bool any_allocated = false;

            while ((points > 0) && (Point_Costs[id] <= Points_Available)) {
                //add point
                Allocated[id]++;

                //one less point to add
                points--;

                //recalc points
                UpdatePointsAndEffective();

                //mark success
                any_allocated = true;
            }

            if (any_allocated) {
                //ui
                CalculateFinal();
                Shortcuts.UpdateUIPSheet(PSHEET);

                //sync
                SyncAttributesEffective();
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

        public void UpdatePointsAndEffective() {
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

            //recalc zero point
            Zero_Point = Attribute.CalculateZeroPoint(PSHEET);

            //apply zero point
            for (byte i = 0; i < Count; i++) {
                Allocated_Effective[i] = Allocated[i] - Zero_Point;
            }
        }

        public void Reset(bool allow_sync = true) {
            //clear allocated
            Allocated = new int[Count];

            //update
            UpdatePointsAndEffective();

            //ui
            CalculateFinal();
            Shortcuts.UpdateUIPSheet(PSHEET);

            //sync?
            if (allow_sync)
                SyncAttributesEffective();
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

            //calculate points
            UpdatePointsAndEffective();

            //calculate final
            CalculateFinal();

            //if too few points, reset allocations
            if (Points_Available < 0)
                Reset(false);
        }
    }
}
