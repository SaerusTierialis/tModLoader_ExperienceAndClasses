using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

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

            icon = new UITransparantImage(Textures.TEXTURE_BLANK, COLOUR_TRANSPARENT);
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

        public void SetStatus(Systems.Status status) {
            this.status = status;

            //TODO set texture

            Update();
        }

        public void Update() {
            //TODO make duration string
            string str = "1 s";

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
        private const float MIN_PROGRESS = 0.08f;

        private UIImage icon;
        private UIPanel bar_back, bar_progress;
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

            bar_back = new UIPanel();
            bar_back.Left.Set(icon.Width.Pixels + Constants.UI_PADDING, 0f);
            bar_back.Width.Set(width - bar_back.Left.Pixels, 0f);
            bar_back.Height.Set(BAR_HEIGHT, 0f);
            bar_back.Top.Set((icon.Height.Pixels - bar_back.Height.Pixels) / 2f, 0f);
            Append(bar_back);

            bar_progress = new UIPanel();
            bar_progress.BackgroundColor = Constants.COLOUR_XP;
            bar_progress.Left.Set(bar_back.Left.Pixels, 0f);
            bar_progress.Top.Set(bar_back.Top.Pixels, 0f);
            bar_progress.Height.Set(bar_back.Height.Pixels, 0f);
            Append(bar_progress);

            text = new UIText("0123 / 45679", TEXT_SCALE);
            text.Top.Set(bar_back.Top.Pixels + ((bar_back.Height.Pixels - (Main.fontMouseText.MeasureString(text.Text).Y * TEXT_SCALE /2f)) / 2f), 0f);
            Append(text);

            Width.Set(width, 0f);
            Height.Set(Math.Max(icon.Height.Pixels, bar_back.Height.Pixels), 0f);
        }

        public void SetClass(Systems.Class class_new) {
            Class_Tracked = class_new;
            icon.SetImage(Class_Tracked.Texture);

            Visible = (Class_Tracked.Tier >= 1);

            Update();
        }

        public void Update() {
            uint xp = ExperienceAndClasses.LOCAL_MPLAYER.Class_XP[Class_Tracked.ID];
            uint xp_needed = Systems.XP.GetXPReq(Class_Tracked.Tier, ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[Class_Tracked.ID]);

            float percent;
            bool maxed;
            if (xp_needed <= 0) {
                percent = 1f;
                maxed = true;
            }
            else {
                percent = MIN_PROGRESS + ((float)xp / xp_needed * (1f - MIN_PROGRESS));
                maxed = false;
            }

            //progress bar
            bar_progress.Width.Set(bar_back.Width.Pixels * percent, 0f);
            bar_progress.Recalculate();

            //text
            string str;
            float string_width;
            if (maxed) {
                str = "MAX";
                string_width = Main.fontMouseText.MeasureString(str).X * TEXT_SCALE;
            }
            else {
                str = xp + " / " + xp_needed;
                string_width = Main.fontMouseText.MeasureString(str).X * TEXT_SCALE;

                if (string_width > bar_back.Width.Pixels) {
                    str = (Math.Round(percent * 10000f) / 100) + "%";
                    string_width = Main.fontMouseText.MeasureString(str).X * TEXT_SCALE;
                }
            }
            text.SetText(str);
            text.Left.Set(bar_back.Left.Pixels + ((bar_back.Width.Pixels - string_width) / 2), 0f);

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
        private UIText title, sum, sum_small, final;
        private float scale, left_final;

        public AttributeText(float width, float height, float scale, Systems.Attribute attribute) {
            this.attribute = attribute;
            this.scale = scale;

            left_final = width - (Textures.TEXTURE_BUTTON_SIZE*2) - RIGHT_SIDE_PADDING - UI.Constants.UI_PADDING;

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

            float top_sum = ((height - (Main.fontMouseText.MeasureString("A").Y * SCALE_SUM)) / 2f) + UI.Constants.UI_PADDING;
            sum = new UIText("", SCALE_SUM);
            sum.Left.Set(LEFT_SUM, 0f);
            sum.Top.Set(top_sum, 0f);
            Append(sum);

            top_sum = ((height - (Main.fontMouseText.MeasureString("A").Y * SCALE_SUM_SMALL)) / 2f) + UI.Constants.UI_PADDING;
            sum_small = new UIText("", SCALE_SUM_SMALL);
            sum_small.Left.Set(LEFT_SUM, 0f);
            sum_small.Top.Set(top_sum, 0f);
            Append(sum_small);

            final = new UIText("", scale);
            final.Left.Set(left_final, 0f);
            final.Top.Set(top, 0f);
            Append(final);

            UIImageButton button_add = new UIImageButton(Textures.TEXTURE_BUTTON_PLUS);
            button_add.Width.Set(Textures.TEXTURE_BUTTON_SIZE, 0f);
            button_add.Height.Set(Textures.TEXTURE_BUTTON_SIZE, 0f);
            button_add.Left.Set(width - (button_add.Width.Pixels * 2f) - RIGHT_SIDE_PADDING, 0f);
            button_add.Top.Set((height - button_add.Height.Pixels) / 2f, 0f);
            button_add.OnMouseDown += new MouseEvent(ClickAdd);
            Append(button_add);

            UIImageButton button_subtract = new UIImageButton(Textures.TEXTURE_BUTTON_MINUS);
            button_subtract.Width.Set(height, 0f);
            button_subtract.Height.Set(height, 0f);
            button_subtract.Left.Set(width - button_add.Width.Pixels - RIGHT_SIDE_PADDING, 0f);
            button_subtract.Top.Set((height - button_add.Height.Pixels) / 2f, 0f);
            button_subtract.OnMouseDown += new MouseEvent(ClickSubtract);
            Append(button_subtract);

            Update();
        }

        public void ClickAdd(UIMouseEvent evt, UIElement listeningElement) {
            ExperienceAndClasses.LOCAL_MPLAYER.LocalAttributeAllocation(attribute.ID, +1);
        }

        public void ClickSubtract(UIMouseEvent evt, UIElement listeningElement) {
            ExperienceAndClasses.LOCAL_MPLAYER.LocalAttributeAllocation(attribute.ID, -1);
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

            float width_cutoff = final.Left.Pixels - sum.Left.Pixels;

            short bonus = ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Bonus[attribute.ID];
            str = "(" + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Base[attribute.ID] + "+" + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID];
            if (bonus != 0) {
                str += "+" + bonus;
            }
            str += ")";

            if ((Main.fontMouseText.MeasureString(str).X * SCALE_SUM) >= width_cutoff) {
                sum_small.SetText(str);
                sum.SetText("");
            }
            else {
                sum_small.SetText("");
                sum.SetText(str);
            }
        }
    }

    class ClassButton : UIImageButton {
        private const float TEXT_SCALE = 0.7f;
        private const float TEXT_OFFSET = 5f;
        private const float LOW_VISIBILITY = 0.4f;
        private float button_size = 0f;

        public byte class_id { get; private set; }
        UIText text;
        UIImage image_lock;

        public ClassButton(Texture2D texture, byte class_id) : base(texture) {
            this.class_id = class_id;
            OnClick += new MouseEvent(ClickPrimary);
            OnRightClick += new MouseEvent(ClickSecondary);
            
            text = new UIText("", TEXT_SCALE);
            Append(text);

            button_size = texture.Width;

            image_lock = new UIImage(Textures.TEXTURE_BLANK);
            image_lock.Width.Set(Textures.TEXTURE_LOCK_WIDTH, 0f);
            image_lock.Height.Set(Textures.TEXTURE_LOCK_HEIGHT, 0f);
            image_lock.Left.Set(button_size / 2 - Textures.TEXTURE_LOCK_WIDTH / 2, 0f);
            image_lock.Top.Set(button_size / 2 - Textures.TEXTURE_LOCK_HEIGHT / 2, 0f);
            Append(image_lock);

            SetVisibility(1f, LOW_VISIBILITY);
        }

        private void ClickPrimary(UIMouseEvent evt, UIElement listeningElement) {
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID == class_id) {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass((byte)Systems.Class.CLASS_IDS.None, true);
            }
            else {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass(class_id, true);
            }
        }

        private void ClickSecondary(UIMouseEvent evt, UIElement listeningElement) {
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID == class_id) {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass((byte)Systems.Class.CLASS_IDS.None, false);
            }
            else {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass(class_id, false);
            }
        }

        public override void MouseOver(UIMouseEvent evt) {
            UIInfo.Instance.ShowTextClass(this, class_id);
            base.MouseOver(evt);
        }

        public override void MouseOut(UIMouseEvent evt) {
            UIInfo.Instance.EndText(this);
            base.MouseOut(evt);
        }

        public void Update() {
            byte level = ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[class_id];
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Unlocked[class_id]) {
                //not locked
                image_lock.SetImage(Textures.TEXTURE_BLANK);

                //text level
                string str = "";
                if (level >= Systems.Class.MAX_LEVEL[Systems.Class.CLASS_LOOKUP[class_id].Tier]) {
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

                if ((ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID == class_id) || (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID == class_id)) {
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
                image_lock.SetImage(Textures.TEXTURE_LOCK);
                SetVisibility(1f, LOW_VISIBILITY);

                //no text
                text.SetText("");
            }
            this.MouseOut(null);
        }
    }

    //combines a UserInterface and a UIState
    abstract class UIStateCombo {
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

    }


    // Copied from ExampleMod on GitHub
    // Added locking of drag panel
    // added auto and close
    // added visible
    // switch drag to right click

    // This DragableUIPanel class inherits from UIPanel. 
    // Inheriting is a great tool for UI design. By inheriting, we get the background drawing for free from UIPanel
    // We've added some code to allow the panel to be dragged around. 
    // We've also added some code to ensure that the panel will bounce back into bounds if it is dragged outside or the screen resizes.
    // UIPanel does not prevent the player from using items when the mouse is clicked, so we've added that as well.
    class DragableUIPanel : UIPanel {
        // Stores the offset from the top left of the UIPanel while dragging.
        private Vector2 offset;

        private bool dragging = false;
        private bool stop_pin = false;
        private UIStateCombo UI;
        private bool buttons_hidden = false;

        public bool visible = true;

        private UIHoverImageButton button_pinned = null, button_auto = null, button_close = null;

        private bool auto = false;
        public bool Auto {
            get {
                return auto;
            }
            set {
                auto = value;
                if (button_auto != null) {
                    if (auto) {
                        button_auto.SetImage(Textures.TEXTURE_CORNER_BUTTON_AUTO);
                        button_auto.hoverText = "Don't Show Menu In Inventory Screen";
                    }
                    else {
                        button_auto.SetImage(Textures.TEXTURE_CORNER_BUTTON_NO_AUTO);
                        button_auto.hoverText = "Show Menu In Inventory Screen";
                    }
                }
            }
        }

        private bool pinned = false;
        public bool Pinned {
            get {
                return pinned;
            }
            set {
                pinned = value;
                if (button_pinned != null) {
                    if (pinned) {
                        button_pinned.SetImage(Textures.TEXTURE_CORNER_BUTTON_PINNED);
                        button_pinned.hoverText = "Allow Dragging";
                    }
                    else {
                        stop_pin = true;
                        button_pinned.SetImage(Textures.TEXTURE_CORNER_BUTTON_UNPINNED);
                        button_pinned.hoverText = "Prevent Dragging";
                    }
                }
            }
        }

        public DragableUIPanel(float width, float height, Color color, UIStateCombo ui, bool enable_close, bool enable_auto, bool enable_pin) : base() {
            UI = ui;

            BackgroundColor = color;

            SetPadding(0);

            Left.Set(0f, 0f);
            Top.Set(0f, 0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);

            if (enable_close) {
                button_close = new UIHoverImageButton(Textures.TEXTURE_CORNER_BUTTON_CLOSE, "Close");
                button_close.Width.Set(Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Height.Set(Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.OnClick += new MouseEvent(ButtonClickClose);
                Append(button_close);
            }

            if (enable_auto) {
                button_auto = new UIHoverImageButton(Textures.TEXTURE_BLANK, "");
                button_auto.Width.Set(Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Height.Set(Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.OnClick += new MouseEvent(ButtonClickAuto);
                Append(button_auto);
                Auto = true; //defaults to true if enabled
            }

            if (enable_pin) {
                button_pinned = new UIHoverImageButton(Textures.TEXTURE_BLANK, "");
                button_pinned.Width.Set(Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.Height.Set(Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.OnClick += new MouseEvent(ButtonClickPin);
                Append(button_pinned);
                Pinned = pinned;
            }

            Recalculate();
        }

        public override void Recalculate() {
            float left = Width.Pixels - Constants.UI_PADDING;
            if (button_close != null) {
                button_close.Left.Set(left -= Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Top.Set(Constants.UI_PADDING, 0f);
            }
            if (button_auto != null) {
                button_auto.Left.Set(left -= Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Top.Set(Constants.UI_PADDING, 0f);
            }
            if (button_pinned != null) {
                button_pinned.Left.Set(left -= Textures.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.Top.Set(Constants.UI_PADDING, 0f);
            }

            base.Recalculate();
        }

        private void ButtonClickPin(UIMouseEvent evt, UIElement listeningElement) {
            if (!buttons_hidden) {
                Pinned = !Pinned;
            }
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
            DragStart(evt);
        }

        public override void RightMouseUp(UIMouseEvent evt) {
            base.RightMouseUp(evt);
            DragEnd(evt);
        }

        private void DragStart(UIMouseEvent evt) {
            if (!Pinned) {
                offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
                dragging = true;
                stop_pin = false;
            }
            else {
                dragging = false;
            }
        }

        private void DragEnd(UIMouseEvent evt) {
            if (stop_pin) {
                Pinned = false;
                stop_pin = false;
            }
            else if (!Pinned) {
                Vector2 end = evt.MousePosition;
                dragging = false;

                Left.Set(end.X - offset.X, 0f);
                Top.Set(end.Y - offset.Y, 0f);

                Recalculate();
            }
            else {
                dragging = false;
            }
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

            // Here we check if the DragableUIPanel is outside the Parent UIElement rectangle. 
            // (In our example, the parent would be ExampleUI, a UIState. This means that we are checking that the DragableUIPanel is outside the whole screen)
            // By doing this and some simple math, we can snap the panel back on screen if the user resizes his window or otherwise changes resolution.
            Parent.Recalculate();
            var parentSpace = Parent.GetDimensions().ToRectangle();
            if (!GetDimensions().ToRectangle().Intersects(parentSpace)) {
                Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
                Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
                // Recalculate forces the UI system to do the positioning math again.
                Recalculate();
            }
        }

        public void SetPosition(float left, float top) {
            Left.Set(left, 0f);
            Top.Set(top, 0f);
            Recalculate();
        }

        public void SetSize(float width, float height) {
            Width.Set(width, 0f);
            Height.Set(height, 0f);
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
            if (button_pinned != null) {
                button_pinned.SetImage(Textures.TEXTURE_BLANK);
            }
            if (button_auto != null) {
                button_auto.SetImage(Textures.TEXTURE_BLANK);
            }
            if (button_close != null) {
                button_close.SetImage(Textures.TEXTURE_BLANK);
            }
        }

        public void ShowButtons() {
            buttons_hidden = false;
            if (button_pinned != null) {
                Pinned = pinned;
            }
            if (button_auto != null) {
                Auto = auto;
            }
            if (button_close != null) {
                button_close.SetImage(Textures.TEXTURE_CORNER_BUTTON_CLOSE);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (visible)
                base.Draw(spriteBatch);
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

        public UIHoverImageButton(Texture2D texture, string hoverText) : base(texture) {
            this.hoverText = hoverText;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);
            if (IsMouseHovering) {
                Main.hoverItemName = hoverText;
            }
        }
    }
}