using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    public class HidableText : UIText {
        public bool visible = true;
        public HidableText(string text, float text_size) : base(text, text_size) {}
        public override void Draw(SpriteBatch spriteBatch) {
            if (visible) {
                base.Draw(spriteBatch);
            }
        }
    }

    public class TextButton : UIElement {
        private static readonly Color COLOUR_SELECT = Color.Yellow;
        private static readonly Color COLOUR_STANDARD = Color.DarkGray;

        public bool visible = true;

        private HidableText text_standard, text_select;

        public TextButton(string text, float text_size, float text_size_hover) {
            SetPadding(0f);

            text_standard = new HidableText(text, text_size);
            text_standard.TextColor = COLOUR_STANDARD;
            Append(text_standard);

            Vector2 text_measure = Main.fontMouseText.MeasureString(text_standard.Text);

            Width.Set(text_measure.X * text_size, 0f);
            Height.Set(text_measure.Y / 2f * text_size, 0f);

            text_select = new HidableText(text, text_size_hover);
            text_measure = Main.fontMouseText.MeasureString(text_select.Text);
            text_select.Width.Set(text_measure.X * text_size_hover, 0f);
            text_select.Height.Set(text_measure.Y / 2f * text_size_hover, 0f);
            Utilities.UIFunctions.CenterUIElement(text_select, this);
            text_select.TextColor = COLOUR_SELECT;
            Append(text_select);

            text_standard.visible = true;
            text_select.visible = false;
        }

        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            text_standard.visible = true;
            text_select.visible = false;
        }

        public override void MouseOver(UIMouseEvent evt) {
            base.MouseOver(evt);
            text_standard.visible = false;
            text_select.visible = true;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (visible) {
                base.Draw(spriteBatch);
            }
        }
    }

    public class HelpTextPanel : UIPanel {
        public string title { get; private set; }
        private UIText text;
        private string help_text_title, help_text;
        private bool center_text, center_vertical;
        private float text_scale;
        private Vector2 text_measure;

        public HelpTextPanel(string title, float text_scale, bool center_text=true, string help_text=null, string help_text_title=null, bool center_vertical = true, bool hide_panel = false, float override_width = -1f) {
            SetPadding(0f);
            this.help_text_title = help_text_title;
            this.help_text = help_text;
            this.center_text = center_text;
            this.center_vertical = center_vertical;
            this.text_scale = text_scale;

            this.title = title;
            text = new UIText(title, text_scale);
            Append(text);

            text_measure = Main.fontMouseText.MeasureString(text.Text);

            Height.Set((text_measure.Y * text_scale / 2f) + (Constants.UI_PADDING * 2) + 2f, 0f);

            if (override_width > 0f) {
                Width.Set(override_width, 0f);
            }

            float width = text_measure.X * text_scale;
            if (Width.Pixels < width)
                Width.Set(width, 0f);

            if (hide_panel) {
                BackgroundColor = Color.Transparent;
                BorderColor = Color.Transparent;
            }
        }

        public void SetTextColour(Color colour) {
            text.TextColor = colour;
        }

        public void SetText(string new_text) {
            text.SetText(new_text);
            Recalculate();
        }

        public override void Recalculate() {
            base.Recalculate();

            if (center_text) {
                text.Top.Set((Height.Pixels - (text_measure.Y * text_scale / 2f)) / 2f , 0f);
                text.Left.Set((Width.Pixels - (text_measure.X * text_scale)) / 2f, 0f);
            }
            else if (center_vertical) {
                text.Top.Set((Height.Pixels - (text_measure.Y * text_scale / 2f)) / 2f, 0f);
            }
        }

        public override void MouseOver(UIMouseEvent evt) {
            base.MouseOver(evt);
            if (help_text != null) {
                UIInfo.Instance.ShowHelpText(this, help_text, help_text_title);
            }
        }

        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            UIInfo.Instance.EndText(this);
        }
    }

    public class ProgressBar : UIElement {
        private const float MIN_WIDTH = 16f;
        private UIPanel bar_background, bar_progress;

        private Color colour_border, colour_background; 
        private bool show_min_for_low;

        public ProgressBar(float width, float height, Color colour, bool show_min_width_for_low=false) {
            this.show_min_for_low = show_min_width_for_low;

            SetPadding(0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);

            bar_background = new UIPanel();
            bar_background.Width.Set(width, 0f);
            bar_background.Height.Set(height, 0f);
            Append(bar_background);

            bar_progress = new UIPanel();
            bar_progress.Width.Set(MIN_WIDTH, 0f);
            bar_progress.Height.Set(height, 0f);
            bar_progress.BackgroundColor = colour;
            Append(bar_progress);

            colour_border = bar_progress.BorderColor;
            colour_background = bar_progress.BackgroundColor;
        }

        public void SetProgress(float percent) {
            if (percent < 0f)
                percent = 0f;
            else if (percent > 1f)
                percent = 1f;

            float width = bar_background.Width.Pixels * percent;
            if (width < MIN_WIDTH) {
                if (show_min_for_low && percent > 0f) {
                    bar_progress.BorderColor = colour_border;
                    bar_progress.BackgroundColor = colour_background;
                    bar_progress.Width.Set(MIN_WIDTH, 0f);
                }
                else {
                    bar_progress.BackgroundColor = Color.Transparent;
                    bar_progress.BorderColor = Color.Transparent;
                }
            }
            else {
                bar_progress.BorderColor = colour_border;
                bar_progress.BackgroundColor = colour_background;
                bar_progress.Width.Set(width, 0f);
            }
        }
    }

    //UITransparantImage from jopojelly forum post
    //set color public 
    public class UITransparantImage : UIElement {
        private Texture2D _texture;
        public float ImageScale = 1f;
        public Color color;

        public UITransparantImage(Texture2D texture, Color color) {
            this._texture = texture;
            this.Width.Set((float)this._texture.Width, 0f);
            this.Height.Set((float)this._texture.Height, 0f);
            this.color = color;
        }

        public void SetImage(Texture2D texture) {
            this._texture = texture;
            this.Width.Set((float)this._texture.Width, 0f);
            this.Height.Set((float)this._texture.Height, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            CalculatedStyle dimensions = base.GetDimensions();
            spriteBatch.Draw(this._texture, dimensions.Position() + this._texture.Size() * (1f - this.ImageScale) / 2f, null, color, 0f, Vector2.Zero, this.ImageScale, SpriteEffects.None, 0f);
        }
    }

    class StatusIcon : UIElement {
        public static readonly float SIZE = Utilities.Textures.TEXTURE_STATUS_DEFAULT.Width;
        private const float TEXT_SCALE = 0.75f;
        private const float TEXT_VERTICAL_SPACE = 2f;

        private readonly Color COLOUR_TRANSPARENT = new Color(128, 128, 128, 120);
        private readonly Color COLOUR_SOLID = new Color(255, 255, 255, 255);
        private readonly Color COLOUR_TEXT = new Color(128, 128, 128, 128);

        private UITransparantImage icon, icon_background;
        private UIText text;
        private Systems.Status status;

        private string prior_text;

        public bool active;

        public StatusIcon() {
            SetPadding(0f);
            active = false;
            prior_text = "";

            Width.Set(SIZE, 0f);
            Height.Set(SIZE, 0f);

            icon_background = new UITransparantImage(Utilities.Textures.TEXTURE_STATUS_BACKGROUND_DEFAULT, COLOUR_TRANSPARENT);
            Append(icon_background);

            icon = new UITransparantImage(Utilities.Textures.TEXTURE_BLANK, COLOUR_TRANSPARENT);
            Append(icon);

            text = new UIText("", TEXT_SCALE);
            text.Left.Set(0f, 0f);
            text.Top.Set(SIZE + TEXT_VERTICAL_SPACE, 0f);
            text.TextColor = COLOUR_TEXT;
            Append(text);
        }

        public void SetPosition(float left, float top) {
            Left.Set(left, 0f);
            Top.Set(top, 0f);
            Recalculate();
            RecalculateChildren();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        public void SetStatus(Systems.Status status) {
            this.status = status;
            icon.SetImage(status.Texture);
            switch (status.Specific_Background) {
                case (Systems.Status.BACKGROUND.BUFF):
                    icon_background.SetImage(Utilities.Textures.TEXTURE_STATUS_BACKGROUND_BUFF);
                    break;
                case (Systems.Status.BACKGROUND.DEBUFF):
                    icon_background.SetImage(Utilities.Textures.TEXTURE_STATUS_BACKGROUND_DEBUFF);
                    break;
                default:
                    icon_background.SetImage(Utilities.Textures.TEXTURE_STATUS_BACKGROUND_DEFAULT);
                    break;
            }
            if (IsMouseHovering) {
                icon.color = COLOUR_SOLID;
                icon_background.color = COLOUR_SOLID;
                UIInfo.Instance.ShowStatus(this, status);
            }
            else {
                icon.color = COLOUR_TRANSPARENT;
                icon_background.color = COLOUR_TRANSPARENT;
                UIInfo.Instance.EndText(this);
            }
        }

        public void Update() {
            string str = status.GetIconDurationString();
            if (!str.Equals(prior_text)) {
                text.SetText(str);
                text.Recalculate();
                prior_text = str;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (active)
                base.Draw(spriteBatch);
            else
                return;
        }

        public override void MouseOver(UIMouseEvent evt) {
            base.MouseOver(evt);
            if (active) {
                icon.color = COLOUR_SOLID;
                icon_background.color = COLOUR_SOLID;
                UIInfo.Instance.ShowStatus(this, status);
            }
        }
        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            if (active) {
                icon.color = COLOUR_TRANSPARENT;
                icon_background.color = COLOUR_TRANSPARENT;
                UIInfo.Instance.EndText(this);
            }
        }
        public override void RightClick(UIMouseEvent evt) {
            base.RightClick(evt);
            if (active && status.Specific_Right_Click_End) {
                status.RemoveEverywhere();
            }
        }
    }

    class XPBar : UIElement {
        private const float TEXT_SCALE = 1f;
        private const float ICON_SCALE = 0.8f;
        private const float BAR_HEIGHT = 24f;
        private static readonly int ICON_SIZE = (int)Math.Ceiling(Utilities.Textures.TEXTURE_CLASS_DEFAULT.Width * ICON_SCALE);

        private UITransparantImage icon_background;
        private UIImage icon;
        private ProgressBar bar;
        private UIText text;
        private float left;

        public bool visible;
        public Systems.Class Class_Tracked { get; private set; }

        public XPBar(float width) {
            visible = false;
            
            SetPadding(0f);

            icon_background = new UITransparantImage(Utilities.Textures.TEXTURE_CLASS_BACKGROUND, Systems.Class.COLOUR_DEFAULT);
            icon_background.ImageScale = ICON_SCALE;
            Append(icon_background);

            icon = new UIImage(Utilities.Textures.TEXTURE_CLASS_DEFAULT);
            icon.ImageScale = ICON_SCALE;
            Append(icon);

            left = ICON_SIZE + Constants.UI_PADDING;
            bar = new ProgressBar(width - left, BAR_HEIGHT, Constants.COLOUR_XP_DIM);
            bar.Left.Set(left, 0f);
            bar.Top.Set((ICON_SIZE - bar.Height.Pixels) / 2f, 0f);
            Append(bar);

            text = new UIText("0123 / 45679", TEXT_SCALE);
            text.Top.Set(bar.Top.Pixels + ((bar.Height.Pixels - (Main.fontMouseText.MeasureString(text.Text).Y * TEXT_SCALE /2f)) / 2f), 0f);
            Append(text);

            Width.Set(width, 0f);
            Height.Set(Math.Max(ICON_SIZE, bar.Height.Pixels), 0f);
        }

        public void SetWidth (float width) {
            Width.Set(width, 0f);
            bar.Width.Set(width - left, 0f);
        }

        public void SetClass(Systems.Class class_new) {
            Class_Tracked = class_new;
            icon.SetImage(Class_Tracked.Texture);
            icon_background.color = class_new.Colour;

            visible = (Class_Tracked.Tier >= 1);

            Update();
        }

        public void Update() {
            if (visible) {
                uint xp = ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[Class_Tracked.ID_num];
                uint xp_needed = Systems.XP.Requirements.GetXPReq(Class_Tracked, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[Class_Tracked.ID_num]);

                float percent;
                bool maxed;
                if (xp_needed <= 0) {
                    percent = 1f;
                    maxed = true;
                }
                else {
                    percent = (float)xp / xp_needed;
                    maxed = false;
                }

                //progress bar
                bar.SetProgress(percent);

                //text
                string str;
                float string_width;
                if (maxed) {
                    str = "MAX";
                    string_width = Main.fontMouseText.MeasureString(str).X * TEXT_SCALE;
                }
                else {
                    str = xp + " / " + xp_needed;
                    string_width = Main.fontMouseText.MeasureString(xp_needed + " / " + xp_needed).X * TEXT_SCALE;

                    if (string_width > bar.Width.Pixels) {
                        str = (Math.Round(percent * 10000f) / 100) + "%";
                        string_width = Main.fontMouseText.MeasureString(str).X * TEXT_SCALE;
                    }
                }
                text.SetText(str);
                text.Left.Set(bar.Left.Pixels + ((bar.Width.Pixels - string_width) / 2), 0f);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (visible) {
                base.Draw(spriteBatch);
            }
        }

    }

    class AttributeText : UIPanel {

        private const float RIGHT_SIDE_PADDING = 5f;
        private const float LEFT_SUM = 60f;
        private const float SCALE_SUM = 0.8f;
        private const float SCALE_SUM_SMALL = 0.6f;

        private Systems.Attribute attribute;
        private UIText title, sum, sum_small, final, cost;
        private float scale, left_final;
        UIImageButton button_add;

        public AttributeText(float width, float height, float scale, Systems.Attribute attribute) {
            this.attribute = attribute;
            this.scale = scale;

            left_final = width - (Utilities.Textures.TEXTURE_BUTTON_SIZE*2) - RIGHT_SIDE_PADDING - UI.Constants.UI_PADDING;

            SetPadding(0f);
            Left.Set(0f, 0f);
            Top.Set(0f, 0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            BackgroundColor = UI.Constants.COLOUR_UI_PANEL_HIGHLIGHT;

            float top = ((height - (Main.fontMouseText.MeasureString("A").Y * scale)) / 2f) + UI.Constants.UI_PADDING;
            title = new UIText(attribute.Specific_Name_Short.ToUpper(), scale);
            title.Left.Set(UI.Constants.UI_PADDING, 0f);
            title.Top.Set(top, 0f);
            Append(title);

            float top_sum = ((height - (Main.fontMouseText.MeasureString("A").Y * SCALE_SUM_SMALL)) / 2f) + UI.Constants.UI_PADDING;
            sum_small = new UIText("", SCALE_SUM_SMALL);
            sum_small.Left.Set(LEFT_SUM, 0f);
            sum_small.Top.Set(top_sum, 0f);
            Append(sum_small);

            top_sum = ((height - (Main.fontMouseText.MeasureString("A").Y * SCALE_SUM)) / 2f) + UI.Constants.UI_PADDING;
            sum = new UIText("", SCALE_SUM);
            sum.Left.Set(LEFT_SUM, 0f);
            sum.Top.Set(top_sum, 0f);
            Append(sum);

            final = new UIText("", scale);
            final.Left.Set(left_final, 0f);
            final.Top.Set(top, 0f);
            Append(final);

            button_add = new UIImageButton(Utilities.Textures.TEXTURE_BUTTON_PLUS);
            button_add.Width.Set(Utilities.Textures.TEXTURE_BUTTON_SIZE, 0f);
            button_add.Height.Set(Utilities.Textures.TEXTURE_BUTTON_SIZE, 0f);
            button_add.Left.Set(width - (button_add.Width.Pixels * 2f) - RIGHT_SIDE_PADDING, 0f);
            button_add.Top.Set((height - button_add.Height.Pixels) / 2f, 0f);
            button_add.OnMouseDown += new MouseEvent(ClickAdd);
            Append(button_add);

            cost = new UIText("", SCALE_SUM);
            cost.Left.Set(button_add.Left.Pixels + button_add.Width.Pixels + Constants.UI_PADDING, 0f);
            cost.Top.Set(top_sum, 0f);
            Append(cost);

            Update();
        }

        public void ClickAdd(UIMouseEvent evt, UIElement listeningElement) {
            if (!UIInfo.AllowClicks()) return;

            ExperienceAndClasses.LOCAL_MPLAYER.LocalAttributeAllocationAddPoint(attribute.ID_num);
        }

        public override void MouseUp(UIMouseEvent evt) {
            UIInfo.Instance.ShowTextAttribute(this, attribute);
            base.MouseUp(evt);
        }

        public override void MouseOver(UIMouseEvent evt) {
            UIInfo.Instance.ShowTextAttribute(this, attribute);
            base.MouseOver(evt);
        }

        public override void MouseOut(UIMouseEvent evt) {
            UIInfo.Instance.EndText(this);
            base.MouseOut(evt);
        }

        public void Update() {
            string str = "" + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Final[attribute.ID_num];
            final.SetText(str);
            final.Left.Set(left_final - (Main.fontMouseText.MeasureString(str).X * scale), 0f);

            int allocation_cost = Systems.Attribute.AllocationPointCost(ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID_num]);
            cost.SetText("" + allocation_cost);

            float width_cutoff = final.Left.Pixels - sum.Left.Pixels;

            str = ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID_num] + "+" +
                    ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Class[attribute.ID_num] + "+" +
                    (ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Status[attribute.ID_num] + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated_Milestone[attribute.ID_num]);

            if ((Main.fontMouseText.MeasureString(str).X * SCALE_SUM) >= width_cutoff) {
                sum_small.SetText(str);
                sum.SetText("");
            }
            else {
                sum_small.SetText("");
                sum.SetText(str);
            }

            if (allocation_cost <= ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Unallocated) {
                button_add.SetVisibility(1f, 0.8f);
            }
            else {
                button_add.SetVisibility(0.4f, 0.2f);
            }
        }
    }

    class ClassButton : UIImageButton {
        private const float TEXT_SCALE = 0.7f;
        private const float TEXT_OFFSET = 5f;
        private const float LOW_VISIBILITY = 0.7f;
        private static readonly Color COLOUR_GRAY_OUT = new Color(170, 170, 170, 255);

        private float button_size = 0f;
        public Systems.Class Class { get; private set; }
        UIText text;
        UIImage image_lock;
        UITransparantImage background;

        public ClassButton(Systems.Class Class) : base(Class.Texture) {
            this.Class = Class;
            
            Width.Set(Class.Texture.Width, 0f);
            Height.Set(Class.Texture.Height, 0f);
            
            text = new UIText("", TEXT_SCALE);
            Append(text);

            button_size = Class.Texture.Width;

            image_lock = new UIImage(Utilities.Textures.TEXTURE_BLANK);
            image_lock.Width.Set(Utilities.Textures.TEXTURE_LOCK_WIDTH, 0f);
            image_lock.Height.Set(Utilities.Textures.TEXTURE_LOCK_HEIGHT, 0f);
            image_lock.Left.Set(button_size / 2 - Utilities.Textures.TEXTURE_LOCK_WIDTH / 2, 0f);
            image_lock.Top.Set(button_size / 2 - Utilities.Textures.TEXTURE_LOCK_HEIGHT / 2, 0f);
            Append(image_lock);

            background = new UITransparantImage(Utilities.Textures.TEXTURE_CLASS_BACKGROUND, Class.Colour);
            Append(background);

            SetVisibility(1f, LOW_VISIBILITY);
        }

        public override void Click(UIMouseEvent evt) {
            if (!UIInfo.AllowClicks()) return;

            base.Click(evt);

            if (!ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[Class.ID_num]) {
                UIInfo.Instance.ShowUnlockClass(this, Class);
            }
            else {
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num == Class.ID_num) {
                    Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None].LocalTrySetClass(true);
                }
                else {
                    Class.LocalTrySetClass(true);
                }
            }
        }

        public override void RightClick(UIMouseEvent evt) {
            if (!UIInfo.AllowClicks()) return;

            base.RightClick(evt);

            if(!ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary) {
                UIInfo.Instance.ShowUnlockSubclass(this);
            }
            else if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num == Class.ID_num) {
                Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None].LocalTrySetClass(false);
            }
            else {
                Class.LocalTrySetClass(false);
            }
        }

        public override void MouseOver(UIMouseEvent evt) {
            UIInfo.Instance.ShowTextClass(this, Class);
            base.MouseOver(evt);
        }

        public override void MouseOut(UIMouseEvent evt) {
            UIInfo.Instance.EndText(this);
            base.MouseOut(evt);
        }

        public void Update() {
            byte level = ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[Class.ID_num];
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[Class.ID_num]) {
                //not locked
                image_lock.SetImage(Utilities.Textures.TEXTURE_BLANK);

                //background
                background.color = Class.Colour;

                //text level
                string str = "";
                if (level >= Class.Max_Level) {
                    str = "MAX";
                }
                else {
                    str = "Lv." + level;
                }
                text.SetText(str, TEXT_SCALE, false);
                float message_size = Main.fontMouseText.MeasureString(str).X * TEXT_SCALE;
                text.Left.Set(button_size / 2 - message_size / 2, 0f);
                text.Top.Set(button_size + TEXT_OFFSET, 0F);
                text.Recalculate();

                if ((ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num == Class.ID_num) || (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num == Class.ID_num)) {
                    //selected
                    SetVisibility(LOW_VISIBILITY, 1f);
                }
                else {
                    //not selected
                    SetVisibility(1f, LOW_VISIBILITY);
                }
            }
            else {
                //locked
                if (Class.LocalHasClassPrereq()) {
                    image_lock.SetImage(Utilities.Textures.TEXTURE_LOCK_BROWN);
                }
                else {
                    image_lock.SetImage(Utilities.Textures.TEXTURE_LOCK_RED);
                }

                //background
                background.color = Class.Colour.MultiplyRGB(COLOUR_GRAY_OUT);

                //no text
                text.SetText("");
            }
            this.MouseOut(null);
        }

        public override void Draw(SpriteBatch spriteBatch) {
            background.Draw(spriteBatch);
            DrawSelf(spriteBatch);
            text.Draw(spriteBatch);
            image_lock.Draw(spriteBatch);
        }
    }

    //combines a UserInterface and a UIState
    public abstract class UIStateCombo {
        public UserInterface UI = null;
        public UIState state = null;

        public bool Visibility { get {
                if (UI == null) {
                    return false;
                }
                else {
                    return UI.IsVisible;
                }
            }
            set {
                if (UI != null) {
                    UI.IsVisible = value;
                    if (!UI.IsVisible) {
                        UIInfo.Instance.EndTextChildren(state);
                    }
                }
            }
        }

        public void Initialize() {
            UI = new UserInterface();
            Visibility = false; //default
            state = new UIState();
            InitializeState();
            state.Activate();
            UI.SetState(state);
        }

        protected abstract void InitializeState();

        public void Update(GameTime game_time) {
            if (Visibility) {
                UI.Update(game_time);
            }
        }

        public void Draw() {
            if (Visibility) {
                state.Draw(Main.spriteBatch);
            }
        }

    }

    // Copied from ExampleMod on GitHub, changes made:
    // added auto and close
    // added visible
    // switch drag to right click
    // improved restriction
    // added argument to prevent drag

    // This DragableUIPanel class inherits from UIPanel. 
    // Inheriting is a great tool for UI design. By inheriting, we get the background drawing for free from UIPanel
    // We've added some code to allow the panel to be dragged around. 
    // We've also added some code to ensure that the panel will bounce back into bounds if it is dragged outside or the screen resizes.
    // UIPanel does not prevent the player from using items when the mouse is clicked, so we've added that as well.
    public class DragableUIPanel : UIPanel {
        // Stores the offset from the top left of the UIPanel while dragging.
        private const float BUTTON_PANEL_EDGE_SPACE = 1f;

        private Vector2 offset;

        private bool dragging = false;
        private UIStateCombo UI;
        private bool buttons_hidden = false;
        private bool can_drag;
        private bool auto_hide_buttons;

        public bool visible = true;

        public float top_space { get; private set; }

        private HelpTextPanel title = null;

        private UIHoverImageButton button_auto = null, button_close = null;
        private DragableUIPanel button_panel = null;

        private bool auto = false;
        public bool Auto {
            get {
                return auto;
            }
            set {
                auto = value;
                if (button_auto != null) {
                    if (auto) {
                        button_auto.SetImage(Utilities.Textures.TEXTURE_CORNER_BUTTON_AUTO);
                        button_auto.hoverText = "Toggle With Inventory";
                    }
                    else {
                        button_auto.SetImage(Utilities.Textures.TEXTURE_CORNER_BUTTON_NO_AUTO);
                        button_auto.hoverText = "Manual";
                    }
                }
            }
        }

        public DragableUIPanel(float width, float height, Color color, UIStateCombo ui, bool enable_close, bool enable_auto, bool enable_drag = true, bool auto_hide_buttons = true) : base() {
            UI = ui;
            can_drag = enable_drag;
            this.auto_hide_buttons = auto_hide_buttons;

            top_space = 0;

            BackgroundColor = color;

            SetPadding(0);

            Left.Set(0f, 0f);
            Top.Set(0f, 0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);

            bool any_buttons = false;

            if (enable_close) {
                button_close = new UIHoverImageButton(Utilities.Textures.TEXTURE_CORNER_BUTTON_CLOSE, "Close");
                button_close.Width.Set(Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Height.Set(Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Top.Set(Constants.UI_PADDING, 0f);
                button_close.OnClick += new MouseEvent(ButtonClickClose);
                Append(button_close);
                any_buttons = true;
            }

            if (enable_auto) {
                button_auto = new UIHoverImageButton(Utilities.Textures.TEXTURE_BLANK, "");
                button_auto.Width.Set(Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Height.Set(Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Top.Set(Constants.UI_PADDING, 0f);
                button_auto.OnClick += new MouseEvent(ButtonClickAuto);
                Append(button_auto);
                Auto = true; //defaults to true if enabled
                any_buttons = true;
            }

            if (any_buttons) {
                button_panel = new DragableUIPanel(1, Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE + Constants.UI_PADDING * 2f - BUTTON_PANEL_EDGE_SPACE, Constants.COLOUR_UI_PANEL_BACKGROUND, UI, false, false, false, false);
                button_panel.Top.Set(BUTTON_PANEL_EDGE_SPACE, 0f);
                button_panel.BorderColor = Color.Transparent;
                Append(button_panel);
            }

            Recalculate();

            if (auto_hide_buttons) {
                HideButtons();
            }
        }

        public void SetTitle(string text, float text_scale=1, bool center=true, string helptext=null, string helptext_title=null) {
            if (title != null) {
                RemoveTitle();
            }
            title = new HelpTextPanel(text, text_scale, center, helptext, helptext_title);
            title.Width.Set(Width.Pixels, 0f);
            title.BackgroundColor = BackgroundColor;
            title.BorderColor = BorderColor;
            top_space = title.Height.Pixels;
            Append(title);
        }

        public void RemoveTitle() {
            if (title != null) {
                title.Remove();
                top_space = 0f;
            }
        }

        public override void Recalculate() {
            float left = Width.Pixels - Constants.UI_PADDING;
            if (button_close != null) {
                button_close.Left.Set(left -= Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            }
            if (button_auto != null) {
                button_auto.Left.Set(left -= Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            }
            if (button_panel != null) {
                button_panel.Left.Set(left -= Constants.UI_PADDING, 0f);
                button_panel.Width.Set(Width.Pixels - button_panel.Left.Pixels - BUTTON_PANEL_EDGE_SPACE, 0f);
            }
            base.Recalculate();
        }

        private void ButtonClickAuto(UIMouseEvent evt, UIElement listeningElement) {
            if (!buttons_hidden) {
                Auto = !Auto;
            }
        }

        private void ButtonClickClose(UIMouseEvent evt, UIElement listeningElement) {
            if (!buttons_hidden) {
                UI.Visibility = !UI.Visibility;
            }
        }

        public override void RightMouseDown(UIMouseEvent evt) {
            base.RightMouseDown(evt);
            if (can_drag && UIInfo.AllowClicks()) {
                DragStart(evt);
            }
        }

        public override void RightMouseUp(UIMouseEvent evt) {
            base.RightMouseUp(evt);
            if (can_drag) {
                DragEnd(evt);
            }
        }

        private void DragStart(UIMouseEvent evt) {
            offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
            dragging = true;
        }

        private void DragEnd(UIMouseEvent evt) {
            if (dragging) {
                Vector2 end = evt.MousePosition;

                Left.Set(end.X - offset.X, 0f);
                Top.Set(end.Y - offset.Y, 0f);

                Recalculate();
            }
            dragging = false;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime); // don't remove.

            // Checking ContainsPoint and then setting mouseInterface to true is very common. This causes clicks on this UIElement to not cause the player to use current items. 
            if (ContainsPoint(Main.MouseScreen)) {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (dragging) {
                Left.Set(Main.mouseX - offset.X, 0f); // Main.MouseScreen.X and Main.mouseX are the same.
                Top.Set(Main.mouseY - offset.Y, 0f);
                Recalculate();
            }

            Restrict();
        }

        private void Restrict() {
            Parent.Recalculate();

            float width = Parent.GetDimensions().Width;
            float height = Parent.GetDimensions().Height;

            float left = Left.Pixels;
            float top = Top.Pixels;

            if ((left + Width.Pixels) > width) {
                left = width - Width.Pixels;
            }
            if (left < 0f) {
                left = 0f;
            }

            if ((top + Height.Pixels) > height) {
                top = height - Height.Pixels;
            }
            if (top < 0f) {
                top = 0f;
            }

            Left.Set(left, 0f);
            Top.Set(top, 0f);
            Recalculate();
        }

        public void SetPosition(float left, float top, bool restrict=false) {
            //move
            Left.Set(left, 0f);
            Top.Set(top, 0f);

            Recalculate();

            if (restrict) {
                Restrict();
            }
        }

        public void SetSize(float width, float height) {
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            if (title != null) {
                title.Width.Set(Width.Pixels, 0f);
            }

            Recalculate();
        }

        public float GetLeft() {
            return Left.Pixels;
        }

        public float GetTop() {
            return Top.Pixels;
        }

        public void HideButtons() {
            buttons_hidden = true;
            if (button_auto != null) {
                button_auto.visible = false;
            }
            if (button_close != null) {
                button_close.visible = false;
            }
            if (button_panel != null) {
                button_panel.visible = false;
            }
        }

        public void ShowButtons() {
            buttons_hidden = false;
            if (button_auto != null) {
                button_auto.visible = true;
            }
            if (button_close != null) {
                button_close.visible = true;
            }
            if (button_panel != null) {
                button_panel.visible = true;
            }
        }

        public override void MouseOver(UIMouseEvent evt) {
            base.MouseOver(evt);
            if (auto_hide_buttons) {
                ShowButtons();
            }
        }

        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            if (auto_hide_buttons) {
                HideButtons();
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (visible) {
                base.Draw(spriteBatch);
                if (button_panel != null) {
                    button_panel.Draw(spriteBatch); //draw panel first
                }
                if (button_auto != null) {
                    button_auto.Draw(spriteBatch);
                }
                if (button_close != null) {
                    button_close.Draw(spriteBatch);
                }
            }
            else
                return;
        }

    }

    // Copied from ExampleMod on GitHub

    // This UIHoverImageButton class inherits from UIImageButton. 
    // Inheriting is a great tool for UI design. 
    // By inheriting, we get the Image drawing, MouseOver sound, and fading for free from UIImageButton
    // We've added some code to allow the Button to show a text tooltip while hovered. 
    internal class UIHoverImageButton : UIImageButton {
        internal string hoverText;

        public bool visible = true;

        public UIHoverImageButton(Texture2D texture, string hoverText) : base(texture) {
            this.hoverText = hoverText;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);
            if (IsMouseHovering) {
                Main.hoverItemName = hoverText;
            }
        }

        public override void Click(UIMouseEvent evt) {
            if (!UIInfo.AllowClicks()) return;
            base.Click(evt);
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (visible) {
                base.Draw(spriteBatch);
            }
        }
    }

    public class AbilityIcon : UIElement {
        public static readonly float SIZE = Utilities.Textures.TEXTURE_ABILITY_DEFAULT.Width;
        private static readonly Color COLOUR_TRANSPARENT = new Color(128, 128, 128, 120);
        private static readonly Color COLOUR_SOLID = new Color(255, 255, 255, 255);
        private static readonly Color COLOUR_GRAY_OUT = new Color(170, 170, 170, 255);

        private UITransparantImage icon, icon_background;
        private Systems.Ability ability;
        public bool active;
        private int add_y;

        public AbilityIcon(int add_y = 0) {
            SetPadding(0f);
            active = false;

            Width.Set(SIZE, 0f);
            Height.Set(SIZE, 0f);

            icon_background = new UITransparantImage(Utilities.Textures.TEXTURE_ABILITY_BACKGROUND, Systems.Class.COLOUR_DEFAULT);
            Append(icon_background);

            icon = new UITransparantImage(Utilities.Textures.TEXTURE_BLANK, COLOUR_TRANSPARENT);
            Append(icon);

            this.add_y = add_y;
        }

        public void Update() {
            if (active && (ability != null)) {
                icon.SetImage(ability.Texture);
                if (ability.Unlocked) {
                    icon.color = COLOUR_SOLID;
                    icon_background.color = ability.Colour;
                }
                else {
                    icon.color = COLOUR_TRANSPARENT;
                    icon_background.color = ability.Colour.MultiplyRGB(COLOUR_GRAY_OUT);
                }
            }
            else {
                icon.SetImage(Utilities.Textures.TEXTURE_BLANK);
                icon.color = COLOUR_TRANSPARENT;
            }
        }

        public void SetAbility(Systems.Ability ability) {
            this.ability = ability;
            if (ability == null) {
                active = false;
            }
            else {
                active = true;
            }
            Update();
        }

        public override void MouseOver(UIMouseEvent evt) {
            base.MouseOver(evt);
            if (active) {
                UIInfo.Instance.ShowAbility(this, ability);
            }
        }

        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            UIInfo.Instance.EndText(this);
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (active) {
                base.Draw(spriteBatch);
            }
        }
    }

    public class ScrollPanel : UIPanel {
        public const float SCROLLBAR_WIDTH = 20f;
        private const float ITEM_SPACING = UI.Constants.UI_PADDING;
        private const float SCROLL_SPEED = 20f;

        private FixedUIScrollbar scrollbar;
        private List<UIElement> items;
        private List<float> item_tops;

        RasterizerState _rasterizerState = new RasterizerState() { ScissorTestEnable = true };

        public ScrollPanel(float width, float height, UserInterface ui, bool transparent = true) {
            SetPadding(0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);

            if (transparent) {
                BackgroundColor = Color.Transparent;
                BorderColor = Color.Transparent;
            }

            scrollbar = new FixedUIScrollbar(ui);
            scrollbar.Height.Set(height - (UI.Constants.UI_PADDING * 3f), 0f);
            scrollbar.Top.Set(UI.Constants.UI_PADDING, 0f);
            scrollbar.Width.Set(SCROLLBAR_WIDTH, 0f);
            scrollbar.Left.Set(width - SCROLLBAR_WIDTH - UI.Constants.UI_PADDING, 0f);
            Append(scrollbar);

            item_tops = new List<float>();
            items = new List<UIElement>();
        }

        public void SetItems(List<UIElement> items) {
            this.items = items;
            RecalcItems();
        }

        private void RecalcItems() {
            RemoveAllChildren();
            Append(scrollbar);
            if (items.Count > 0) {
                item_tops.Clear();
                float top = ITEM_SPACING;
                foreach (UIElement item in items) {
                    item.Top.Set(top, 0f);
                    item.Left.Set(UI.Constants.UI_PADDING, 0f);
                    item_tops.Add(top);
                    Append(item);
                    top += item.Height.Pixels + ITEM_SPACING;
                }
                if (top < Height.Pixels) {
                    top = Height.Pixels;
                }
                scrollbar.SetView(Height.Pixels, top);
                scrollbar.ViewPosition = 0f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //draw panel in the back
            DrawSelf(spriteBatch);
            
            //stop normal draw
            spriteBatch.End();

            //start clipping draw
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, _rasterizerState, null, Main.UIScaleMatrix);
            Rectangle prior_rect = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = GetClippingRectangle(spriteBatch);

            //draw each item in position (clips if outside panel)
            for (int i = 0; i < items.Count; i++) {
                items[i].Top.Set(item_tops[i] - scrollbar.GetValue(), 0f);
                items[i].Draw(spriteBatch);
            }
            
            //put settings back as they were
            spriteBatch.GraphicsDevice.ScissorRectangle = prior_rect;

            //stop clipping draw
            spriteBatch.End();

            //start normal draw again
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

            //draw scrollbar over everything
            scrollbar.Draw(spriteBatch);
        }

        public override void ScrollWheel(UIScrollWheelEvent evt) {
            base.ScrollWheel(evt);
            if (evt.ScrollWheelValue > 0) {
                scrollbar.ViewPosition -= SCROLL_SPEED;
            }
            else {
                scrollbar.ViewPosition += SCROLL_SPEED;
            }
        }
    }

    public class PassiveIcon : UIElement {
        private const float ICON_SIZE = 22;
        private static readonly float ICON_SCALE = ICON_SIZE / Utilities.Textures.TEXTURE_PASSIVE_DEFAULT.Width;
        private static readonly int background_shift = (int)Math.Ceiling(Utilities.Textures.TEXTURE_RESOURCE_DEFAULT.Height * (1f - ICON_SCALE) / 2f);
        private const float TEXT_SCALE = 0.7f;
        private const float TEXT_WIDTH = 150f;
        private static readonly Color COLOUR_TRANSPARENT = new Color(128, 128, 128, 120);
        private static readonly Color COLOUR_SOLID = new Color(255, 255, 255, 255);
        private static readonly Color COLOUR_GRAY_OUT = new Color(170, 170, 170, 255);

        private UITransparantImage icon, icon_background;
        private HelpTextPanel text;
        private Systems.Passive passive;

        public PassiveIcon(Systems.Passive passive) {
            this.passive = passive;

            SetPadding(0f);

            icon_background = new UITransparantImage(passive.Texture_Background, passive.Colour);
            icon_background.ImageScale = ICON_SCALE;
            Append(icon_background);

            Texture2D texture = passive.Texture;
            Color colour_text;
            if (passive.Unlocked) {
                icon = new UITransparantImage(texture, COLOUR_SOLID);
                colour_text = Color.White;
            }
            else {
                icon = new UITransparantImage(texture, COLOUR_TRANSPARENT);
                colour_text = Color.Gray;
                icon_background.color = icon_background.color.MultiplyRGB(COLOUR_GRAY_OUT);
            }
            icon.ImageScale = ICON_SCALE;
            icon.Width.Set(ICON_SIZE, 0f);
            icon.Height.Set(ICON_SIZE, 0f);
            icon.Left.Set(0f, 0f);
            icon.Top.Set(0f, 0f);
            Append(icon);

            text = new HelpTextPanel(passive.Specific_Name, TEXT_SCALE, false, null, null, true, true);
            text.SetTextColour(colour_text);
            text.Height.Set(icon.Height.Pixels, 0f);
            text.Left.Set(icon.Width.Pixels + UI.Constants.UI_PADDING, 0f);
            text.Top.Set(UI.Constants.UI_PADDING, 0f);
            text.Recalculate();
            Append(text);

            Width.Set(text.Left.Pixels + text.Width.Pixels + UI.Constants.UI_PADDING, 0f);
            Height.Set(icon.Height.Pixels , 0f);
        }

        public override void MouseOver(UIMouseEvent evt) {
            base.MouseOver(evt);
            UIInfo.Instance.ShowPassive(this, passive);
        }

        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            UIInfo.Instance.EndText(this);
        }
    }

    public class AbilityIconCooldown : UIElement {
        public const float SIZE = 24f;
        public static readonly float ICON_SCALE = SIZE / Utilities.Textures.TEXTURE_ABILITY_DEFAULT.Width;
        private static readonly int background_shift = (int)Math.Ceiling(Utilities.Textures.TEXTURE_ABILITY_DEFAULT.Height * (1f - ICON_SCALE) / 2f);
        private static readonly Color COLOUR_TRANSPARENT = new Color(0, 0, 0, 255);
        private static readonly Color COLOUR_SOLID = new Color(255, 255, 255, 255);
        private static readonly Color COLOUR_COOLDOWN = new Color(128, 128, 128, 200);
        private static readonly Color COLOUR_GRAY_OUT = new Color(170, 170, 170, 255);

        private Systems.Ability ability;
        public bool active = false;
        private UITransparantImage icon, icon_background;
        private int cooldown_shift;
        private bool on_cooldown;

        public AbilityIconCooldown() {
            Width.Set(SIZE, 0f);
            Height.Set(SIZE, 0f);

            icon_background = new UITransparantImage(Utilities.Textures.TEXTURE_ABILITY_BACKGROUND, Systems.Class.COLOUR_DEFAULT);
            icon_background.ImageScale = ICON_SCALE;
            Append(icon_background);

            icon = new UITransparantImage(Utilities.Textures.TEXTURE_ABILITY_DEFAULT, COLOUR_SOLID);
            icon.ImageScale = ICON_SCALE;
            Append(icon);
        }

        /// <summary>
        /// return true if on cooldown
        /// </summary>
        /// <returns></returns>
        public bool Update() {
            on_cooldown = false;
            if (active) {
                float cooldown_percent = ability.CooldownPercent();
                if (cooldown_percent > 0f) {
                    icon.color = COLOUR_TRANSPARENT;
                    icon_background.color = ability.Colour.MultiplyRGB(COLOUR_GRAY_OUT);
                    on_cooldown = true;
                    cooldown_shift = (int)Math.Round((1f - cooldown_percent) * SIZE);
                }
                else {
                    icon.color = COLOUR_SOLID;
                    icon_background.color = ability.Colour;
                }
            }
            return on_cooldown;
        }

        public void SetAbility(Systems.Ability ability) {
            this.ability = ability;
            active = true;
            icon.SetImage(ability.Texture);
            Update();
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (active) {
                //draw icon
                icon_background.Draw(spriteBatch);
                icon.Draw(spriteBatch);

                if (on_cooldown) {
                    //start clipping draw
                    Rectangle rect = icon.GetDimensions().ToRectangle(); // icon.GetClippingRectangle(spriteBatch);
                    rect.X += background_shift;
                    rect.Y += background_shift;
                    rect.Width = (int)SIZE; 
                    rect.Height = (int)SIZE;
                    spriteBatch.GraphicsDevice.ScissorRectangle = rect;

                    //draw cover
                    rect.Y += cooldown_shift;
                    rect.Height -= cooldown_shift;
                    spriteBatch.Draw(Utilities.Textures.TEXTURE_ABILITY_COOLDOWN_COVER, rect, COLOUR_COOLDOWN);
                }
            }
        }
    }

    public class ResourceBar : UIElement {
        public const float HEIGHT = 24f;
        private const float HEIGHT_BAR = 20f;
        private static readonly float DOT_HEIGHT = Utilities.Textures.TEXTURE_RESOURCE_DOT.Height;
        private static readonly float DOT_WIDTH = Utilities.Textures.TEXTURE_RESOURCE_DOT.Width;
        public static readonly float ICON_SCALE = HEIGHT / Utilities.Textures.TEXTURE_RESOURCE_DEFAULT.Height;
        private static readonly int background_shift = (int)Math.Ceiling(Utilities.Textures.TEXTURE_RESOURCE_DEFAULT.Height * (1f - ICON_SCALE) / 2f);
        private Systems.Resource resource;
        private UIImage icon;
        private UITransparantImage icon_background;
        private bool bar_mode;
        private ProgressBar bar;
        private byte max_dots;
        private float dot_top;
        private byte dot_value, dot_capacity;
        private float left;
        private Color colour;

        public ResourceBar(Systems.Resource resource, float width) {
            Width.Set(width, 0f);
            Height.Set(HEIGHT, 0f);

            colour = resource.colour;

            icon_background = new UITransparantImage(Utilities.Textures.TEXTURE_RESOURCE_BACKGROUND, resource.colour);
            icon_background.ImageScale = ICON_SCALE;
            Append(icon_background);

            this.resource = resource;
            icon = new UIImage(resource.Texture);
            icon.ImageScale = ICON_SCALE;
            Append(icon);

            left = HEIGHT + Constants.UI_PADDING;
            float width_tracker = width - left;

            //bar
            bar = new ProgressBar(width_tracker, HEIGHT_BAR, colour, true);
            bar.Left.Set(left, 0f);
            bar.Top.Set(UIHUD.SPACING + (HEIGHT - HEIGHT_BAR)/2f, 0f);
            Append(bar);

            //dots
            max_dots = (byte)(width_tracker / DOT_WIDTH);
            dot_top = UIHUD.SPACING + ((HEIGHT - DOT_HEIGHT) / 2f) + 1f;

            Update();
        }

        public void Update() {
            //set bar/dot mode
            if (bar_mode && (resource.Specific_Capacity <= max_dots)) {
                bar_mode = false;
            }
            else if (!bar_mode && (resource.Specific_Capacity > max_dots)) {
                bar_mode = true;
            }

            //set value
            if (bar_mode) {
                bar.SetProgress(resource.Value / (float)resource.Specific_Capacity);
            }
            else {
                dot_value = (byte)resource.Value;
                dot_capacity = (byte)resource.Specific_Capacity;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            icon_background.Draw(spriteBatch);
            icon.Draw(spriteBatch);
            if (bar_mode) {
                bar.Draw(spriteBatch);
            }
            else {
                Rectangle rect = icon.GetClippingRectangle(spriteBatch);
                rect.Y += (int)dot_top;
                rect.X += (int)left;
                rect.Width = (int)DOT_WIDTH;
                rect.Height = (int)DOT_HEIGHT;
                for (byte i = 0; i < dot_value; i++) {
                    spriteBatch.Draw(Utilities.Textures.TEXTURE_RESOURCE_DOT, rect, colour);
                    rect.X += (int)DOT_WIDTH;
                }
                for (byte i = dot_value; i < dot_capacity; i++) {
                    spriteBatch.Draw(Utilities.Textures.TEXTURE_RESOURCE_DOT, rect, Color.Gray);
                    rect.X += (int)DOT_WIDTH;
                }
            }
        }
    }

    public class BetterTextButton : UIElement {
        DragableUIPanel panel;
        private UIText text_normal, text_select;

        public BetterTextButton(string text, float scale_normal, float scale_select, UIStateCombo combo, UIElement.MouseEvent click, bool big_font = false) {

            float scale_subtract = 0f;
            if (big_font) {
                scale_subtract = 1f;
            }

            text_normal = new UIText(text, scale_normal - scale_subtract, big_font);
            text_select = new UIText(text, scale_select - scale_subtract, big_font);

            Vector2 text_measure = Main.fontMouseText.MeasureString(text_normal.Text);
            float width = text_measure.X * scale_normal;
            float height = text_measure.Y / 2f * scale_normal;
            Width.Set(width, 0f);
            Height.Set(height, 0f);

            text_measure = Main.fontMouseText.MeasureString(text_select.Text);
            float width_select = text_measure.X * scale_select;
            float height_select = text_measure.Y / 2f * scale_select;
            text_select.Left.Set(-(width_select - width) / 2f, 0f);
            text_select.Top.Set(-(height_select - height) / 2f, 0f);

            panel = new DragableUIPanel(width, height, Color.Transparent, combo, false, false, false, true);
            panel.OnClick += click;
            panel.OnMouseOver += ClickSound;
            panel.Append(text_normal);
            panel.Append(text_select);
            
            Append(panel);
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (IsMouseHovering) {
                text_select.Draw(spriteBatch);
            }
            else {
                text_normal.Draw(spriteBatch);
            }
        }

        private void ClickSound(UIMouseEvent evt, UIElement listeningElement) {
            Main.PlaySound(SoundID.MenuTick);
        }

        public void SetText(string text) {
            text_normal.SetText(text);
            text_select.SetText(text);
        }

        public void SetColour(Color colour_normal, Color colour_select) {
            text_normal.TextColor = colour_normal;
            text_select.TextColor = colour_select;
        }
    }

    public class SettingsToggle : UIElement {
        private BetterTextButton button;
        private Utilities.Containers.Setting setting;

        public SettingsToggle(Utilities.Containers.Setting setting, float text_scale, float text_scale_hover, float width, UIStateCombo combo) {
            this.setting = setting;

            HelpTextPanel label = new HelpTextPanel(setting.NAME + ": ", text_scale, false, setting.DESCRIPTION, setting.NAME, false, true);
            label.OnClick += new UIElement.MouseEvent(Click);

            Vector2 text_measure = Main.fontMouseText.MeasureString("False");
            float width_value = text_measure.X * text_scale;

            button = new BetterTextButton("False", text_scale, text_scale_hover, combo, Click);
            button.Left.Set(width - width_value - Constants.UI_PADDING, 0f);

            if (button.Left.Pixels < label.Width.Pixels) {
                button.Left.Set(label.Width.Pixels, 0f);
            }

            label.Width.Set(width, 0f);

            Width.Set(width, 0f);
            Height.Set(label.Height.Pixels, 0f);

            Append(button);
            Append(label);

            SetValue(setting.value);
        }

        private void SetValue(bool new_value) {
            setting.value = new_value;
            button.SetText("" + setting.value);
            if (setting.value) {
                button.SetColour(Color.Green, Color.Green);
            }
            else {
                button.SetColour(Color.Red, Color.Red);
            }
        }

        private void Click(UIMouseEvent evt, UIElement listeningElement) {
            SetValue(!setting.value);
        }
    }
}