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
        protected enum LIMIT_TYPES : byte {
            MANY, //up to limit of container
            ONE_PER_OWNER,
            ONE,
        }

        /// <summary>
        /// Which instances to apply if there are multiple
        /// </summary>
        protected enum APPLY_TYPES : byte {
            ALL,
            BEST_PER_OWNER,
            BEST,
        }

        /// <summary>
        /// Which instances to show in UI
        /// </summary>
        protected enum UI_TYPES : byte {
            NONE,
            ONE, //even if more than one is taking effect, only one of these is shown (first applied by instance id)
            ALL_APPLY, //all that are being applied
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// this texture index value indicates that there is no texture
        /// </summary>
        private static int TEXTURE_INDEX_NONE = -1;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Status[] LOOKUP { get; private set; }

        static Status() {
            LOOKUP = new Status[(ushort)Status.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + IDs[i]));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Varibles ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// contains one element per status that has a texture, these status contain texture_index
        /// </summary>
        private static List<Texture2D> Textures = new List<Texture2D>();

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
        protected float specific_effect_update_frequency_sec = 1f;

        /// <summary>
        /// instance limitation type | default is many (up to a max of whatever the status container is set to hold)
        /// </summary>
        protected LIMIT_TYPES specific_limit_type = LIMIT_TYPES.MANY;

        /// <summary>
        /// when there are multiple instances, the apply type determines which take effect | default is BEST_PER_OWNER
        /// </summary>
        protected APPLY_TYPES specific_apply_type = APPLY_TYPES.BEST_PER_OWNER;

        /// <summary>
        /// when a status would replace another, merges best of both status autosync fields instead (sets owner to latest owner) (there is an additional status-specific merge method that is also called) | default is TRUE
        /// </summary>
        protected bool specific_allow_merge = true;

        /// <summary>
        /// sync in multiplayer mode | default is true
        /// </summary>
        protected bool specific_syncs = true;

        /// <summary>
        /// ui display type | default is ALL_APPLY (all that are allowed to apply based on APPLY_TYPES)
        /// </summary>
        protected UI_TYPES specific_ui_type = UI_TYPES.ALL_APPLY;

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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID
        public IDs ID { get; private set; }
        public ushort ID_num { get; private set; }
        public byte Instance_ID { get; private set; }

        //target
        protected bool target_is_player; //else NPC
        protected int target_index;
        protected MPlayer target_mplayer;
        protected MNPC target_mnpc;

        //owner
        protected bool owner_is_player; //else NPC
        protected int owner_index;
        protected MPlayer owner_mplayer;
        protected MNPC owner_MNPC;

        //generic
        private DateTime time_end, time_next_effect;
        protected bool locally_owned; //the local client is the owner (always false for server)
        private bool local_enforce_duration; //is this client (or server) responsible for enforcing duration
        protected Dictionary<AUTOSYNC_DATA_TYPES, float> autosync_data;

        //for UI
        public string Time_Remaining_String { get; private set; }
        private int time_remaining_min = -1;
        private int time_remaining_sec = -1;
        private long time_remaining_ticks = long.MaxValue;
        public bool draw = false;

        //set during init
        private int texture_index = TEXTURE_INDEX_NONE; 

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Core Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Status(IDs id) {
            ID = id;
            ID_num = (ushort)id;
            Instance_ID = Utilities.Containers.StatusList.UNASSIGNED_INSTANCE_KEY;
            Time_Remaining_String = "";
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
        /// Returns the status texture (default texture if not set)
        /// </summary>
        /// <returns></returns>
        public Texture2D Texture() {
            if (texture_index == TEXTURE_INDEX_NONE) {
                return Utilities.Textures.TEXTURE_STATUS_DEFAULT;
            }
            else {
                return Textures[texture_index];
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
            //if this was being drawn, need complete redraw
            if (draw) {
                UI.UIStatus.needs_redraw_complete = true;
            }

            //remove
            if (target_is_player) {
                target_mplayer.Statuses.Remove(this);
            }
            else {
                target_mnpc.Statuses.Remove(this);
            }
        }

        protected void RemoveEverywhere() {
            //remove locally
            RemoveLocally();

            //remove everywhere else (send end packet)
            if (specific_syncs) {
                //TODO - send remove packet
            }
        }

        protected void Add(int target_index, int owner_index, Dictionary<AUTOSYNC_DATA_TYPES, float> sync_data = null, bool target_is_player = true, bool owner_is_player = true) {
            //TODO
            //check if target is valid
        }

        /// <summary>
        /// Called on every cycles even if effect is not applied. Returns false if status was removed.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool PreUpdate() {
            //remove?
            bool remove = PreUpdate_CheckRemoval();
            if (remove) {
                RemoveEverywhere();
            }
            else {
                //new string for ui?
                if (draw) {
                    TimeSpan time_remain = time_end.Subtract(ExperienceAndClasses.Now);
                    time_remaining_ticks = time_remain.Ticks;
                    int time_remaining_min_now = time_remain.Minutes;
                    int time_remaining_sec_now = time_remain.Seconds;
                    if ((time_remaining_min_now != time_remaining_min) || (time_remaining_sec_now != time_remaining_sec)) {
                        time_remaining_min = time_remaining_min_now;
                        time_remaining_sec = time_remaining_sec_now;
                        if (time_remaining_min > 0) {
                            Time_Remaining_String = time_remaining_min + " min";
                        }
                        else {
                            Time_Remaining_String = time_remaining_sec + " sec";
                        }
                        //a new string was created so ui must update
                        UI.UIStatus.needs_redraw_times_only = true;
                    }
                }

                //set as not part of UI
                draw = false;

                //TODO

            }

            //return removal
            return remove;
        }

        /// <summary>
        /// Check if status should be removed. Includes calls to override methods ShouldRemove and ShouldRemoveLocal.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        private bool PreUpdate_CheckRemoval() {
            //remove if this client/server is responsible for the duration (else update time remaining if target is a player)
            if (local_enforce_duration && (ExperienceAndClasses.Now.CompareTo(time_end) >= 0)) { //timeup
                return true;
            }

            //requirements: server/singleplayer (not client) checks
            if (!Utilities.Netmode.IS_CLIENT) {
                //remove if owner player leaves
                if ((specific_remove_if_owner_player_leaves || (specific_duration_type == DURATION_TYPES.TOGGLE)) && owner_is_player && !owner_mplayer.player.active) {
                    return true;
                }

                //remove if owner died
                if (specific_remove_on_owner_death) {
                    if (owner_is_player && owner_mplayer.player.dead) { //player owner died
                        return true;
                    }
                    else if (!Main.npc[owner_index].active) { //npc owner died
                        return true;
                    }
                }

                //remove if target died
                if (specific_remove_on_target_death) {
                    if (target_is_player && target_mplayer.player.dead) { //player target died
                        return true;
                    }
                    else if (!Main.npc[target_index].active) { //npc target died
                        return true;
                    }
                }

                //remove if target lacks required status
                if (specific_target_required_status != IDs.NONE) {
                    if (target_is_player && !target_mplayer.Statuses.Contains(specific_target_required_status)) {
                        return true;
                    }
                    else if (target_mnpc.Statuses.Contains(specific_target_required_status)) {
                        return true;
                    }
                }

                //remove if target has antirequisite status
                if (specific_target_antirequisite_status != IDs.NONE) {
                    if (target_is_player && target_mplayer.Statuses.Contains(specific_target_antirequisite_status)) {
                        return true;
                    }
                    else if (target_mnpc.Statuses.Contains(specific_target_antirequisite_status)) {
                        return true;
                    }
                }
            }

            //requirements: local
            if (locally_owned) { //owner is always player if locally_owned
                //required status
                if ((specific_owner_player_required_status != IDs.NONE) && !owner_mplayer.Statuses.Contains(specific_owner_player_required_status)) {
                    return true;
                }

                //required passive
                if ((specific_owner_player_required_passive != Systems.Passive.IDs.NONE) && !owner_mplayer.Passives.Contains(specific_owner_player_required_passive)) {
                    return true;
                }

                //status-specific check
                if (ShouldRemoveLocal()) {
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

        public bool IsBetterThan(Status status) {
            if (ID != status.ID) {
                //different types of status
                return false;
            }
            else if (autosync_data.Count == 0) {
                //no autosync data to check
                return false;
            }
            //TODO - compare each autosync_data
            //TODO - check duration if response for durations

            return false;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods To Override (Required) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/



        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods To Override (Optional) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        //these are for any additional status-specific code
        protected virtual void OnStart() {}
        protected virtual void OnUpdate() {}
        protected virtual void OnEnd() {}
        protected virtual void DoEffect() {}
        protected virtual bool ShouldRemove() { return false; }
        protected virtual bool ShouldRemoveLocal() { return false; }
        protected virtual void Merge() {}

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void ProcessStatuses(Utilities.Containers.StatusList statuses) {
            //reset ui status list
            UI.UIStatus.status_to_draw = new SortedList<float, Status>();

            bool removed;
            Status reference;
            SortedList<int, Status> best_per_owner;
            Status best;
            foreach (List<Status> status_list in statuses.GetAllStatuses()) {
                reference = status_list[0];
                best = reference;
                best_per_owner = new SortedList<int, Status>();

                foreach (Status status in status_list) {
                    removed = status.PreUpdate();
                    if (!removed) {

                        switch (reference.specific_apply_type) {
                            case APPLY_TYPES.ALL:
                                status.DoEffect();
                                if (reference.specific_ui_type == UI_TYPES.ALL_APPLY) {
                                    status.draw = true;
                                    UI.UIStatus.status_to_draw.Add(status.time_remaining_ticks, status);
                                }
                                break;
                                break;
                            case APPLY_TYPES.BEST_PER_OWNER:
                                best_per_owner = new SortedList<int, Status>();
                                break;
                        }



                    }
                }




            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Example ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

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

    }
}
