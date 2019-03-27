using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.Utilities.Containers {
    public struct LoadedUIData {
        public readonly float LEFT, TOP;
        public readonly bool AUTO;

        public LoadedUIData(float left = 0f, float top = 0f, bool auto = true) {
            LEFT = left;
            TOP = top;
            AUTO = auto;
        }
    }

    public class StatusList {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const byte MAXIMUM_INSTANCES_PER_STATUS = byte.MaxValue;
        public const byte UNASSIGNED_INSTANCE_KEY = 0;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private SortedList<Systems.Status.IDs, StatusInstances> statuses = new SortedList<Systems.Status.IDs, StatusInstances>((int)Systems.Status.IDs.NUMBER_OF_IDs);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public bool ContainsStatus(Systems.Status.IDs status_id) {
            return statuses.ContainsKey(status_id);
        }

        public Systems.Status GetStatus(Systems.Status.IDs status_id, byte instance_id) {
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances))
                return status_instances.GetStatus(instance_id);
            else
                return null;
        }

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

        public void RemoveStatus(Systems.Status status) {
            RemoveStatus(status.ID, status.instance_id);
        }

        public void RemoveStatus(Systems.Status.IDs status_id, byte instance_id) {
            //get the StatusInstances
            StatusInstances status_instances;
            if (statuses.TryGetValue(status_id, out status_instances)) {
                //remove the status
                if (!status_instances.RemoveStatus(instance_id)) {
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
        private class StatusInstances {
            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
            private SortedList<byte, Systems.Status> instances = new SortedList<byte, Systems.Status>(MAXIMUM_INSTANCES_PER_STATUS);
            private Queue<byte> available_keys = new Queue<byte>(Enumerable.Range(1, MAXIMUM_INSTANCES_PER_STATUS).Select(i => (byte)i)); //instance_id = 0 is reserved for default value and instant statuses
            
            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            public List<Systems.Status> GetInstances() {
                //should never be empty
                return instances.Values.ToList();
            }

            public Systems.Status GetStatus(byte instance_id) {
                Systems.Status status;
                if (instances.TryGetValue(instance_id, out status))
                    return status;
                else
                    return null;
            }

            public void AddStatus(Systems.Status status) {
                if (status.instance_id == UNASSIGNED_INSTANCE_KEY) {
                    //needs an instance id
                    if (available_keys.Count == 0)
                        Commons.Error("Cannot add any more instances of " + status.core_display_name);
                    else {
                        //get first available instance id
                        status.instance_id = available_keys.Dequeue();
                        //add instance
                        instances.Add(status.instance_id, status);
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
                }
            }

            public bool RemoveStatus(byte instance_id) {
                //remove status
                instances.Remove(instance_id);

                //make key available
                if (!available_keys.Contains(instance_id))
                    available_keys.Enqueue(instance_id);

                //check if no more instances
                return (instances.Count == 0);
            }
        }
    }
}
