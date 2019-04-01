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

        public enum IDs : ushort {
            Heal,

            //insert here

            NUMBER_OF_IDs //leave this last
        }

        /// <summary>
        /// Auto-syncing data types available (always float)
        /// </summary>
        protected enum AUTOSYNC_DATA_TYPES : byte {
            MAGNITUDE1,
            MAGNITUDE2,
            RANGE,

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
            UNLIMITED,
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
            ONE,
            ALL_APPLY,
            ALL,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const int TEXTURE_INDEX_NONE = -1;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Status[] LOOKUP { get; private set; }

        static Status() {
            LOOKUP = new Status[(ushort)Status.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + IDs[i]));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static List<Texture2D> Textures = new List<Texture2D>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID
        public IDs ID { get; private set; }
        public ushort ID_num { get; private set; }
        public byte Instance_ID { get; private set; }

        //status-specific vars
        protected string specific_texture_path, specific_name, specific_description;
        protected List<AUTOSYNC_DATA_TYPES> specific_autosync_data_types;
        protected bool specific_target_can_be_player, specific_target_can_be_npc, specific_owner_can_be_player, specific_owner_can_be_npc;
        protected DURATION_TYPES specific_duration_type;
        protected float specific_duration_sec;
        protected EFFECT_TYPES specific_effect_type;
        protected float specific_effect_update_frequency_sec;
        protected LIMIT_TYPES specific_limit_type;
        protected APPLY_TYPES specific_apply_type;
        protected bool specific_allow_merge; //when a status would overwrite another, merge them instead (use max duration, magnitude, etc. - use new owner)
        protected bool specific_syncs;
        protected UI_TYPES specific_ui_type;
        protected Systems.Class.IDs specific_owner_required_class;
        protected byte specific_owner_required_class_level;
        protected IDs specific_owner_required_status;
        protected IDs specific_target_required_status;

        //target
        protected bool target_is_player; //else NPC
        protected MPlayer target_mplayer;
        protected MNPC target_MNPC;

        //owner
        protected bool owner_is_player; //else NPC
        protected MPlayer owner_mplayer;
        protected MNPC owner_MNPC;

        //generic
        private DateTime time_end, time_next_effect;
        protected bool locally_owned; //the local client is the owner (always false for server)
        private bool check_duration; //is this client (or server) responsible for enforcing duration
        protected Dictionary<AUTOSYNC_DATA_TYPES, float> autosync_data;

        //for UI
        public string Time_Remaining { get; private set; }
        protected ushort time_remaining_min, time_remaining_sec;

        //set during init
        private int texture_index; 

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Status(IDs id) {
            //ID
            ID = id;
            ID_num = (ushort)id;
            Instance_ID = Utilities.Containers.StatusList.UNASSIGNED_INSTANCE_KEY;
            
            //status-specific defaults
            specific_name = "default_status";
            specific_description = "default_description";
            specific_autosync_data_types = new List<AUTOSYNC_DATA_TYPES>();
            specific_target_can_be_player = specific_target_can_be_npc = specific_owner_can_be_player = specific_owner_can_be_npc = true;
            specific_duration_type = DURATION_TYPES.TOGGLE;
            specific_duration_sec = -1;
            specific_effect_type = EFFECT_TYPES.NONE;
            specific_effect_update_frequency_sec = -1;
            specific_limit_type = LIMIT_TYPES.UNLIMITED;
            specific_apply_type = APPLY_TYPES.ALL;
            specific_allow_merge = true;
            specific_syncs = true;
            specific_ui_type = UI_TYPES.ALL;

            //generic
            autosync_data = new Dictionary<AUTOSYNC_DATA_TYPES, float>();

            //texture
            texture_index = TEXTURE_INDEX_NONE;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Run one during init
        /// </summary>
        public void LoadTexture() {
            if (specific_texture_path != null) {
                texture_index = (ushort)Textures.Count;
                Textures.Add(ModLoader.GetTexture(specific_texture_path));
            }
        }

        public Texture2D Texture() {
            if (texture_index == TEXTURE_INDEX_NONE) {
                return Utilities.Textures.TEXTURE_STATUS_DEFAULT;
            }
            else {
                return Textures[texture_index];
            }
        }

        public void SetInstanceID(byte instance_id) {
            Instance_ID = instance_id;
        }

        /// <summary>
        /// Update the time remaining string. Returns true if the string has changed.
        /// </summary>
        /// <returns></returns>
        private bool UpdateTimeRemaining() {
            //TODO
            return false;
        }

        protected void RemoveLocally() {
            //TODO
        }

        protected void RemoveEverywhere() {
            //TODO
        }

        protected void Add(int target_index, int owner_index, Dictionary<AUTOSYNC_DATA_TYPES, float> sync_data, bool target_is_player = true, bool owner_is_player = true) {
            //TODO
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods To Override ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        protected virtual void OnStart() {}

        protected virtual void OnUpdate() {}

        protected virtual void OnEnd() {}

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
