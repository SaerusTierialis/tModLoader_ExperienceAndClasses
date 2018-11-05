using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    //UI for displaying statuses

    class UIStatus : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIStatus Instance = new UIStatus();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float WIDTH = 450f;
        private const float HEIGHT = 100f;

        private readonly Color COLOR_BACKGROUND = new Color(0, 0, 0, 0);
        private readonly Color COLOR_BACKGROUND_HIGHLIGHT = new Color(Shared.COLOR_UI_PANEL_BACKGROUND.R, Shared.COLOR_UI_PANEL_BACKGROUND.G, Shared.COLOR_UI_PANEL_BACKGROUND.B, 0);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel = new DragableUIPanel(WIDTH, HEIGHT, COLOR_BACKGROUND, this, false, true, true);

            panel.BorderColor = COLOR_BACKGROUND;

            panel.OnMouseOver += new UIElement.MouseEvent(MouseOver);
            panel.OnMouseOut += new UIElement.MouseEvent(MouseOut);

            panel.HideButtons();

            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private void MouseOver(UIMouseEvent evt, UIElement listeningElement) {
            panel.BackgroundColor = COLOR_BACKGROUND_HIGHLIGHT;
            panel.ShowButtons();
        }

        private void MouseOut(UIMouseEvent evt, UIElement listeningElement) {
            panel.BackgroundColor = COLOR_BACKGROUND;
            panel.HideButtons();
        }

    }
}
