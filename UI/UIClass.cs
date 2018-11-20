using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System.Collections.Generic;
using Terraria;

namespace ExperienceAndClasses.UI {

    //UI for class selection, attributes, and ability info

    class UIClass : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIClass Instance = new UIClass();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float CLASS_BUTTON_SIZE = 36f;
        private const float CLASS_ROW_PADDING = 40f;
        private const float CLASS_COL_PADDING = 10f;
        private static readonly float CLASS_WIDTH = (Constants.UI_PADDING * 4) + ((CLASS_BUTTON_SIZE + CLASS_COL_PADDING) * Systems.Class.class_locations.GetLength(1)) - CLASS_COL_PADDING;

        private const float WIDTH_ATTRIBUTES = 230f;
        private const float HEIGHT_ATTRIBUTES = 250f;
        private const float HEIGHT_ATTRIBUTE = 30f;

        private const float WIDTH_ABILITY = WIDTH_ATTRIBUTES;
        private const float HEIGHT_ABILITY = (HEIGHT - HEIGHT_ATTRIBUTES) - (Constants.UI_PADDING * 2) + 1;

        private const float WIDTH_UNLOCK = 180f;
        private static readonly float HEIGHT_UNLOCK = HEIGHT - HEIGHT_HELP - Textures.TEXTURE_CORNER_BUTTON_SIZE + 1 - (Constants.UI_PADDING * 2) + 1;

        private const float WIDTH_HELP = WIDTH_UNLOCK;
        private const float HEIGHT_HELP = 160f;

        private static readonly float WIDTH = CLASS_WIDTH + WIDTH_ATTRIBUTES + WIDTH_UNLOCK + (Constants.UI_PADDING * 2) - 2;
        private const float HEIGHT = 430f;

        private readonly Color COLOR_SUBPANEL = new Color(73, 94, 200);

        private const float INDICATOR_WIDTH = CLASS_BUTTON_SIZE + (Constants.UI_PADDING * 2);
        private const float INDICATOR_HEIGHT = CLASS_BUTTON_SIZE + CLASS_ROW_PADDING - (Constants.UI_PADDING * 2f);
        private const float INDICATOR_OFFSETS = -Constants.UI_PADDING;
        private const byte INDICATOR_ALPHA = 50;

