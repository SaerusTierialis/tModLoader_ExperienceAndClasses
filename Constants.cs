using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ExperienceAndClasses {

    //shared constants and the like
    class Constants {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP & Levels ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const byte MAX_LEVEL = 100;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Colours ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Color COLOUR_ERROR = new Color(1f, 0f, 0f);

        public static readonly Color COLOUR_CLASS_PRIMARY = new Color(0.5f, 1f, 0f);
        public static readonly Color COLOUR_CLASS_SECONDARY = new Color(1f, 0.9f, 0f);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Common Textures ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Texture2D TEXTURE_BLANK = ModLoader.GetTexture("ExperienceAndClasses/Textures/Blank");

        public static readonly Texture2D TEXTURE_LOCK = ModLoader.GetTexture("ExperienceAndClasses/Textures/Lock_24_30");
        public static readonly float TEXTURE_LOCK_WIDTH = TEXTURE_LOCK.Width;
        public static readonly float TEXTURE_LOCK_HEIGHT = TEXTURE_LOCK.Height;

    }
}
