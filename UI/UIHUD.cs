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

        private const float ITEM_WIDTH = WIDTH - (Constants.UI_PADDING * 2);

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }
        private XPBar[] xp_bars;
        private DragableUIPanel panel_resource, panel_cooldown;
        private AbilityIconCooldown[] ability_icons;
        private List<ResourceBar> resource_bars;
        private bool any_classes;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            any_classes = false;
            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOUR_BAR_UI, this, false, ExperienceAndClasses.LOCAL_MPLAYER.loaded_ui_hud.AUTO);
            panel.Width.Set(WIDTH, 0f);

            //xp bars
            xp_bars = new XPBar[2];

            xp_bars[0] = new XPBar(ITEM_WIDTH);
            xp_bars[0].Top.Set(Constants.UI_PADDING, 0f);
            xp_bars[0].Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(xp_bars[0]);

            xp_bars[1] = new XPBar(ITEM_WIDTH);
            xp_bars[1].Top.Set(xp_bars[0].Top.Pixels + xp_bars[0].Height.Pixels + Constants.UI_PADDING, 0f);
            xp_bars[1].Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(xp_bars[1]);

            //resource panel and bars
            resource_bars = new List<ResourceBar>();
            panel_resource = new DragableUIPanel(panel.Width.Pixels, 0f, panel.BackgroundColor, this, false, false, false);
            panel.Append(panel_resource);

            //cooldown icons
            ability_icons = new AbilityIconCooldown[ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS * 4];
            panel_cooldown = new DragableUIPanel(panel.Width.Pixels, 0f, panel.BackgroundColor, this, false, false, false);
            panel.Append(panel_cooldown);
            //TODO add in multiple rows

            state.Append(panel);
            panel.SetPosition(ExperienceAndClasses.LOCAL_MPLAYER.loaded_ui_hud.LEFT, ExperienceAndClasses.LOCAL_MPLAYER.loaded_ui_hud.TOP, true);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void Update() {
            Main.NewText("HERE");
            foreach (XPBar xp_bar in xp_bars) {
                xp_bar.Update();
            }
            foreach(ResourceBar rb in resource_bars) {
                rb.Update();
            }
            foreach(AbilityIconCooldown icon in ability_icons) {
                if (icon != null)
                    icon.Update();
            }
        }

        public void UpdateClassInfo() {
            MPlayer local = ExperienceAndClasses.LOCAL_MPLAYER;
            any_classes = (local.Class_Primary.Tier > 0);

            if (!any_classes) {
                panel.visible = false;
            }
            else {
                panel.visible = true;

                //xp bars
                xp_bars[0].SetClass(local.Class_Primary);
                if (local.Class_Secondary.Tier > 0) {
                    xp_bars[1].SetClass(local.Class_Secondary);
                    xp_bars[1].visible = true;
                    panel.Height.Set(xp_bars[1].Top.Pixels + xp_bars[1].Height.Pixels + Constants.UI_PADDING, 0f);
                }
                else {
                    xp_bars[1].visible = false;
                    panel.Height.Set(xp_bars[0].Top.Pixels + xp_bars[0].Height.Pixels + Constants.UI_PADDING, 0f);
                }

                //resource
                panel_resource.Top.Set(panel.Height.Pixels, 0f);
                panel_resource.RemoveAllChildren();
                if (local.Resources.Count == 0) {
                    panel_resource.visible = false;
                    panel_resource.Height.Set(0f, 0f);
                }
                else {
                    panel_resource.visible = true;
                    float top = Constants.UI_PADDING;
                    ResourceBar rb;
                    foreach (Systems.Resource resource in local.Resources.Values) {
                        rb = new ResourceBar(resource, ITEM_WIDTH);
                        rb.Left.Set(Constants.UI_PADDING, 0f);
                        rb.Top.Set(top, 0f);
                        panel_resource.Append(rb);
                        top += ResourceBar.HEIGHT + Constants.UI_PADDING;
                    }
                    panel_resource.Height.Set(top, 0f);
                }
            }

            //create Abilities_Summary_For_UIHUD
            List<Systems.Ability> abilities = new List<Systems.Ability>();
            for (int i = 0; i < ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS; i++) {
                foreach (Systems.Ability ability in new Systems.Ability[] { local.Abilities_Primary[i], local.Abilities_Primary_Alt[i], local.Abilities_Secondary[i], local.Abilities_Secondary_Alt[i] }) {
                    if (ability != null && ability.Unlocked && !abilities.Contains(ability)) {
                        abilities.Add(ability);
                    }
                }
            }

            //cooldown
            if (abilities.Count == 0) {
                panel_cooldown.visible = false;
            }
            else {
                panel_cooldown.visible = true;
                panel_cooldown.Top.Set(panel_resource.Top.Pixels + panel_resource.Height.Pixels, 0f);
                byte counter = 0;
                foreach (Systems.Ability ability in abilities) {
                    ability_icons[counter++].SetAbility(ability);
                }
                //TODO get height for panel
                while (counter < ability_icons.Length) {
                    ability_icons[counter].active = false;
                }
            }

            Update();
        }
 
    }
}
