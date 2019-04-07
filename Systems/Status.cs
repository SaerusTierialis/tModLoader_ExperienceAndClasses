using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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
        public enum AUTOSYNC_DATA_TYPES : byte {
            MAGNITUDE1,
            MAGNITUDE2,
            RANGE,
            STACKS,

            //insert here

            NUMBER_OF_TYPES //leave this last
        }

        public enum DURATION_TYPES : byte {
            INSTANT,
            TIMED,
            TOGGLE,
        }

        /// <summary>
        /// Is there an OnUpdate effect? Is there a repeating timed effect?
        /// </summary>
        public enum EFFECT_TYPES : byte {
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
        private const int TEXTURE_INDEX_NONE = -1;

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
            LOOKUP = new Status[(ushort)IDs.NUMBER_OF_IDs];
            for (ushort i = 0; i < LOOKUP.Length; i++) {
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
        public static SortedDictionary<IDs, DateTime> Times_Next_Timed_Effect { get; private set; } = new SortedDictionary<IDs, DateTime>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Status-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        //all of these begin with "specific_" and have a description including the default value to make adding more statuses easier

        /// <summary>
        /// name of status | leave if not shown in ui
        /// </summary>
        public string Specific_Name { get; protected set; } = "default_name";

        /// <summary>
        /// description of status (mouse-over text) | leave if not shown in ui
        /// </summary>
        public string Specific_Description { get; protected set; } = "default_description";

        /// <summary>
        /// path to status icon | leave null if not shown in ui
        /// </summary>
        protected string specific_texture_path = null;

        /// <summary>
        /// list of autosync data types | leave null if not using any
        /// </summary>
        public List<AUTOSYNC_DATA_TYPES> Specific_Autosync_Data_Types { get; protected set; } = new List<AUTOSYNC_DATA_TYPES>();

        /// <summary>
        /// allow target to be player | default is TRUE
        /// </summary>
        protected bool specific_target_can_be_player = true;

        /// <summary>
        /// allow target to be npc | default is TRUE
        /// </summary>
        protected bool specific_target_can_be_npc = true;

        /// <summary>
        /// allow owner to be player | default is TRUE
        /// </summary>
        protected bool specific_owner_can_be_player = true;

        /// <summary>
        /// allow owner to be npc | default is TRUE
        /// </summary>
        protected bool specific_owner_can_be_npc = true;

        /// <summary>
        /// duration type | default is timed
        /// </summary>
        public DURATION_TYPES Specific_Duration_Type { get; protected set; } = DURATION_TYPES.TIMED;

        /// <summary>
        /// duration in seconds if timed | default is 5 seconds
        /// </summary>
        protected float specific_duration_sec = 5f;

        /// <summary>
        /// type of effect | default is constant
        /// </summary>
        public EFFECT_TYPES Specific_Effect_Type { get; protected set; } = EFFECT_TYPES.CONSTANT;

        /// <summary>
        /// frequency of effect if effect type is timed | default is 1 second
        /// </summary>
        protected float specific_timed_effect_sec = 1f;

        /// <summary>
        /// instance limitation type | default is many (up to a max of whatever the status container is set to hold)
        /// </summary>
        public LIMIT_TYPES Specific_Limit_Type { get; protected set; } = LIMIT_TYPES.MANY;

        /// <summary>
        /// when there are multiple instances, the apply type determines which take effect | default is BEST_PER_OWNER
        /// </summary>
        public APPLY_TYPES Specific_Apply_Type { get; protected set; } = APPLY_TYPES.BEST_PER_OWNER;

        /// <summary>
        /// when a status would replace another, merges best of both status autosync fields and take the longer remaining duration instead (sets owner to latest owner) (there is an additional status-specific merge method that is also called) | default is TRUE
        /// </summary>
        public bool Specific_Allow_Merge { get; protected set; } = true;

        /// <summary>
        /// When merging, merge durations too (use highest) | default is true
        /// </summary>
        protected bool specific_merge_duration = true;

        /// <summary>
        /// sync in multiplayer mode | default is true
        /// </summary>
        public bool Specific_Syncs { get; protected set; } = true;

        /// <summary>
        /// ui display type | default is ALL_APPLY (all that are allowed to apply based on APPLY_TYPES)
        /// </summary>
        public UI_TYPES Specific_UI_Type { get; protected set; } = UI_TYPES.ALL_APPLY;

        /// <summary>
        /// remove if the local client is the owner and does not have specified status | default is none
        /// </summary>
        protected IDs specific_owner_player_required_status = IDs.NONE;

        /// <summary>
        /// remove if the local client is the owner and does not have specified passive ability | default is null
        /// </summary>
        protected Systems.Passive.IDs specific_owner_player_required_passive = Systems.Passive.IDs.NONE;

        /// <summary>
        /// remove if the local client is the owner and does not have specified passive ability | default is null
        /// </summary>
        protected Systems.Ability.IDs specific_owner_player_required_ability = Systems.Ability.IDs.NONE;

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
        protected bool specific_has_visual_back = false;

        /// <summary>
        /// has a visual drawn in front of target (uses DrawEffectFront) | default is false
        /// </summary>
        protected bool specific_has_visual_front = false;

        /// <summary>
        /// The target player is considered channeling while it has a status with this set true. No effect for NPC targets. | default is false
        /// </summary>
        public bool Specific_Target_Channelling { get; protected set; } = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID
        public IDs ID { get; private set; }
        public ushort ID_num { get; private set; }
        public byte Instance_ID { get; private set; }

        /// <summary>
        /// could be player or npc
        /// </summary>
        public Utilities.Containers.Thing Target { get; private set; }

        /// <summary>
        /// could be player or npc
        /// </summary>
        public Utilities.Containers.Thing Owner { get; private set; }

        /// <summary>
        /// Time to end the status (for duration status). Instant status use DateTime.MinValue and toggle status use DateTime.MaxValue.
        /// </summary>
        public DateTime Time_End { get; private set; }

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
            //remove
            Target.Statuses.Remove(this);

            //UI and visuals
            if (applied) {
                if (was_in_ui) {
                    //if this was being drawn, need ui update
                    UI.UIStatus.needs_redraw_complete = true;
                }

                if (!Utilities.Netmode.IS_SERVER) {
                    //if not server and this status had visuals, update the visual lists
                    if (specific_has_visual_back) {
                        Target.needs_update_status_visuals_back = true;
                    }
                    if (specific_has_visual_front) {
                        Target.needs_update_status_visuals_front = true;
                    }
                }
            }

            //if timed-effect and no more instances of this status, clear timer
            if ((Specific_Effect_Type == EFFECT_TYPES.TIMED) && !Target.HasStatus(ID)) {
                Times_Next_Timed_Effect.Remove(ID);
            }

            //end
            OnEnd();
        }

        public void RemoveEverywhere() {
            //remove locally
            RemoveLocally();

            //remove everywhere else (send end packet)
            if (Specific_Syncs) {
                if (Owner.Local) {
                    //sending local status (could be server if a status is evere created there)
                    Utilities.PacketHandler.RemoveStatus.Send(this, Utilities.Netmode.WHO_AM_I);
                }
                else if (Utilities.Netmode.IS_SERVER) {
                    //not local, relaying status to clients through server (can assume owner is player and is origin)
                    Utilities.PacketHandler.RemoveStatus.Send(this, Owner.whoAmI);
                }
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

                //apply channel
                if (Specific_Target_Channelling && Target.Is_Player) {
                    Target.MPlayer.channelling = true;
                }
            }

            //return removal
            return remove;
        }

        public String GetIconDurationString() {
            if (Specific_Duration_Type == DURATION_TYPES.TIMED) {

                TimeSpan time_remain = Time_End.Subtract(ExperienceAndClasses.Now);
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
            if (Specific_Duration_Type == DURATION_TYPES.INSTANT) {
                return true;
            }

            //remove if this client/server is responsible for the duration (else update time remaining if target is a player)
            if (local_enforce_duration && (ExperienceAndClasses.Now.CompareTo(Time_End) >= 0)) { //timeup
                return true;
            }

            //requirements: server/singleplayer (not client) checks
            if (!Utilities.Netmode.IS_CLIENT) {
                //remove if owner player leaves
                if ((specific_remove_if_owner_player_leaves || (Specific_Duration_Type == DURATION_TYPES.TOGGLE)) && Owner.Is_Player && !Owner.Active) {
                    return true;
                }

                //remove if owner died
                if (specific_remove_on_owner_death) {
                    if (Owner.Dead) {
                        return true;
                    }
                }

                //remove if target died
                if (specific_remove_on_target_death) {
                    if (Target.Dead) {
                        return true;
                    }
                }

                //remove if target lacks required status
                if (specific_target_required_status != IDs.NONE) {
                    if (!Target.HasStatus(specific_target_required_status)) {
                        return true;
                    }
                }

                //remove if target has antirequisite status
                if (specific_target_antirequisite_status != IDs.NONE) {
                    if (Target.HasStatus(specific_target_antirequisite_status)) {
                        return true;
                    }
                }
            }

            //requirements: local
            if (Owner.Is_Player && Owner.Local) { //owner is always player if locally_owned
                //required status
                if ((specific_owner_player_required_status != IDs.NONE) && !Owner.HasStatus(specific_owner_player_required_status)) {
                    return true;
                }

                //required passive
                if ((specific_owner_player_required_passive != Systems.Passive.IDs.NONE) && !Owner.MPlayer.Passives.Contains(specific_owner_player_required_passive)) {
                    return true;
                }

                //required ability
                if ((specific_owner_player_required_ability != Systems.Ability.IDs.NONE) && !Owner.MPlayer.Abilities_Primary.Contains(Systems.Ability.LOOKUP[(byte)specific_owner_player_required_ability]) && !Owner.MPlayer.Abilities_Secondary.Contains(Systems.Ability.LOOKUP[(byte)specific_owner_player_required_ability])) {
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
        /// Call on new status and pass the existing status. Returns false if no improvements were made.
        /// If improvements were made, autostack
        /// </summary>
        /// <param name="existing"></param>
        /// <returns></returns>
        public bool Merge(Status existing) {
            //should only ever be called by the owner
            if (!existing.Owner.Local) {
                Utilities.Commons.Error("Merge status called by non-owner!");
                return false;
            }

            //track if improvements would be made
            bool improved = false;

            //allowed to merge? (shouldn't be called if not allowed, but might as well check)
            if (Specific_Allow_Merge) {
                //autosync data
                foreach (AUTOSYNC_DATA_TYPES type in Specific_Autosync_Data_Types) {
                    if (autosync_data[type] > existing.autosync_data[type]) {
                        improved = true;
                    }
                    else {
                        autosync_data[type] = existing.autosync_data[type];
                    }
                }

                //duration (if timed and specific_merge_duration)
                if (specific_merge_duration && (Specific_Duration_Type == DURATION_TYPES.TIMED)) {
                    if (Time_End.CompareTo(existing.Time_End) > 0) {
                        improved = true;
                    }
                    else {
                        Time_End = existing.Time_End;
                    }
                }

                //optional override
                if (MergeCheck(existing)) {
                    improved = true;
                }

                //if improved...
                if (improved) {
                    //add stack if autostack (and not maxed)
                    if (specific_autostack_on_merge && (autosync_data[AUTOSYNC_DATA_TYPES.STACKS] < specific_max_stacks)) {
                        autosync_data[AUTOSYNC_DATA_TYPES.STACKS] += 1f;
                    }

                    //optional override
                    OnMerge();
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
                    if (Specific_Autosync_Data_Types.Contains(type_test)) {
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
            if (Specific_Duration_Type == DURATION_TYPES.TIMED) {
                if (Time_End.CompareTo(status.Time_End) >= 0) {
                    return true;
                }
                else {
                    return false;
                }
            }

            //default
            return false;
        }

        /// <summary>
        /// Mark effect as done during this cycle. Do the effect if constant. Check/Update/Do for timed effects.
        /// </summary>
        public void DoEffect() {
            //effect was applied (or tested if timed effect) on latest cycle
            applied = true;

            //do effect
            if (Specific_Effect_Type == EFFECT_TYPES.CONSTANT) {
                Effect();
            }
            else if (Specific_Effect_Type == EFFECT_TYPES.TIMED) {
                if (ExperienceAndClasses.Now.CompareTo(Times_Next_Timed_Effect[ID]) >= 0) {
                    //call effect
                    Effect();

                    //calculate next time of effect
                    Times_Next_Timed_Effect[ID].AddSeconds(specific_timed_effect_sec);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Shortcuts to Sync Data ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public double GetData(AUTOSYNC_DATA_TYPES key, float default_value = -1) {
            float value = default_value;
            if (!autosync_data.TryGetValue(key, out value)) {
                Utilities.Commons.Error("Status attempted to access invalid sync data: " + ID + " " + key);
            }
            return value;
        }

        protected double Magitude1 { get { return GetData(AUTOSYNC_DATA_TYPES.MAGNITUDE1); } }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Create and attach the status. If the owner is local and the status syncs, then a sync is triggered.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="target"></param>
        /// <param name="owner"></param>
        /// <param name="sync_data"></param>
        /// <param name="seconds_remaining"></param>
        /// <param name="seconds_until_effect"></param>
        public static void Add(IDs id, Utilities.Containers.Thing target, Utilities.Containers.Thing owner, Dictionary<AUTOSYNC_DATA_TYPES, float> sync_data = null, float seconds_remaining = 0f, float seconds_until_effect = 0f, byte instance_id = Utilities.Containers.StatusList.UNASSIGNED_INSTANCE_KEY, BinaryReader reader = null, bool set_statuses = false) {
            //create instance
            Status status = Utilities.Commons.CreateObjectFromName<Status>(Enum.GetName(typeof(IDs), id));
            
            //valid target (stop if not)
            if ((target.Is_Player && !status.specific_target_can_be_player) || (target.Is_Npc && !status.specific_target_can_be_npc)) {
                Utilities.Commons.Error("Invalid target for " + status.Specific_Name);
                return;
            }

            //valid owner (stop if not)
            if ((owner.Is_Player && !status.specific_owner_can_be_player) || (owner.Is_Npc && !status.specific_owner_can_be_npc)) {
                Utilities.Commons.Error("Invalid owner for " + status.Specific_Name);
                return;
            }

            //instance id
            if (instance_id != Utilities.Containers.StatusList.UNASSIGNED_INSTANCE_KEY) {
                status.SetInstanceID(instance_id);
            }

            //target
            status.Target = target;

            //owner
            status.Owner = owner;

            //autosync data
            if (sync_data != null) {
                status.autosync_data = sync_data;
            }

            //calcualte end time
            switch (status.Specific_Duration_Type) {
                case (DURATION_TYPES.TIMED):
                    status.Time_End = ExperienceAndClasses.Now.AddSeconds(seconds_remaining);
                    break;

                case (DURATION_TYPES.INSTANT):
                    status.Time_End = DateTime.MinValue;
                    break;

                case (DURATION_TYPES.TOGGLE):
                    status.Time_End = DateTime.MaxValue;
                    break;

                default:
                    Utilities.Commons.Error("Unsupported DURATION_TYPES: " + status.Specific_Duration_Type);
                    break;
            }

            //read any extra stuff
            if (reader != null) {
                status.PacketAddRead(reader);
            }

            //attach to target (may cause merge or overwrite)
            if (!target.Statuses.Add(status)) {
                //not added for any reaon, stop
                //typically this would mean that merge was performance and was not an improvement
                //could mean an error occured
                return;
            }

            //periodic effect time (not stored in status itself because timer should be across instances of the status)
            if (status.Specific_Effect_Type == EFFECT_TYPES.TIMED) {

                DateTime time_next;
                if (seconds_until_effect != 0) {
                    //use provided time
                    time_next = ExperienceAndClasses.Now.AddSeconds(seconds_until_effect);
                }
                else {
                    //no provided time so do first effect asap
                    time_next = ExperienceAndClasses.Now;
                }

                Times_Next_Timed_Effect[id] = time_next;
            }

            //locally enforce duration?
            if (status.Specific_Duration_Type == DURATION_TYPES.TIMED) {    //has a duration, AND
                if (!status.Specific_Syncs ||                               //not shared with other clients, OR
                    target.Local) {                                         //target is the local client OR this is server and target is npc

                    status.local_enforce_duration = true;

                }
            }

            //if this is from SetStatuses, don't start events (unless it's instant)
            if (!set_statuses || (status.Specific_Duration_Type == DURATION_TYPES.INSTANT)) {
                //start
                status.OnStart();
            }

            //do effect if instant
            if (status.Specific_Duration_Type == DURATION_TYPES.INSTANT) {
                status.DoEffect();
            }

            //visuals (update occurs during next ProcessStatuses so applied is accurate)
            if (!Utilities.Netmode.IS_SERVER) {
                if (status.specific_has_visual_back) {
                    status.Target.needs_update_status_visuals_back = true;
                }
                if (status.specific_has_visual_front) {
                    status.Target.needs_update_status_visuals_front = true;
                }
            }

            //sync (unless set_statuses)
            if (!set_statuses && status.Specific_Syncs) {
                if (owner.Local) {
                    //sending local status (could be server if a status is ever created there - npc owned status)
                    Utilities.PacketHandler.AddStatus.Send(status, Utilities.Netmode.WHO_AM_I);
                }
                else if (Utilities.Netmode.IS_SERVER) {
                    //not local, relaying status to clients through server (can assume owner is player and is origin)
                    Utilities.PacketHandler.AddStatus.Send(status, status.Owner.whoAmI);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods To Override (Optional) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        //these are for any additional status-specific code
        protected virtual void OnStart() {}
        protected virtual void Effect() {}
        protected virtual bool ShouldRemove() { return false; }
        protected virtual bool ShouldRemoveLocal() { return false; }

        /// <summary>
        /// Return true to mark the merge as an improvement and trigger a sync. False leaves the value as it was.
        /// Called on new status, passed the existing status
        /// Called after merging autosync data and duration, but before (potentially) autostacking.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        protected virtual bool MergeCheck(Status status) { return false; }

        protected virtual void OnMerge() { }

        /// <summary>
        /// Called when status is removed (not when merged over)
        /// </summary>
        protected virtual void OnEnd() { }

        /// <summary>
        /// Called on every cycles (even if effect is not done) 
        /// </summary>
        protected virtual void OnUpdate() { }

        public virtual void PacketAddWrite(ModPacket packet) { }
        public virtual void PacketAddRead(BinaryReader reader) { }

        public virtual void PacketRemoveWrite(ModPacket packet) { }
        public virtual void PacketRemoveRead(BinaryReader reader) { }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Example ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Heal : Status {
            public Heal() : base(IDs.Heal) {
                //any overwrites
                Specific_Name = "Heal"; //not needed unless displayed as a buff
                specific_texture_path = "ExperienceAndClasses/Textures/Status/Heal"; //not needed unless displayed as a buff
                Specific_Duration_Type = DURATION_TYPES.INSTANT;

                //add any sync data types that will be used (for syncing)
                Specific_Autosync_Data_Types.Add(AUTOSYNC_DATA_TYPES.MAGNITUDE1);
            }

            //must inlcude a static add method with target/owner and any extra info
            public static void CreateNew(Utilities.Containers.Thing target, Utilities.Containers.Thing owner, float magnitude) {
                Add(IDs.Heal, target, owner, new Dictionary<AUTOSYNC_DATA_TYPES, float> {
                    { AUTOSYNC_DATA_TYPES.MAGNITUDE1, magnitude }
                });
            }
        }

    }
}
