using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.Utilities
{
    public class Text
    {
        public static void LoadLocalizedText()
        {
            //attributes
            foreach (Systems.Attribute attribute in Systems.Attribute.LOOKUP)
            {
                attribute.LoadLocalizedText();
            }

            //class
            foreach (Systems.PlayerClass playerclass in Systems.PlayerClass.LOOKUP)
            {
                playerclass.LoadLocalizedText();
            }

            //class categories
            foreach (Systems.PlayerClassCategory category in Systems.PlayerClassCategory.LOOKUP)
            {
                category.LoadLocalizedText();
            }

            //TODO - passive
            //TODO - resource
            //TODO - ability
        }
    }
}
