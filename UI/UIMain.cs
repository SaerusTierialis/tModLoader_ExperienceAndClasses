using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System.Collections.Generic;
using Terraria;
using System;

namespace ExperienceAndClasses.UI {

    //UI for class selection, attributes, and ability info

    public class UIMain : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIMain Instance = new UIMain();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float CLASS_BUTTON_SIZE = 36f;
        private const float CLASS_ROW_PADDING = 40f;
        private const float CLASS_COL_PADDING = 10f;
        private static readonly float CLASS_WIDTH = (Constants.UI_PADDING * 4) + ((CLASS_BUTTON_SIZE + CLASS_COL_PADDING) * Systems.PlayerClass.Class_Locations.GetLength(1)) - CLASS_COL_PADDING;

        private const float WIDTH_ATTRIBUTES = 264f;
        private const float HEIGHT_ATTRIBUTES = 250f;
        private const float HEIGHT_ATTRIBUTE = 30f;
        private const float WIDTH_ATTRIBUTES_RESET = 135f;

        private const float WIDTH_ABILITY = WIDTH_ATTRIBUTES;
        private const float HEIGHT_ABILITY = (HEIGHT - HEIGHT_ATTRIBUTES) - (Constants.UI_PADDING * 2) + 1;

        private const float WIDTH_HELP_AND_PASSIVES = 180f;
        private static readonly float HEIGHT_UNLOCK = HEIGHT - HEIGHT_HELP + 1 - (Constants.UI_PADDING * 2) + 1;

        private const float HEIGHT_HELP = 65f;

        private static readonly float WIDTH = CLASS_WIDTH + WIDTH_ATTRIBUTES + WIDTH_HELP_AND_PASSIVES + (Constants.UI_PADDING * 2) - 4;
        private const float HEIGHT = 430f;

        private const float WIDTH_CONFIRM = 200f;

        private const float INDICATOR_WIDTH = CLASS_BUTTON_SIZE + (Constants.UI_PADDING * 2);
        private const float INDICATOR_HEIGHT = CLASS_BUTTON_SIZE + CLASS_ROW_PADDING - (Constants.UI_PADDING * 2f);
        private const float INDICATOR_OFFSETS = -Constants.UI_PADDING;
        private const byte INDICATOR_ALPHA = 50;

        private const float FONT_SCALE_TITLE = 1.5f;
        private const float FONT_SCALE_HELP = 1.2f;
        private const float FONT_SCALE_ATTRIBUTE = 1f;
        private const float FONT_SCALE_ABILITY = 0.9f;

