using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// A container for status instances on a single target.
    /// </summary>
    public class StatusList {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public const byte UNASSIGNED_INSTANCE_KEY = byte.MaxValue;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private SortedList<Systems.Status.IDs, StatusInstances> statuses = new SortedList<Systems.Status.IDs, StatusInstances>((int)Systems.Status.IDs.NUMBER_OF_IDs);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Check if there are any instances of the specified type
        /// </summary>
        /// <param name="status_id"></param>
        /// <returns></returns>
        public bool ContainsStatus(Systems.Status.IDs status_id) {
            return statuses.ContainsKey(status_id);
        }

        /// <summary>
        /// Get status by type and instance id
        /// </summary>
        /// <param name="status_id"></param>
        /// <param name="instance_id"></param>
        /// <returns></returns>
        public Systems.Status GetStatus(Systems.Status.IDs status_id, byte instance_id) {
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances))
                return status_instances.GetStatus(instance_id);
            else
                return null;
        }

        /// <summary>
        /// Add status. Will create a new StatusInstances if needed. Will assign instance id if not set.
        /// </summary>
        /// <param name="status"></param>
        public void AddStatus(Systems.Status status) {
            //add StatusInstances if there are no other instances of the status
            if (!ContainsStatus(status.ID)) {
                statuses.Add(status.ID, new StatusInstances());
            }

            //get the StatusInstances
            StatusInstances status_instances;
            if (statuses.TryGetValue(status.ID, out status_instances))
                //add the status
                status_instances.AddStatus(status);
            else
                Commons.Error("Failed to create StatusInstances for new status " + status.core_display_name);
        }

        /// <summary>
        /// Remove status directly
        /// </summary>
        /// <param name="status"></param>
        public void RemoveStatus(Systems.Status status) {
            RemoveStatus(status.ID, status.instance_id);
        }

        /// <summary>
        /// Remove status by type and instance id
        /// </summary>
        /// <param name="status_id"></param>
        /// <param name="instance_id"></param>
        public void RemoveStatus(Systems.Status.IDs status_id, byte instance_id) {
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
        /// Returns list of instances for each active status (empty if no statuses)
        /// </summary>
        /// <returns></returns>
        public List<List<Systems.Status>> GetAllStatuses() {
            List<List<Systems.Status>> list = new List<List<Systems.Status>>();
            foreach (StatusInstances status in statuses.Values) {
                list.Add(status.GetInstances());
            }
            return list;
        }

        /// <summary>
        /// Returns list of instances for specified status type (empty if no instances)
        /// </summary>
        /// <param name="status_id"></param>
        /// <returns></returns>
        public List<Systems.Status> GetStatuses(Systems.Status.IDs status_id) {
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances))
                return status_instances.GetInstances();
            else
                return new List<Systems.Status>();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ StatusInstances ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// Stores all instances of a single status type on a target. Statuses are assigned an instance id.
        /// 
        /// Instances are stored in a storted list for fast lookup and to allow all instances to be returned as a list without needing to select from an array.
        /// 
        /// The keys for the stored list correspond with indices in a bool[]. This was done instead of a queue/stack of unique values because a default 
        /// bool[] can be constructed more quickly. However, adding instances when there are already several is slower with the array method because it
        /// must search through the array for the first available key index.
        /// It is expected that targets will generally have just one instance of a status at a time so initialization time is more important than efficiency
        /// while near capacity.
        /// </summary>
        private class StatusInstances {
            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
            private SortedList<byte, Systems.Status> instances = new SortedList<byte, Systems.Status>(UNASSIGNED_INSTANCE_KEY);
            private bool[] key_taken = new bool[UNASSIGNED_INSTANCE_KEY]; //keys are 0 to (UNASSIGNED_INSTANCE_KEY - 1)

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
            /// </summary>
            /// <param name="status"></param>
            public void AddStatus(Systems.Status status) {
                if (status.instance_id == UNASSIGNED_INSTANCE_KEY) {
                    //assign new key and add
                    if (!AddNewStatus(status)) {
                        Commons.Error("Cannot add instances of " + status.core_display_name);
                    }
                }
                else {
                    //already has an instance id (is from another client)
                    if (instances.ContainsKey(status.instance_id)) {
                        //replace existing instance
                        instances[status.instance_id] = status;
                    }
                    else {
                        //add another instance
                        instances.Add(status.instance_id, status);
                    }

                    //be certain that key is marked as taken
                    key_taken[status.instance_id] = true;
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
            /// Assigns a key and adds the instance. Returns false if no keys available or status already has a key.
            /// </summary>
            /// <param name="status"></param>
            /// <returns></returns>
            private bool AddNewStatus(Systems.Status status) {
                if (status.instance_id != UNASSIGNED_INSTANCE_KEY) {
                    //status already has key
                    return false;
                }
                else {
                    //find first available key
                    int key = Array.FindIndex<bool>(key_taken, i => i == false);
                    if (key < 0) {
                        //no keys available
                        return false;
                    }
                    else {
                        //assign key and add status
                        status.instance_id = (byte)key;
                        key_taken[key] = true;
                        instances.Add(status.instance_id, status);
                        return true;
                    }
                }
            }
        }
    }
}
