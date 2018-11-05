using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    //UI for displaying info for mouse-hovered UI elements

    class UIInfo : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIInfo Instance = new UIInfo();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float WIDTH = 300f;
        private const float HEIGHT = 500f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }
        private UIElement source = null;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel = new DragableUIPanel(WIDTH, HEIGHT, Shared.COLOR_UI_PANEL_BACKGROUND, this, false, false, false);
            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void ShowText(UIElement source, string text) {
            if ((this.source == null) || !this.source.Equals(source)) {
                this.source = source;
                panel.SetPosition(source.GetDimensions().X + source.Width.Pixels, source.GetDimensions().Y);
                Visibility = true;
            }
        }

        public void EndText(UIElement source) {
            if ((this.source != null) && this.source.Equals(source)) {
                this.source = null;
                Visibility = false;
            }
        }

        public void EndTextChildren(UIState state) {
            if (source != null) {
                UIElement parent = source.Parent;
                while (parent != null) {
                    if (parent.Equals(state)) {
                        this.source = null;
                        Visibility = false;
                    }
                    parent = parent.Parent;
                }
            }
        }

    }
}
