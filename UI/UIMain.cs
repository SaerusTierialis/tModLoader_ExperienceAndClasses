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
using System.Collections.Generic;

namespace ExperienceAndClasses.UI {
    class UIMain : UIState {
        private const float WIDTH = 600f;
        private const float HEIGHT = 400f;
        private const float CORNER_BUTTON_SIZE = 22f;
        private const float PADDING = 5f;
        public const float CLASS_BUTTON_SIZE = 36f;
        private const float CLASS_ROW_PADDING = 40f;
        private const float CLASS_COL_PADDING = 10f;

        private readonly Color COLOR_MAIN = new Color(73, 94, 171);
        private readonly Color COLOR_CLASS= new Color(73, 94, 200);

        private DragableUIPanel panel;
        private UIHoverImageButton button_pin, button_auto;

        private List<ClassButton> class_buttons;

        public bool Visible { get; set; }
        public bool Auto { get; private set; }

        public override void OnInitialize() {
            Visible = false;
            Auto = true;

            panel = new DragableUIPanel();
            panel.SetPadding(0);

            //panel.Left.Set(100f, 0f);
            //panel.Top.Set(100f, 0f);
            panel.Width.Set(WIDTH, 0f);
            panel.Height.Set(HEIGHT, 0f);
            panel.BackgroundColor = COLOR_MAIN;

            Texture2D buttonPlayClose = ModLoader.GetTexture("ExperienceAndClasses/UI/ButtonClose");
            UIHoverImageButton button_close = new UIHoverImageButton(buttonPlayClose, "Close");
            button_close.Left.Set(WIDTH - PADDING - CORNER_BUTTON_SIZE, 0f);
            button_close.Top.Set(PADDING, 0f);
            button_close.Width.Set(CORNER_BUTTON_SIZE, 0f);
            button_close.Height.Set(CORNER_BUTTON_SIZE, 0f);
            button_close.OnClick += new MouseEvent(ButtonClickClose);
            panel.Append(button_close);

            button_auto = new UIHoverImageButton(buttonPlayClose, "Error");
            button_auto.Left.Set(WIDTH - PADDING - (CORNER_BUTTON_SIZE * 2), 0f);
            button_auto.Top.Set(PADDING, 0f);
            button_auto.Width.Set(CORNER_BUTTON_SIZE, 0f);
            button_auto.Height.Set(CORNER_BUTTON_SIZE, 0f);
            button_auto.OnClick += new MouseEvent(ButtonClickAuto);
            panel.Append(button_auto);

            button_pin = new UIHoverImageButton(buttonPlayClose, "Error");
            button_pin.Left.Set(WIDTH - PADDING - (CORNER_BUTTON_SIZE * 3), 0f);
            button_pin.Top.Set(PADDING, 0f);
            button_pin.Width.Set(CORNER_BUTTON_SIZE, 0f);
            button_pin.Height.Set(CORNER_BUTTON_SIZE, 0f);
            button_pin.OnClick += new MouseEvent(ButtonClickLock);
            panel.Append(button_pin);

            SetPinned(panel.pinned);
            SetAuto(Auto);

            UIPanel panel_class = new UIPanel();
            panel_class.SetPadding(0);
            panel_class.Left.Set(PADDING, 0f);
            panel_class.Top.Set(PADDING, 0f);
            panel_class.Width.Set((PADDING * 2) + ((CLASS_BUTTON_SIZE+ CLASS_COL_PADDING) * Systems.Classes.class_locations.GetLength(1)), 0f);
            panel_class.Height.Set(HEIGHT - (PADDING*2), 0f);
            panel_class.BackgroundColor = COLOR_CLASS;
            panel.Append(panel_class);

            class_buttons = new List<ClassButton>();
            ClassButton button;
            Systems.Classes.ID id;

            for (byte row = 0; row<Systems.Classes.class_locations.GetLength(0); row++) {
                for (byte col = 0; col<Systems.Classes.class_locations.GetLength(1); col++) {
                    id = Systems.Classes.class_locations[row, col];

                    if (id != Systems.Classes.ID.New) {
                        button = new ClassButton(Systems.Classes.CLASS_LOOKUP[(byte)id].Texture, id);
                        button.Left.Set(PADDING + (col * (CLASS_BUTTON_SIZE + CLASS_COL_PADDING)), 0f);
                        button.Top.Set(PADDING + (row * (CLASS_BUTTON_SIZE + CLASS_ROW_PADDING)), 0f);
                        button.Width.Set(CLASS_BUTTON_SIZE, 0f);
                        button.Height.Set(CLASS_BUTTON_SIZE, 0f);
                        panel_class.Append(button);
                        class_buttons.Add(button);
                    }
                }
            }

            base.Append(panel);
        }

        private void ButtonClickLock(UIMouseEvent evt, UIElement listeningElement) {
            SetPinned(!panel.pinned);
        }

        private void ButtonClickClose(UIMouseEvent evt, UIElement listeningElement) {
            Visible = !Visible;
        }

        private void ButtonClickAuto(UIMouseEvent evt, UIElement listeningElement) {
            SetAuto(!Auto);
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
            Auto = new_state;
            if (Auto) {
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

        public bool GetPinned() {
            return panel.pinned;
        }
    }
}
