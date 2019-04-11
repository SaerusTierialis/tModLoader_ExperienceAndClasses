using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Utilities {
    class Textures {

        public static Texture2D TEXTURE_BLANK { get; private set; }

        public static Texture2D TEXTURE_BUTTON_PLUS { get; private set; }
        public static Texture2D TEXTURE_BUTTON_MINUS { get; private set; }
        public static float TEXTURE_BUTTON_SIZE { get; private set; }

        public static Texture2D TEXTURE_CORNER_BUTTON_CLOSE { get; private set; }
        public static Texture2D TEXTURE_CORNER_BUTTON_AUTO { get; private set; }
        public static Texture2D TEXTURE_CORNER_BUTTON_NO_AUTO { get; private set; }
        public static float TEXTURE_CORNER_BUTTON_SIZE { get; private set; }

        public static Texture2D TEXTURE_LOCK_BROWN { get; private set; }
        public static Texture2D TEXTURE_LOCK_RED { get; private set; }
        public static float TEXTURE_LOCK_WIDTH { get; private set; }
        public static float TEXTURE_LOCK_HEIGHT { get; private set; }

        public static Texture2D TEXTURE_CLASS_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_STATUS_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_ABILITY_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_ABILITY_COOLDOWN_COVER { get; private set; }
        public static Texture2D TEXTURE_PASSIVE_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_RESOURCE_DEFAULT { get; private set; }

        /// <summary>
        /// (Re)Load all textures
        /// Autoload does not seem to work if loading is done in static variable declarations or in static constructors (because these are not recreated on reload)
        /// </summary>
        public static void LoadTextures() {
            TEXTURE_BLANK = ModLoader.GetTexture("ExperienceAndClasses/Textures/Blank");

            TEXTURE_BUTTON_PLUS = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonPlus");
            TEXTURE_BUTTON_MINUS = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/ButtonMinus");
            TEXTURE_BUTTON_SIZE = TEXTURE_BUTTON_PLUS.Width;

            TEXTURE_CORNER_BUTTON_CLOSE = ModLoader.GetTexture("Terraria/UI/ButtonDelete");
            TEXTURE_CORNER_BUTTON_AUTO = ModLoader.GetTexture("Terraria/UI/ButtonFavoriteActive");
            TEXTURE_CORNER_BUTTON_NO_AUTO = ModLoader.GetTexture("Terraria/UI/ButtonFavoriteInactive");
            TEXTURE_CORNER_BUTTON_SIZE = TEXTURE_CORNER_BUTTON_CLOSE.Width;

            TEXTURE_LOCK_BROWN = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/Lock_Brown");
            TEXTURE_LOCK_RED = ModLoader.GetTexture("ExperienceAndClasses/Textures/UI/Lock_Red");
            TEXTURE_LOCK_WIDTH = TEXTURE_LOCK_BROWN.Width;
            TEXTURE_LOCK_HEIGHT = TEXTURE_LOCK_BROWN.Height;

            TEXTURE_CLASS_DEFAULT = ModLoader.GetTexture("ExperienceAndClasses/Textures/Class/Default");
            TEXTURE_STATUS_DEFAULT = ModLoader.GetTexture("ExperienceAndClasses/Textures/Status/Default");
            TEXTURE_ABILITY_DEFAULT = ModLoader.GetTexture("ExperienceAndClasses/Textures/Ability/Default");
            TEXTURE_ABILITY_COOLDOWN_COVER = ModLoader.GetTexture("ExperienceAndClasses/Textures/Ability/Cooldown");
            TEXTURE_PASSIVE_DEFAULT = ModLoader.GetTexture("ExperienceAndClasses/Textures/Passive/Default");
            TEXTURE_RESOURCE_DEFAULT = ModLoader.GetTexture("ExperienceAndClasses/Textures/Resource/Default");

            foreach (Systems.Class c in Systems.Class.LOOKUP) {
                c.LoadTexture();
            }

            foreach (Systems.Status s in Systems.Status.LOOKUP) {
                s.LoadTexture();
            }

            foreach (Systems.Ability a in Systems.Ability.LOOKUP) {
                a.LoadTexture();
            }

            foreach (Systems.Passive p in Systems.Passive.LOOKUP) {
                p.LoadTexture();
            }

            foreach (Systems.Resource r in Systems.Resource.LOOKUP) {
                r.LoadTexture();
            }

        }

    }
}
