using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.UI {

    //UI for displaying XP and cooldown bars

    class UIBars : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIBars Instance = new UIBars();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float WIDTH = 280f;
        private const float HEIGHT = 200f;

        private readonly Color COLOR_BACKGROUND = new Color(Constants.COLOR_UI_PANEL_BACKGROUND.R, Constants.COLOR_UI_PANEL_BACKGROUND.G, Constants.COLOR_UI_PANEL_BACKGROUND.B, 50);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }
        private XPBar xp_bar_primary, xp_bar_secondary;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel = new DragableUIPanel(WIDTH, HEIGHT, COLOR_BACKGROUND, this, false, true, true);

            xp_bar_primary = new XPBar(WIDTH - (Constants.UI_PADDING * 2), Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.New]);
            panel.Append(xp_bar_primary);

            xp_bar_secondary = new XPBar(WIDTH - (Constants.UI_PADDING * 2), Systems.Class.CLASS_LOOKUP[(byte)Systems.Class.CLASS_IDS.New]);
            panel.Append(xp_bar_secondary);

            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void Update() {
            bool needs_rearrangement = false;

            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID != xp_bar_primary.Class_Tracked.ID) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier < 1 || xp_bar_primary.Class_Tracked.ID < 1) {
                    needs_rearrangement = true;
                }
                xp_bar_primary.SetClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary);
            }
            xp_bar_primary.Update();

            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID != xp_bar_secondary.Class_Tracked.ID) {
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier < 1 || xp_bar_secondary.Class_Tracked.ID < 1) {
                    needs_rearrangement = true;
                }
                xp_bar_secondary.SetClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary);
            }
            xp_bar_secondary.Update();

        }

    }
}
