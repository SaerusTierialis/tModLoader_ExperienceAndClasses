﻿using Microsoft.Xna.Framework;
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

        public byte class_id { get; private set; }
        UIText text;
        UIImage image_lock;

        public ClassButton(Texture2D texture, byte class_id) : base(texture) {
            this.class_id = class_id;
            OnClick += new MouseEvent(ClickPrimary);
            OnRightClick += new MouseEvent(ClickSecondary);
            
            text = new UIText("", TEXT_SCALE);
            Append(text);

            image_lock = new UIImage(Shared.TEXTURE_BLANK);
            image_lock.Width.Set(Shared.TEXTURE_LOCK_WIDTH, 0f);
            image_lock.Height.Set(Shared.TEXTURE_LOCK_HEIGHT, 0f);
            image_lock.Left.Set(UIMain.CLASS_BUTTON_SIZE/2 - Shared.TEXTURE_LOCK_WIDTH/2, 0f);
            image_lock.Top.Set(UIMain.CLASS_BUTTON_SIZE/2 - Shared.TEXTURE_LOCK_HEIGHT/2, 0f);
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
                text.Left.Set(UIMain.CLASS_BUTTON_SIZE/2 - message_size / 2, 0f);
                text.Top.Set(UIMain.CLASS_BUTTON_SIZE + TEXT_OFFSET, 0F);
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
        }
    }


    // Copied from ExampleMod on GitHub
    // Added locking of drag panel

    // This DragableUIPanel class inherits from UIPanel. 
    // Inheriting is a great tool for UI design. By inheriting, we get the background drawing for free from UIPanel
    // We've added some code to allow the panel to be dragged around. 
    // We've also added some code to ensure that the panel will bounce back into bounds if it is dragged outside or the screen resizes.
    // UIPanel does not prevent the player from using items when the mouse is clicked, so we've added that as well.
    class DragableUIPanel : UIPanel {
        // Stores the offset from the top left of the UIPanel while dragging.
        private Vector2 offset;
        public bool dragging = false;
        public bool pinned = false;
        public bool stop_pin = false;

        public override void MouseDown(UIMouseEvent evt) {
            DragStart(evt);
        }

        public override void MouseUp(UIMouseEvent evt) {
            DragEnd(evt);
        }

        private void DragStart(UIMouseEvent evt) {
            if (!pinned) {
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
                pinned = false;
                stop_pin = false;
            }
            else if (!pinned) {
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