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
        private UIText ui_text_body;
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

            state.Append(panel_body);
            state.Append(panel_title);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private void ShowText(UIElement source, string title, string body, float width) {
            if ((this.source == null) || !this.source.Equals(source)) {
                this.source = source;

                //title
                panel_title.SetPosition(source.GetDimensions().X + source.Width.Pixels, source.GetDimensions().Y);
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
                text += c.GetPrereqString() + "\n\n";
            }
            text += c.GetDamagetring() + "\n\n" + c.GetAttributeString();

            ShowText(source, title, text, WIDTH_CLASS);
        }

        public void ShowTextAttribute(UIElement source, Systems.Attribute attribute) {
            string title = attribute.Name;
            string text = attribute.Description + "\n\n" + attribute.Bonus;
            ShowText(source, title, text, WIDTH_ATTRIBUTE);
        }

    }
}
