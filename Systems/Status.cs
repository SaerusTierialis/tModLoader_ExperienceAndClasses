using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
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
        public Texture2D Texture { get; protected set; }
        private string texture_path;

        public Status(IDs id) {
            ID = (uint)id;
        }

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
