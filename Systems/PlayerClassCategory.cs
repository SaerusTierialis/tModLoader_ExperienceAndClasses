using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace ExperienceAndClasses.Systems
{
    public abstract class PlayerClassCategory
    {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants/Readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum TYPES : byte
        {
            Novice,
            CloseCombat,
            Projectile,
            Control,
            Stealth,
            Minion,
            Eclipse,
            Musical,
            Mechanical,
            Hybrid,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        public enum RECOMMENDED_WEAPON : byte
        {
            Any,
            Minion,
            NonMinion,
            Projectile,
            Multiple,
        }

        public readonly static Color COLOUR_DEFAULT = new Color(255, 255, 255);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static readonly PlayerClassCategory[] LOOKUP;

        static PlayerClassCategory()
        {
            LOOKUP = new PlayerClassCategory[(byte)PlayerClassCategory.TYPES.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++)
            {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<PlayerClassCategory>(Enum.GetName(typeof(TYPES), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Fields ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private readonly string INTERNAL_NAME;

        public string Name { get; protected set; } = "?";
        public string Recommended_Weapon { get; protected set; } = "?";
        public Color[] Colours { get; protected set; } = new Color[3];

        public readonly TYPES ID;
        public readonly byte ID_num;

        private readonly RECOMMENDED_WEAPON recommended_weapon;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Base Type ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public PlayerClassCategory(TYPES id, RECOMMENDED_WEAPON rwep)
        {
            ID = id;
            ID_num = (byte)id;
            INTERNAL_NAME = Enum.GetName(typeof(TYPES), ID_num);
            recommended_weapon = rwep;
        }

        public void LoadLocalizedText()
        {
            Name = Language.GetTextValue("Mods.ExperienceAndClasses.Common.ClassCategory_" + INTERNAL_NAME + "_Name");
            Recommended_Weapon = Language.GetTextValue("Mods.ExperienceAndClasses.Common.RecommendedWeapon_" + recommended_weapon + "_Name");
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Types ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Novice : PlayerClassCategory
        {
            public Novice() : base(TYPES.Novice, RECOMMENDED_WEAPON.Any)
            {
                Colours[0] = new Color(168, 185, 127);
            }
        }

        public class CloseCombat : PlayerClassCategory
        {
            public CloseCombat() : base(TYPES.CloseCombat, RECOMMENDED_WEAPON.Any)
            {
                Colours[1] = new Color(204, 89, 89);
                Colours[2] = new Color(198, 43, 43);
            }
        }

        public class Projectile : PlayerClassCategory
        {
            public Projectile() : base(TYPES.Projectile, RECOMMENDED_WEAPON.Projectile)
            {
                Colours[1] = new Color(127, 146, 255);
                Colours[2] = new Color(81, 107, 255);
            }
        }

        public class Control : PlayerClassCategory
        {
            public Control() : base(TYPES.Control, RECOMMENDED_WEAPON.Any)
            {
                Colours[1] = new Color(158, 255, 255);
                Colours[2] = new Color(49, 160, 160);
            }
        }

        public class Stealth : PlayerClassCategory
        {
            public Stealth() : base(TYPES.Stealth, RECOMMENDED_WEAPON.NonMinion)
            {
                Colours[1] = new Color(158, 158, 158);
                Colours[2] = new Color(107, 107, 107);
            }
        }

        public class Minion : PlayerClassCategory
        {
            public Minion() : base(TYPES.Minion, RECOMMENDED_WEAPON.Minion)
            {
                Colours[1] = new Color(142, 79, 142);
                Colours[2] = new Color(145, 37, 145);
            }
        }

        public class Eclipse : PlayerClassCategory
        {
            public Eclipse() : base(TYPES.Eclipse, RECOMMENDED_WEAPON.Any)
            {
                Colours[1] = new Color(185, 185, 185);
                Colours[2] = new Color(225, 225, 225);
            }
        }

        public class Musical : PlayerClassCategory
        {
            public Musical() : base(TYPES.Musical, RECOMMENDED_WEAPON.Any)
            {
                Colours[1] = new Color(34, 177, 76);
                Colours[2] = new Color(0, 128, 0);
            }
        }

        public class Mechanical : PlayerClassCategory
        {
            public Mechanical() : base(TYPES.Mechanical, RECOMMENDED_WEAPON.Any)
            {
                Colours[1] = new Color(188, 136, 120);
                Colours[2] = new Color(165, 98, 77);
            }
        }

        public class Hybrid : PlayerClassCategory
        {
            public Hybrid() : base(TYPES.Hybrid, RECOMMENDED_WEAPON.Multiple)
            {
                Colours[1] = new Color(204, 87, 138);
                Colours[2] = new Color(193, 36, 104);
            }
        }

    }
}
