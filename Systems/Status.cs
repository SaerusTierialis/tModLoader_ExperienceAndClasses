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
     * 
     * TODO:
     * 1. trymerge
     * 2. finish sync
     * 3. update status ui
     * 4. add player draw
     * 5. add non-player draw (if separate)
     * 6. add full sync
    */
    public abstract class Status {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ IDs (order does not matter) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum IDs : short {
            Heal,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        protected enum SYNC_DATA_TYPES : byte {
            MAGNITUDE,
        }
        protected static readonly IEnumerable<SYNC_DATA_TYPES> SYNC_DATA_TYPES_STRINGS = Enum.GetValues(typeof(SYNC_DATA_TYPES)).Cast< SYNC_DATA_TYPES>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Status[] LOOKUP { get; private set; }

        static Status() {
            LOOKUP = new Status[(short)Status.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + IDs[i]));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID (must override)
        public abstract IDs ID();
        public abstract short ID_num();

        //core info (can override)
        protected virtual string Core_Texture_Path_Buff { get { return null; } }
        public virtual bool Core_Duration_Instant { get { return false; } }
        public virtual bool Core_Duration_Unlimited { get { return false; } }
        public virtual uint Core_Duration_msec { get { return 0; } }
        public virtual bool Core_Sync { get { return true; } }
        public virtual bool Core_Show_Buff { get { return true; } }
        public virtual string Core_Name { get { return "Undefined"; } }
        public virtual string Core_Description { get { return "Undefined"; } }

        //public variables
        public MPlayer Owner { get; protected set; }
        public MPlayer Target { get; protected set; }
        public double Percent_Remaining { get; protected set; } //track duration on local player only

        //protected variables
        protected Dictionary<SYNC_DATA_TYPES, double> sync_data;

        //private variables
        private Texture2D texture_buff;
        private DateTime time_end;

        //get texture from any instance
        public Texture2D Texture_Buff {
            get {
                if (texture_buff != null) {
                    return texture_buff;
                }
                else {
                    return LOOKUP[ID_num()].Texture_Buff;
                }
            }
            protected set {
                texture_buff = value;
            }
        }

        public Status() {
            //defaults
            time_end = DateTime.MinValue;
            Percent_Remaining = 100;
            Owner = null;
            Target = null;
            sync_data = new Dictionary<SYNC_DATA_TYPES, double>();
        }

        public void Update() {
            //target of status ends it if out of time or owner has left //TODO or if owner no longer has source ability
            if (Target.player.whoAmI == Main.LocalPlayer.whoAmI) {
                //update remaining time
                if (HasDuration()) {
                    Percent_Remaining = time_end.Subtract(DateTime.Now).TotalMilliseconds / Core_Duration_msec * 100;
                }

                //check if needs to end
                if ((Percent_Remaining < 0) || !Main.player[Owner.player.whoAmI].Equals(Owner.player)) {
                    Remove();
                    return;
                }
            }
            OnUpdate();
        }

        protected static void Add(Player target, MPlayer owner, Type type, Dictionary<SYNC_DATA_TYPES, double> sync_data) {
            //create new instance
            Status status = (Status)(Assembly.GetExecutingAssembly().CreateInstance(type.FullName));
            
            //set sync_data
            if (sync_data != null) {
                status.sync_data = sync_data;
            }

            //set
            status.Target = target.GetModPlayer<MPlayer>();
            status.Owner = owner;

            //calculate end time if there is one
            if (status.HasDuration()) {
                status.time_end = DateTime.Now.AddMilliseconds(status.Core_Duration_msec);
            }

            //add to target (unless instant)
            if (!status.Core_Duration_Instant) {
                Status prior_instance = status.Target.Status[status.ID_num()];
                if (prior_instance != null) {
                    if (!prior_instance.TryMerge(status)) {
                        //don't add if cannot merge to existing
                        return;
                    }
                }
                else {
                    status.Target.Status[status.ID_num()] = status;
                    status.OnStart();
                }
            }

            //if owner is local and netmode is multiplayer, send to other clients (unless not sync)
            if (status.Core_Sync && ExperienceAndClasses.IS_CLIENT && status.Owner.Equals(ExperienceAndClasses.LOCAL_MPLAYER)) {
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
            //remove from player (unless it was instant)
            if (!Core_Duration_Instant) {
                Target.Status[ID_num()] = null;
            }



            OnEnd();
        }

        private void SendAddPacket(int origin=-1) {
            //origin is local player unless told otherwise
            if (origin == -1) {
                origin = Main.LocalPlayer.whoAmI;
            }

            //create packet
            ModPacket packet = PacketHandler.AddStatus.Instance.GetPacket(origin);

            //fill packet
            WriteAddPacketBody(packet);

            //send packet
            packet.Send(-1, origin);
        }

        /// <summary>
        /// 1:  ID (int32)
        /// 2:  Owner (byte)
        /// 3:  Target (byte)
        /// 4:  number of extra data values, can be 0 (byte)
        /// 5+: extra data values in enum order  (double)
        /// </summary>
        /// <param name="packet"></param>
        private void WriteAddPacketBody(ModPacket packet) {
            packet.Write(ID_num());
            packet.Write((byte)Owner.player.whoAmI);
            packet.Write((byte)Target.player.whoAmI);

            //gather data
            List<double> data = new List<double>();
            double value;
            foreach (SYNC_DATA_TYPES type in SYNC_DATA_TYPES_STRINGS) {
                if (sync_data.TryGetValue(type, out value)) {
                    data.Add(value);
                }
            }

            //write data
            packet.Write((byte)data.Count);
            foreach(double d in data) {
                packet.Write(d);
            }
        }

        public void ReadAddPacketBody(BinaryReader reader) {
            //TODO
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

        /// <summary>
        /// 1:  ID (int32)
        /// 2:  Owner (byte)
        /// 3:  number of extra data values, can be 0 (byte)
        /// 4+: extra data values in enum order  (double)
        /// </summary>
        /// <param name="packet"></param>
        public void WriteSetPacketBody(ModPacket packet) {
            //TODO
        }

        public void ReadSetPacketBody(BinaryReader reader, MPlayer target) {
            //TODO
        }

        public void LoadTexture() {
            if (Core_Texture_Path_Buff != null) {
                Texture_Buff = ModLoader.GetTexture(Core_Texture_Path_Buff);
            }
            else {
                Texture_Buff = Textures.TEXTURE_STATUS_DEFAULT;
            }
        }

        protected bool HasDuration() {
            return (!Core_Duration_Instant && !Core_Duration_Unlimited);
        }

        //override with any specific effects
        protected virtual void OnUpdate() { }
        protected virtual void OnStart() { }
        protected virtual void OnEnd() { }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Shortcuts to Sync Data ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public double Magitude() {
            return GetData(this, SYNC_DATA_TYPES.MAGNITUDE);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        protected static double GetData(Status status, SYNC_DATA_TYPES key, double default_value=-1) {
            double value = default_value;
            if (!status.sync_data.TryGetValue(key, out value)) {
                Commons.Error("Status attempted to access invalid sync data: " + status.GetType() + " " + key);
            }
            return value;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Example ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Heal : Status {
            //must override ID
            public override IDs ID() { return IDs.Heal; }
            public override short ID_num() { return (short)ID(); }

            //may override any core info
            protected override string Core_Texture_Path_Buff { get { return "ExperienceAndClasses/Textures/Status/Heal"; } }

            //must inlcude a static add method which creates a new instance, adds any extra data, and then adds it to the target
            //if there is no sync_data, just pass null
            public static void Add(Player target, MPlayer owner, double magnitude) {
                Add(target, owner, typeof(Heal), new Dictionary<SYNC_DATA_TYPES, double> {
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
