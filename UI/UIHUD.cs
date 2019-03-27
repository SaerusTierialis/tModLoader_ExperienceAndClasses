using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.UI {

    //UI for displaying XP and cooldown bars

    public class UIHUD : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIHUD Instance = new UIHUD();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float WIDTH = 200f;
        private const float HEIGHT = 200f; //default, actual height is dynamic

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }
        private XPBar xp_bar_primary, xp_bar_secondary;
        private bool needs_first_arrange;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            needs_first_arrange = true;

            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOR_BAR_UI, this, false, ExperienceAndClasses.LOCAL_MPLAYER.loaded_ui_hud.AUTO);

            xp_bar_primary = new XPBar(WIDTH - (Constants.UI_PADDING * 2), Systems.Class.LOOKUP[(byte)Systems.Class.IDs.Novice]);
            xp_bar_primary.Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(xp_bar_primary);

            xp_bar_secondary = new XPBar(WIDTH - (Constants.UI_PADDING * 2), Systems.Class.LOOKUP[(byte)Systems.Class.IDs.Novice]);
            xp_bar_secondary.Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(xp_bar_secondary);

            state.Append(panel);

            panel.SetPosition(ExperienceAndClasses.LOCAL_MPLAYER.loaded_ui_hud.LEFT, ExperienceAndClasses.LOCAL_MPLAYER.loaded_ui_hud.TOP, true);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void Update() {
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier < 1 && ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier < 1) {
                //no class or ability
                panel.visible = false;
            }
            else {
                panel.visible = true;

                bool needs_rearrangement = false;

                if (needs_first_arrange) {
                    needs_rearrangement = true;
                    needs_first_arrange = false;
                }

                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID != xp_bar_primary.Class_Tracked.ID) {
                    if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier < 1 || xp_bar_primary.Class_Tracked.Tier < 1) {
                        needs_rearrangement = true;
                    }
                    xp_bar_primary.SetClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary);
                }
                xp_bar_primary.Update();

                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID != xp_bar_secondary.Class_Tracked.ID) {
                    if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier < 1 || xp_bar_secondary.Class_Tracked.Tier < 1) {
                        needs_rearrangement = true;
                    }
                    xp_bar_secondary.SetClass(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary);
                }
                xp_bar_secondary.Update();

                if (needs_rearrangement) {
                    float y = Constants.UI_PADDING;

                    if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier > 0) {
                        xp_bar_primary.Top.Set(y, 0f);
                        y += xp_bar_primary.Height.Pixels;
                    }

                    if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier > 0) {
                        xp_bar_secondary.Top.Set(y, 0f);
                        y += (Constants.UI_PADDING / 2f) + xp_bar_secondary.Height.Pixels;
                    }

                    y += Constants.UI_PADDING;

                    panel.Height.Set(y, 0f);
                }
            }
        }
    }
}