        public const float SPACING_ABILITY_ICON = 2f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }

        private DragableUIPanel indicate_primary, indicate_secondary;
        private ClassButton button_primary, button_secondary;

        private List<ClassButton> class_buttons;

        private List<AttributeText> attribute_texts;
        private HelpTextPanel attribute_point_text;

        private AbilityIcon[] ability_primary, ability_secondary;
        private HelpTextPanel level_primary, level_secondary;

        private ScrollPanel passives;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            //main panel
            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOUR_UI_PANEL_BACKGROUND, this, true, true);

            //class panel
            DragableUIPanel panel_class = new DragableUIPanel(CLASS_WIDTH, (HEIGHT - (Constants.UI_PADDING * 2)), Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_class.Left.Set(Constants.UI_PADDING, 0f);
            panel_class.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(panel_class);

            //class title
            panel_class.SetTitle("Classes", FONT_SCALE_TITLE, true, "Left click to select primary class\nRight click to select secondary class", "Classes");

            //indicator for primary class
            Color color = Constants.COLOUR_CLASS_PRIMARY;
            color.A = INDICATOR_ALPHA;
            indicate_primary = new DragableUIPanel(INDICATOR_WIDTH, INDICATOR_HEIGHT, color, this, false, false, false);
            indicate_primary.OnClick += new UIElement.MouseEvent(PrimaryButtonLeft);
            indicate_primary.OnRightClick += new UIElement.MouseEvent(PrimaryButtonRight);
            indicate_primary.OnMouseOver += new UIElement.MouseEvent(PrimaryButtonHover);
            indicate_primary.OnMouseOut += new UIElement.MouseEvent(PrimaryButtonDeHover);
            panel_class.Append(indicate_primary);

            //indicator for secondary class
            color = Constants.COLOUR_CLASS_SECONDARY;
            color.A = INDICATOR_ALPHA;
            indicate_secondary = new DragableUIPanel(INDICATOR_WIDTH, INDICATOR_HEIGHT, color, this, false, false, false);
            indicate_secondary.OnClick += new UIElement.MouseEvent(SecondaryButtonLeft);
            indicate_secondary.OnRightClick += new UIElement.MouseEvent(SecondaryButtonRight);
            indicate_secondary.OnMouseOver += new UIElement.MouseEvent(SecondaryButtonHover);
            indicate_secondary.OnMouseOut += new UIElement.MouseEvent(SecondaryButtonDeHover);
            panel_class.Append(indicate_secondary);

            //class selection buttons
            class_buttons = new List<ClassButton>();
            ClassButton button;
            byte id;
            for (byte row = 0; row < Systems.PlayerClass.Class_Locations.GetLength(0); row++) {
                for (byte col = 0; col < Systems.PlayerClass.Class_Locations.GetLength(1); col++) {
                    id = Systems.PlayerClass.Class_Locations[row, col];

                    if ((id != (byte)Systems.PlayerClass.IDs.New) && Systems.PlayerClass.LOOKUP[id].Enabled) {
                        button = new ClassButton(Systems.PlayerClass.LOOKUP[id]);
                        button.Left.Set((Constants.UI_PADDING * 2) + (col * (CLASS_BUTTON_SIZE + CLASS_COL_PADDING)), 0f);
                        button.Top.Set(panel_class.top_space + (Constants.UI_PADDING * 2) + (row * (CLASS_BUTTON_SIZE + CLASS_ROW_PADDING)), 0f);
                        button.Width.Set(CLASS_BUTTON_SIZE, 0f);
                        button.Height.Set(CLASS_BUTTON_SIZE, 0f);
                        panel_class.Append(button);
                        class_buttons.Add(button);
                    }
                }
            }

            //attribute panel
            DragableUIPanel panel_attribute = new DragableUIPanel(WIDTH_ATTRIBUTES, HEIGHT_ATTRIBUTES, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_attribute.Left.Set(panel_class.Left.Pixels + panel_class.Width.Pixels - 2f, 0f);
            panel_attribute.Top.Set(HEIGHT - Constants.UI_PADDING - HEIGHT_ATTRIBUTES, 0f);
            panel.Append(panel_attribute);

            //attribute title
            panel_attribute.SetTitle("Attributes", FONT_SCALE_TITLE, true, "Allocated + Class + Bonus = Final\n\nCost to increase is displayed on the right\n\nHold the Ability Alternate Effect key when clicking the add button to invest up to 10 points instead of 1", "Attribute Points");

            //attributes
            float top = panel_attribute.top_space + Constants.UI_PADDING;
            AttributeText attribute_text;
            attribute_texts = new List<AttributeText>();
            foreach (Systems.Attribute.IDs attribute_id in Systems.Attribute.ATTRIBUTES_UI_ORDER) {
                attribute_text = new AttributeText(WIDTH_ATTRIBUTES - (Constants.UI_PADDING * 2), HEIGHT_ATTRIBUTE, FONT_SCALE_ATTRIBUTE, Systems.Attribute.LOOKUP[(byte)attribute_id]);
                attribute_text.Left.Set(Constants.UI_PADDING, 0f);
                attribute_text.Top.Set(top, 0f);
                top += HEIGHT_ATTRIBUTE;

                panel_attribute.Append(attribute_text);
                attribute_texts.Add(attribute_text);
            }

            //attribute reset
            TextButton attribute_point_reset = new TextButton("   Reset", FONT_SCALE_ATTRIBUTE, FONT_SCALE_ATTRIBUTE + 0.1f);
            attribute_point_reset.Left.Set(Constants.UI_PADDING, 0f);
            attribute_point_reset.Top.Set(top + Constants.UI_PADDING, 0f);
            attribute_point_reset.Width.Set(WIDTH_ATTRIBUTES_RESET, 0f);
            attribute_point_reset.OnClick += new UIElement.MouseEvent(ClickReset);
            panel_attribute.Append(attribute_point_reset);

            //attribute points
            attribute_point_text = new HelpTextPanel("Points: 0", FONT_SCALE_ATTRIBUTE, false, "Allocation points are earned with every level and higher tier classes award more points. These are character-wide rather than tied to a specific class.", "Attribute Allocation");
            attribute_point_text.Left.Set(WIDTH_ATTRIBUTES_RESET + (Constants.UI_PADDING * 2f), 0f);
            attribute_point_text.Top.Set(top, 0f);
            attribute_point_text.Width.Set(panel_attribute.Width.Pixels - WIDTH_ATTRIBUTES_RESET - (Constants.UI_PADDING * 3f), 0f);
            attribute_point_text.BackgroundColor = Color.Transparent;
            attribute_point_text.BorderColor = Color.Transparent;
            panel_attribute.Append(attribute_point_text);

            //ability panel
            DragableUIPanel panel_ability = new DragableUIPanel(WIDTH_ABILITY, HEIGHT_ABILITY, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_ability.Left.Set(panel_attribute.Left.Pixels, 0f);
            panel_ability.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(panel_ability);

            //ability title
            panel_ability.SetTitle("Abilities", FONT_SCALE_TITLE, true, "To use the class abilities, you must first set the mod hotkeys in settings. After setting the hotkeys, the keys shown in the tooltips will not update until you next level up or toggle a class.", "Class Abilities");

            //ability panel primary info
            level_primary = new HelpTextPanel("DEFAULT", FONT_SCALE_ABILITY, false, "The level shown here is your effective level. The level of the secondary class is capped at half the level of the primary. If the secondary class is a higher tier, then it is capped at level 1.", "Effective Level", true, true);
            level_primary.Left.Set(Constants.UI_PADDING, 0f);
            level_primary.Top.Set(panel_ability.top_space + Constants.UI_PADDING, 0f);
            level_primary.Width.Set(panel_ability.Width.Pixels - (Constants.UI_PADDING * 2f), 0f);
            panel_ability.Append(level_primary);

            //ability panel primary skills
            ability_primary = new AbilityIcon[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS * 2];
            top = level_primary.Top.Pixels + level_primary.Height.Pixels;
            float left = Constants.UI_PADDING;
            for (byte i = 0; i < ability_primary.Length; i++) {
                ability_primary[i] = new AbilityIcon(1);
                ability_primary[i].Top.Set(top, 0f);
                ability_primary[i].Left.Set(left, 0f);
                ability_primary[i].Width.Set(AbilityIcon.SIZE, 0f);
                ability_primary[i].Height.Set(AbilityIcon.SIZE, 0f);
                panel_ability.Append(ability_primary[i]);
                left += ability_primary[i].Width.Pixels + SPACING_ABILITY_ICON;
            }

            //ability panel secondary info
            level_secondary = new HelpTextPanel("DEFAULT", FONT_SCALE_ABILITY, false, "The level shown here is your effective level. The level of the secondary class is capped at half the level of the primary. If the secondary class is a higher tier, then it is capped at level 1.", "Effective Level", true, true);
            level_secondary.Left.Set(Constants.UI_PADDING, 0f);
            level_secondary.Top.Set(ability_primary[0].Top.Pixels + ability_primary[0].Height.Pixels + (Constants.UI_PADDING * 2f), 0f);
            level_secondary.Width.Set(panel_ability.Width.Pixels - (Constants.UI_PADDING * 2f), 0f);
            panel_ability.Append(level_secondary);

            //ability panel secondary skills
            ability_secondary = new AbilityIcon[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS * 2];
            top = level_secondary.Top.Pixels + level_secondary.Height.Pixels;
            left = Constants.UI_PADDING;
            for (byte i = 0; i < ability_secondary.Length; i++) {
                ability_secondary[i] = new AbilityIcon();
                ability_secondary[i].Top.Set(top, 0f);
                ability_secondary[i].Left.Set(left, 0f);
                ability_secondary[i].Width.Set(AbilityIcon.SIZE, 0f);
                ability_secondary[i].Height.Set(AbilityIcon.SIZE, 0f);
                panel_ability.Append(ability_secondary[i]);
                left += ability_secondary[i].Width.Pixels + SPACING_ABILITY_ICON;
            }

            //unlock panel
            DragableUIPanel panel_passive = new DragableUIPanel(WIDTH_HELP_AND_PASSIVES, HEIGHT_UNLOCK, Color.Transparent, this, false, false, false);
            panel_passive.Left.Set(panel_ability.Left.Pixels + panel_ability.Width.Pixels - 2f, 0f);
            panel_passive.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(panel_passive);

            //passives title
            panel_passive.SetTitle("Passives", FONT_SCALE_TITLE, true, "These include ability upgrades, special resources, and more!", "Passive Abilities");

            //passives
            passives = new ScrollPanel(panel_passive.Width.Pixels, panel_passive.Height.Pixels - panel_passive.top_space, this.UI, true);
            passives.Top.Set(panel_passive.top_space, 0f);
            panel_passive.Append(passives);

            //help panel
            DragableUIPanel panel_help = new DragableUIPanel(WIDTH_HELP_AND_PASSIVES, HEIGHT_HELP, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_help.Left.Set(panel_passive.Left.Pixels, 0f);
            panel_help.Top.Set(HEIGHT - Constants.UI_PADDING - HEIGHT_HELP, 0f);
            panel_help.BackgroundColor = Color.Transparent;
            panel_help.BorderColor = Color.Transparent;
            panel.Append(panel_help);

            //help button
            TextButton help = new TextButton("Help", FONT_SCALE_HELP, FONT_SCALE_HELP + 0.1f);
            //help.OnClick += new UIElement.MouseEvent(ClickHelp);
            help.HAlign = 0.5f;
            help.Top.Set(Constants.UI_PADDING * 2, 0f);
            panel_help.Append(help);

            //done adding to main panel
            state.Append(panel);

            //initial panel position
            Systems.PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;
            panel.SetPosition(psheet.Misc.UIMain_Left, psheet.Misc.UIMain_Top, true);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void UpdatePSheet(Systems.PSheet psheet) {
            //class buttons
            indicate_primary.visible = false;
            indicate_secondary.visible = false;
            foreach (ClassButton button in class_buttons) {
                if (button.Class.ID_num == psheet.Classes.Primary.ID) {
                    indicate_primary.SetPosition(button.Left.Pixels + INDICATOR_OFFSETS, button.Top.Pixels + INDICATOR_OFFSETS);
                    button_primary = button;
                    indicate_primary.visible = true;
                }
                else if (button.Class.ID_num == psheet.Classes.Secondary.ID) {
                    indicate_secondary.SetPosition(button.Left.Pixels + INDICATOR_OFFSETS, button.Top.Pixels + INDICATOR_OFFSETS);
                    button_secondary = button;
                    indicate_secondary.visible = true;
                }
                button.Update();
            }

            //attribute
            foreach (AttributeText button in attribute_texts) {
                button.Update();
            }

            //attribute points
            UpdateAttributePoints();

            //primary effective level
            Systems.PlayerClass c = psheet.Classes.Primary.Class;
            if (c != null && c.Tier > 0) {
                level_primary.SetText(c.Name + " Lv" + psheet.Classes.Primary.Level_Effective);
            }
            else {
                level_primary.SetText("No Primary Class");
            }

            //primary ability
            foreach (AbilityIcon icon in ability_primary) {
                icon.active = false;
            }
            byte counter = 0;
            Systems.Ability ability;
            for (byte i = 0; i < Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS; i++) {
                ability = psheet.Abilities_Primary[i];
                if (ability != null) {
                    ability_primary[counter++].SetAbility(ability);
                }
                ability = psheet.Abilities_Primary_Alt[i];
                if (ability != null) {
                    ability_primary[counter++].SetAbility(ability);
                }
            }

            //secondary effective level
            c = psheet.Classes.Secondary.Class;
            if (c != null && c.Tier > 0) {
                level_secondary.SetText(c.Name + " Lv" + psheet.Classes.Secondary.Level_Effective);
            }
            else {
                level_secondary.SetText("No Secondary Class");
            }

            //secondary ability
            foreach (AbilityIcon icon in ability_secondary) {
                icon.active = false;
            }
            counter = 0;
            for (byte i = 0; i < Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS; i++) {
                ability = psheet.Abilities_Secondary[i];
                if (ability != null) {
                    ability_secondary[counter++].SetAbility(ability);
                }
                ability = psheet.Abilities_Secondary_Alt[i];
                if (ability != null) {
                    ability_secondary[counter++].SetAbility(ability);
                }
            }

            //passives
            List<UIElement> passive_icons = new List<UIElement>();
            foreach (Systems.Passive passive in psheet.Passives) {
                passive_icons.Add(new PassiveIcon(passive));
            }
            passives.SetItems(passive_icons);
        }

        private void UpdateAttributePoints() {
            attribute_point_text.SetText("Points: " + String.Format("{0,5}", Shortcuts.LOCAL_PLAYER.PSheet.Attributes.Points_Available));
        }

        //the panel behind the selected buttons prevents their use without this workaround
        private void PrimaryButtonLeft(UIMouseEvent evt, UIElement listeningElement) {
            if (button_primary != null)
                button_primary.Click(evt);
        }
        private void PrimaryButtonRight(UIMouseEvent evt, UIElement listeningElement) {
            if (button_primary != null)
                button_primary.RightClick(evt);
        }
        private void PrimaryButtonHover(UIMouseEvent evt, UIElement listeningElement) {
            if (button_primary != null)
                button_primary.MouseOver(evt);
        }
        private void PrimaryButtonDeHover(UIMouseEvent evt, UIElement listeningElement) {
            if (button_primary != null)
                button_primary.MouseOut(evt);
        }
        private void SecondaryButtonLeft(UIMouseEvent evt, UIElement listeningElement) {
            if (button_secondary != null)
                button_secondary.Click(evt);
        }
        private void SecondaryButtonRight(UIMouseEvent evt, UIElement listeningElement) {
            if (button_secondary != null)
                button_secondary.RightClick(evt);
        }
        private void SecondaryButtonHover(UIMouseEvent evt, UIElement listeningElement) {
            if (button_secondary != null)
                button_secondary.MouseOver(evt);
        }
        private void SecondaryButtonDeHover(UIMouseEvent evt, UIElement listeningElement) {
            if (button_secondary != null)
                button_secondary.MouseOut(evt);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void ClickReset(UIMouseEvent evt, UIElement listeningElement) {
            UIPopup.Instance.ShowResetAttributes(listeningElement);
        }

        /*
        private void ClickHelp(UIMouseEvent evt, UIElement listeningElement) {
            UIHelpSettings.Instance.OpenHelp();
        }
        */

    }
}
