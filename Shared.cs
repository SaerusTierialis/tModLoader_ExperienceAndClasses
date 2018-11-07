using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses {

    //shared constants and the like
    class Shared {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP & Levels ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const byte LEVEL_REQUIRED_TIER_2 = 10;
        public const byte LEVEL_REQUIRED_TIER_3 = 50;

        public static readonly byte[] MAX_LEVEL = new byte[] { 0, 10, 50, 100 };

        public const double SUBCLASS_PENALTY_MULTIPLIER_PRIMARY = 0.7;
        public const double SUBCLASS_PENALTY_MULTIPLIER_SECONDARY = 0.4;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Colours ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Color COLOR_UI_PANEL_BACKGROUND = new Color(73, 94, 171);
        public static readonly Color COLOR_UI_PANEL_HIGHLIGHT = new Color(103, 124, 201);

        public static readonly Color COLOUR_CLASS_PRIMARY = new Color(128, 255, 0);
        public static readonly Color COLOUR_CLASS_SECONDARY = new Color(250, 220, 0);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Common Textures ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Texture2D TEXTURE_BLANK = ModLoader.GetTexture("ExperienceAndClasses/Textures/Blank");

        public static readonly Texture2D TEXTURE_BUTTON_PLUS = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonPlus");
        public static readonly Texture2D TEXTURE_BUTTON_MINUS = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonMinus");
        public const float TEXTURE_BUTTON_SIZE = 22f;

        public static readonly Texture2D TEXTURE_CORNER_BUTTON_CLOSE = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonClose");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_PINNED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonPinned");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_UNPINNED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonUnpinned");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_AUTO = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonAuto");
        public static readonly Texture2D TEXTURE_CORNER_BUTTON_NO_AUTO = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonUnauto");
        public const float TEXTURE_CORNER_BUTTON_SIZE = 22f;

        public static readonly Texture2D TEXTURE_LOCK = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/Lock_24_30");
        public static readonly float TEXTURE_LOCK_WIDTH = TEXTURE_LOCK.Width;
        public static readonly float TEXTURE_LOCK_HEIGHT = TEXTURE_LOCK.Height;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const float UI_PADDING = 5f;

    }
}
