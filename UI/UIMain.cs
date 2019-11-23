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
        private const float CLASS_HEIGHT = HEIGHT - Constants.UI_PADDING - HEIGHT_ABILITY;

        private const float WIDTH_ATTRIBUTES = 264f;
        private const float HEIGHT_ATTRIBUTES = 250f;
        private const float HEIGHT_ATTRIBUTE = 30f;
        private const float WIDTH_ATTRIBUTES_RESET = 135f;

        private const float HEIGHT_CHARACTER = (HEIGHT - HEIGHT_ATTRIBUTES) - (Constants.UI_PADDING * 2) + 1;

        private static readonly float WIDTH_ABILITY = CLASS_WIDTH;
        private const float HEIGHT_ABILITY = 80f;

        private const float WIDTH_HELP_AND_PASSIVES = 180f;
        private static readonly float HEIGHT_PASSIVES = HEIGHT - HEIGHT_HELP - HEIGHT_POWER_SCALING + 1 - (Constants.UI_PADDING * 2) + 1;

        private const float HEIGHT_HELP = 35f;
        private const float HEIGHT_POWER_SCALING = 50f;

        private static readonly float WIDTH = CLASS_WIDTH + WIDTH_ATTRIBUTES + WIDTH_HELP_AND_PASSIVES + (Constants.UI_PADDING * 2) - 4;
        private const float HEIGHT = 430f;

        private const float WIDTH_CONFIRM = 200f;

        private const float INDICATOR_WIDTH = CLASS_BUTTON_SIZE + (Constants.UI_PADDING * 2);
        private const float INDICATOR_HEIGHT = CLASS_BUTTON_SIZE + CLASS_ROW_PADDING - (Constants.UI_PADDING * 2f);
        private const float INDICATOR_OFFSETS = -Constants.UI_PADDING;
        private const byte INDICATOR_ALPHA = 50;

        private const float FONT_SCALE_TITLE = 1.5f;
        private const float FONT_SCALE_HELP = 1f;
        private const float FONT_SCALE_ATTRIBUTE = 1f;
        private const float FONT_SCALE_ABILITY = 0.9f;
        private const float FONT_SCALE_LEVEL = 0.9f;
        private const float FONT_SCALE_POWER_SCALING_TITLE = 1.1f;
        private const float FONT_SCALE_POWER_SCALING = 1f;

        public const float SPACING_ABILITY_ICON = 2f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public DragableUIPanel panel { get; private set; }

        private DragableUIPanel indicate_primary, indicate_secondary;
        private ClassButton button_primary, button_secondary;

        private List<ClassButton> class_buttons;

        private List<AttributeText> attribute_texts;
        private HelpTextPanel attribute_point_text;

        private AbilityIcon[] ability_primary, ability_secondary;
        private HelpTextPanel label_character, label_primary, label_secondary;
        private UIText level_character, level_primary, level_secondary;
        private UIText level_label_character, level_label_primary, level_label_secondary;
        private CharacterXPBar xp_character;
        private ClassXPBar xp_primary, xp_secondary;

        private ScrollPanel passives;

        private TextButton old_xp_button;

        private DragableUIPanel panel_power_scaling;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            //main panel
            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOUR_UI_PANEL_BACKGROUND, this, false, true);

            //class panel
            DragableUIPanel panel_class = new DragableUIPanel(CLASS_WIDTH, CLASS_HEIGHT, Constants.COLOUR_SUBPANEL, this, false, false, false);
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

            //old xp button
            old_xp_button = new TextButton("Apply Legacy XP", FONT_SCALE_ATTRIBUTE, FONT_SCALE_ATTRIBUTE + 0.1f);
            old_xp_button.OnClick += new UIElement.MouseEvent(ClickPreRevampXP);
            old_xp_button.OnMouseOver += new UIElement.MouseEvent(MouseOverPreRevampXP);
            old_xp_button.OnMouseOut += new UIElement.MouseEvent(MouseOutPreRevampXP);
            old_xp_button.HAlign = 1f;
            old_xp_button.VAlign = 1f;
            old_xp_button.Left.Set(old_xp_button.Left.Pixels - (Constants.UI_PADDING * 2f), 0f);
            old_xp_button.Top.Set(old_xp_button.Top.Pixels - (Constants.UI_PADDING * 2f), 0f);
            old_xp_button.visible = false;
            panel_class.Append(old_xp_button);

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

            //character panel
            DragableUIPanel panel_character = new DragableUIPanel(WIDTH_ATTRIBUTES, HEIGHT_CHARACTER, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_character.Left.Set(panel_attribute.Left.Pixels, 0f);
            panel_character.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(panel_character);

            //character title
            panel_character.SetTitle("Level", FONT_SCALE_TITLE, true, "TODO", "Level");
            top = panel_character.top_space;

            //character level
            label_character = new HelpTextPanel("Character", FONT_SCALE_LEVEL, false, "TODO", "Character Level", false, true);
            label_character.Left.Set(Constants.UI_PADDING, 0f);
            label_character.Top.Set(top, 0f);
            top += label_character.Height.Pixels - 2 * Constants.UI_PADDING;
            label_character.Height.Set(45f, 0f);
            label_character.Width.Set(panel_character.Width.Pixels - (Constants.UI_PADDING * 2f), 0f);
            panel_character.Append(label_character);

            level_label_character = new UIText("Level", FONT_SCALE_LEVEL, false);
            level_label_character.Left.Set(panel_character.Width.Pixels - 75f, 0f);
            level_label_character.Top.Set(label_character.Top.Pixels, 0f);
            panel_character.Append(level_label_character);

            level_character = new UIText("000", FONT_SCALE_LEVEL, false);
            level_character.Top.Set(label_character.Top.Pixels, 0f);
            panel_character.Append(level_character);

            //character xp
            xp_character = new CharacterXPBar(panel_character.Width.Pixels - (Constants.UI_PADDING * 2f), Shortcuts.LOCAL_PLAYER.PSheet.Character);
            xp_character.Top.Set(top, 0f);
            xp_character.Left.Set(Constants.UI_PADDING, 0f);
            panel_character.Append(xp_character);
            top = label_character.Top.Pixels + label_character.Height.Pixels;

            //character primary level
            label_primary = new HelpTextPanel("DEFAULT", FONT_SCALE_LEVEL, false, "TODO", "Primary Class Level", false, true);
            label_primary.Left.Set(Constants.UI_PADDING, 0f);
            label_primary.Top.Set(top, 0f);
            top += label_primary.Height.Pixels - 2 * Constants.UI_PADDING;
            label_primary.Height.Set(45f, 0f);
            label_primary.Width.Set(panel_character.Width.Pixels - (Constants.UI_PADDING * 2f), 0f);
            panel_character.Append(label_primary);

            level_label_primary = new UIText("Level", FONT_SCALE_LEVEL, false);
            level_label_primary.Left.Set(panel_character.Width.Pixels - 75f, 0f);
            level_label_primary.Top.Set(label_primary.Top.Pixels, 0f);
            panel_character.Append(level_label_primary);

            level_primary = new UIText("000", FONT_SCALE_LEVEL, false);
            level_primary.Top.Set(label_primary.Top.Pixels, 0f);
            panel_character.Append(level_primary);

            //character primary xp
            xp_primary = new ClassXPBar(panel_character.Width.Pixels - (Constants.UI_PADDING * 2f));
            xp_primary.Top.Set(top, 0f);
            xp_primary.Left.Set(Constants.UI_PADDING, 0f);
            panel_character.Append(xp_primary);
            top = label_primary.Top.Pixels + label_primary.Height.Pixels;

            //character secondary level
            label_secondary = new HelpTextPanel("DEFAULT", FONT_SCALE_LEVEL, false, "TODO", "Secondary Class Level (Effective)", false, true);
            label_secondary.Left.Set(Constants.UI_PADDING, 0f);
            label_secondary.Top.Set(top, 0f);
            top += label_secondary.Height.Pixels - 2 * Constants.UI_PADDING;
            label_secondary.Height.Set(45f, 0f);
            label_secondary.Width.Set(panel_character.Width.Pixels - (Constants.UI_PADDING * 2f), 0f);
            panel_character.Append(label_secondary);

            level_label_secondary = new UIText("Level", FONT_SCALE_LEVEL, false);
            level_label_secondary.Left.Set(panel_character.Width.Pixels - 75f, 0f);
            level_label_secondary.Top.Set(label_secondary.Top.Pixels, 0f);
            panel_character.Append(level_label_secondary);

            level_secondary = new UIText("000", FONT_SCALE_LEVEL, false);
            level_secondary.Top.Set(label_secondary.Top.Pixels, 0f);
            panel_character.Append(level_secondary);

            //character secondary xp
            xp_secondary = new ClassXPBar(panel_character.Width.Pixels - (Constants.UI_PADDING * 2f));
            xp_secondary.Top.Set(top, 0f);
            xp_secondary.Left.Set(Constants.UI_PADDING, 0f);
            panel_character.Append(xp_secondary);
            top = label_secondary.Top.Pixels + label_secondary.Height.Pixels;

            //ability panel
            DragableUIPanel panel_ability = new DragableUIPanel(WIDTH_ABILITY, HEIGHT_ABILITY, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_ability.Left.Set(panel_class.Left.Pixels, 0f);
            panel_ability.Top.Set(HEIGHT - Constants.UI_PADDING - HEIGHT_ABILITY, 0f);
            panel.Append(panel_ability);

            //ability title
            panel_ability.SetTitle("Abilities", FONT_SCALE_TITLE, true, "To use the class abilities, you must first set the mod hotkeys in settings. After setting the hotkeys, the keys shown in the tooltips will not update until you next level up or toggle a class.", "Class Abilities");

            //ability panel primary skills
            ability_primary = new AbilityIcon[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS * 2];
            float left = Constants.UI_PADDING;
            for (byte i = 0; i < ability_primary.Length; i++) {
                ability_primary[i] = new AbilityIcon(1);
                ability_primary[i].Top.Set(Constants.UI_PADDING, 0f);
                ability_primary[i].Left.Set(left, 0f);
                ability_primary[i].Width.Set(AbilityIcon.SIZE, 0f);
                ability_primary[i].Height.Set(AbilityIcon.SIZE, 0f);
                panel_ability.Append(ability_primary[i]);
                left += ability_primary[i].Width.Pixels + SPACING_ABILITY_ICON;
            }

            //ability panel secondary skills
            ability_secondary = new AbilityIcon[Systems.Ability.NUMBER_ABILITY_SLOTS_PER_CLASS * 2];
            for (byte i = 0; i < ability_secondary.Length; i++) {
                ability_secondary[i] = new AbilityIcon();
                ability_secondary[i].Top.Set(Constants.UI_PADDING, 0f);
                ability_secondary[i].Left.Set(left, 0f);
                ability_secondary[i].Width.Set(AbilityIcon.SIZE, 0f);
                ability_secondary[i].Height.Set(AbilityIcon.SIZE, 0f);
                panel_ability.Append(ability_secondary[i]);
                left += ability_secondary[i].Width.Pixels + SPACING_ABILITY_ICON;
            }

            //unlock panel
            DragableUIPanel panel_passive = new DragableUIPanel(WIDTH_HELP_AND_PASSIVES, HEIGHT_PASSIVES, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_passive.Left.Set(panel_attribute.Left.Pixels + panel_attribute.Width.Pixels - 2f, 0f);
            panel_passive.Top.Set(HEIGHT - HEIGHT_PASSIVES - Constants.UI_PADDING, 0f);
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
            panel_help.Top.Set(Constants.UI_PADDING, 0f);
            panel_help.BackgroundColor = Color.Transparent;
            panel_help.BorderColor = Color.Transparent;
            panel.Append(panel_help);

            //top right buttons
            TextButton stats = new TextButton("Stats", FONT_SCALE_HELP, FONT_SCALE_HELP + 0.1f);
            stats.OnMouseOver += new UIElement.MouseEvent(MouseOverStats);
            stats.OnMouseOut += new UIElement.MouseEvent(MouseOutStats);
            stats.HAlign = 0.1f;
            stats.Top.Set(Constants.UI_PADDING, 0f);
            panel_help.Append(stats);

            TextButton help = new TextButton("Help", FONT_SCALE_HELP, FONT_SCALE_HELP + 0.1f);
            help.OnClick += new UIElement.MouseEvent(ClickHelp);
            help.HAlign = 0.5f;
            help.Top.Set(Constants.UI_PADDING, 0f);
            panel_help.Append(help);

            TextButton close = new TextButton("Close", FONT_SCALE_HELP, FONT_SCALE_HELP + 0.1f);
            close.OnClick += new UIElement.MouseEvent(ClickClose);
            close.HAlign = 0.9f;
            close.Top.Set(Constants.UI_PADDING, 0f);
            panel_help.Append(close);

            //power scaling
            DragableUIPanel panel_power_scaling_outer = new DragableUIPanel(WIDTH_HELP_AND_PASSIVES, HEIGHT_POWER_SCALING, Constants.COLOUR_SUBPANEL, this, false, false, false);
            panel_power_scaling_outer.Left.Set(panel_passive.Left.Pixels, 0f);
            panel_power_scaling_outer.Top.Set(panel_help.Top.Pixels + panel_help.Height.Pixels, 0f);
            panel_power_scaling_outer.SetTitle("Power Scaling", FONT_SCALE_POWER_SCALING_TITLE, true, "TODO");
            panel.Append(panel_power_scaling_outer);

            panel_power_scaling = new DragableUIPanel(WIDTH_HELP_AND_PASSIVES, HEIGHT_POWER_SCALING - panel_power_scaling_outer.top_space, Color.Transparent, this, false, false, false);
            panel_power_scaling.Top.Set(panel_power_scaling_outer.top_space  - 3f, 0f);
            panel_power_scaling.BackgroundColor = Color.Transparent;
            panel_power_scaling.BorderColor = Color.Transparent;
            panel_power_scaling.SetTitle("???", FONT_SCALE_POWER_SCALING, true);
            panel_power_scaling_outer.Append(panel_power_scaling);

            UIImageButton button_left = new UIImageButton(Utilities.Textures.TEXTURE_BUTTON_LEFT);
            button_left.Top.Set(Constants.UI_PADDING, 0f);
            button_left.Left.Set(Constants.UI_PADDING, 0f);
            button_left.SetVisibility(0.8f, 0.5f);
            button_left.OnMouseDown += new UIElement.MouseEvent(ClickPowerScalingLeft);
            panel_power_scaling.Append(button_left);

            UIImageButton button_right = new UIImageButton(Utilities.Textures.TEXTURE_BUTTON_RIGHT);
            button_right.Top.Set(Constants.UI_PADDING, 0f);
            button_right.Left.Set(panel_power_scaling_outer.Width.Pixels - Utilities.Textures.TEXTURE_BUTTON_LEFTRIGHT_SIZE - Constants.UI_PADDING, 0f);
            button_right.SetVisibility(0.8f, 0.5f);
            button_right.OnMouseDown += new UIElement.MouseEvent(ClickPowerScalingRight);
            panel_power_scaling.Append(button_right);

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

            //character level
            string str = "" + psheet.Character.Level;
            level_character.SetText(str);
            level_character.Left.Set(WIDTH_ATTRIBUTES - Constants.UI_PADDING * 2 - (Main.fontMouseText.MeasureString(str).X * FONT_SCALE_ABILITY), 0f);

            //set class xp
            xp_primary.SetClass(psheet.Classes.Primary);
            xp_secondary.SetClass(psheet.Classes.Secondary);

            //update xp
            UpdateXP();

            //primary effective level
            Systems.PlayerClass c = psheet.Classes.Primary.Class;
            if (c != null && c.Tier > 0) {
                label_primary.SetText(c.Name);

                str = "" + psheet.Classes.Primary.Level_Effective;
                level_primary.SetText(str);
                level_primary.Left.Set(WIDTH_ATTRIBUTES - Constants.UI_PADDING * 2 - (Main.fontMouseText.MeasureString(str).X * FONT_SCALE_ABILITY), 0f);

                level_label_primary.SetText("Level");
            }
            else {
                label_primary.SetText("No Primary Class");
                level_primary.SetText("");
                level_label_primary.SetText("");
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
                label_secondary.SetText(c.Name);

                str = "" + psheet.Classes.Secondary.Level_Effective;
                level_secondary.SetText(str);
                level_secondary.Left.Set(WIDTH_ATTRIBUTES - Constants.UI_PADDING * 2 - (Main.fontMouseText.MeasureString(str).X * FONT_SCALE_ABILITY), 0f);

                level_label_secondary.SetText("Level");
            }
            else {
                label_secondary.SetText("No Secondary Class");
                level_secondary.SetText("");
                level_label_secondary.SetText("");
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

            //apply prerevamp xp
            UpdatePreRevampXPButtonVisible();

            //power scaling
            panel_power_scaling.SetTitle(psheet.Attributes.Power_Scaling.Name, FONT_SCALE_POWER_SCALING);
        }

        public void UpdatePreRevampXPButtonVisible() {
            if (Main.LocalPlayer.GetModPlayer<Systems.Legacy.MyPlayer>().GetXPAvailable() > 0) {
                old_xp_button.visible = true;
            }
            else {
                old_xp_button.visible = false;
            }
        }

        public void UpdateXP() {
            xp_character.Update();
            xp_primary.Update();
            xp_secondary.Update();
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

        private void ClickHelp(UIMouseEvent evt, UIElement listeningElement) {
            UIHelp.Instance.OpenHelp();
        }

        private void ClickClose(UIMouseEvent evt, UIElement listeningElement) {
            Visibility = false;
        }

        private void MouseOverStats(UIMouseEvent evt, UIElement listeningElement) {
            UIPopup.Instance.ShowStats(listeningElement);
        }

        private void MouseOutStats(UIMouseEvent evt, UIElement listeningElement) {
            UIPopup.Instance.EndText(listeningElement);
        }

        private void ClickPowerScalingLeft(UIMouseEvent evt, UIElement listeningElement) {
            Shortcuts.LOCAL_PLAYER.PSheet.Attributes.LocalPowerScalingPrior();
        }
        private void ClickPowerScalingRight(UIMouseEvent evt, UIElement listeningElement) {
            Shortcuts.LOCAL_PLAYER.PSheet.Attributes.LocalPowerScalingNext();
        }

        private void ClickPreRevampXP(UIMouseEvent evt, UIElement listeningElement) {
            if (old_xp_button.visible) {
                Systems.PSheet psheet = Shortcuts.LOCAL_PLAYER.PSheet;

                if (psheet.Classes.Can_Gain_XP) {
                    Systems.Legacy.MyPlayer old_modplayer = Main.LocalPlayer.GetModPlayer<Systems.Legacy.MyPlayer>();

                    uint max_xp_to_add = uint.MaxValue;
                    uint xp;
                    if (psheet.Classes.Primary.Can_Gain_XP) {
                        xp = psheet.Classes.Primary.XP_Level_Remaining;
                        if (psheet.Classes.Secondary.Can_Gain_XP) {
                            xp = (uint)Math.Ceiling(max_xp_to_add / Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY);
                        }
                        max_xp_to_add = (uint)Math.Min(xp, max_xp_to_add);
                    }

                    if (psheet.Classes.Secondary.Can_Gain_XP) {
                        xp = psheet.Classes.Secondary.XP_Level_Remaining;
                        if (psheet.Classes.Primary.Can_Gain_XP) {
                            xp = (uint)Math.Ceiling(max_xp_to_add / Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY);
                        }
                        max_xp_to_add = (uint)Math.Min(xp, max_xp_to_add);
                    }

                    uint xp_to_apply = (uint)Math.Min(max_xp_to_add, old_modplayer.GetXPAvailable());
                    old_modplayer.SpendXP(xp_to_apply);
                    Systems.XP.Adjustments.LocalAddXP(xp_to_apply, false);

                    Main.NewText(xp_to_apply + " legacy experience has been allocated! " + old_modplayer.GetXPAvailable() + " remains.");
                    MouseOverPreRevampXP(evt, listeningElement);

                    UpdatePreRevampXPButtonVisible();

                }
                else {
                    Main.NewText("Cannot currently gain XP!");
                }
            }
        }

        private void MouseOverPreRevampXP(UIMouseEvent evt, UIElement listeningElement) {
            if (old_xp_button.visible) {
                Systems.Legacy.MyPlayer old_modplayer = Main.LocalPlayer.GetModPlayer<Systems.Legacy.MyPlayer>();
                double xp_available = old_modplayer.GetXPAvailable();
                if (xp_available > 0) {
                    UIPopup.Instance.ShowHelpText(old_xp_button, xp_available + " XP is available", "Legacy Redistribution");
                }
                else {
                    UIPopup.Instance.EndText(old_xp_button);
                }
            }
            else {
                UIPopup.Instance.EndText(old_xp_button);
            }
        }

        private void MouseOutPreRevampXP(UIMouseEvent evt, UIElement listeningElement) {
            UIPopup.Instance.EndText(old_xp_button);
        }

    }
}
