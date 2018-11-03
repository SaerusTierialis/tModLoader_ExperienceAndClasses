using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System.Collections.Generic;

namespace ExperienceAndClasses.UI {

    //UI for class selection, attributes, and ability info

    class UIMain : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIMain Instance = new UIMain();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float WIDTH = 600f;
        private const float HEIGHT = 400f;
        private const float CLASS_BUTTON_SIZE = 36f;
        private const float CLASS_ROW_PADDING = 40f;
        private const float CLASS_COL_PADDING = 10f;

        private readonly Color COLOR_CLASS_PANEL = new Color(73, 94, 200);

        private const float INDICATOR_WIDTH = CLASS_BUTTON_SIZE + (Shared.UI_PADDING * 2);
        private const float INDICATOR_HEIGHT = CLASS_BUTTON_SIZE + CLASS_ROW_PADDING - (Shared.UI_PADDING * 2);
        private const float INDICATOR_OFFSETS = -Shared.UI_PADDING;
        private const byte INDICATOR_ALPHA = 50;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }

        private UIPanel indicate_primary, indicate_secondary;
        private ClassButton button_primary, button_secondary;

        private List<ClassButton> class_buttons;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel = new DragableUIPanel(WIDTH, HEIGHT, Shared.COLOR_UI_MAIN, this, true, true, true);

            UIPanel panel_class = new UIPanel();
            panel_class.SetPadding(0);
            panel_class.Left.Set(Shared.UI_PADDING, 0f);
            panel_class.Top.Set(Shared.UI_PADDING, 0f);
            panel_class.Width.Set((Shared.UI_PADDING * 4) + ((CLASS_BUTTON_SIZE + CLASS_COL_PADDING) * Systems.Classes.class_locations.GetLength(1)) - CLASS_COL_PADDING, 0f);
            panel_class.Height.Set(HEIGHT - (Shared.UI_PADDING * 2), 0f);
            panel_class.BackgroundColor = COLOR_CLASS_PANEL;
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
            indicate_primary.OnClick += new UIElement.MouseEvent(PrimaryButtonLeft);
            indicate_primary.OnRightClick += new UIElement.MouseEvent(PrimaryButtonRight);
            indicate_primary.OnMouseOver += new UIElement.MouseEvent(PrimaryButtonHover);
            indicate_primary.OnMouseOut += new UIElement.MouseEvent(PrimaryButtonDeHover);
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
            indicate_secondary.OnClick += new UIElement.MouseEvent(SecondaryButtonLeft);
            indicate_secondary.OnRightClick += new UIElement.MouseEvent(SecondaryButtonRight);
            indicate_secondary.OnMouseOver += new UIElement.MouseEvent(SecondaryButtonHover);
            indicate_secondary.OnMouseOut += new UIElement.MouseEvent(SecondaryButtonDeHover);
            panel_class.Append(indicate_secondary);

            class_buttons = new List<ClassButton>();
            ClassButton button;
            byte id;
            for (byte row = 0; row<Systems.Classes.class_locations.GetLength(0); row++) {
                for (byte col = 0; col<Systems.Classes.class_locations.GetLength(1); col++) {
                    id = Systems.Classes.class_locations[row, col];

                    if (id != (byte)Systems.Classes.ID.New) {
                        button = new ClassButton(Systems.Classes.CLASS_LOOKUP[id].Texture, id);
                        button.Left.Set((Shared.UI_PADDING*2) + (col * (CLASS_BUTTON_SIZE + CLASS_COL_PADDING)), 0f);
                        button.Top.Set((Shared.UI_PADDING*2) + (row * (CLASS_BUTTON_SIZE + CLASS_ROW_PADDING)), 0f);
                        button.Width.Set(CLASS_BUTTON_SIZE, 0f);
                        button.Height.Set(CLASS_BUTTON_SIZE, 0f);
                        panel_class.Append(button);
                        class_buttons.Add(button);
                    }
                }
            }

            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
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

        //the panel behind the selected buttons prevents their use without this workaround
        private void PrimaryButtonLeft(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.Click(evt);
        }
        private void PrimaryButtonRight(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.RightClick(evt);
        }
        private void PrimaryButtonHover(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.MouseOver(evt);
        }
        private void PrimaryButtonDeHover(UIMouseEvent evt, UIElement listeningElement) {
            button_primary.MouseOut(evt);
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
        private void SecondaryButtonDeHover(UIMouseEvent evt, UIElement listeningElement) {
            button_secondary.MouseOut(evt);
        }
    }
}
