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
    */
    public abstract class Status {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ IDs (order does not matter) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum IDs : uint {
            Heal,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        public enum SYNC_DATA_TYPES : byte {
            MAGNITUDE,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Status[] LOOKUP { get; private set; }

        static Status() {
            LOOKUP = new Status[(uint)Status.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + IDs[i]));
                LOOKUP[i].prevent_reuse = true;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //ID (must override)
        public abstract IDs ID();
        public abstract uint ID_num();

        //core info (can override)
        protected virtual string Core_Texture_Path { get { return null; } }
        public virtual bool Core_Duration_Instant { get { return false; } }
        public virtual bool Core_Duration_Unlimited { get { return false; } }
        public virtual uint Core_Duration_msec { get { return 0; } }
        public virtual bool Core_Sync { get { return true; } }

        //public variables
        public MPlayer Owner { get; protected set; }
        public MPlayer Target { get; protected set; }
        public double Percent_Remaining { get; protected set; } //track duration on local player only

        //protected variables
        protected Dictionary<SYNC_DATA_TYPES, double> sync_data;

        //private variables
        private Texture2D texture;
        private DateTime time_end;
        private bool prevent_reuse; //lookup instances are locked to prevent accidental reuse

        //get texture from any instance
        public Texture2D Texture {
            get {
                if (texture != null) {
                    return texture;
                }
                else {
                    return LOOKUP[ID_num()].Texture;
                }
            }
            protected set {
                texture = value;
            }
        }

        public Status() {
            //defaults
            time_end = DateTime.MinValue;
            Percent_Remaining = 100f;
            Owner = null;
            Target = null;
            prevent_reuse = false;
            sync_data = new Dictionary<SYNC_DATA_TYPES, double>();
        }

        public void Update() {
            //target of status ends it if out of time or owner has left //TODO or if owner no longer has source ability
            if (Target.player.whoAmI == Main.LocalPlayer.whoAmI) {
                Percent_Remaining = time_end.Subtract(DateTime.Now).TotalMilliseconds / Core_Duration_msec;
                if ((!Core_Duration_Instant && Percent_Remaining < 0) || !Main.player[Owner.player.whoAmI].Equals(Owner.player)) {
                    Remove();
                    return;
                }
            }
            OnUpdate();
        }

        protected void Add(Player target, MPlayer owner) {
            //don't allow add if locked (do not reuse lookup instance!)
            if (prevent_reuse) {
                Commons.Error("Status lookup instance attempted reuse for type " + GetType());
                return;
            }

            //set
            Target = target.GetModPlayer<MPlayer>();
            Owner = owner;

            //calculate end time if there is one
            if (!Core_Duration_Instant && !Core_Duration_Unlimited ) {
                time_end = DateTime.Now.AddMilliseconds(Core_Duration_msec);
            }

            //add to target (unless instant)
            bool allow_update = true;
            if (!Core_Duration_Instant) {
                if (Target.HasStatus(ID())) {
                    allow_update = TryMerge();
                }
                else {
                    Target.Status[ID_num()] = this;
                }
            }

            //if owner is local and netmode is multiplayer, send to other clients (unless not sync)
            if (Core_Sync && ExperienceAndClasses.IS_CLIENT && Owner.Equals(ExperienceAndClasses.LOCAL_MPLAYER)) {
                SendPacket();
            }

            //on start events
            OnStart();
        }

        protected bool TryMerge() {
            //TODO (merge, keep highest values)
            //dont merge if the only thing that has changed is duration and the duration has changed by less than 0.5 sec
            return true;
        }

        public void Remove() {
            //remove (unless instant)
            if (!Core_Duration_Instant) {
                Target.Status[ID_num()] = null;
            }

            OnEnd();
        }

        public void SendPacket() {
            //send the status info to other clients
            //TODO
        }
        public void WritePacketBody() {
            //write the packet core:
            //1:  ID
            //2:  byte number of extra data values
            //3+: extra data values in enum order 
            //TODO
        }
        public void ReadPacketBody() {
            //TODO
        }

        public void LoadTexture() {
            if (Core_Texture_Path != null) {
                Texture = ModLoader.GetTexture(Core_Texture_Path);
            }
            else {
                Texture = Textures.TEXTURE_STATUS_DEFAULT;
            }
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
            public override uint ID_num() { return (uint)ID(); }

            //may override any core info
            protected override string Core_Texture_Path { get { return "ExperienceAndClasses/Textures/Status/Heal"; } }

            //must inlcude a static add method which creates a new instance (DO NOT REUSE THE LOOKUP INSTANCES)
            //add any non-core info to sync_data
            public static void Add(Player target, MPlayer owner, double magnitude) {
                Status status = new Heal();
                status.sync_data.Add(SYNC_DATA_TYPES.MAGNITUDE, magnitude);
                status.Add(target, owner);
            }

            //optional overrides (base methods are empty)
            protected override void OnStart() {}
            protected override void OnUpdate() { }
            protected override void OnEnd() { }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Statuses ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/


    }
}
