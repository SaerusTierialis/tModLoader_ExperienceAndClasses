using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExperienceAndClasses.UI {
    public class UIPopup : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIPopup Instance = new UIPopup();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private enum MODE : byte {
            HOVER,
            INPUT,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private static MODE mode;
        private UIElement source = null;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            mode = MODE.HOVER;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void EndText(UIElement source) {
            if ((this.source != null) && this.source.Equals(source) && (mode == MODE.HOVER)) {
                this.source = null;
                Visibility = false;
            }
        }

        public void EndTextChildren(UIState state) {
            mode = MODE.HOVER;
            if (source != null) {
                UIElement parent = source.Parent;
                while (parent != null) {
                    if (parent.Equals(state)) {
                        source = null;
                        Visibility = false;
                        break;
                    }
                    parent = parent.Parent;
                }
            }
        }
    }
}
