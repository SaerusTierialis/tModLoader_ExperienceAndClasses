using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace ExperienceAndClasses.Systems {
    public class Passive {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// includes ability unlocking, resource unlocking, ability altering, and passive effects (i.e., toggles status)
        /// </summary>
        public enum IDs : ushort {


            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        public enum PASSIVE_TYPE : byte {
            RESOURCE_UNLOCK,
            RESOURCE_UPGRADE,
            ABILITY_UPGRADE,
            STAT_BONUS,
            MISC,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Fields ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private readonly string INTERNAL_NAME;

        public readonly IDs ID;
        public readonly ushort ID_num;
        public readonly PASSIVE_TYPE Type;
        public readonly PlayerClass.IDs Required_Class;
        public readonly byte Required_Class_num;
        public readonly byte Required_Class_Level;
        public readonly Color Colour;

        public Texture2D Texture { get; private set; } = null;
        public Texture2D Texture_Background { get; private set; } = null;

        public string Name { get { return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Passive_" + INTERNAL_NAME + "_Name"); } }
        public string Description { get { return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Passive_" + INTERNAL_NAME + "_Description"); } }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public bool Unlocked { get; private set; } = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Passive(IDs id, PASSIVE_TYPE type, PlayerClass.IDs class_id, byte level) {
            ID = id;
            ID_num = (ushort)id;
            INTERNAL_NAME = Enum.GetName(typeof(IDs), ID_num);
            Type = type;
            Required_Class = class_id;
            Required_Class_num = (byte)class_id;
            Required_Class_Level = level;
            Colour = PlayerClass.LOOKUP[Required_Class_num].Colour;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Run once during init
        /// </summary>
        public void LoadTexture() {
            //TODO
        }

    }
}
