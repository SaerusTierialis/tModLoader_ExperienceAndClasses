using Terraria.UI;

namespace ExperienceAndClasses.Utilities {
    class UIFunctions {
        /// <summary>
        /// Center one UIElement on anther
        /// </summary>
        /// <param name="element"></param>
        /// <param name="target"></param>
        public static void CenterUIElement(UIElement element, UIElement target) {
            CenterUIElement(element, target.Left.Pixels + (target.Width.Pixels / 2f), target.Top.Pixels + (target.Height.Pixels / 2f));
        }

        /// <summary>
        /// Center one UIElement at location
        /// </summary>
        /// <param name="element"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void CenterUIElement(UIElement element, float x, float y) {
            element.Left.Set(x - (element.Width.Pixels / 2f), 0f);
            element.Top.Set(y - (element.Height.Pixels / 2f), 0f);
        }
    }
}
