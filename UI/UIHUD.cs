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

        private const float MAX_WIDTH_PER_ITEM_ROW = WIDTH - (Constants.UI_PADDING * 2);
        public const float SPACING = 2f;

        private const float UPDATE_COOLDOWN_SECONDS = 0.25f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }
        private XPBar[] xp_bars;
        private DragableUIPanel panel_resource, panel_cooldown;
        private AbilityIconCooldown[] ability_icons;
        private List<ResourceBar> resource_bars;
        private bool any_classes;
        private bool any_cooldowns;
        private DateTime time_next_cooldown_update;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            any_classes = false;
            any_cooldowns = false;
            time_next_cooldown_update = Shortcuts.Now;
            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOUR_BAR_UI, this, false);

            //xp bars
            xp_bars = new XPBar[2];

            xp_bars[0] = new XPBar(MAX_WIDTH_PER_ITEM_ROW);
            xp_bars[0].Top.Set(Constants.UI_PADDING, 0f);
            xp_bars[0].Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(xp_bars[0]);

            xp_bars[1] = new XPBar(MAX_WIDTH_PER_ITEM_ROW);
            xp_bars[1].Top.Set(xp_bars[0].Top.Pixels + xp_bars[0].Height.Pixels + SPACING, 0f);
            xp_bars[1].Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(xp_bars[1]);

            //resource panel and bars
            resource_bars = new List<ResourceBar>();
            panel_resource = new DragableUIPanel(panel.Width.Pixels, 0f, Color.Transparent, this, false, false, false);
            panel_resource.BorderColor = Color.Transparent;
            panel.Append(panel_resource);

            //cooldown icons
            ability_icons = new AbilityIconCooldown[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS * 4];
            panel_cooldown = new DragableUIPanel(panel.Width.Pixels, 0f, Color.Transparent, this, false, false, false);
            panel_cooldown.BorderColor = Color.Transparent;
            panel.Append(panel_cooldown);
            float left = Constants.UI_PADDING;
            float left_max = panel_cooldown.Width.Pixels - Constants.UI_PADDING - AbilityIconCooldown.SIZE;
            float top = Constants.UI_PADDING;
            for (byte i=0; i<ability_icons.Length; i++) {
                ability_icons[i] = new AbilityIconCooldown();
                ability_icons[i].Left.Set(left, 0f);
                ability_icons[i].Top.Set(top, 0f);
                panel_cooldown.Append(ability_icons[i]);
                left += AbilityIconCooldown.SIZE + SPACING;
                if (left > left_max) {
                    left = Constants.UI_PADDING;
                    top += AbilityIconCooldown.SIZE + SPACING;
                }
            }

            state.Append(panel);

            Systems.PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;
            panel.SetPosition(psheet.Misc.UIHUD_Left, psheet.Misc.UIHUD_Top, true);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void OnUpdate() {
            if (any_cooldowns) {
                if (Shortcuts.Now.CompareTo(time_next_cooldown_update) > 0) {
                    UpdateCooldown();
                    time_next_cooldown_update = Shortcuts.Now.AddSeconds(UPDATE_COOLDOWN_SECONDS);
                }
            }
        }

        public void UpdateAll() {
            UpdateXP();
            UpdateResource();
            UpdateCooldown();
        }

        public void UpdateXP() {
            foreach (XPBar xp_bar in xp_bars) {
                xp_bar.Update();
            }
        }

        public void UpdateResource() {
            foreach (ResourceBar rb in resource_bars) {
                rb.Update();
            }
        }

        public void UpdateCooldown() {
            any_cooldowns = false;
            foreach (AbilityIconCooldown icon in ability_icons) {
                if (icon != null) {
                    if (icon.Update()) {
                        any_cooldowns = true;
                    }
                }
            }
        }

        public void UpdatePSheet(Systems.PSheet psheet) {
            any_classes = psheet.Classes.Primary.Valid_Class;

            if (!any_classes) {
                panel.visible = false;
            }
            else {
                panel.visible = true;

                float final_height;

                //xp bars
                xp_bars[0].SetClass(psheet.Classes.Primary.Class);
                if (psheet.Classes.Secondary.Valid_Class) {
                    xp_bars[1].SetClass(psheet.Classes.Secondary.Class);
                    xp_bars[1].visible = true;
                    final_height = xp_bars[1].Top.Pixels + xp_bars[1].Height.Pixels;
                }
                else {
                    xp_bars[1].visible = false;
                    final_height = xp_bars[0].Top.Pixels + xp_bars[0].Height.Pixels;
                }

                //resource
                panel_resource.RemoveAllChildren();
                if (psheet.Resources.Count == 0) {
                    panel_resource.visible = false;
                }
                else {
                    panel_resource.visible = true;
                    resource_bars.Clear();
                    ResourceBar rb;
                    foreach (Systems.Resource resource in psheet.Resources.Values) {
                        rb = new ResourceBar(resource, MAX_WIDTH_PER_ITEM_ROW);
                        rb.Left.Set(Constants.UI_PADDING, 0f);
                        rb.Top.Set(final_height += SPACING, 0f);
                        resource_bars.Add(rb);
                        panel_resource.Append(rb);
                        final_height += ResourceBar.HEIGHT;
                    }
                }

                //create ability list
                List<Systems.Ability> abilities = new List<Systems.Ability>();
                for (int i = 0; i < Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS; i++) {
                    foreach (Systems.Ability ability in new Systems.Ability[] { psheet.Classes.Primary.Class.Abilities[i], psheet.Classes.Primary.Class.Abilities_Alt[i] }) {
                        if (ability != null && ability.Unlocked && !abilities.Contains(ability)) {
                            abilities.Add(ability);
                        }
                    }
                }
                for (int i = 0; i < Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS; i++) {
                    foreach (Systems.Ability ability in new Systems.Ability[] { psheet.Classes.Secondary.Class.Abilities[i], psheet.Classes.Secondary.Class.Abilities[i] }) {
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
                    panel_cooldown.Top.Set(final_height, 0f);
                    byte counter = 0;
                    foreach (Systems.Ability ability in abilities) {
                        ability_icons[counter++].SetAbility(ability);
                    }
                    final_height += ability_icons[counter - 1].Top.Pixels + AbilityIconCooldown.SIZE + Constants.UI_PADDING;
                    while (counter < ability_icons.Length) {
                        ability_icons[counter++].active = false;
                    }
                }

                panel.Height.Set(final_height + Constants.UI_PADDING, 0f);
                panel_resource.Height.Set(panel.Height.Pixels, 0f);

                UpdateAll();
            }
        }
 
    }
}
