using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExperienceAndClasses.UI {

    //UI for displaying info for mouse-hovered UI elements

    public class UIPopup : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIPopup Instance = new UIPopup();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float TEXT_SCALE_TITLE = 1.1f;
        private const float TEXT_SCALE_BUTTON = 1f;
        private const float TEXT_SCALE_BUTTON_HOVER = 1.1f;
        private const float TEXT_SCALE_BODY = 0.9f;
        private const float TEXT_SCALE_BODY_STATUS = 1f;

        private const float WIDTH_CLASS = 400f;
        private const float WIDTH_ATTRIBUTE = 450f;
        private const float WIDTH_HELP = 300f;
        private const float WIDTH_STATUS = 300f;
        private const float WIDTH_UNLOCK = 300f;
        private const float WIDTH_RESET = 400f;
        private const float WIDTH_ABILITY = 400f;
        private const float WIDTH_PASSIVE = 400f;
        private const float WIDTH_STATS = 525f;

        private const float BUTTON_SEPARATION = 100f;

        private enum MODE : byte {
            HOVER,
            INPUT,
        }

        private enum INPUT_MODE : byte {
            CLASS,
            SUBCLASS,
            RESET_ATTRIBUTES,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private DragableUIPanel panel;
        private UIText ui_text_body, ui_text_extra;
        private UIElement source = null;
        private UIImage image;
        private static MODE mode;
        private INPUT_MODE unlock_mode;
        private Systems.PlayerClass unlock_class;
        private TextButton button_yes, button_no;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public UIPopup() {
            auto = UIAutoMode.Never;
        }

        protected override void InitializeState() {
            mode = MODE.HOVER;
            unlock_mode = INPUT_MODE.SUBCLASS;
            unlock_class = null;

            panel = new DragableUIPanel(1f, 1f, Constants.COLOUR_UI_PANEL_BACKGROUND, this, false, false, false);

            ui_text_body = new UIText("", TEXT_SCALE_BODY, false);
            ui_text_body.Left.Set(Constants.UI_PADDING, 0f);
            ui_text_body.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(ui_text_body);

            ui_text_extra = new UIText("", TEXT_SCALE_BODY, false);
            ui_text_extra.Left.Set(Constants.UI_PADDING, 0f);
            ui_text_extra.Top.Set(Constants.UI_PADDING, 0f);
            panel.Append(ui_text_extra);

            image = new UIImage(Utilities.Textures.TEXTURE_BLANK);
            image.Left.Set(Constants.UI_PADDING, 0f);
            panel.Append(image);

            button_yes = new TextButton("Confirm", TEXT_SCALE_BUTTON, TEXT_SCALE_BUTTON_HOVER);
            button_yes.OnClick += new UIElement.MouseEvent(ClickYes);
            panel.Append(button_yes);
            
            button_no = new TextButton("Cancel", TEXT_SCALE_BUTTON, TEXT_SCALE_BUTTON_HOVER);
            button_no.OnClick += new UIElement.MouseEvent(ClickNo);
            panel.Append(button_no);

            state.Append(panel);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void ClickYes(UIMouseEvent evt, UIElement listeningElement) {
            if (button_yes.visible) {
                switch (unlock_mode) {
                    case INPUT_MODE.CLASS:
                        unlock_class.LocalTryUnlockClass();
                        break;

                    case INPUT_MODE.SUBCLASS:
                        Systems.PlayerClass.LocalTryUnlockSubclass();
                        break;

                    case INPUT_MODE.RESET_ATTRIBUTES:
                        Systems.Attribute.LocalTryReset();
                        break;

                    default:
                        Utilities.Logger.Error("Unsupported unlock action " + unlock_mode);
                        break;
                }
                ResetState();
            }
        }

        public void ClickNo(UIMouseEvent evt, UIElement listeningElement) {
            if (button_no.visible) {
                ResetState();
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static bool AllowClicks() {
            return mode == MODE.HOVER;
        }

        private void ResetState() {
            mode = MODE.HOVER;
            EndText(source);
        }

        private void ShowText(UIElement source, string title, string body, float width, string extra=null, float extra_left=0f, Texture2D texture=null, bool force=false, bool transparent = false, float body_scale = TEXT_SCALE_BODY) {
            if (mode == MODE.HOVER || force) {

                this.source = source;

                //colour
                if (transparent) {
                    panel.BackgroundColor = Color.Transparent;
                    panel.BorderColor = Color.Transparent;
                }
                else {
                    panel.BackgroundColor = Constants.COLOUR_UI_PANEL_BACKGROUND;
                    panel.BorderColor = Color.Black;
                }

                //title
                if (title == null) {
                    panel.RemoveTitle();
                }
                else {
                    panel.SetTitle(title, TEXT_SCALE_TITLE);
                }

                //image
                float add_left;
                float min_height_body = 0f;
                if (texture == null) {
                    image.SetImage(Utilities.Textures.TEXTURE_BLANK);
                    add_left = 0;
                }
                else {
                    image.SetImage(texture);
                    add_left = texture.Width + Constants.UI_PADDING*2;
                    image.Top.Set(panel.top_space + Constants.UI_PADDING, 0f);
                    min_height_body = texture.Height + Constants.UI_PADDING;
                }

                body = Main.fontMouseText.CreateWrappedText(body, (width - Constants.UI_PADDING * 2 - add_left) / TEXT_SCALE_BODY);
                float height = Math.Max((Main.fontMouseText.MeasureString(body).Y * TEXT_SCALE_BODY), min_height_body) + panel.top_space + Constants.UI_PADDING;

                //buttons
                if (mode == MODE.INPUT) {
                    button_yes.visible = true;
                    button_no.visible = true;

                    height += button_yes.Height.Pixels + Constants.UI_PADDING*3;
                    float height_button_center = height - Constants.UI_PADDING*2 - (button_yes.Height.Pixels / 2f);

                    Utilities.UIFunctions.CenterUIElement(button_yes, (width - BUTTON_SEPARATION) / 2f, height_button_center);
                    Utilities.UIFunctions.CenterUIElement(button_no, (width + BUTTON_SEPARATION) / 2f, height_button_center);
                }
                else {
                    button_yes.visible = false;
                    button_no.visible = false;
                }

                //body
                panel.SetSize(width, height);
                panel.SetPosition(source.GetDimensions().X + source.Width.Pixels, source.GetDimensions().Y, true);
                ui_text_body.Left.Set(Constants.UI_PADDING + add_left, 0f);
                ui_text_body.Top.Set(Constants.UI_PADDING + panel.top_space, 0f);
                ui_text_body.SetText(body, body_scale, false);

                //extra
                if (extra != null) {
                    ui_text_extra.SetText(extra);
                    ui_text_extra.Left.Set(extra_left, 0f);
                    ui_text_extra.Top.Set(panel.Height.Pixels - (Main.fontMouseText.MeasureString(extra).Y * TEXT_SCALE_BODY), 0f);
                }
                else {
                    ui_text_extra.SetText("");
                }

                //show
                Visibility = true;
            }
        }

        public void EndText(UIElement source) {
            if ((this.source != null) && this.source.Equals(source) && (mode == MODE.HOVER)) {
                this.source = null;
                Visibility = false;
            }
        }

        public void EndTextChildren(UIState state) {
            mode = MODE.HOVER;
            if (source != null) {
                UIElement parent = source.Parent;
                while (parent != null) {
                    if (parent.Equals(state)) {
                        source = null;
                        Visibility = false;
                        break;
                    }
                    parent = parent.Parent;
                }
            }
        }

        public void ShowTextClass(UIElement source, Systems.PlayerClass c) {
            string title = c.Tooltip_Title;

            string text = "";
            if (!Shortcuts.LOCAL_PLAYER.PSheet.Classes.GetClassInfo(c.ID_num).Unlocked) {
                title += " [locked]";
                text += "Left click for class unlock requirements.\n";
            }

            if (!Shortcuts.LOCAL_PLAYER.PSheet.Character.Secondary_Unlocked) {
                text += "Right click for multiclass unlock requirements.\n";
            }

            if (text.Length > 0) {
                text += "\n";
            }

            text += c.Tooltip_Main;

            ShowText(source, title, text, WIDTH_CLASS, c.Tooltip_Attribute_Growth, 100f);
        }

        public void ShowTextAttribute(UIElement source, Systems.Attribute attribute) {
            string title = attribute.Name;

            Systems.PlayerSheet.AttributeSheet sheet = Shortcuts.LOCAL_PLAYER.PSheet.Attributes;
            string points = "Zero Point: -" + sheet.Zero_Point + " (based on allocation points spent)\n" +
                            "From Allocated: " + sheet.Allocated[attribute.ID_num] + "\n" +
                            "From Class(es): " + sheet.From_Class[attribute.ID_num] + "\n" +
                            "From Other: " + sheet.Bonuses[attribute.ID_num] + "\n" +
                            "Total: " + sheet.Final[attribute.ID_num];

            string text = attribute.Description + "\n\n" + points + "\n" + attribute.Effect_Text;
            ShowText(source, title, text, WIDTH_ATTRIBUTE);
        }

        public void ShowHelpText(UIElement source, string help_text, string title=null) {
            if (title == null) {
                title = "Help";
            }
            ShowText(source, title, help_text, WIDTH_HELP);
        }

        /*
        public void ShowStatus(UIElement source, Systems.Status status) {
            ShowText(source, null, " \n" + status.Name + "\n" + status.Description, WIDTH_STATUS, null, 0, null, false, true, TEXT_SCALE_BODY_STATUS);
        }
        */

        public void ShowUnlockClass(UIElement source, Systems.PlayerClass c) {
            Items.MItem item = c.Unlock_Item;

            string str = "Requirements:\n";

            if (c.Tier == 3) {
                str += "Defeat the Wall of Flesh\n";
            }

            //str += c.Prereq.Name + " Level " + c.Prereq.Max_Level + "\n" + item.item.Name + "\n\nToken Recipe:\n" + item.GetRecipeString(true) + "\n(Work Bench)";

            str += c.Prereq.Name;
            str += " Level " + c.Prereq.Max_Level + "\n";
            //str += item.item.Name;
            //str += "\n\nToken Recipe:\n" + item.GetRecipeString(true);
            str += "\n(Work Bench)";

            mode = MODE.INPUT;
            unlock_mode = INPUT_MODE.CLASS;
            unlock_class = c;

            ShowText(source, "Unlock " + c.Name, str , WIDTH_UNLOCK, null, 0, ModContent.GetTexture(item.Texture), true);
        }

        public void ShowUnlockSubclass(UIElement source) {
            Items.MItem item = ModContent.GetInstance<Items.Unlock_Subclass>();

            string str = "Unlocking multiclassing will allow you to freely set any class as your subclass.\n\nRequirements:\nx1 " + item.item.Name + "\n\nToken Recipe:\n" + item.GetRecipeString(true) + "\n(Work Bench)";

            mode = MODE.INPUT;
            unlock_mode = INPUT_MODE.SUBCLASS;

            ShowText(source, "Unlock Multiclassing", str, WIDTH_UNLOCK, null, 0, ModContent.GetTexture(item.Texture), true);
        }

        public void ShowResetAttributes(UIElement source) {
            int cost = Systems.Attribute.LocalCalculateResetCost();

            string str = "Resetting attributes is free for the first " + (Systems.Attribute.RESET_POINTS_FREE + 1) +
                " points. After that, " + Systems.Attribute.RESET_COST_ITEM.item.Name + " are required.\n\n" +
                "Allocated: " + Shortcuts.LOCAL_PLAYER.PSheet.Attributes.Points_Spent + "\n" +
                "Cost: " + cost + " " + Systems.Attribute.RESET_COST_ITEM.item.Name + "(s)\n" +
                "\nWould you like to reset your attributes?";

            mode = MODE.INPUT;
            unlock_mode = INPUT_MODE.RESET_ATTRIBUTES;

            ShowText(source, "Attribute Reset", str, WIDTH_RESET, null, 0, ModContent.GetTexture(Systems.Attribute.RESET_COST_ITEM.Texture), true);
        }

        public void ShowAbility(UIElement source, Systems.Ability ability) {
            ShowText(source, ability.Name, ability.Description, WIDTH_ABILITY);
        }

        public void ShowPassive(UIElement source, Systems.Passive passive) {
            ShowText(source, passive.Name, passive.Description, WIDTH_PASSIVE);
        }

        public void ShowStats(UIElement source) {
            //generate stat text
            string str = "";
            string str2 = "";

            //get player
            EACPlayer eacplayer = Shortcuts.LOCAL_PLAYER;

            //damage
            float global_damage_add = eacplayer.player.allDamage - 1f;
            str += Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Damage_Header");
            str += Systems.Attribute.BonusValueString(eacplayer.player.meleeDamage + global_damage_add, "Stat_Damage_Vanilla_Melee", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.rangedDamage + global_damage_add, "Stat_Damage_Vanilla_Ranged", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.thrownDamage + global_damage_add, "Stat_Damage_Vanilla_Throwing", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.magicDamage + global_damage_add, "Stat_Damage_Vanilla_Magic", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.minionDamage + global_damage_add, "Stat_Damage_Vanilla_Minion", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Damage_Light + global_damage_add, "Stat_Damage_Light", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Damage_Harmonic + global_damage_add, "Stat_Damage_Harmonic", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Damage_Other_Add + global_damage_add, "Stat_Damage_Other", true, default, default, true);

            float global_crit_chance = eacplayer.PSheet.Stats.Crit_All;
            str += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Crit_Header");
            str += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Crit_Damage_Mult, "Stat_Crit_Mult", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.meleeCrit / 100f + global_crit_chance, "Stat_Crit_Vanilla_Melee", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.rangedCrit / 100f + global_crit_chance, "Stat_Crit_Vanilla_Ranged", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.thrownCrit / 100f + global_crit_chance, "Stat_Crit_Vanilla_Throwing", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.player.magicCrit / 100f + global_crit_chance, "Stat_Crit_Vanilla_Magic", true, default, default, false);
            str += Systems.Attribute.BonusValueString(global_crit_chance, "Stat_Crit_Minion", true, default, default, false);
            str += Systems.Attribute.BonusValueString(global_crit_chance, "Stat_Crit_Light", true, default, default, false);
            str += Systems.Attribute.BonusValueString(global_crit_chance, "Stat_Crit_Harmonic", true, default, default, false);
            str += Systems.Attribute.BonusValueString(global_crit_chance, "Stat_Crit_Other", true, default, default, true);

            str += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Abilities_Header");
            str += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Ability_Delay_Reduction, "Stat_Abilities_Cooldown", true, default, default, false);
            str += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Healing_Mult, "Stat_Abilities_Healing", true, default, default, false);

            str += "\n\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Sheet_Disclaimer");


            str2 += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Defensive_Header");
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.statLifeMax2, "Stat_Defensive_Vanilla_Life", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.lifeRegen, "Stat_Defensive_Vanilla_LifeRegen", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.statDefense, "Stat_Defensive_Vanilla_Defense", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Dodge, "Stat_Defensive_Dodge", true, default, default, false);

            str2 += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Mana_Header");
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.statManaMax2, "Stat_Mana_Vanilla_Mana", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.manaRegenBonus, "Stat_Mana_Vanilla_ManaRegen", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Mana_Regen_Delay_Reduction, "Stat_Mana_ManaRegenDelay", true, default, default, false);

            str2 += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Mobility_Header");
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.maxRunSpeed, "Stat_Mobility_Vanilla_MaxRun", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.runAcceleration, "Stat_Mobility_Vanilla_RunAccel", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.jumpSpeedBoost, "Stat_Mobility_Vanilla_Jump", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.wingTimeMax, "Stat_Mobility_Vanilla_WingTime", false, default, default, false);

            str2 += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_ItemSpeed_Header");
            str2 += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Item_Speed_Weapon, "Stat_ItemSpeed_Weapon", true, default, default, true);
            str2 += Systems.Attribute.BonusValueString(eacplayer.PSheet.Stats.Item_Speed_Tool, "Stat_ItemSpeed_Tool", true, default, default, true);
            str2 += Systems.Attribute.BonusValueString(1f + (1f - eacplayer.player.meleeSpeed), "Stat_ItemSpeed_Vanilla_Melee", true, default, default, false);

            str2 += "\n\n" + Language.GetTextValue("Mods.ExperienceAndClasses.Common.Stat_Misc_Header");
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.maxMinions, "Stat_Misc_Vanilla_MinionCap", false, default, default, false);
            str2 += Systems.Attribute.BonusValueString(eacplayer.player.fishingSkill, "Stat_Misc_Vanilla_FishingPower", false, default, default, false);

            str2 += "\n\n\n";


            //display
            ShowText(source, "Stats", str, WIDTH_STATS, str2, 265f);
        }

    }
}
