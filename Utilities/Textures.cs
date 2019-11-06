using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

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
        public static Texture2D TEXTURE_CLASS_BACKGROUND { get; private set; }

        public static Texture2D TEXTURE_STATUS_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_STATUS_BACKGROUND_BUFF { get; private set; }
        public static Texture2D TEXTURE_STATUS_BACKGROUND_DEBUFF { get; private set; }
        public static Texture2D TEXTURE_STATUS_BACKGROUND_DEFAULT { get; private set; }

        public static Texture2D TEXTURE_ABILITY_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_ABILITY_BACKGROUND { get; private set; }
        public static Texture2D TEXTURE_ABILITY_COOLDOWN_COVER { get; private set; }

        public static Texture2D TEXTURE_PASSIVE_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_PASSIVE_BACKGROUND { get; private set; }

        public static Texture2D TEXTURE_RESOURCE_DEFAULT { get; private set; }
        public static Texture2D TEXTURE_RESOURCE_BACKGROUND { get; private set; }
        public static Texture2D TEXTURE_RESOURCE_DOT { get; private set; }

        /// <summary>
        /// (Re)Load all textures
        /// Autoload does not seem to work if loading is done in static variable declarations or in static constructors (because these are not recreated on reload)
        /// </summary>
        public static void LoadTextures() {

            TEXTURE_BLANK = GetTexture("ExperienceAndClasses/Textures/Blank");

            TEXTURE_BUTTON_PLUS = GetTexture("ExperienceAndClasses/Textures/UI/ButtonPlus");
            TEXTURE_BUTTON_MINUS = GetTexture("ExperienceAndClasses/Textures/UI/ButtonMinus");
            TEXTURE_BUTTON_SIZE = TEXTURE_BUTTON_PLUS.Width;

            TEXTURE_CORNER_BUTTON_CLOSE = GetTexture("Terraria/UI/ButtonDelete");
            TEXTURE_CORNER_BUTTON_AUTO = GetTexture("Terraria/UI/ButtonFavoriteActive");
            TEXTURE_CORNER_BUTTON_NO_AUTO = GetTexture("Terraria/UI/ButtonFavoriteInactive");
            TEXTURE_CORNER_BUTTON_SIZE = TEXTURE_CORNER_BUTTON_CLOSE.Width;

            TEXTURE_LOCK_BROWN = GetTexture("ExperienceAndClasses/Textures/UI/Lock_Brown");
            TEXTURE_LOCK_RED = GetTexture("ExperienceAndClasses/Textures/UI/Lock_Red");
            TEXTURE_LOCK_WIDTH = TEXTURE_LOCK_BROWN.Width;
            TEXTURE_LOCK_HEIGHT = TEXTURE_LOCK_BROWN.Height;

            TEXTURE_CLASS_DEFAULT = GetTexture("ExperienceAndClasses/Textures/Class/Default");
            TEXTURE_CLASS_BACKGROUND = GetTexture("ExperienceAndClasses/Textures/Class/Background");

            TEXTURE_STATUS_DEFAULT = GetTexture("ExperienceAndClasses/Textures/Status/Default");
            TEXTURE_STATUS_BACKGROUND_BUFF = GetTexture("ExperienceAndClasses/Textures/Status/Background_Buff");
            TEXTURE_STATUS_BACKGROUND_DEBUFF = GetTexture("ExperienceAndClasses/Textures/Status/Background_Debuff");
            TEXTURE_STATUS_BACKGROUND_DEFAULT = GetTexture("ExperienceAndClasses/Textures/Status/Background_Default");

            TEXTURE_ABILITY_DEFAULT = GetTexture("ExperienceAndClasses/Textures/Ability/Default");
            TEXTURE_ABILITY_COOLDOWN_COVER = GetTexture("ExperienceAndClasses/Textures/Ability/Cooldown");
            TEXTURE_ABILITY_BACKGROUND = GetTexture("ExperienceAndClasses/Textures/Ability/Background");

            TEXTURE_PASSIVE_DEFAULT = GetTexture("ExperienceAndClasses/Textures/Passive/Default");
            TEXTURE_PASSIVE_BACKGROUND = GetTexture("ExperienceAndClasses/Textures/Passive/Background");

            TEXTURE_RESOURCE_DEFAULT = GetTexture("ExperienceAndClasses/Textures/Resource/Default");
            TEXTURE_RESOURCE_BACKGROUND = GetTexture("ExperienceAndClasses/Textures/Resource/Background");
            TEXTURE_RESOURCE_DOT = GetTexture("ExperienceAndClasses/Textures/Resource/Dot");

            foreach (Systems.PlayerClass c in Systems.PlayerClass.LOOKUP) {
                c.LoadTexture();
            }

            /*
            foreach (Systems.Status s in Systems.Status.LOOKUP) {
                s.LoadTexture();
            }

            foreach (Systems.Ability a in Systems.Ability.LOOKUP) {
                a.LoadTexture();
            }

            foreach (Systems.Resource r in Systems.Resource.LOOKUP) {
                r.LoadTexture();
            }

            foreach (Systems.Passive p in Systems.Passive.LOOKUP) {
                p.LoadTexture();
            }
            */
        }

    }
}
