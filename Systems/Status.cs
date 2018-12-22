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
     * there is a hard limit on the number of buffs that a player can have at one time.
     * 
     * Statuses can be used for instant effects, toggles, duration buffs, and more. They can even be displayed with
     * buffs in the top left UI.
     * 
     * Statuses have builtin syncing and unified duration (the player with the status is the only one who can end it).
    */
    public abstract class Status {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ IDs (order does not matter) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum IDs : uint {
            Heal,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static Status[] LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        static Status() {
            LOOKUP = new Status[(uint)Status.IDs.NUMBER_OF_IDs];
            string[] IDs = Enum.GetNames(typeof(IDs));
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = (Status)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Status).FullName + "+" + IDs[i]));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public uint ID { get; protected set; }

        public int Owner { get; protected set; }

        public Texture2D Texture { get; protected set; }
        private string texture_path;

        public bool Unlimited_Duration { get; private set; }
        public uint Duration_msec { get; private set; }
        private DateTime time_end;
        public float Percent_Remaining { get; private set; }

        public Status(IDs id) {
            ID = (uint)id;

            //defaults
            Owner = -1;
            texture_path = null;
            Unlimited_Duration = false;
            Duration_msec = 0;
            time_end = DateTime.MinValue;
            Percent_Remaining = 1f;
        }

        public void Update() {
            if (!ExperienceAndClasses.IS_SERVER && (Owner==Main.LocalPlayer.whoAmI)) {
                //end conditions
            }
            UpdateSpecific();
        }

        public virtual void UpdateSpecific() { }

        public void LoadTexture() {
            if (texture_path != null) {
                Texture = ModLoader.GetTexture(texture_path);
            }
            else {
                Texture = Textures.TEXTURE_STATUS_DEFAULT;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Statuses ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Heal : Status {
            public Heal() : base(IDs.Heal) {
                texture_path = "ExperienceAndClasses/Textures/Status/Heal";
            }
        }
    }
}
