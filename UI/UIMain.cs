using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;
using Terraria.ID;
using System.Linq;
using Terraria.Localization;

namespace ExperienceAndClasses.UI {
    class UIMain : UIState {
        private const float WIDTH = 800f;
        private const float HEIGHT = 600f;
        private const float BUTTON_SIZE = 22f;
        private const float BUTTON_OFFSET = 5f;

        private DragableUIPanel panel;
        private UIHoverImageButton button_pin, button_auto;

        public bool visible = false;
        private bool auto = true;

        public override void OnInitialize() {
            panel = new DragableUIPanel();
            panel.SetPadding(0);

            //panel.Left.Set(100f, 0f);
            //panel.Top.Set(100f, 0f);
            panel.Width.Set(WIDTH, 0f);
            panel.Height.Set(HEIGHT, 0f);
            panel.BackgroundColor = new Color(73, 94, 171);

            Texture2D buttonPlayClose = ModLoader.GetTexture("ExperienceAndClasses/UI/ButtonClose");
            UIHoverImageButton button_close = new UIHoverImageButton(buttonPlayClose, "Close");
            button_close.Left.Set(WIDTH - BUTTON_OFFSET - BUTTON_SIZE, 0f);
            button_close.Top.Set(BUTTON_OFFSET, 0f);
            button_close.Width.Set(BUTTON_SIZE, 0f);
            button_close.Height.Set(BUTTON_SIZE, 0f);
            button_close.OnClick += new MouseEvent(ButtonClickClose);
            panel.Append(button_close);

            button_auto = new UIHoverImageButton(buttonPlayClose, "Error");
            button_auto.Left.Set(WIDTH - BUTTON_OFFSET - (BUTTON_SIZE * 2), 0f);
            button_auto.Top.Set(BUTTON_OFFSET, 0f);
            button_auto.Width.Set(BUTTON_SIZE, 0f);
            button_auto.Height.Set(BUTTON_SIZE, 0f);
            button_auto.OnClick += new MouseEvent(ButtonClickAuto);
            panel.Append(button_auto);

            button_pin = new UIHoverImageButton(buttonPlayClose, "Error");
            button_pin.Left.Set(WIDTH - BUTTON_OFFSET - (BUTTON_SIZE * 3), 0f);
            button_pin.Top.Set(BUTTON_OFFSET, 0f);
            button_pin.Width.Set(BUTTON_SIZE, 0f);
            button_pin.Height.Set(BUTTON_SIZE, 0f);
            button_pin.OnClick += new MouseEvent(ButtonClickLock);
            panel.Append(button_pin);

            SetPinned(panel.pinned);
            SetAuto(auto);

            base.Append(panel);
        }

        private void ButtonClickLock(UIMouseEvent evt, UIElement listeningElement) {
            SetPinned(!panel.pinned);
        }

        private void ButtonClickClose(UIMouseEvent evt, UIElement listeningElement) {
            visible = !visible;
        }

        private void ButtonClickAuto(UIMouseEvent evt, UIElement listeningElement) {
            SetAuto(!auto);
        }

        public void SetPinned(bool new_state) {
            panel.pinned = new_state;
            if (panel.pinned) {
                button_pin.SetImage(ModLoader.GetTexture("ExperienceAndClasses/UI/ButtonPinned"));
                button_pin.hoverText = "Allow Dragging";
            }
            else {
                panel.stop_pin = true;
                button_pin.SetImage(ModLoader.GetTexture("ExperienceAndClasses/UI/ButtonUnpinned"));
                button_pin.hoverText = "Prevent Dragging";
            }
        }

        public void SetAuto(bool new_state) {
            auto = new_state;
            if (auto) {
                button_auto.SetImage(ModLoader.GetTexture("ExperienceAndClasses/UI/ButtonAuto"));
                button_auto.hoverText = "Don't Show Menu In Inventory Screen";
            }
            else {
                button_auto.SetImage(ModLoader.GetTexture("ExperienceAndClasses/UI/ButtonUnauto"));
                button_auto.hoverText = "Show Menu In Inventory Screen";
            }
        }

        public void SetPosition(float left, float top) {
            panel.Left.Set(left, 0f);
            panel.Top.Set(top, 0f);
            panel.Recalculate();
        }

        public float GetLeft() {
            return panel.Left.Pixels;
        }

        public float GetTop() {
            return panel.Top.Pixels;
        }

        public bool GetAuto() {
            return auto;
        }

        public bool GetPinned() {
            return panel.pinned;
        }
    }
}
