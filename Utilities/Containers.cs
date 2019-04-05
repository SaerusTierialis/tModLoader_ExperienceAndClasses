using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Utilities.Containers {
    /// <summary>
    /// A container for loaded ui information
    /// </summary>
    public struct LoadedUIData {
        public readonly float LEFT, TOP;
        public readonly bool AUTO;

        public LoadedUIData(float left = 0f, float top = 0f, bool auto = true) {
            LEFT = left;
            TOP = top;
            AUTO = auto;
        }
    }

    /// <summary>
    /// Sorts by time_end with later times at the end of list. Supports duplicate time_end (toggle duration status all have same time_end)
    /// </summary>
    public class TimeSortedStatusList : List<Systems.Status> {

        public TimeSortedStatusList() {}
        public TimeSortedStatusList(int capacity) : base(capacity) {}

        /// <summary>
        /// Adds status sorting by end time
        /// </summary>
        /// <param name="status"></param>
        public new void Add(Systems.Status status) {
            //is full?
            if (Count == Capacity) {
                Commons.Error("A status icon cannot be displayed because there are not enough slots!");
                return;
            }

            //insert before first status that is equal/later
            for (int i=0; i<Count; i++) {
                if (status.Time_End.CompareTo(this.ElementAt<Systems.Status>(i).Time_End) <= 0) {
                    Insert(i, status);
                    return;
                }
            }
            //default to insert at end
            Add(status);
        }
    }

    /// <summary>
    /// Can contain a player or an npc.
    /// Maintains a SortedDictionary of all things sorted by their Index.
    /// Index is identical for clients/server so this can be references in sync
    /// </summary>
    public class Thing {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static SortedDictionary<ushort, Thing> Things { get; private set; } = new SortedDictionary<ushort, Thing>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public readonly bool Is_Player;
        private readonly MPlayer mplayer;
        public readonly bool Is_Npc;
        private readonly MNPC mnpc;

        /// <summary>
        /// A reference index which is identical across clients/server. This Thing is Thing[Index].
        /// </summary>
        public readonly ushort Index;

        /// <summary>
        /// In singleplayer, all things are local.
        /// 
        /// To the server, all NPC are local.
        /// To the clients, their own player is local (not NPCs).
        /// </summary>
        public readonly bool Local;

        /// <summary>
        /// Update status front visuals on next cycle
        /// </summary>
        public bool needs_update_status_visuals_front = false;

        /// <summary>
        /// Update status back visuals on next cycle
        /// </summary>
        public bool needs_update_status_visuals_back = false;

        public Thing(MPlayer mplayer) {
            Is_Player = true;
            Is_Npc = false;

            this.mplayer = mplayer;
            mnpc = null;

            Index = (ushort)whoAmI;

            if ((Netmode.IS_SINGLEPLAYER) || (Netmode.IS_CLIENT && (whoAmI == Main.LocalPlayer.whoAmI))) {
                //singleplayer OR this is the local player on a client
                Local = true;
            }
            else {
                Local = false;
            }

            Add();
        }

        public Thing(MNPC mnpc) {
            Is_Player = false;
            Is_Npc = true;

            mplayer = null;
            this.mnpc = mnpc;

            Index = (ushort)(Main.maxPlayers + whoAmI);

            if (!Netmode.IS_CLIENT) {
                //singleplayer OR this is an npc on the server
                Local = true;
            }
            else {
                Local = false;
            }

            Add();
        }

        /// <summary>
        /// Add to Things
        /// </summary>
        private void Add() {
            if (Things.ContainsKey(Index)) {
                Things[Index] = this;
            }
            else {
                Things.Add(Index, this);
            }
        }

        public MPlayer MPlayer {
            get {
                if (mplayer == null) {
                    Commons.Error("Attempted to get MPlayer from non-player Thing!");
                    return null;
                }
                else {
                    return mplayer;
                }
            }
        }

        public MNPC MNPC {
            get {
                if (mnpc == null) {
                    Commons.Error("Attempted to get MNPC from non-NPC Thing!");
                    return null;
                }
                else {
                    return mnpc;
                }
            }
        }

        private void Remove() {
            Things.Remove(Index);
        }

        /// <summary>
        /// Returns the StatusList of the thing
        /// </summary>
        public StatusList Statuses {
            get {
                if (Is_Player) {
                    return mplayer.Statuses;
                }
                else {
                    return mnpc.Statuses;
                }
            }
        }

        /// <summary>
        /// Checks if the thing has the status
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasStatus(Systems.Status.IDs id) {
            return Statuses.Contains(id);
        }

        /// <summary>
        /// Returns the player or npc index (player.whoAmI or npc.whoAmI)
        /// </summary>
        public int whoAmI {
            get {
                if (Is_Player) {
                    return mplayer.player.whoAmI;
                }
                else {
                    int who = mnpc.npc.whoAmI;
                    if (who < 0) {
                        Remove();
                    }
                    return who;
                }
            }
        }

        /// <summary>
        /// Returns whether the thing is alive. If an NPC is found to be dead, it is removed from Things.
        /// </summary>
        public bool Dead {
            get {
                if (Is_Player) {
                    return mplayer.player.dead;
                }
                else {
                    bool dead = !mnpc.npc.active;
                    if (dead) {
                        Remove();
                    }
                    return dead;
                }
            }
        }

        /// <summary>
        /// Returns whether the thing is active. If not, the thing is removed from Things.
        /// </summary>
        public bool Active {
            get {
                bool active;
                if (Is_Player) {
                    active = mplayer.player.active;
                }
                else {
                    active = mnpc.npc.active;
                }
                if (!active) {
                    Remove();
                }
                return active;
            }
        }

        /// <summary>
        /// Checks if the things are the same thing :)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Thing other) {
            return (Index == other.Index);
        }

        public void Heal(uint amount) {
            //TODO
        }

        public void Hurt(uint amount, Thing source) {
            //TODO
        }

        /// <summary>
        /// Called at the end of ProcessStatuses so "status.applied" is accurate
        /// </summary>
        public void StatusUpdateVisuals() {
            if (needs_update_status_visuals_front) {
                //TODO
            }

            if (needs_update_status_visuals_back) {
                //TODO
            }

            needs_update_status_visuals_front = false;
            needs_update_status_visuals_back = false;
        }

        /// <summary>
        /// Call on every cycle
        /// </summary>
        /// <param name="statuses"></param>
        public void ProcessStatuses() {
            //update every instance of every status (may remove some)
            foreach (Systems.Status status in Statuses.GetAll()) {
                status.Update();
            }

            //do effects
            List<Systems.Status> apply = Statuses.GetAllApply();
            foreach (Systems.Status status in apply) {
                status.DoEffect();
            }

            //update visuals
            StatusUpdateVisuals();
        }
    }

    /// <summary>
    /// A container for status instances on a single target.
    /// </summary>
    public class StatusList {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public const byte UNASSIGNED_INSTANCE_KEY = byte.MaxValue;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private SortedDictionary<Systems.Status.IDs, StatusInstances> statuses = new SortedDictionary<Systems.Status.IDs, StatusInstances>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Check if there are any instances of the specified type
        /// </summary>
        /// <param name="status_id"></param>
        /// <returns></returns>
        public bool Contains(Systems.Status.IDs status_id) {
            return statuses.ContainsKey(status_id);
        }

        /// <summary>
        /// Get status by type and instance id
        /// </summary>
        /// <param name="status_id"></param>
        /// <param name="instance_id"></param>
        /// <returns></returns>
        public Systems.Status Get(Systems.Status.IDs status_id, byte instance_id) {
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances))
                return status_instances.GetStatus(instance_id);
            else
                return null;
        }

        /// <summary>
        /// Add status. Will create a new StatusInstances if needed. Will assign instance id if not set.
        /// Instance IDs are assigned following LIMIT_TYPES
        /// </summary>
        /// <param name="status"></param>
        public void Add(Systems.Status status) {
            //add StatusInstances if there are no other instances of the status
            if (!Contains(status.ID)) {
                statuses.Add(status.ID, new StatusInstances(status));
            }

            //get the StatusInstances
            StatusInstances status_instances;
            if (statuses.TryGetValue(status.ID, out status_instances))
                //add the status
                status_instances.AddStatus(status);
            else
                Commons.Error("Failed to create StatusInstances for new status " + status.Specific_Name);
        }

        /// <summary>
        /// Remove status directly
        /// </summary>
        /// <param name="status"></param>
        public void Remove(Systems.Status status) {
            Remove(status.ID, status.Instance_ID);
        }

        /// <summary>
        /// Remove status by type and instance id
        /// </summary>
        /// <param name="status_id"></param>
        /// <param name="instance_id"></param>
        public void Remove(Systems.Status.IDs status_id, byte instance_id) {
            //get the StatusInstances
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances)) {
                //remove the status from instances
                status_instances.RemoveStatus(instance_id);
                //remove the StatusInstances if no more instances
                if (status_instances.IsEmpty()) {
                    //no more instances so remove the status_instances too
                    statuses.Remove(status_id);
                }
            }
            else
                Commons.Error("Failed to find StatusInstances for removing status " + status_id + " (instance " + instance_id + ")");
        }

        /// <summary>
        /// Remove all instances of specified status
        /// </summary>
        /// <param name="status_id"></param>
        public void RemoveAll(Systems.Status.IDs status_id) {
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances)) {
                foreach (Systems.Status status in status_instances.GetInstances()) {
                    status.RemoveEverywhere();
                }
            }
        }

        /// <summary>
        /// Remove all chanelling statuses
        /// </summary>
        public void RemoveChannelling() {
            foreach (Systems.Status.IDs id in statuses.Keys) {
                if (Systems.Status.LOOKUP[(ushort)id].Specific_Target_Channelling) {
                    RemoveAll(id);
                }
            }
        }

        /// <summary>
        /// Remove all sync instances in preparation for repopulating from full sync status list
        /// </summary>
        public void RemoveAllSync(Thing target) {
            //directly remove all instances on sync statuses (doesn't sync, OnEnd, etc.)
            foreach (Systems.Status.IDs id in statuses.Keys) {
                if (!Systems.Status.LOOKUP[(ushort)id].Specific_Syncs) {
                    statuses.Remove(id);
                }
            }
            if (!Utilities.Netmode.IS_SERVER) {
                if (target.Local) {
                    //need to update ui
                    UI.UIStatus.needs_redraw_complete = true;
                }
                //need to update visuals
                target.needs_update_status_visuals_front = true;
                target.needs_update_status_visuals_back = true;
            }
        }

        /// <summary>
        /// Returns list of instances for each active status (empty if no statuses)
        /// </summary>
        /// <returns></returns>
        public List<Systems.Status> GetAll() {
            List<Systems.Status> list = new List<Systems.Status>();
            foreach (StatusInstances status_type in statuses.Values) {
                list.AddRange(status_type.GetInstances());
            }
            return list;
        }

        /// <summary>
        /// Returns list of instances for specified status type (empty if no instances)
        /// </summary>
        /// <param name="status_id"></param>
        /// <returns></returns>
        public List<Systems.Status> GetAllOfType(Systems.Status.IDs status_id) {
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances))
                return status_instances.GetInstances();
            else
                return new List<Systems.Status>();
        }

        /// <summary>
        /// Returns list of instances that will apply their effect following the APPLY_TYPES rule of each status type
        /// </summary>
        /// <returns></returns>
        public List<Systems.Status> GetAllApply() {
            List<Systems.Status> instances, list = new List<Systems.Status>();
            SortedDictionary<ushort, Systems.Status> bests;
            ushort key;
            Systems.Status best;
            foreach (StatusInstances status_type in statuses.Values) {
                instances = status_type.GetInstances();

                switch (instances[0].Specific_Apply_Type) {
                    case Systems.Status.APPLY_TYPES.ALL:
                        //include all
                        list.AddRange(instances);
                        break;

                    case Systems.Status.APPLY_TYPES.BEST:
                        //include single best instance
                        best = instances[0];
                        foreach (Systems.Status status in instances) {
                            if (status.IsBetterThan(best)) {
                                best = status;
                            }
                        }
                        list.Add(best);
                        break;

                    case Systems.Status.APPLY_TYPES.BEST_PER_OWNER:
                        //include best per owner
                        bests = new SortedDictionary<ushort, Systems.Status>();
                        foreach (Systems.Status status in instances) {
                            key = status.Owner.Index;
                            if (bests.TryGetValue(key, out best)) {
                                if (status.IsBetterThan(best)) {
                                    bests[key] = status;
                                }
                            }
                            else {
                                bests.Add(key, status);
                            }
                        }
                        list.AddRange(bests.Values);

                        break;

                    default:
                        Utilities.Commons.Error("Unsupported APPLY_TYPES:" + instances[0].Specific_Apply_Type);
                        break;
                }

            }
            return list;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ StatusInstances ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// Stores all instances of a single status type on a target. Statuses are assigned an instance id.
        /// 
        /// Instances are stored in a storted dictionary for fast lookup and to allow all instances to be returned as a list without needing to select from an array.
        /// 
        /// The keys for the stored list correspond with indices in a bool[]. This was done instead of a queue/stack of unique values because a default 
        /// bool[] can be constructed more quickly. However, adding instances when there are already several is slower with the array method because it
        /// must search through the array for the first available key index.
        /// It is expected that targets will generally have just one instance of a status at a time so initialization time is more important than efficiency
        /// while near capacity.
        /// </summary>
        private class StatusInstances {
            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
            private SortedDictionary<byte, Systems.Status> instances;
            private bool[] key_taken;
            private Systems.Status.LIMIT_TYPES limit_type;

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            public StatusInstances(Systems.Status reference) {
                //type
                limit_type = reference.Specific_Limit_Type;

                //set capacity (save some space if LIMIT_TYPES.ONE)
                byte capacity;
                if (limit_type == Systems.Status.LIMIT_TYPES.ONE) {
                    capacity = 1;
                }
                else {
                    capacity = UNASSIGNED_INSTANCE_KEY;
                }
                key_taken = new bool[capacity];
                instances = new SortedDictionary<byte, Systems.Status>();
            }

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            /// <summary>
            /// Return all active instances
            /// </summary>
            /// <returns></returns>
            public List<Systems.Status> GetInstances() {
                return instances.Values.ToList();
            }

            /// <summary>
            /// Get status by id
            /// </summary>
            /// <param name="instance_id"></param>
            /// <returns></returns>
            public Systems.Status GetStatus(byte instance_id) {
                Systems.Status status;
                if (instances.TryGetValue(instance_id, out status))
                    return status;
                else
                    return null;
            }

            /// <summary>
            /// Add status. Instance id will be assigned if not already set.
            /// Keys are assigned following LIMIT_TYPES
            /// </summary>
            /// <param name="status"></param>
            public void AddStatus(Systems.Status status) {
                if (status.Instance_ID == UNASSIGNED_INSTANCE_KEY) {
                    //assigns new key and then adds/replaces (merges if needed)
                    AddNewStatus(status);
                }
                else {
                    //already has an instance id (is from another client)
                    if (instances.ContainsKey(status.Instance_ID)) {
                        //replace existing instance
                        instances[status.Instance_ID] = status;
                    }
                    else {
                        //add another instance
                        instances.Add(status.Instance_ID, status);
                    }

                    //be certain that key is marked as taken
                    key_taken[status.Instance_ID] = true;
                }
            }

            /// <summary>
            /// Removes instance by id
            /// </summary>
            /// <param name="instance_id"></param>
            /// <returns></returns>
            public void RemoveStatus(byte instance_id) {
                //remove status
                instances.Remove(instance_id);

                //make key available
                key_taken[instance_id] = false;
            }

            public bool IsEmpty() {
                return (instances.Count == 0);
            }

            /// <summary>
            /// Assigns a key and adds the instance. Keys are assigned following LIMIT_TYPES
            /// </summary>
            /// <param name="status"></param>
            /// <returns></returns>
            private void AddNewStatus(Systems.Status status) {
                if (status.Instance_ID != UNASSIGNED_INSTANCE_KEY) {
                    //status already has key
                    Utilities.Commons.Error("AddNewStatus called incorrectly!");
                    return;
                }
                else {

                    int key_temp = -1;

                    switch (limit_type) {
                        case Systems.Status.LIMIT_TYPES.MANY:
                            //no limit on instances
                            //find first available key (any key is fine)
                            key_temp = Array.FindIndex<bool>(key_taken, i => i == false);
                            if (key_temp < 0) {
                                //no more room - cannot add status!
                                Utilities.Commons.Error("Status instance limit reached for [" + status.ID + "] (this probably means that something went wrong, please report)");
                                return;
                            }
                            break;

                        case Systems.Status.LIMIT_TYPES.ONE:
                            //there can be only one instance
                            //all instances use key 0
                            key_temp = 0;
                            break;

                        case Systems.Status.LIMIT_TYPES.ONE_PER_OWNER:
                            //there can be one instance per owner (players and/or npcs)
                            //get current key if any instances from same owner (could be npc-owned so can't just use index as key)
                            foreach (Systems.Status s in instances.Values) {
                                if (status.Owner.Equals(s.Owner)) {
                                    key_temp = s.Instance_ID;
                                    break;
                                }
                            }
                            //if no instances from same owner...
                            if (key_temp == -1) {
                                //find first available key (any key is fine)
                                key_temp = Array.FindIndex<bool>(key_taken, i => i == false);
                                if (key_temp < 0) {
                                    //no more room - cannot add status!
                                    Utilities.Commons.Error("Status instance limit reached for [" + status.ID + "] (this probably means that something went wrong, please report)");
                                    return;
                                }
                            }
                            break;

                        default:
                            //something not implemented
                            Utilities.Commons.Error("Unsupported limit type: " + limit_type);
                            return;
                    }

                    //key
                    byte key = (byte)key_temp;

                    //replace/merge if there is existing
                    if (instances.ContainsKey(key)) {
                        Systems.Status existing;
                        if (instances.TryGetValue(key, out existing)) {
                            //merge
                            if (existing.Specific_Allow_Merge) {
                                //try to merge, will sync if anything changed
                                existing.Merge(status);
                                //even if merge does nothing, this status does not need to be added (either it's worse or it's redundant)
                                return;
                            }
                            else {
                                //remove existing (merge not allowed)
                                existing.RemoveEverywhere();
                            }
                        }
                        else {
                            Utilities.Commons.Error("Failed to get existing status: " + status.Specific_Name);
                        }
                    }

                    //assign key and add status
                    status.SetInstanceID(key);
                    key_taken[key_temp] = true;
                    instances.Add(status.Instance_ID, status);
                    return;
                }
            }
        }
    }
}
