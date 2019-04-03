using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Status {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ IDs ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// inlcudes NUMBER_OF_IDs and NONE
        /// </summary>
        public enum IDs : ushort {
            Heal,

            //insert here

            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        /// <summary>
        /// Auto-syncing data types available (always float)
        /// </summary>
        protected enum AUTOSYNC_DATA_TYPES : byte {
            MAGNITUDE1,
            MAGNITUDE2,
            RANGE,
            STACKS,

            //insert here

            NUMBER_OF_TYPES //leave this last
        }
        protected static readonly IEnumerable<AUTOSYNC_DATA_TYPES> SYNC_DATA_TYPES_STRINGS = Enum.GetValues(typeof(AUTOSYNC_DATA_TYPES)).Cast<AUTOSYNC_DATA_TYPES>();

        protected enum DURATION_TYPES : byte {
            INSTANT,
            TIMED,
            TOGGLE,
        }

        /// <summary>
        /// Is there an OnUpdate effect? Is there a repeating timed effect?
        /// </summary>
        protected enum EFFECT_TYPES : byte {
            NONE,
            CONSTANT,
            TIMED,
        }

        /// <summary>
        /// Limit on how many instances can be on a single target
        /// </summary>
        public enum LIMIT_TYPES : byte {
            MANY, //up to limit of container
            ONE_PER_OWNER,
            ONE,
        }

        /// <summary>
        /// Which instances to apply if there are multiple
        /// </summary>
        public enum APPLY_TYPES : byte {
            ALL,
            BEST_PER_OWNER,
            BEST,
        }

        /// <summary>
        /// Which instances to show in UI
        /// </summary>
        public enum UI_TYPES : byte {
            NONE,
            ONE, //even if more than one is taking effect, only one of these is shown (first applied by instance id)
            ALL_APPLY, //all that are being applied
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// this texture index value indicates that there is no texture
        /// </summary>
        private static int TEXTURE_INDEX_NONE = -1;

        /// <summary>
        /// Used in IsBetterThan for order of priority
        /// </summary>
        private readonly AUTOSYNC_DATA_TYPES[] AUTOSYNC_COMPARE_IN_ORDER = { AUTOSYNC_DATA_TYPES.MAGNITUDE1, AUTOSYNC_DATA_TYPES.MAGNITUDE2, AUTOSYNC_DATA_TYPES.STACKS, AUTOSYNC_DATA_TYPES.RANGE };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Status[] LOOKUP { get; private set; }

        static Status() {
            LOOKUP = new Status[(ushort)Status.IDs.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Status>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Varibles ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// contains one element per status that has a texture, these status contain texture_index
        /// </summary>
        private static List<Texture2D> Textures = new List<Texture2D>();

        /// <summary>
        /// timers for timed-effect statuses
        /// (not stored in status itself because timer should be across instances of the status)
        /// (cleared when there are no more instances of a status)
        /// </summary>
        private static SortedDictionary<IDs, DateTime> Times_Next_Timed_Effect = new SortedDictionary<IDs, DateTime>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Status-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        //all of these begin with "specific_" and have a description including the default value to make adding more statuses easier

        /// <summary>
        /// name of status | leave if not shown in ui
        /// </summary>
        public string specific_name = "default_name";

        /// <summary>
        /// description of status (mouse-over text) | leave if not shown in ui
        /// </summary>
        public string specific_description = "default_desc";

        /// <summary>
        /// path to status icon | leave if not shown in ui
        /// </summary>
        protected string specific_texture_path;

        /// <summary>
        /// list of autosync data types | leave null if not using any
        /// </summary>
        protected List<AUTOSYNC_DATA_TYPES> specific_autosync_data_types;

        /// <summary>
        /// allow target to be player | default is TRUE
        /// </summary>
        protected bool specific_target_can_be_player = true;

        /// <summary>
        /// allow target to be npc | default is FALSE
        /// </summary>
        protected bool specific_target_can_be_npc = false;

        /// <summary>
        /// allow owner to be player | default is TRUE
        /// </summary>
        protected bool specific_owner_can_be_player = true;

        /// <summary>
        /// allow owner to be npc | default is FALSE
        /// </summary>
        protected bool specific_owner_can_be_npc = false;

        /// <summary>
        /// duration type | default is timed
        /// </summary>
        protected DURATION_TYPES specific_duration_type = DURATION_TYPES.TIMED;

        /// <summary>
        /// duration in seconds if timed | default is 5 seconds
        /// </summary>
        protected float specific_duration_sec = 5f;

        /// <summary>
        /// type of effect | default is constant
        /// </summary>
        protected EFFECT_TYPES specific_effect_type = EFFECT_TYPES.CONSTANT;

        /// <summary>
        /// frequency of effect if effect type is timed | default is 1 second
        /// </summary>
        protected float specific_timed_effect_sec = 1f;

        /// <summary>
        /// instance limitation type | default is many (up to a max of whatever the status container is set to hold)
        /// </summary>
        public LIMIT_TYPES specific_limit_type { get; protected set; } = LIMIT_TYPES.MANY;

        /// <summary>
        /// when there are multiple instances, the apply type determines which take effect | default is BEST_PER_OWNER
        /// </summary>
        public APPLY_TYPES specific_apply_type = APPLY_TYPES.BEST_PER_OWNER;

        /// <summary>
        /// when a status would replace another, merges best of both status autosync fields and take the longer remaining duration instead (sets owner to latest owner) (there is an additional status-specific merge method that is also called) | default is TRUE
        /// </summary>
        public bool specific_allow_merge { get; protected set; } = true;

        /// <summary>
        /// When merging, merge durations too (use highest) | default is true
        /// </summary>
        protected bool specific_merge_duration = true;

        /// <summary>
        /// sync in multiplayer mode | default is true
        /// </summary>
        protected bool specific_syncs = true;

        /// <summary>
        /// ui display type | default is ALL_APPLY (all that are allowed to apply based on APPLY_TYPES)
        /// </summary>
        public UI_TYPES specific_ui_type = UI_TYPES.ALL_APPLY;

        /// <summary>
        /// remove if the local client is the owner and does not have specified status | default is none
        /// </summary>
        protected IDs specific_owner_player_required_status = IDs.NONE;

        /// <summary>
        /// remove if the local client is the owner and does not have specified passive ability | default is none
        /// </summary>
        protected Systems.Passive.IDs specific_owner_player_required_passive = Systems.Passive.IDs.NONE;

        /// <summary>
        /// remove if target DOES NOT have specified status (checked by server) | default is none
        /// </summary>
        protected IDs specific_target_required_status = IDs.NONE;

        /// <summary>
        /// remove if target DOES have specified status (checked by server) | default is none
        /// </summary>
        protected IDs specific_target_antirequisite_status = IDs.NONE;

        /// <summary>
        /// when merging, add to stack count | default is FALSE
        /// </summary>
        protected bool specific_autostack_on_merge = false;

        /// <summary>
        /// Max stacks when autostack is used
        /// </summary>
        protected ushort specific_max_stacks = 0;

        /// <summary>
        /// remove if owner died | default is FALSE
        /// </summary>
        protected bool specific_remove_on_owner_death = false;

        /// <summary>
        /// remove if target died | default is TRUE
        /// </summary>
        protected bool specific_remove_on_target_death = true;

        /// <summary>
        /// remove if the owner is a player and has left the game (always treated as true for toggle status) | default is FALSE
        /// </summary>
        protected bool specific_remove_if_owner_player_leaves = false;

        /// <summary>
        /// remove if owner is local player and they are not pressing this key | default is null
        /// </summary>
        protected ModHotKey specific_remove_if_key_not_pressed = null;

        /// <summary>
        /// has a visual drawn behind target (uses DrawEffectBack) | default is false
        /// </summary>
        protected bool specified_has_visual_back = false;

        /// <summary>
        /// has a visual drawn in front of target (uses DrawEffectFront) | default is false
        /// </summary>
        protected bool specified_has_visual_front = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID
        public IDs ID { get; private set; }
        public ushort ID_num { get; private set; }
        public byte Instance_ID { get; private set; }

        /// <summary>
        /// could be player or npc
        /// </summary>
        public Utilities.Containers.Thing target { get; private set; }

        /// <summary>
        /// could be player or npc
        /// </summary>
        public Utilities.Containers.Thing owner { get; private set; }

        /// <summary>
        /// Time to end the status (for duration status). Instant status use DateTime.MinValue and toggle status use DateTime.MaxValue.
        /// </summary>
        public DateTime time_end { get; private set; }

        /// <summary>
        /// data to automatically sync
        /// </summary>
        protected Dictionary<AUTOSYNC_DATA_TYPES, float> autosync_data;

        /// <summary>
        /// effect was applied (or test in case of timed effect) in latest cycle
        /// </summary>
        private bool applied = false;

        /// <summary>
        /// had a ui icon the last time it was applied (can be left true when applied is false so check "applied && in_ui")
        /// note that a status cannot have a ui icon without being applied
        /// </summary>
        public bool was_in_ui = false;

        /// <summary>
        /// set during init and used in Texture
        /// </summary>
        private int texture_index = TEXTURE_INDEX_NONE;

        /// <summary>
        /// local is responsible for enforcing end time (false if not duration type)
        /// </summary>
        private bool local_enforce_duration = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Core Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Status(IDs id) {
            ID = id;
            ID_num = (ushort)id;
            Instance_ID = Utilities.Containers.StatusList.UNASSIGNED_INSTANCE_KEY;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Run once during init
        /// </summary>
        public void LoadTexture() {
            if (specific_texture_path != null) {
                texture_index = Textures.Count;
                Textures.Add(ModLoader.GetTexture(specific_texture_path));
            }
        }

        /// <summary>
        /// Status icon texture (default texture if not set)
        /// </summary>
        /// <returns></returns>
        public Texture2D Texture {
            get {
                if (texture_index == TEXTURE_INDEX_NONE) {
                    return Utilities.Textures.TEXTURE_STATUS_DEFAULT;
                }
                else {
                    return Textures[texture_index];
                }
            }
        }

        /// <summary>
        /// Called by the container when status is created locally or by the packet-recieved when originating from elsewhere
        /// </summary>
        /// <param name="instance_id"></param>
        public void SetInstanceID(byte instance_id) {
            Instance_ID = instance_id;
        }

        protected void RemoveLocally() {
            //if this was being drawn, need ui update
            if (applied && was_in_ui) {
                UI.UIStatus.needs_redraw_complete = true;
            }

            //remove
            target.Statuses.Remove(this);

            //if timed-effect and no more instances of this status, clear timer
            if ((specific_effect_type == EFFECT_TYPES.TIMED) && !target.Statuses.Contains(ID)) {
                Times_Next_Timed_Effect.Remove(ID);
            }

            //end
            OnEnd();
        }

        public void RemoveEverywhere() {
            //remove locally
            RemoveLocally();

            //remove everywhere else (send end packet)
            if (specific_syncs) {
                //TODO - send remove packet
            }
        }

        /// <summary>
        /// Called on every cycles even if effect is not applied. Returns false if status was removed.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool Update() {
            //remove?
            bool remove = CheckRemoval();
            if (remove) {
                RemoveEverywhere();
            }
            else {
                //override method
                OnUpdate();

                //default to not applied during this cycle
                applied = false;
            }

            //return removal
            return remove;
        }

        public String GetIconDurationString() {
            if (specific_duration_type == DURATION_TYPES.TIMED) {

                TimeSpan time_remain = time_end.Subtract(ExperienceAndClasses.Now);
                int time_remaining_min = time_remain.Minutes;
                int time_remaining_sec = time_remain.Seconds;

                if (time_remaining_min > 0) {
                    return time_remaining_min + " min";
                }
                else {
                    return time_remaining_sec + " sec";
                }
            }
            else {
                return "";
            }
        }

        /// <summary>
        /// Check if status should be removed. Includes calls to override methods ShouldRemove and ShouldRemoveLocal.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        private bool CheckRemoval() {
            //remove if duration type is instant (shouldn't happen)
            if (specific_duration_type == DURATION_TYPES.INSTANT) {
                return true;
            }

            //remove if this client/server is responsible for the duration (else update time remaining if target is a player)
            if (local_enforce_duration && (ExperienceAndClasses.Now.CompareTo(time_end) >= 0)) { //timeup
                return true;
            }

            //requirements: server/singleplayer (not client) checks
            if (!Utilities.Netmode.IS_CLIENT) {
                //remove if owner player leaves
                if ((specific_remove_if_owner_player_leaves || (specific_duration_type == DURATION_TYPES.TOGGLE)) && owner.Is_Player && !owner.Active) {
                    return true;
                }

                //remove if owner died
                if (specific_remove_on_owner_death) {
                    if (owner.Dead) {
                        return true;
                    }
                }

                //remove if target died
                if (specific_remove_on_target_death) {
                    if (target.Dead) {
                        return true;
                    }
                }

                //remove if target lacks required status
                if (specific_target_required_status != IDs.NONE) {
                    if (!target.Statuses.Contains(specific_target_required_status)) {
                        return true;
                    }
                }

                //remove if target has antirequisite status
                if (specific_target_antirequisite_status != IDs.NONE) {
                    if (target.Statuses.Contains(specific_target_antirequisite_status)) {
                        return true;
                    }
                }
            }

            //requirements: local
            if (owner.Is_Player && owner.Local) { //owner is always player if locally_owned
                //required status
                if ((specific_owner_player_required_status != IDs.NONE) && !owner.Statuses.Contains(specific_owner_player_required_status)) {
                    return true;
                }

                //required passive
                if ((specific_owner_player_required_passive != Systems.Passive.IDs.NONE) && !owner.mplayer.Passives.Contains(specific_owner_player_required_passive)) {
                    return true;
                }

                //status-specific check
                if (ShouldRemoveLocal()) {
                    return true;
                }

                //key press
                if (specific_remove_if_key_not_pressed != null && !specific_remove_if_key_not_pressed.Current) {
                    return true;
                }
            }

            //status-specific check
            if (ShouldRemove()) {
                return true;
            }

            //default to not remove
            return false;
        }

        /// <summary>
        /// Merges passed status into this one. Returns false if no improvements were made.
        /// If improvements were made, then the owner is set to the owner of the merged in status and autostack may occur.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Merge(Status status) {
            //track if improvements would be made
            bool improved = false;

            //allowed to merge? (shouldn't be called if not allowed, but might as well check)
            if (specific_allow_merge) {
                //autosync data
                foreach (AUTOSYNC_DATA_TYPES type in specific_autosync_data_types) {
                    if (autosync_data[type] < status.autosync_data[type]) {
                        autosync_data[type] = status.autosync_data[type];
                        improved = true;
                    }
                }

                //duration (if timed and specific_merge_duration)
                if (specific_merge_duration && (specific_duration_type == DURATION_TYPES.TIMED)) {
                    if (status.time_end.CompareTo(time_end) > 0) {
                        time_end = status.time_end;
                        improved = true;
                    }
                }

                //optional override
                if (OnMerge(status)) {
                    improved = true;
                }

                //if improved...
                if (improved) {
                    //copy new owner
                    owner = status.owner;

                    //add stack if autostack (and not maxed)
                    if (specific_autostack_on_merge && (autosync_data[AUTOSYNC_DATA_TYPES.STACKS] < specific_max_stacks)) {
                        autosync_data[AUTOSYNC_DATA_TYPES.STACKS] += 1f;
                    }
                }

                //sync
                if (specific_syncs) {
                    //TODO
                }
            }
            return improved;
        }

        /// <summary>
        /// Checks the following in order: MAGNITUDE1 > MAGNITUDE2 > STACKS > RANGE > remaining_duration
        /// The first non-tie determines which is "better"
        /// Default is false
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool IsBetterThan(Status status) {
            if (ID != status.ID) {
                //different types of status (shouldn't happen if used correctly)
                return false;
            }

            //auto sync fields
            double value_this, value_other;
            if (autosync_data.Count != 0) {
                foreach (AUTOSYNC_DATA_TYPES type_test in AUTOSYNC_COMPARE_IN_ORDER) {
                    if (specific_autosync_data_types.Contains(type_test)) {
                        value_this = autosync_data[type_test];
                        value_other = status.autosync_data[type_test];
                        if (value_this > value_other) {
                            return true;
                        }
                        else if (value_this < value_other) {
                            return false;
                        }
                    }
                }
            }
            
            //remaining duration
            if (specific_duration_type == DURATION_TYPES.TIMED) {
                if (time_end.CompareTo(status.time_end) >= 0) {
                    return true;
                }
                else {
                    return false;
                }
            }

            //default
            return false;
        }

        private void DoEffect() {
            //effect was applied (or tested if timed effect) on latest cycle
            applied = true;

            //do effect
            if (specific_effect_type == EFFECT_TYPES.CONSTANT) {
                Effect();
            }
            else if (specific_effect_type == EFFECT_TYPES.TIMED) {
                if (ExperienceAndClasses.Now.CompareTo(Times_Next_Timed_Effect[ID]) >= 0) {
                    //call effect
                    Effect();

                    //calculate next time of effect
                    Times_Next_Timed_Effect[ID].AddSeconds(specific_timed_effect_sec);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods To Override (Required) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        protected static void Create(IDs id, Utilities.Containers.Thing target, Utilities.Containers.Thing owner, Dictionary<AUTOSYNC_DATA_TYPES, float> sync_data = null, float seconds_remaining = 0f, float seconds_until_effect = 0f) {
            //create instance
            Status status = Utilities.Commons.CreateObjectFromName<Status>(Enum.GetName(typeof(IDs), id));
            
            //valid target (stop if not)
            if ((target.Is_Player && !status.specific_target_can_be_player) || (target.Is_Npc && !status.specific_target_can_be_npc)) {
                Utilities.Commons.Error("Invalid target for " + status.specific_name);
                return;
            }

            //valid owner (stop if not)
            if ((owner.Is_Player && !status.specific_owner_can_be_player) || (owner.Is_Npc && !status.specific_owner_can_be_npc)) {
                Utilities.Commons.Error("Invalid owner for " + status.specific_name);
                return;
            }

            //target
            status.target = target;

            //owner
            status.owner = owner;

            //calcualte end time
            switch (status.specific_duration_type) {
                case (DURATION_TYPES.TIMED):
                    status.time_end = ExperienceAndClasses.Now.AddSeconds(seconds_remaining);
                    break;

                case (DURATION_TYPES.INSTANT):
                    status.time_end = DateTime.MinValue;
                    break;

                case (DURATION_TYPES.TOGGLE):
                    status.time_end = DateTime.MaxValue;
                    break;

                default:
                    Utilities.Commons.Error("Unsupported DURATION_TYPES: " + status.specific_duration_type);
                    break;
            }

            //start
            status.OnStart();

            //do effect if instant
            if (status.specific_duration_type == DURATION_TYPES.INSTANT) {
                status.DoEffect();
            }
            else {
                //not instant... do duration stuff

                //periodic effect time (not stored in status itself because timer should be across instances of the status)
                if (status.specific_effect_type == EFFECT_TYPES.TIMED) {
                    if (Times_Next_Timed_Effect.ContainsKey(id)) {
                        //had a timer so update it
                        Times_Next_Timed_Effect[id] = ExperienceAndClasses.Now.AddSeconds(seconds_until_effect);
                    }
                    else {
                        //did not already have timer so start one with first effect now
                        Times_Next_Timed_Effect.Add(id, ExperienceAndClasses.Now);
                    }
                }

                //locally enforce duration?
                if (status.specific_effect_type == EFFECT_TYPES.TIMED) {    //has a duration, AND
                    if (!status.specific_syncs ||                           //not shared with other clients, OR
                        target.Local) {                                     //target is the local client OR this is server and target is npc

                        status.local_enforce_duration = true;

                    }
                }
            }

            //attach to target (may cause merge or overwrite)
            target.Statuses.Add(status);

            //sync
            if (status.specific_syncs) {
                //TODO
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods To Override (Optional) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        //these are for any additional status-specific code
        protected virtual void OnStart() {}
        protected virtual void Effect() {}
        protected virtual bool ShouldRemove() { return false; }
        protected virtual bool ShouldRemoveLocal() { return false; }

        /// <summary>
        /// Return true to mark the merge as an improvement. False leaves the value as it was.
        /// Called after merging autosync data and duration, but before (potentially) setting new owner and autostacking.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        protected virtual bool OnMerge(Status status) { return false; }

        /// <summary>
        /// Called when status is removed (not when merged over)
        /// </summary>
        protected virtual void OnEnd() { }

        /// <summary>
        /// Called on every cycles (even if effect is not done) 
        /// </summary>
        protected virtual void OnUpdate() { }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Call on every cycle
        /// </summary>
        /// <param name="statuses"></param>
        public static void ProcessStatuses(Utilities.Containers.StatusList statuses) {
            //update every instance of every status (may remove some)
            foreach (Status status in statuses.GetAll()) {
                status.Update();
            }

            List<Status> apply = statuses.GetAllApply();
            foreach (Status status in apply) {
                status.DoEffect();
            }



            //TODO - do we need to update UI or draw?
        }

        /// <summary>
        /// Updates DrawFront and DrawBack lists if needed.
        /// </summary>
        /// <param name="mnpc"></param>
        public static void UpdateVisuals(MNPC mnpc) {
            //TODO
        }

        /// <summary>
        /// Updates DrawFront and DrawBack lists if needed. Also updates UI list if needed.
        /// </summary>
        /// <param name="mplayer"></param>
        public static void UpdateVisuals(MPlayer mplayer) {
            //TODO
        }

        /// <summary>
        /// Get the list of statuses to draw effects for
        /// </summary>
        /// <param name="statuses"></param>
        /// <param name="get_front"></param>
        /// <returns></returns>
        private static List<Status> GetDrawList(Utilities.Containers.StatusList statuses, bool get_front) {
            //TODO
            return null;
        }

        

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Example ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /*
        public class Heal : Status {
            public Heal() : base(IDs.Heal) {
                //any overwrites
                specific_name = "Heal"; //not needed unless displayed as a buff
                specific_texture_path = "ExperienceAndClasses/Textures/Status/Heal"; //not needed unless displayed as a buff

                //add any sync data types that will be used (for syncing)
                specific_autosync_data_types.Add(AUTOSYNC_DATA_TYPES.MAGNITUDE1);
            }

            //must inlcude a static add method with target/owner and any extra info
            public static void CreateNew(Player target, MPlayer owner, float magnitude) {
                Add(target, owner, IDs.Heal, new Dictionary<AUTOSYNC_DATA_TYPES, float> {
                    { AUTOSYNC_DATA_TYPES.MAGNITUDE1, magnitude }
                });
            }

            //optional overrides (base methods are empty)
            protected override void OnStart() { }
            protected override void OnUpdate() { }
            protected override void OnEnd() { }
        }
        */

    }
}
