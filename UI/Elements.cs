﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    class AttributeText : UIPanel {

        private const float RIGHT_SIDE_PADDING = 5f;
        private const float LEFT_SUM = 60f;

        private Systems.Attribute attribute;
        private UIText title, sum, final;
        private float scale, left_final;

        public AttributeText(float width, float height, float scale, Systems.Attribute attribute) {
            this.attribute = attribute;
            this.scale = scale;

            left_final = width - (Shared.TEXTURE_BUTTON_SIZE*2) - RIGHT_SIDE_PADDING - Shared.UI_PADDING;

            SetPadding(0f);
            Left.Set(0f, 0f);
            Top.Set(0f, 0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            BackgroundColor = Shared.COLOR_UI_PANEL_HIGHLIGHT;

            float top = ((height - (Main.fontMouseText.MeasureString("A").Y * scale)) / 2f) + Shared.UI_PADDING;
            title = new UIText(attribute.Name_Short.ToUpper(), scale);
            title.Left.Set(Shared.UI_PADDING, 0f);
            title.Top.Set(top, 0f);
            Append(title);

            float scale_sum = scale * 0.85f;
            float top_sum = ((height - (Main.fontMouseText.MeasureString("A").Y * scale_sum)) / 2f) + Shared.UI_PADDING;
            sum = new UIText("", scale_sum);
            sum.Left.Set(LEFT_SUM, 0f);
            sum.Top.Set(top_sum, 0f);
            Append(sum);

            final = new UIText("", scale);
            final.Left.Set(left_final, 0f);
            final.Top.Set(top, 0f);
            Append(final);

            UIImageButton button_add = new UIImageButton(Shared.TEXTURE_BUTTON_PLUS);
            button_add.Width.Set(Shared.TEXTURE_BUTTON_SIZE, 0f);
            button_add.Height.Set(Shared.TEXTURE_BUTTON_SIZE, 0f);
            button_add.Left.Set(width - (button_add.Width.Pixels * 2f) - RIGHT_SIDE_PADDING, 0f);
            button_add.Top.Set((height - button_add.Height.Pixels) / 2f, 0f);
            Append(button_add);

            UIImageButton button_subtract = new UIImageButton(Shared.TEXTURE_BUTTON_MINUS);
            button_subtract.Width.Set(height, 0f);
            button_subtract.Height.Set(height, 0f);
            button_subtract.Left.Set(width - button_add.Width.Pixels - RIGHT_SIDE_PADDING, 0f);
            button_subtract.Top.Set((height - button_add.Height.Pixels) / 2f, 0f);
            Append(button_subtract);

            Update();
        }

        public override void MouseOver(UIMouseEvent evt) {
            UIInfo.Instance.ShowTextAttribute(this, attribute);
        }

        public override void MouseOut(UIMouseEvent evt) {
            UIInfo.Instance.EndText(this);
        }

        public void Update() {
            sum.SetText("(" + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Base[attribute.ID] + " + " + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[attribute.ID] + ")");

            string str = "" + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Final[attribute.ID];
            final.SetText(str);
            final.Left.Set(left_final - (Main.fontMouseText.MeasureString(str).X * scale), 0f);
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

            image_lock = new UIImage(Shared.TEXTURE_BLANK);
            image_lock.Width.Set(Shared.TEXTURE_LOCK_WIDTH, 0f);
            image_lock.Height.Set(Shared.TEXTURE_LOCK_HEIGHT, 0f);
            image_lock.Left.Set(button_size / 2 - Shared.TEXTURE_LOCK_WIDTH / 2, 0f);
            image_lock.Top.Set(button_size / 2 - Shared.TEXTURE_LOCK_HEIGHT / 2, 0f);
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
            if (level > 0) {
                //not locked
                image_lock.SetImage(Shared.TEXTURE_BLANK);

                //text level
                string str = "";
                if (level >= Shared.MAX_LEVEL) {
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
                image_lock.SetImage(Shared.TEXTURE_LOCK);
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
                        button_auto.SetImage(Shared.TEXTURE_CORNER_BUTTON_AUTO);
                        button_auto.hoverText = "Don't Show Menu In Inventory Screen";
                    }
                    else {
                        button_auto.SetImage(Shared.TEXTURE_CORNER_BUTTON_NO_AUTO);
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
                        button_pinned.SetImage(Shared.TEXTURE_CORNER_BUTTON_PINNED);
                        button_pinned.hoverText = "Allow Dragging";
                    }
                    else {
                        stop_pin = true;
                        button_pinned.SetImage(Shared.TEXTURE_CORNER_BUTTON_UNPINNED);
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
                button_close = new UIHoverImageButton(Shared.TEXTURE_CORNER_BUTTON_CLOSE, "Close");
                button_close.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.OnClick += new MouseEvent(ButtonClickClose);
                Append(button_close);
            }

            if (enable_auto) {
                button_auto = new UIHoverImageButton(Shared.TEXTURE_BLANK, "");
                button_auto.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.OnClick += new MouseEvent(ButtonClickAuto);
                Append(button_auto);
                Auto = true; //defaults to true if enabled
            }

            if (enable_pin) {
                button_pinned = new UIHoverImageButton(Shared.TEXTURE_BLANK, "");
                button_pinned.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.OnClick += new MouseEvent(ButtonClickPin);
                Append(button_pinned);
                Pinned = pinned;
            }

            Recalculate();
        }

        public override void Recalculate() {
            float left = Width.Pixels - Shared.UI_PADDING;
            if (button_close != null) {
                button_close.Left.Set(left -= Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Top.Set(Shared.UI_PADDING, 0f);
            }
            if (button_auto != null) {
                button_auto.Left.Set(left -= Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Top.Set(Shared.UI_PADDING, 0f);
            }
            if (button_pinned != null) {
                button_pinned.Left.Set(left -= Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.Top.Set(Shared.UI_PADDING, 0f);
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

        public override void MouseDown(UIMouseEvent evt) {
            DragStart(evt);
        }

        public override void MouseUp(UIMouseEvent evt) {
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
                button_pinned.SetImage(Shared.TEXTURE_BLANK);
            }
            if (button_auto != null) {
                button_auto.SetImage(Shared.TEXTURE_BLANK);
            }
            if (button_close != null) {
                button_close.SetImage(Shared.TEXTURE_BLANK);
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
                button_close.SetImage(Shared.TEXTURE_CORNER_BUTTON_CLOSE);
            }
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