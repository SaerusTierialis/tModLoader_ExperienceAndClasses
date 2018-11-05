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
        private const float TEXT_SCALE = 1;

        private const float WIDTH_CLASS = 300;


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private DragableUIPanel panel;
        private UIText ui_text;
        private UIElement source = null;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel = new DragableUIPanel(1f, 1f, Shared.COLOR_UI_PANEL_BACKGROUND, this, false, false, false);

            ui_text = new UIText("", TEXT_SCALE, false);
            ui_text.Left.Set(Shared.UI_PADDING, 0f);
            ui_text.Top.Set(Shared.UI_PADDING, 0f);
            panel.Append(ui_text);

            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private void ShowText(UIElement source, string text, float width) {
            if ((this.source == null) || !this.source.Equals(source)) {
                this.source = source;
                panel.SetPosition(source.GetDimensions().X + source.Width.Pixels, source.GetDimensions().Y);

                text = Main.fontMouseText.CreateWrappedText(text, (width - Shared.UI_PADDING*2) / TEXT_SCALE);
                panel.SetSize(width, Main.fontMouseText.MeasureString(text).Y*TEXT_SCALE);

                ui_text.SetText(text);

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

        public void ShowTextClass(UIElement source, byte class_id) {
            Systems.Class c = Systems.Classes.CLASS_LOOKUP[class_id];

            string text = c.Name.ToUpper();
            if (ExperienceAndClasses.LOCAL_MPLAYER.class_levels[class_id] <= 0) {
                text += " (locked)\n\n";
                text += "Requirement: " + c.GetPrereqString() + "\n\n";
            }
            else {
                text += "\n\n";
            }
            text += c.GetAttributeString() + "\n\n" + c.Description;

            ShowText(source, text, WIDTH_CLASS);
        }

    }
}
