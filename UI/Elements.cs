using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
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
        private UIText text;
        private string help_text_title, help_text;
        private bool center_text;
        private float text_scale;
        private Vector2 text_measure;

        public HelpTextPanel(string title, float text_scale, bool center_text=true, string help_text=null, string help_text_title=null) {
            SetPadding(0f);
            this.help_text_title = help_text_title;
            this.help_text = help_text;
            this.center_text = center_text;
            this.text_scale = text_scale;

            text = new UIText(title, text_scale);
            Append(text);

            text_measure = Main.fontMouseText.MeasureString(text.Text);

            Height.Set((text_measure.Y * text_scale / 2f) + (Constants.UI_PADDING * 2) + 2f, 0f);
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

        public ProgressBar(float width, float height, Color colour) {
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
                bar_progress.BackgroundColor = Color.Transparent;
                bar_progress.BorderColor = Color.Transparent;
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
        private const float TEXT_SCALE = 0.65f;
        private const float TEXT_VERTICAL_SPACE = 2f;

        private readonly Color COLOUR_TRANSPARENT = new Color(128, 128, 128, 120);
        private readonly Color COLOUR_SOLID = new Color(255, 255, 255, 255);
        private readonly Color COLOUR_TEXT = new Color(255, 255, 255, 128);

        private UITransparantImage icon;
        private UIText text;
        private Systems.Status status;

        private string prior_text;

        public bool active;

        public StatusIcon() {
            SetPadding(0f);
            active = false;
            prior_text = "";

            Width.Set(UIStatus.BUFF_SIZE, 0f);
            Height.Set(UIStatus.BUFF_SIZE, 0f);

            icon = new UITransparantImage(Utilities.Textures.TEXTURE_BLANK, COLOUR_TRANSPARENT);
            Append(icon);

            text = new UIText("", TEXT_SCALE);
            text.Left.Set(0f, 0f);
            text.Top.Set(UIStatus.BUFF_SIZE + TEXT_VERTICAL_SPACE, 0f);
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
        }

        public void Update() {
            string str = status.GetIconDurationString();
            if (!str.Equals(prior_text)) {
                text.SetText(str);
                text.Recalculate();
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
                UIInfo.Instance.ShowStatus(this, status);
            }
        }
        public override void MouseOut(UIMouseEvent evt) {
            base.MouseOut(evt);
            if (active) {
                icon.color = COLOUR_TRANSPARENT;
                UIInfo.Instance.EndText(this);
            }
        }
    }

    class XPBar : UIElement {
        private const float TEXT_SCALE = 1f;
        private const float ICON_SCALE = 0.8f;
        private const float BAR_HEIGHT = 24f;

        private UIImage icon;
        private ProgressBar bar;
        private UIText text;

        public bool Visible { get; private set; }
        public Systems.Class Class_Tracked { get; private set; }

        public XPBar(float width, Systems.Class c) {
            Class_Tracked = c;

            Visible = true;
            
            SetPadding(0f);

            icon = new UIImage(Class_Tracked.Texture);
            icon.ImageScale = ICON_SCALE;
            icon.Top.Set(-(Class_Tracked.Texture.Height * (1f - ICON_SCALE) / 2f), 0f);
            icon.Left.Set(-(Class_Tracked.Texture.Width * (1f - ICON_SCALE) / 2f), 0f);
            icon.Width.Set(Class_Tracked.Texture.Width * ICON_SCALE, 0f);
            icon.Height.Set(Class_Tracked.Texture.Height * ICON_SCALE, 0f);
            Append(icon);

            float left = icon.Width.Pixels + Constants.UI_PADDING;
            bar = new ProgressBar(width - left, BAR_HEIGHT, Constants.COLOUR_XP_DIM);
            bar.Left.Set(left, 0f);
            bar.Top.Set((icon.Height.Pixels - bar.Height.Pixels) / 2f, 0f);
            Append(bar);

            text = new UIText("0123 / 45679", TEXT_SCALE);
            text.Top.Set(bar.Top.Pixels + ((bar.Height.Pixels - (Main.fontMouseText.MeasureString(text.Text).Y * TEXT_SCALE /2f)) / 2f), 0f);
            Append(text);

            Width.Set(width, 0f);
            Height.Set(Math.Max(icon.Height.Pixels, bar.Height.Pixels), 0f);
        }

        public void SetClass(Systems.Class class_new) {
            Class_Tracked = class_new;
            icon.SetImage(Class_Tracked.Texture);

            Visible = (Class_Tracked.Tier >= 1);

            Update();
        }

        public void Update() {
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

        public override void Draw(SpriteBatch spriteBatch) {
            if (Visible)
                base.Draw(spriteBatch);
            else
                return;
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
            BackgroundColor = UI.Constants.COLOR_UI_PANEL_HIGHLIGHT;

            float top = ((height - (Main.fontMouseText.MeasureString("A").Y * scale)) / 2f) + UI.Constants.UI_PADDING;
            title = new UIText(attribute.Name_Short.ToUpper(), scale);
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

            ExperienceAndClasses.LOCAL_MPLAYER.LocalAttributeAllocationAddPoint(attribute.ID);
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
            string str = "" + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Final[attribute.ID];
            final.SetText(str);
            final.Left.Set(left_final - (Main.fontMouseText.MeasureString(str).X * scale), 0f);

            int allocation_cost = Systems.Attribute.AllocationPointCost(ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID]);
            cost.SetText("" + allocation_cost);

            float width_cutoff = final.Left.Pixels - sum.Left.Pixels;

            str = ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID] + "+" +
                    ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Class[attribute.ID] + "+" +
                    ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Status[attribute.ID];

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
        private const float LOW_VISIBILITY = 0.4f;
        private float button_size = 0f;

        public Systems.Class Class { get; private set; }
        UIText text;
        UIImage image_lock;

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
                SetVisibility(1f, LOW_VISIBILITY);

                //no text
                text.SetText("");
            }
            this.MouseOut(null);
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
                        button_auto.hoverText = "Hide In Inventory";
                    }
                    else {
                        button_auto.SetImage(Utilities.Textures.TEXTURE_CORNER_BUTTON_NO_AUTO);
                        button_auto.hoverText = "Always Show";
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
                button_panel = new DragableUIPanel(1, Utilities.Textures.TEXTURE_CORNER_BUTTON_SIZE + Constants.UI_PADDING * 2f - BUTTON_PANEL_EDGE_SPACE, Constants.COLOR_UI_PANEL_BACKGROUND, UI, false, false, false, false);
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
}