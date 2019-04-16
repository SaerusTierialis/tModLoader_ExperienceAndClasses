using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI;

namespace ExperienceAndClasses.UI {
    class UIHelpSettings : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIHelpSettings Instance = new UIHelpSettings();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const float WIDTH = 300;
        private const float HEIGHT = 450;
        private const float WIDTH_ITEM = 300 - (Constants.UI_PADDING * 2) - ScrollPanel.SCROLLBAR_WIDTH;
        private const float TEXT_SCALE_TITLE = 1.4f;
        private const float TEXT_SCALE_ITEMS = 1f;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Varibles ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private DragableUIPanel panel;
        private ScrollPanel scroll;
        private List<UIElement> help, settings;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            panel = new DragableUIPanel(WIDTH, HEIGHT, Constants.COLOUR_UI_PANEL_BACKGROUND, this, true, false, true, false);

            panel.SetTitle("Default", TEXT_SCALE_TITLE);

            scroll = new ScrollPanel(WIDTH, HEIGHT - panel.top_space, UI);
            scroll.Top.Set(panel.top_space, 0f);
            panel.Append(scroll);

            //create list of help
            SortedDictionary<string, UIElement> sorted_items = new SortedDictionary<string, UIElement>();

            AddHelpTextPanel("Nearby Targets", "Some classes gain a damage bonus again nearby targets. This refers to any hit that occurs within a distance of " + MPlayer.DISTANCE_CLOSE_RANGE + ". For reference, this distance is equal to the width of " + (MPlayer.DISTANCE_CLOSE_RANGE / Main.LocalPlayer.width) + " players." , ref sorted_items);
            AddHelpTextPanel("Orbs", "Orbs are rare drops that are used to unlock classes, unlock subclass, and reset attributes. They can also be consumed for XP that scales with your overall progression.\n\n" + 
                Items.Orb_Monster.NAME + " drops from all monsters and " + Items.Orb_Boss.NAME + " drop from all bosses. The drop chance is based on the XP value of the monster relative to your overall progression. The more levels you have in any class, the harder monsters you must fight to have a decent drop chance. However, there is a minimum chance so all monsters have the potential to drop an orb." , ref sorted_items);
            AddHelpTextPanel("Subclass", "The subclass system allows you to have a primary class and a secondary class active at the same time. Primary class is set by left-click and secondary class is set by right-click.\n\n" + 
                "When gaining XP, the primary class recieves " + (Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY * 100f) + "% and the secondary class recieves " + (Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY * 100f) + "%.\n\n" + 
                "The power scaling of the two classes is combined using the primary and half of the secondary. In some cases, the power scaling types combine to create a more flexible bonus.\n\n" + 
                "The effective level of the secondary class is limited to half the level of the primary class. If the secondary class is a higher tier, then it is limited to level 1.", ref sorted_items);
            AddHelpTextPanel("Hotkeys", "TODO", ref sorted_items);
            AddHelpTextPanel("Abilities", "TODO", ref sorted_items);
            AddHelpTextPanel("Passives", "TODO", ref sorted_items);
            AddHelpTextPanel("Attributes", "TODO", ref sorted_items);
            AddHelpTextPanel("Attribute Allocation", "TODO", ref sorted_items);
            AddHelpTextPanel("Class Unlock", "TODO", ref sorted_items);
            AddHelpTextPanel("Tokens", "TODO", ref sorted_items);
            AddHelpTextPanel("Channelling", "TODO", ref sorted_items);
            AddHelpTextPanel("AFK", "TODO", ref sorted_items);
            AddHelpTextPanel("In Combat", "TODO", ref sorted_items);
            AddHelpTextPanel("Class", "TODO", ref sorted_items);
            AddHelpTextPanel("Death Penalty", "TODO", ref sorted_items);
            AddHelpTextPanel("Power Scaling", "TODO", ref sorted_items);
            AddHelpTextPanel("Experience (XP)", "TODO", ref sorted_items);
            AddHelpTextPanel("Resources", "TODO", ref sorted_items);

            help = sorted_items.Values.ToList();

            //create list of settings
            sorted_items.Clear();

            //TODO

            settings = sorted_items.Values.ToList();

            state.Append(panel);
        }

        private void AddHelpTextPanel(string title, string text, ref SortedDictionary<string, UIElement> items) {
            items.Add(title, new HelpTextPanel(title, TEXT_SCALE_ITEMS, true, text, title, true, true, WIDTH_ITEM));
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void OpenHelp() {
            //close main ui
            UIMain.Instance.Visibility = false;

            //set help
            panel.SetTitle("Help", TEXT_SCALE_TITLE);
            scroll.SetItems(help);

            //center
            panel.Left.Set((Main.screenWidth - panel.Width.Pixels) / 2f, 0f);
            panel.Top.Set((Main.screenHeight - panel.Height.Pixels) / 2f, 0f);

            //set visible
            Visibility = true;
        }
    }
}
