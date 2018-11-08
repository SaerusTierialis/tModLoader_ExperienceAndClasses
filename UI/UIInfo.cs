using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    //UI for displaying info for mouse-hovered UI elements

    class UIInfo : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIInfo Instance = new UIInfo();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float TEXT_SCALE_TITLE = 1.1f;
        private const float TEXT_SCALE_BODY = 0.9f;

        private const float WIDTH_CLASS = 300;
        private const float WIDTH_ATTRIBUTE = 300;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private DragableUIPanel panel_title;
        private DragableUIPanel panel_body;
        private UIText ui_text_title;
        private UIText ui_text_body, ui_text_extra;
        private UIElement source = null;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel_title = new DragableUIPanel(1f, 1f, Shared.COLOR_UI_PANEL_BACKGROUND, this, false, false, false);
            panel_body = new DragableUIPanel(1f, 1f, Shared.COLOR_UI_PANEL_BACKGROUND, this, false, false, false);

            ui_text_title = new UIText("", TEXT_SCALE_TITLE, false);
            ui_text_title.Left.Set(0f, 0f);
            ui_text_title.Top.Set(Shared.UI_PADDING, 0f);
            panel_title.Append(ui_text_title);

            ui_text_body = new UIText("", TEXT_SCALE_BODY, false);
            ui_text_body.Left.Set(Shared.UI_PADDING, 0f);
            ui_text_body.Top.Set(Shared.UI_PADDING, 0f);
            panel_body.Append(ui_text_body);

            ui_text_extra = new UIText("", TEXT_SCALE_BODY, false);
            ui_text_extra.Left.Set(Shared.UI_PADDING, 0f);
            ui_text_extra.Top.Set(Shared.UI_PADDING, 0f);
            panel_body.Append(ui_text_extra);

            state.Append(panel_body);
            state.Append(panel_title);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private void ShowText(UIElement source, string title, string body, float width, string extra=null, float extra_left=0f) {
            if ((this.source == null) || !this.source.Equals(source)) {
                this.source = source;

                //left position
                float screen_width = state.GetDimensions().Width;
                float left = source.GetDimensions().X + source.Width.Pixels;
                if (screen_width < (left + width)) {
                    left -= ((left + width) - screen_width);
                }

                //title
                panel_title.SetPosition(left, source.GetDimensions().Y);
                Vector2 measure_title = Main.fontMouseText.MeasureString(title);
                panel_title.SetSize(width, measure_title.Y * TEXT_SCALE_TITLE);
                ui_text_title.Left.Set((width - (measure_title.X*TEXT_SCALE_TITLE)) / 2f, 0f);
                ui_text_title.SetText(title);

                //body
                panel_body.SetPosition(panel_title.Left.Pixels, panel_title.Top.Pixels);
                body = Main.fontMouseText.CreateWrappedText(body, (width - Shared.UI_PADDING*2) / TEXT_SCALE_BODY);
                panel_body.SetSize(width, (Main.fontMouseText.MeasureString(body).Y*TEXT_SCALE_BODY) + panel_title.Height.Pixels + Shared.UI_PADDING);
                ui_text_body.Top.Set(Shared.UI_PADDING + panel_title.Height.Pixels, 0f);
                ui_text_body.SetText(body);

                //extra
                if (extra != null) {
                    ui_text_extra.SetText(extra);
                    ui_text_extra.Left.Set(extra_left, 0f);
                    ui_text_extra.Top.Set(panel_body.Height.Pixels - (Main.fontMouseText.MeasureString(extra).Y * TEXT_SCALE_BODY), 0f);
                }
                else {
                    ui_text_extra.SetText("");
                }

                //adjust vertical position
                float screen_height = state.GetDimensions().Height;
                if (screen_height < (panel_body.Top.Pixels + panel_body.Height.Pixels)) {
                    float adjust = (panel_body.Top.Pixels + panel_body.Height.Pixels) - screen_height;
                    panel_title.Top.Set(panel_title.Top.Pixels - adjust, 0f);
                    panel_body.Top.Set(panel_body.Top.Pixels - adjust, 0f);
                }

                //show
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
                        source = null;
                        Visibility = false;
                    }
                    parent = parent.Parent;
                }
            }
        }

        public void ShowTextClass(UIElement source, byte class_id) {
            Systems.Class c = Systems.Class.CLASS_LOOKUP[class_id];

            string title = c.Name.ToUpper();
            string text = c.Description + "\n\n";
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[class_id] <= 0) {
                title += " (locked)";
                text += "REQUIREMENT: " + Systems.Class.CLASS_LOOKUP[c.ID_Prereq].Name;
                switch (c.Tier) {
                    case 2:
                        text += " Lv." + Shared.LEVEL_REQUIRED_TIER_2;
                        break;

                    case 3:
                        text += " Lv." + Shared.LEVEL_REQUIRED_TIER_3;
                        break;
                }
                text += "\n\n";
            }
            text += "DAMAGE TYPE: " + c.Power_Scaling.Name + "\n\nATTRIBUTES:";

            //attributes
            bool first = true;
            string attribute_names = "";
            string attribute_growth = "";
            foreach (byte id in Systems.Attribute.ATTRIBUTES_UI_ORDER) {
                if (first) {
                    first = false;
                }
                else {
                    attribute_names += "\n";
                    attribute_growth += "\n";
                }
                attribute_names += Systems.Attribute.ATTRIBUTE_LOOKUP[id].Name + ":";

                for (byte i = 0; i < 5; i++) {
                    if (c.Attribute_Growth[id] >= (i + 1)) {
                        attribute_growth += "★";
                    }
                    else if (c.Attribute_Growth[id] > i) {
                        attribute_growth += "✯";
                    }
                    else {
                        attribute_growth += "☆";
                    }
                }
            }

            float extra_left = (Main.fontMouseText.MeasureString(attribute_names).X * TEXT_SCALE_BODY) + 10f;

            text += "\n" + attribute_names;

            ShowText(source, title, text, WIDTH_CLASS, attribute_growth, extra_left);
        }

        public void ShowTextAttribute(UIElement source, Systems.Attribute attribute) {
            string title = attribute.Name;
            string text = attribute.Description + "\n" + attribute.Bonus;
            ShowText(source, title, text, WIDTH_ATTRIBUTE);
        }

    }
}
