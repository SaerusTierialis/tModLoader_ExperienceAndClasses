using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ExperienceAndClasses {

    //shared constants and the like
    class Shared {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP & Levels ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const byte MAX_LEVEL = 100;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Colours ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Color COLOUR_ERROR = new Color(1f, 0f, 0f);

        public static readonly Color COLOUR_CLASS_PRIMARY = new Color(0.5f, 1f, 0f);
        public static readonly Color COLOUR_CLASS_SECONDARY = new Color(1f, 0.9f, 0f);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Common Textures ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Texture2D TEXTURE_BLANK = ModLoader.GetTexture("ExperienceAndClasses/Textures/Blank");

        public static readonly Texture2D TEXTURE_CORNER_BUTTON_CLOSE = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonClose");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_PINNED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonPinned");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_UNPINNED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonUnpinned");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_AUTO = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonAuto");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_NO_AUTO = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonUnauto");
        public const float TEXTURE_CORNER_BUTTON_SIZE = 22f;

        public static readonly Texture2D TEXTURE_LOCK = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/Lock_24_30");
        public static readonly float TEXTURE_LOCK_WIDTH = TEXTURE_LOCK.Width;
        public static readonly float TEXTURE_LOCK_HEIGHT = TEXTURE_LOCK.Height;

    }
}
