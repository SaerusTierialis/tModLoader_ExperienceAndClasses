using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI;

namespace ExperienceAndClasses.UI {
    class UIOverlay : UIStateCombo {
        public static readonly UIOverlay Instance = new UIOverlay();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float LEFT_FROM_MAX = 270f;
        private const float TOP_FROM_MAX = 37f;
        private const float TEXT_SCALE = 1.6f;
        private const float TEXT_SCALE_HOVER = TEXT_SCALE + 0.1f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Varibles ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private BetterTextButton button;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public UIOverlay() {
            auto = UIAutoMode.InventoryOpen;
        }

        protected override void InitializeState() {
            button = new BetterTextButton("Classes", TEXT_SCALE, TEXT_SCALE_HOVER, this, Click, true);
            state.Append(button);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void UpdatePosition() {
            button.Left.Set(Main.screenWidth - LEFT_FROM_MAX, 0f);
            button.Top.Set(Main.screenHeight - TOP_FROM_MAX, 0f);
            button.Recalculate();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void Click(UIMouseEvent evt, UIElement listeningElement) {
            UIMain.Instance.Visibility = !UIMain.Instance.Visibility;
            //UIHelpSettings.Instance.Visibility = false;
        }
    }
}
