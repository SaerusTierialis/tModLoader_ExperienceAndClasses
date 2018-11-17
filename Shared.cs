using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses {

    //shared constants and the like
    class Shared {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Colours ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Color COLOR_UI_PANEL_BACKGROUND = new Color(73, 94, 171);
        public static readonly Color COLOR_UI_PANEL_HIGHLIGHT = new Color(103, 124, 201);

        public static readonly Color COLOUR_CLASS_PRIMARY = new Color(128, 255, 0);
        public static readonly Color COLOUR_CLASS_SECONDARY = new Color(250, 220, 0);

        public static readonly Color COLOUR_MESSAGE_ERROR = new Color(255, 25, 25);
        public static readonly Color COLOUR_MESSAGE_SUCCESS = new Color(25, 255, 25);
        public static readonly Color COLOUR_MESSAGE_TRACE = new Color(255, 0, 255);
        public static readonly Color COLOUR_MESSAGE_ANNOUNCE = new Color(255, 255, 0);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const float UI_PADDING = 5f;

    }
}