        private const float FONT_SCALE_TITLE = 1.5f;
        private const float FONT_SCALE_ATTRIBUTE = 1f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }

        private UIPanel indicate_primary, indicate_secondary;
        private ClassButton button_primary, button_secondary;

        private List<ClassButton> class_buttons;

        private List<AttributeText> attribute_texts;
        private UIText attribute_point_text;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            //main panel
            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOR_UI_PANEL_BACKGROUND, this, true, true, true);

            //class panel
            UIPanel panel_class = new UIPanel();
            panel_class.SetPadding(0);
            panel_class.Left.Set(Constants.UI_PADDING, 0f);
            panel_class.Top.Set(Constants.UI_PADDING, 0f);
            panel_class.Width.Set(CLASS_WIDTH, 0f);
            panel_class.Height.Set(HEIGHT - (Constants.UI_PADDING * 2), 0f);
            panel_class.BackgroundColor = COLOR_SUBPANEL;
            panel.Append(panel_class);

            //class title
            HelpTextPanel panel_class_title = new HelpTextPanel("Classes", FONT_SCALE_TITLE, true, "Classes", "TODO_help_text");
            panel_class_title.BackgroundColor = COLOR_SUBPANEL;
            panel_class_title.Width.Set(panel_class.Width.Pixels, 0f);
            panel_class.Append(panel_class_title);

            //indicator for primary class
            Color color = Constants.COLOUR_CLASS_PRIMARY;
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

            //indicator for secondary class
            color = Constants.COLOUR_CLASS_SECONDARY;
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

            //class selection buttons
            class_buttons = new List<ClassButton>();
            ClassButton button;
            byte id;
            for (byte row = 0; row < Systems.Class.class_locations.GetLength(0); row++) {
                for (byte col = 0; col < Systems.Class.class_locations.GetLength(1); col++) {
                    id = Systems.Class.class_locations[row, col];

                    if (id != (byte)Systems.Class.CLASS_IDS.New) {
                        button = new ClassButton(Systems.Class.CLASS_LOOKUP[id].Texture, id);
                        button.Left.Set((Constants.UI_PADDING * 2) + (col * (CLASS_BUTTON_SIZE + CLASS_COL_PADDING)), 0f);
                        button.Top.Set(panel_class_title.Height.Pixels + (Constants.UI_PADDING * 2) + (row * (CLASS_BUTTON_SIZE + CLASS_ROW_PADDING)), 0f);
                        button.Width.Set(CLASS_BUTTON_SIZE, 0f);
                        button.Height.Set(CLASS_BUTTON_SIZE, 0f);
                        panel_class.Append(button);
                        class_buttons.Add(button);
                    }
                }
            }

            //attribute panel
            UIPanel panel_attribute = new UIPanel();
            panel_attribute.SetPadding(0);
            panel_attribute.Left.Set(panel_class.Left.Pixels + panel_class.Width.Pixels - 1f, 0f);
            panel_attribute.Top.Set(HEIGHT - Constants.UI_PADDING - HEIGHT_ATTRIBUTES, 0f);
            panel_attribute.Width.Set(WIDTH_ATTRIBUTES, 0f);
            panel_attribute.Height.Set(HEIGHT_ATTRIBUTES, 0f);
            panel_attribute.BackgroundColor = COLOR_SUBPANEL;
            panel.Append(panel_attribute);

            //attribute title
            HelpTextPanel panel_attribute_title = new HelpTextPanel("Attributes", FONT_SCALE_TITLE, true, "Attribute Points", "TODO_help_text");
            panel_attribute_title.BackgroundColor = COLOR_SUBPANEL;
            panel_attribute_title.Width.Set(panel_attribute.Width.Pixels, 0f);
            panel_attribute.Append(panel_attribute_title);

            //attributes
            float top = panel_attribute_title.Height.Pixels + Constants.UI_PADDING;
            AttributeText attribute_text;
            attribute_texts = new List<AttributeText>();
            foreach (Systems.Attribute.ATTRIBUTE_IDS attribute_id in Systems.Attribute.ATTRIBUTES_UI_ORDER) {
                attribute_text = new AttributeText(WIDTH_ATTRIBUTES - (Constants.UI_PADDING * 2), HEIGHT_ATTRIBUTE, FONT_SCALE_ATTRIBUTE, Systems.Attribute.ATTRIBUTE_LOOKUP[(byte)attribute_id]);
                attribute_text.Left.Set(Constants.UI_PADDING, 0f);
                attribute_text.Top.Set(top, 0f);
                top += HEIGHT_ATTRIBUTE;

                panel_attribute.Append(attribute_text);
                attribute_texts.Add(attribute_text);
            }

            //attribute points
            attribute_point_text = new UIText("Available Points: 0", FONT_SCALE_ATTRIBUTE);
            attribute_point_text.Left.Set(Constants.UI_PADDING, 0f);
            attribute_point_text.Top.Set(top + Constants.UI_PADDING, 0f);
            panel_attribute.Append(attribute_point_text);

            //ability panel
            UIPanel panel_ability = new UIPanel();
            panel_ability.SetPadding(0);
            panel_ability.Left.Set(panel_class.Left.Pixels + panel_class.Width.Pixels - 1f, 0f);
            panel_ability.Top.Set(Constants.UI_PADDING, 0f);
            panel_ability.Width.Set(WIDTH_ABILITY, 0f);
            panel_ability.Height.Set(HEIGHT_ABILITY, 0f);
            panel_ability.BackgroundColor = COLOR_SUBPANEL;
            panel.Append(panel_ability);

            //ability title
            HelpTextPanel panel_ability_title = new HelpTextPanel("Abilities", FONT_SCALE_TITLE, true, "Class Abilities", "TODO_help_text");
            panel_ability_title.BackgroundColor = COLOR_SUBPANEL;
            panel_ability_title.Width.Set(panel_attribute.Width.Pixels, 0f);
            panel_ability.Append(panel_ability_title);

            //unlock panel
            UIPanel panel_unlock = new UIPanel();
            panel_unlock.SetPadding(0);
            panel_unlock.Left.Set(panel_ability.Left.Pixels + panel_ability.Width.Pixels - 1f, 0f);
            panel_unlock.Top.Set(Constants.UI_PADDING + Textures.TEXTURE_CORNER_BUTTON_SIZE + 1, 0f);
            panel_unlock.Width.Set(WIDTH_UNLOCK, 0f);
            panel_unlock.Height.Set(HEIGHT_UNLOCK, 0f);
            panel_unlock.BackgroundColor = COLOR_SUBPANEL;
            panel.Append(panel_unlock);

            //unlock title
            HelpTextPanel panel_unlock_title = new HelpTextPanel("Passives", FONT_SCALE_TITLE, true, "Passive Abilities", "TODO_help_text");
            panel_unlock_title.BackgroundColor = COLOR_SUBPANEL;
            panel_unlock_title.Width.Set(panel_unlock.Width.Pixels, 0f);
            panel_unlock.Append(panel_unlock_title);

            //help panel
            UIPanel panel_help = new UIPanel();
            panel_help.SetPadding(0);
            panel_help.Left.Set(panel_unlock.Left.Pixels, 0f);
            panel_help.Top.Set(HEIGHT - Constants.UI_PADDING - HEIGHT_HELP, 0f);
            panel_help.Width.Set(WIDTH_HELP, 0f);
            panel_help.Height.Set(HEIGHT_HELP, 0f);
            panel_help.BackgroundColor = COLOR_SUBPANEL;
            panel.Append(panel_help);

            //help title
            HelpTextPanel panel_help_title = new HelpTextPanel("Help", FONT_SCALE_TITLE, true, "Help", "TODO_help_text");
            panel_help_title.BackgroundColor = COLOR_SUBPANEL;
            panel_help_title.Width.Set(panel_help.Width.Pixels, 0f);
            panel_help.Append(panel_help_title);

            //done adding to main panel
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

            //attribute
            foreach (AttributeText button in attribute_texts) {
                button.Update();
            }

            //attribute points
            UpdateAttributePoints();
        }

        private void UpdateAttributePoints() {
            attribute_point_text.SetText("Available Points: " + ExperienceAndClasses.LOCAL_MPLAYER.Attribute_Points_Unallocated);
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
