using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.UI {
    static class Constants {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Colours ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly Color COLOR_UI_PANEL_BACKGROUND = new Color(73, 94, 171, 200);
        public static readonly Color COLOR_UI_PANEL_HIGHLIGHT = new Color(103, 124, 201, 200);
        public static readonly Color COLOR_SUBPANEL = new Color(73, 94, 200, 200);
        public static readonly Color COLOR_BAR_UI = new Color(COLOR_UI_PANEL_BACKGROUND.R, COLOR_UI_PANEL_BACKGROUND.G, COLOR_UI_PANEL_BACKGROUND.B, 50);

        public static readonly Color COLOUR_CLASS_PRIMARY = new Color(128, 255, 0, 200);
        public static readonly Color COLOUR_CLASS_SECONDARY = new Color(250, 220, 0, 200);

        public static readonly Color COLOUR_MESSAGE_ERROR = new Color(255, 25, 25);
        public static readonly Color COLOUR_MESSAGE_SUCCESS = new Color(25, 255, 25);
        public static readonly Color COLOUR_MESSAGE_TRACE = new Color(255, 0, 255);
        public static readonly Color COLOUR_MESSAGE_ANNOUNCE = new Color(255, 255, 0);

        public static readonly Color COLOUR_XP_BRIGHT = new Color(0, 255, 0);
        public static readonly Color COLOUR_XP_DIM = new Color(0, 200, 0);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UI ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const float UI_PADDING = 5f;

    }
}
