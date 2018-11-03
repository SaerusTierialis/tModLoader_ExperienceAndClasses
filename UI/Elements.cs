using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    class ClassButton : UIImageButton {
        private const float TEXT_SCALE = 0.7f;
        private const float TEXT_OFFSET = 5f;
        private const float VISIBILITY_SELECTED = 1f;
        private const float VISIBILITY_NOT_SELECTED = 0.4f;
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

            SetVisibility(1f, VISIBILITY_NOT_SELECTED);
        }

        private void ClickPrimary(UIMouseEvent evt, UIElement listeningElement) {
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID == class_id) {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass((byte)Systems.Classes.ID.None, true);
            }
            else {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass(class_id, true);
            }
        }

        private void ClickSecondary(UIMouseEvent evt, UIElement listeningElement) {
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID == class_id) {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass((byte)Systems.Classes.ID.None, false);
            }
            else {
                ExperienceAndClasses.LOCAL_MPLAYER.LocalSetClass(class_id, false);
            }
        }

        public void Update() {
            byte level = ExperienceAndClasses.LOCAL_MPLAYER.class_levels[class_id];
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
                    SetVisibility(1f, VISIBILITY_SELECTED);
                }
                else {
                    //not selected
                    SetVisibility(1f, VISIBILITY_NOT_SELECTED);
                }
            }
            else {
                //locked
                image_lock.SetImage(Shared.TEXTURE_LOCK);
                SetVisibility(1f, VISIBILITY_NOT_SELECTED);

                //no text
                text.SetText("");
            }
            this.MouseOut(null);
        }
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

        public bool dragging = false;

        private bool stop_pin = false;
        public bool Auto { get; private set; }
        public bool Pinned { get; private set; }

        private UIHoverImageButton button_pinned, button_auto, button_close;

        public DragableUIPanel(float width, float height, Color color, MouseEvent event_close, bool enable_auto, bool enable_pin) : base() {
            Auto = true;
            Pinned = false;

            button_pinned = null;
            button_auto = null;
            button_close = null;

            BackgroundColor = color;

            SetPadding(0);

            Left.Set(0f, 0f);
            Top.Set(0f, 0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);

            if (event_close != null) {
                button_close = new UIHoverImageButton(Shared.TEXTURE_CORNER_BUTTON_CLOSE, "Close");
                button_close.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_close.OnClick += event_close;
                Append(button_close);
            }

            if (enable_auto) {
                button_auto = new UIHoverImageButton(Shared.TEXTURE_BLANK, "");
                button_auto.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_auto.OnClick += new MouseEvent(ButtonClickAuto);
                Append(button_auto);
                SetAuto(Auto);
            }

            if (enable_pin) {
                button_pinned = new UIHoverImageButton(Shared.TEXTURE_BLANK, "");
                button_pinned.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
                button_pinned.OnClick += new MouseEvent(ButtonClickPin);
                Append(button_pinned);
                SetPinned(Pinned);
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
            SetPinned(!Pinned);
        }

        private void ButtonClickAuto(UIMouseEvent evt, UIElement listeningElement) {
            SetAuto(!Auto);
        }

        public void SetPinned(bool new_state) {
            Pinned = new_state;
            if (button_pinned != null) {
                if (Pinned) {
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

        public void SetAuto(bool new_state) {
            Auto = new_state;
            if (button_auto != null) {
                if (Auto) {
                    button_auto.SetImage(Shared.TEXTURE_CORNER_BUTTON_AUTO);
                    button_auto.hoverText = "Don't Show Menu In Inventory Screen";
                }
                else {
                    button_auto.SetImage(Shared.TEXTURE_CORNER_BUTTON_NO_AUTO);
                    button_auto.hoverText = "Show Menu In Inventory Screen";
                }
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
            var parentSpace = Parent.GetDimensions().ToRectangle();
            if (!GetDimensions().ToRectangle().Intersects(parentSpace)) {
                Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
                Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
                // Recalculate forces the UI system to do the positioning math again.
                Recalculate();
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