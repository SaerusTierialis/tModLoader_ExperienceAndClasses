using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    //UI for displaying info for mouse-hovered UI elements

    public class UIInfo : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIInfo Instance = new UIInfo();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float TEXT_SCALE_TITLE = 1.1f;
        private const float TEXT_SCALE_BUTTON = 1f;
        private const float TEXT_SCALE_BUTTON_HOVER = 1.1f;
        private const float TEXT_SCALE_BODY = 0.9f;

        private const float WIDTH_CLASS = 400f;
        private const float WIDTH_ATTRIBUTE = 350f;
        private const float WIDTH_HELP = 300f;
        private const float WIDTH_STATUS = 300f;
        private const float WIDTH_UNLOCK = 300f;

        private const float BUTTON_SEPARATION = 100f;

        private enum MODE : byte {
            HOVER,
            INPUT,
        }

        private enum UNLOCK_MODE : byte {
            CLASS,
            SUBCLASS,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private DragableUIPanel panel;
        private UIText ui_text_body, ui_text_extra;
        private UIElement source = null;
        private UIImage image;
        private static MODE mode;
        private UNLOCK_MODE unlock_mode;
        private Systems.Class unlock_class;
        private TextButton button_yes, button_no;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            mode = MODE.HOVER;
            unlock_mode = UNLOCK_MODE.SUBCLASS;
            unlock_class = null;

            panel = new DragableUIPanel(1f, 1f, Constants.COLOR_UI_PANEL_BACKGROUND, this, false, false, false);

            ui_text_body = new UIText("", TEXT_SCALE_BODY, false);
            ui_text_body.Left.Set(Constants.UI_PADDING, 0f);
            ui_text_body.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(ui_text_body);

            ui_text_extra = new UIText("", TEXT_SCALE_BODY, false);
            ui_text_extra.Left.Set(Constants.UI_PADDING, 0f);
            ui_text_extra.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(ui_text_extra);

            image = new UIImage(Utilities.Textures.TEXTURE_BLANK);
            image.Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(image);

            button_yes = new TextButton("Confirm", TEXT_SCALE_BUTTON, TEXT_SCALE_BUTTON_HOVER);
            button_yes.OnClick += new UIElement.MouseEvent(ClickYes);
            panel.Append(button_yes);
            
            button_no = new TextButton("Cancel", TEXT_SCALE_BUTTON, TEXT_SCALE_BUTTON_HOVER);
            button_no.OnClick += new UIElement.MouseEvent(ClickNo);
            panel.Append(button_no);

            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ClickYes(UIMouseEvent evt, UIElement listeningElement) {
            if (button_yes.visible) {
                switch (unlock_mode) {
                    case UNLOCK_MODE.CLASS:
                        ExperienceAndClasses.LOCAL_MPLAYER.UnlockClass(unlock_class);
                        break;

                    case UNLOCK_MODE.SUBCLASS:
                        ExperienceAndClasses.LOCAL_MPLAYER.UnlockSubclass();
                        break;

                    default:
                        Utilities.Commons.Error("Unsupported unlock action " + unlock_mode);
                        break;
                }
                ResetState();
            }
        }

        public void ClickNo(UIMouseEvent evt, UIElement listeningElement) {
            if (button_no.visible) {
                ResetState();
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static bool AllowClicks() {
            return mode == MODE.HOVER;
        }

        private void ResetState() {
            mode = MODE.HOVER;
            EndText(source);
        }

        private void ShowText(UIElement source, string title, string body, float width, string extra=null, float extra_left=0f, Texture2D texture=null, bool force=false) {
            if (mode == MODE.HOVER || force) {

                this.source = source;

                //title
                if (title == null) {
                    panel.RemoveTitle();
                }
                else {
                    panel.SetTitle(title, TEXT_SCALE_TITLE);
                }

                //image
                float add_left;
                float min_height_body = 0f;
                if (texture == null) {
                    image.SetImage(Utilities.Textures.TEXTURE_BLANK);
                    add_left = 0;
                }
                else {
                    image.SetImage(texture);
                    add_left = texture.Width + Constants.UI_PADDING*2;
                    image.Top.Set(panel.top_space + Constants.UI_PADDING, 0f);
                    min_height_body = texture.Height + Constants.UI_PADDING;
                }

                body = Main.fontMouseText.CreateWrappedText(body, (width - Constants.UI_PADDING * 2 - add_left) / TEXT_SCALE_BODY);
                float height = Math.Max((Main.fontMouseText.MeasureString(body).Y * TEXT_SCALE_BODY), min_height_body) + panel.top_space + Constants.UI_PADDING;

                //buttons
                if (mode == MODE.INPUT) {
                    button_yes.visible = true;
                    button_no.visible = true;

                    height += button_yes.Height.Pixels + Constants.UI_PADDING*3;
                    float height_button_center = height - Constants.UI_PADDING*2 - (button_yes.Height.Pixels / 2f);

                    Utilities.Commons.CenterUIElement(button_yes, (width - BUTTON_SEPARATION) / 2f, height_button_center);
                    Utilities.Commons.CenterUIElement(button_no, (width + BUTTON_SEPARATION) / 2f, height_button_center);
                }
                else {
                    button_yes.visible = false;
                    button_no.visible = false;
                }

                //body
                panel.SetSize(width, height);
                panel.SetPosition(source.GetDimensions().X + source.Width.Pixels, source.GetDimensions().Y, true);
                ui_text_body.Left.Set(Constants.UI_PADDING + add_left, 0f);
                ui_text_body.Top.Set(Constants.UI_PADDING + panel.top_space, 0f);
                ui_text_body.SetText(body);

                //extra
                if (extra != null) {
                    ui_text_extra.SetText(extra);
                    ui_text_extra.Left.Set(extra_left, 0f);
                    ui_text_extra.Top.Set(panel.Height.Pixels - (Main.fontMouseText.MeasureString(extra).Y * TEXT_SCALE_BODY), 0f);
                }
                else {
                    ui_text_extra.SetText("");
                }

                //show
                Visibility = true;
            }
        }

        public void EndText(UIElement source) {
            if ((this.source != null) && this.source.Equals(source) && (mode == MODE.HOVER)) {
                this.source = null;
                Visibility = false;
            }
        }

        public void EndTextChildren(UIState state) {
            mode = MODE.HOVER;
            if (source != null) {
                UIElement parent = source.Parent;
                while (parent != null) {
                    if (parent.Equals(state)) {
                        source = null;
                        Visibility = false;
                        break;
                    }
                    parent = parent.Parent;
                }
            }
        }

        public void ShowTextClass(UIElement source, Systems.Class c) {
            string title = c.Name + " (Tier " + c.Tier + ")";
            string text = "";
            if (!ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[c.ID]) {
                title += " (locked)";
                text += "Left click for class unlock requirements.\n";
            }

            if (!ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary) {
                text += "Right click for multiclass unlock requirements.\n";
            }

            if (text.Length > 0) {
                text += "\n";
            }

            text += c.Description + "\n\n" + "DAMAGE TYPE: " + c.Power_Scaling.Name + "\n\nATTRIBUTES:";

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
                attribute_names += Systems.Attribute.LOOKUP[id].Name + ":";

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
            string text = attribute.Description + "\n\nAllocation Cost: " + Systems.Attribute.AllocationPointCost(ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID]) + "\n" + attribute.Bonus;
            ShowText(source, title, text, WIDTH_ATTRIBUTE);
        }

        public void ShowHelpText(UIElement source, string help_text, string title=null) {
            if (title == null) {
                title = "Help";
            }
            ShowText(source, title, help_text, WIDTH_HELP);
        }

        public void ShowStatus(UIElement source, Systems.Status status) {
            ShowText(source, "TODO_status", "TODO_description", WIDTH_STATUS);
        }

        public void ShowUnlockClass(UIElement source, Systems.Class c) {
            Items.MItem item = c.Unlock_Item;

            string str = "Requirements:\n" + "Level " + c.Prereq.Max_Level + " " + c.Prereq.Name + "\nx1 " + item.item.Name + "\n\nToken Recipe:\n" + item.GetRecipeString(true) + "\n(Work Bench)";

            mode = MODE.INPUT;
            unlock_mode = UNLOCK_MODE.CLASS;
            unlock_class = c;

            ShowText(source, "Unlock " + c.Name, str , WIDTH_UNLOCK, null, 0, ModLoader.GetTexture(item.Texture), true);
        }

        public void ShowUnlockSubclass(UIElement source) {
            Items.MItem item = ExperienceAndClasses.MOD.GetItem<Items.Unlock_Subclass>();

            string str = "Unlocking multiclassing will allow you to freely set any class as your subclass.\n\nRequirements:\nx1 " + item.item.Name + "\n\nToken Recipe:\n" + item.GetRecipeString(true) + "\n(Work Bench)";

            mode = MODE.INPUT;
            unlock_mode = UNLOCK_MODE.SUBCLASS;

            ShowText(source, "Unlock Multiclassing", str, WIDTH_UNLOCK, null, 0, ModLoader.GetTexture(item.Texture), true);
        }

    }
}
