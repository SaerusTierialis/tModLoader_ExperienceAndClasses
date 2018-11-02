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
        private const float PADDING = 5f;
        public const float CLASS_BUTTON_SIZE = 36f;
        private const float CLASS_ROW_PADDING = 40f;
        private const float CLASS_COL_PADDING = 10f;

        private readonly Color COLOR_MAIN = new Color(73, 94, 171);
        private readonly Color COLOR_CLASS= new Color(73, 94, 200);

        private DragableUIPanel panel;
        private UIHoverImageButton button_pin, button_auto;

        private const float INDICATOR_WIDTH = CLASS_BUTTON_SIZE + (PADDING * 2);
        private const float INDICATOR_HEIGHT = CLASS_BUTTON_SIZE + CLASS_ROW_PADDING - (PADDING * 2);
        private const float INDICATOR_OFFSETS = -PADDING;
        private const byte INDICATOR_ALPHA = 50;
        private UIPanel indicate_primary, indicate_secondary;
        private ClassButton button_primary, button_secondary;

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

            UIHoverImageButton button_close = new UIHoverImageButton(Shared.TEXTURE_CORNER_BUTTON_CLOSE, "Close");
            button_close.Left.Set(WIDTH - PADDING - Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_close.Top.Set(PADDING, 0f);
            button_close.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_close.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_close.OnClick += new MouseEvent(ButtonClickClose);
            panel.Append(button_close);

            button_auto = new UIHoverImageButton(Shared.TEXTURE_BLANK, "Error");
            button_auto.Left.Set(WIDTH - PADDING - (Shared.TEXTURE_CORNER_BUTTON_SIZE * 2), 0f);
            button_auto.Top.Set(PADDING, 0f);
            button_auto.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_auto.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_auto.OnClick += new MouseEvent(ButtonClickAuto);
            panel.Append(button_auto);

            button_pin = new UIHoverImageButton(Shared.TEXTURE_BLANK, "Error");
            button_pin.Left.Set(WIDTH - PADDING - (Shared.TEXTURE_CORNER_BUTTON_SIZE * 3), 0f);
            button_pin.Top.Set(PADDING, 0f);
            button_pin.Width.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_pin.Height.Set(Shared.TEXTURE_CORNER_BUTTON_SIZE, 0f);
            button_pin.OnClick += new MouseEvent(ButtonClickLock);
            panel.Append(button_pin);

            SetPinned(panel.pinned);
            SetAuto(Auto);

            UIPanel panel_class = new UIPanel();
            panel_class.SetPadding(0);
            panel_class.Left.Set(PADDING, 0f);
            panel_class.Top.Set(PADDING, 0f);
            panel_class.Width.Set((PADDING * 4) + ((CLASS_BUTTON_SIZE + CLASS_COL_PADDING) * Systems.Classes.class_locations.GetLength(1)) - CLASS_COL_PADDING, 0f);
            panel_class.Height.Set(HEIGHT - (PADDING*2), 0f);
            panel_class.BackgroundColor = COLOR_CLASS;
            panel.Append(panel_class);

            Color color = Shared.COLOUR_CLASS_PRIMARY;
            color.A = INDICATOR_ALPHA;
            indicate_primary = new UIPanel();
            indicate_primary.SetPadding(0);
            indicate_primary.Left.Set(0f, 0f);
            indicate_primary.Top.Set(0f, 0f);
            indicate_primary.Width.Set(INDICATOR_WIDTH, 0f);
            indicate_primary.Height.Set(INDICATOR_HEIGHT, 0f);
            indicate_primary.BackgroundColor = color;
            indicate_primary.OnClick += new MouseEvent(PrimaryButtonLeft);
            indicate_primary.OnRightClick += new MouseEvent(PrimaryButtonRight);
            indicate_primary.OnMouseOver += new MouseEvent(PrimaryButtonHover);
            panel_class.Append(indicate_primary);

            color = Shared.COLOUR_CLASS_SECONDARY;
            color.A = INDICATOR_ALPHA;
            indicate_secondary = new UIPanel();
            indicate_secondary.SetPadding(0);
            indicate_secondary.Left.Set(0f, 0f);
            indicate_secondary.Top.Set(0f, 0f);
            indicate_secondary.Width.Set(INDICATOR_WIDTH, 0f);
            indicate_secondary.Height.Set(INDICATOR_HEIGHT, 0f);
            indicate_secondary.BackgroundColor = color;
            indicate_secondary.OnClick += new MouseEvent(SecondaryButtonLeft);
            indicate_secondary.OnRightClick += new MouseEvent(SecondaryButtonRight);
            indicate_secondary.OnMouseOver += new MouseEvent(SecondaryButtonHover);
            panel_class.Append(indicate_secondary);

            class_buttons = new List<ClassButton>();
            ClassButton button;
            byte id;
            for (byte row = 0; row<Systems.Classes.class_locations.GetLength(0); row++) {
                for (byte col = 0; col<Systems.Classes.class_locations.GetLength(1); col++) {
                    id = Systems.Classes.class_locations[row, col];

                    if (id != (byte)Systems.Classes.ID.New) {
                        button = new ClassButton(Systems.Classes.CLASS_LOOKUP[id].Texture, id);
                        button.Left.Set((PADDING*2) + (col * (CLASS_BUTTON_SIZE + CLASS_COL_PADDING)), 0f);
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
                button_pin.SetImage(Shared.TEXTURE_CORNER_BUTTON_PINNED);
                button_pin.hoverText = "Allow Dragging";
            }
            else {
                panel.stop_pin = true;
                button_pin.SetImage(Shared.TEXTURE_CORNER_BUTTON_UNPINNED);
                button_pin.hoverText = "Prevent Dragging";
            }
        }

        public void SetAuto(bool new_state) {
            Auto = new_state;
            if (Auto) {
                button_auto.SetImage(Shared.TEXTURE_CORNER_BUTTON_AUTO);
                button_auto.hoverText = "Don't Show Menu In Inventory Screen";
            }
            else {
                button_auto.SetImage(Shared.TEXTURE_CORNER_BUTTON_NO_AUTO);
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

        public void UpdateClassInfo() {
            //class buttons
            indicate_primary.Left.Set(-10000f, 0f);
            indicate_secondary.Left.Set(-10000f, 0f);
            foreach (ClassButton button in class_buttons) {
                if (button.class_id == ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID) {
                    indicate_primary.Left.Set(button.Left.Pixels + INDICATOR_OFFSETS, 0f);
                    indicate_primary.Top.Set(button.Top.Pixels + INDICATOR_OFFSETS, 0f);
                    button_primary = button;
                }
                else if (button.class_id == ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID) {
                    indicate_secondary.Left.Set(button.Left.Pixels + INDICATOR_OFFSETS, 0f);
                    indicate_secondary.Top.Set(button.Top.Pixels + INDICATOR_OFFSETS, 0f);
                    button_secondary = button;
                }
                button.Update();
            }
            indicate_primary.Recalculate();
            indicate_secondary.Recalculate();
        }

        private void PrimaryButtonLeft(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.Click(evt);
        }
        private void PrimaryButtonRight(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.RightClick(evt);
        }
        private void PrimaryButtonHover(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.MouseOver(evt);
        }
        private void SecondaryButtonLeft(UIMouseEvent evt, UIElement listeningElement) {
            button_secondary.Click(evt);
        }
        private void SecondaryButtonRight(UIMouseEvent evt, UIElement listeningElement) {
            button_secondary.RightClick(evt);
        }
        private void SecondaryButtonHover(UIMouseEvent evt, UIElement listeningElement) {
            button_secondary.MouseOver(evt);
        }
    }
}
