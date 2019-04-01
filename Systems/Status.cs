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
    /*
     * TODO check if this is still accurate after changes
     * 
     * Statuses are like the builtin buff system, but can carry any amount of extra data. The system exists because
     * there is a hard limit on the number of buffs that a player can have at one time and the buff system is limited
     * in the amount of data that is automatically synced.
     * 
     * Statuses can be used for instant effects, toggles, duration buffs, and much more. They can even be displayed with
     * buffs in the top left UI. Even abilities such as the support class' Sanctuary can be implemented as a status (with
     * a location in the sync data).
     * 
     * Statuses have builtin syncing. Duration checking is handled by the client who is targeted by the status.
     * 
     * The lookup instances cannot be reused. A new instance must be created when a status is added to a player.
    */
    public abstract class Status {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ IDs (order does not matter) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum IDs : ushort {
            Heal,

            //insert here

            NUMBER_OF_IDs //leave this last
        }

        protected enum SYNC_DATA_TYPES : byte {
            MAGNITUDE,

            //insert here

            NUMBER_OF_TYPES //leave this last
        }
        protected static readonly IEnumerable<SYNC_DATA_TYPES> SYNC_DATA_TYPES_STRINGS = Enum.GetValues(typeof(SYNC_DATA_TYPES)).Cast< SYNC_DATA_TYPES>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Status[] LOOKUP { get; private set; }

        static Status() {
            LOOKUP = new Status[(ushort)Status.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + IDs[i]));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID
        public IDs ID { get; private set; }
        public ushort ID_num { get; private set; }

        //core info (overwrite default as needed)
        protected string core_texture_path_buff;
        protected bool core_duration_instant;
        protected bool core_duration_unlimited;
        protected uint core_duration_msec;
        protected bool core_sync;
        public bool core_show_buff { get; protected set; }
        public string core_display_name { get; protected set; }
        public string core_description { get; protected set; }
        protected List<SYNC_DATA_TYPES> core_sync_data_types;

        //public variables
        public MPlayer Owner { get; protected set; }
        public MPlayer Target { get; protected set; }
        public double Time_Remaining_Percent { get; protected set; } //track duration on local player only
        public uint Time_Remaining_msec { get; protected set; }

        //protected variables
        protected Dictionary<SYNC_DATA_TYPES, double> sync_data;

        //private variables
        private Texture2D texture_buff;
        private DateTime time_end;

        //public instance_id (don't touch this except in StatusList)
        public byte instance_id;

        //get texture from any instance
        public Texture2D Texture_Buff {
            get {
                if (texture_buff != null) {
                    return texture_buff;
                }
                else {
                    return LOOKUP[ID_num].Texture_Buff;
                }
            }
            protected set {
                texture_buff = value;
            }
        }

        public Status(IDs id) {
            //ID
            ID = id;
            ID_num = (ushort)id;

            //core info defaults
            core_display_name = "Undefined";
            core_description = "Undefined";
            core_show_buff = false;
            core_duration_instant = false;
            core_duration_unlimited = false;
            core_duration_msec = 5000;
            core_texture_path_buff = null;
            core_sync = true;
            core_sync_data_types = new List<SYNC_DATA_TYPES>();

            //defaults
            time_end = DateTime.MinValue;
            Time_Remaining_Percent = 100;
            Owner = null;
            Target = null;
            sync_data = new Dictionary<SYNC_DATA_TYPES, double>();
            instance_id = Utilities.Containers.StatusList.UNASSIGNED_INSTANCE_KEY;
        }

        public void Update() {
            //target of status ends it if out of time or owner has left //TODO or if owner no longer has source ability
            if (Target.player.whoAmI == Main.LocalPlayer.whoAmI) {
                //update remaining time
                if (HasDuration()) {
                    Time_Remaining_Percent = time_end.Subtract(DateTime.Now).TotalMilliseconds / core_duration_msec * 100;
                }

                //check if needs to end
                if ((Time_Remaining_Percent < 0) || !Main.player[Owner.player.whoAmI].Equals(Owner.player)) {
                    Remove();
                    return;
                }
            }
            OnUpdate();
        }

        protected static void Add(Player target, MPlayer owner, IDs ID, Dictionary<SYNC_DATA_TYPES, double> sync_data) {
            //create new instance
            Status status = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + ID));
            
            //set sync_data
            if (sync_data != null) {
                status.sync_data = sync_data;
            }

            //set
            status.Target = target.GetModPlayer<MPlayer>();
            status.Owner = owner;

            //calculate end time if there is one
            if (status.HasDuration()) {
                status.time_end = DateTime.Now.AddMilliseconds(status.core_duration_msec);
            }

            //TODO

            /*
            //add to target (unless instant)
            if (!status.core_duration_instant) {
                Status prior_instance = status.Target.Status[status.ID_num];
                if (prior_instance != null) {
                    if (!prior_instance.TryMerge(status)) {
                        //don't add if cannot merge to existing
                        return;
                    }
                }
                else {
                    status.Target.Status[status.ID_num] = status;
                    status.OnStart();
                }
            }
            */

            //if owner is local and netmode is multiplayer, send to other clients (unless not sync)
            if (status.core_sync && Utilities.Netmode.IS_CLIENT && status.Owner.Equals(ExperienceAndClasses.LOCAL_MPLAYER)) {
                status.SendAddPacket();
            }
        }

        /// <summary>
        /// merge two instances of a status keeping the highest values
        /// return false if nothing other than 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        protected bool TryMerge(Status status) {
            //TODO (merge, keep highest values)
            //dont merge if the only thing that has changed is duration and the duration has changed by less than 0.5 sec
            return true;
        }

        public void Remove() {
            //remove from player
            Target.Statuses.RemoveStatus(this);

            //TODO

            OnEnd();
        }

        private void SendAddPacket(int origin=-1) {
            //origin is local player unless told otherwise
            if (origin == -1) {
                origin = Main.LocalPlayer.whoAmI;
            }

            //create packet
            ModPacket packet = Utilities.PacketHandler.AddStatus.Instance.GetPacket(origin);

            //fill packet
            WriteAddPacketBody(packet);

            //send packet
            packet.Send(-1, origin);
        }

        /// <summary>
        /// 1:  ID (ushort = uint16)
        /// 2:  Owner (byte)
        /// 3:  Target (byte)
        /// 4+: extra data values in enum order  (double[])
        /// </summary>
        /// <param name="packet"></param>
        private void WriteAddPacketBody(ModPacket packet) {
            //1:  ID (ushort = uint16)
            packet.Write(ID_num);

            //2:  Owner (byte)
            packet.Write((byte)Owner.player.whoAmI);

            //3:  Target (byte)
            packet.Write((byte)Target.player.whoAmI);

            //4+: extra data values in enum order  (double[])
            foreach (SYNC_DATA_TYPES type in core_sync_data_types) {
                packet.Write(GetData(type));
            }
        }

        public void ReadAddPacketBody(BinaryReader reader) {
            //1:  ID (ushort = uint16)
            int id_num = reader.ReadUInt16();
            IDs id = LOOKUP[id_num].ID;

            //2:  Owner (byte)
            byte owner_byte = reader.ReadByte();
            MPlayer owner = Main.player[owner_byte].GetModPlayer<MPlayer>();

            //3:  Target (byte)
            byte target_byte = reader.ReadByte();
            Player target = Main.player[target_byte];

            //4+: extra data values in enum order  (double[])
            Dictionary<SYNC_DATA_TYPES, double> data = new Dictionary<SYNC_DATA_TYPES, double>();
            foreach (SYNC_DATA_TYPES type in core_sync_data_types) {
                data.Add(type, reader.ReadDouble());
            }

            //Add status
            Add(target, owner, ID, data);
        }

        /// <summary>
        /// 1:  ID (int32)
        /// 2:  Owner (byte)
        /// 3:  Target (byte)
        /// </summary>
        private void SendRemovePacket() {
            //TODO
        }

        public void ReadRemovePacketBody(BinaryReader reader) {
            //TODO
        }

        public void LoadTexture() {
            if (core_texture_path_buff != null) {
                Texture_Buff = ModLoader.GetTexture(core_texture_path_buff);
            }
            else {
                Texture_Buff = Utilities.Textures.TEXTURE_STATUS_DEFAULT;
            }
        }

        public bool HasDuration() {
            return (!core_duration_instant && !core_duration_unlimited);
        }

        //override with any specific effects
        protected virtual void OnUpdate() { }
        protected virtual void OnStart() { }
        protected virtual void OnEnd() { }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Shortcuts to Sync Data ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        protected double GetData(SYNC_DATA_TYPES key, double default_value = -1) {
            double value = default_value;
            if (!sync_data.TryGetValue(key, out value)) {
                Utilities.Commons.Error("Status attempted to access invalid sync data: " + GetType() + " " + key);
            }
            return value;
        }

        public double Magitude { get { return GetData(SYNC_DATA_TYPES.MAGNITUDE); } }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Example ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Heal : Status {
            public Heal() : base(IDs.Heal) {
                //any overwrites
                core_display_name = "Heal"; //not needed unless displayed as a buff
                core_texture_path_buff = "ExperienceAndClasses/Textures/Status/Heal"; //not needed unless displayed as a buff

                //add any sync data types that will be used (for syncing)
                core_sync_data_types.Add(SYNC_DATA_TYPES.MAGNITUDE);
            }

            //must inlcude a static add method with target/owner and any extra info
            public static void Add(Player target, MPlayer owner, double magnitude) {
                Add(target, owner, IDs.Heal, new Dictionary<SYNC_DATA_TYPES, double> {
                    { SYNC_DATA_TYPES.MAGNITUDE, magnitude }
                });
            }

            //optional overrides (base methods are empty)
            protected override void OnStart() { }
            protected override void OnUpdate() { }
            protected override void OnEnd() { }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Statuses ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/


    }
}
