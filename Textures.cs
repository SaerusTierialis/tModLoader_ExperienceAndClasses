using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    class Textures {

        public static Texture2D TEXTURE_BLANK { get; private set; }

        public static Texture2D TEXTURE_BUTTON_PLUS { get; private set; }
        public static Texture2D TEXTURE_BUTTON_MINUS { get; private set; }
        public static float TEXTURE_BUTTON_SIZE { get; private set; }

        public static Texture2D TEXTURE_CORNER_BUTTON_CLOSE { get; private set; }
        public static Texture2D TEXTURE_CORNER_BUTTON_PINNED { get; private set; }
        public static Texture2D TEXTURE_CORNER_BUTTON_UNPINNED { get; private set; }
        public static Texture2D TEXTURE_CORNER_BUTTON_AUTO { get; private set; }
        public static Texture2D TEXTURE_CORNER_BUTTON_NO_AUTO { get; private set; }
        public static float TEXTURE_CORNER_BUTTON_SIZE { get; private set; }

        public static Texture2D TEXTURE_LOCK_BROWN { get; private set; }
        public static Texture2D TEXTURE_LOCK_RED { get; private set; }
        public static float TEXTURE_LOCK_WIDTH { get; private set; }
        public static float TEXTURE_LOCK_HEIGHT { get; private set; }

        /// <summary>
        /// (Re)Load all textures
        /// Autoload does not seem to work if loading is done in static variable declarations or in static constructors (because these are not recreated on reload)
        /// </summary>
        public static void LoadTextures() {
            TEXTURE_BLANK = ModLoader.GetTexture("ExperienceAndClasses/Textures/Blank");

            TEXTURE_BUTTON_PLUS = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonPlus");
            TEXTURE_BUTTON_MINUS = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonMinus");
            TEXTURE_BUTTON_SIZE = TEXTURE_BUTTON_PLUS.Width;

            TEXTURE_CORNER_BUTTON_CLOSE = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonClose");
            TEXTURE_CORNER_BUTTON_PINNED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonPinned");
            TEXTURE_CORNER_BUTTON_UNPINNED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonUnpinned");
            TEXTURE_CORNER_BUTTON_AUTO = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonAuto");
            TEXTURE_CORNER_BUTTON_NO_AUTO = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonUnauto");
            TEXTURE_CORNER_BUTTON_SIZE = TEXTURE_CORNER_BUTTON_CLOSE.Width;

            TEXTURE_LOCK_BROWN = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/Lock_Brown");
            TEXTURE_LOCK_RED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/Lock_Red");
            TEXTURE_LOCK_WIDTH = TEXTURE_LOCK_BROWN.Width;
            TEXTURE_LOCK_HEIGHT = TEXTURE_LOCK_BROWN.Height;

            foreach (Systems.Class c in Systems.Class.CLASS_LOOKUP) {
                c.LoadTexture();
            }
        }

    }
}
