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

            string power = Systems.Attribute.LOOKUP[(byte)Systems.Attribute.IDs.Power].Specifc_Name;

            AddHelpTextPanel("Nearby Targets", "Some classes gain a damage bonus again nearby targets. This refers to any hit that occurs within a distance of " + MPlayer.DISTANCE_CLOSE_RANGE + ". For reference, this distance is equal to the width of " + (MPlayer.DISTANCE_CLOSE_RANGE / Main.LocalPlayer.width) + " players.\n\n" + 
                "These hits do NOT need to be melee hits or from melee weapons. Projectiles and Minions count so long as the target is nearby.", ref sorted_items);
            AddHelpTextPanel("Orbs", "Orbs are rare drops that are used to unlock classes, unlock subclass, and reset attributes. They can also be consumed for XP that scales with your overall progression.\n\n" + 
                Items.Orb_Monster.NAME + " drops from all monsters and " + Items.Orb_Boss.NAME + " drop from all bosses. The drop chance is based on the XP value of the monster relative to your overall progression. The more levels you have in any class, the harder monsters you must fight to have a decent drop chance. However, there is a minimum chance so all monsters have the potential to drop an orb." , ref sorted_items);
            AddHelpTextPanel("Subclass", "The subclass system allows you to have a primary class and a secondary class active at the same time. Primary class is set by left-click and secondary class is set by right-click. Right clicking a class when subclass is not unlocked will open the unlock dialogue which lists the requirements. The unlock is performed just once rather than per class.\n\n" + 
                "When gaining XP, the primary class recieves " + (Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_PRIMARY * 100f) + "% and the secondary class recieves " + (Systems.XP.SUBCLASS_PENALTY_XP_MULTIPLIER_SECONDARY * 100f) + "%.\n\n" + 
                "The power scaling of the two classes is combined using the primary and half of the secondary. In some cases, the power scaling types combine to create a more flexible bonus.\n\n" + 
                "The effective level of the secondary class is limited to half the level of the primary class. If the secondary class is a higher tier, then it is limited to level 1.", ref sorted_items);
            AddHelpTextPanel("Hotkeys", "This mod makes use of several hotkeys that need to be set before they can be used. To set these: click Settings either in the main menu or in the inventory screen, select Controls, scroll down to the Mod Controls section, and set each key (or click default).\n\n" +
                "There are " + ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS + " ability keys for the primary and secondary class. Some classes have more than " + ExperienceAndClasses.NUMBER_ABILITY_SLOTS_PER_CLASS +
                " abilities. The additional abilities are used by holding the Ability Alternate Effect key when pressing the main key. If you are unsure how to use an ability, mouse over it in the class UI to view its tooltip. The hotkeys in the tooltip update when you change class or levelup. You can toggle your class off/on to manually update.", ref sorted_items);
            AddHelpTextPanel("Abilities", "Abilities are class-specific techniques with various effects that are activated with hotkeys. Most classes have at least 2 abiltiies and additionally retain the abilities of the prerequisite class. As such, Tier 3 classes have at least 4 abilities. Most abilities have a mana or resource cost as well as a cooldown.", ref sorted_items);
            AddHelpTextPanel("Passives", "Passives are class-specific unlocks that modify abilities, unlock resources, add new mechanics, or otherwise provide a bonus.", ref sorted_items);
            AddHelpTextPanel("Attributes", "Attributes is a system for providing related bonuses. You can gain attributes from your active class(es) based on their level and scaling pattern, from your allocation choices, and from various buffs. Mouse over each attribute in the interface to see what it does. Note that the effect of " + power + " is different for each class.\n\n" + 
                "Attributes are displayed as A+C+B for A=Allocated, C=Class, and B=Bonus (e.g., from statuses)", ref sorted_items);
            AddHelpTextPanel("Attribute Allocation", "Every level in a class earns attribute allocation points and higher tier classes earn more points. These points are character-wide so leveling several classes will yield many points. These points are used to increase your base attributes. The cost to increase an attribute increases every " + Systems.Attribute.ALLOCATION_POINTS_PER_INCREASED_COST + " points, but bonus points are added for every " + Systems.Attribute.ALLOCATION_POINTS_PER_MILESTONE + " points allocated. This bonuses increase with further investment and is displayed in the mouse-over tooltip.", ref sorted_items);
            AddHelpTextPanel("Class Unlock", "To unlock a class, simply click on it in the interface. This will open the unlock dialogue, which lists the requirements including any crafting recipes.", ref sorted_items);
            AddHelpTextPanel("Channelling", "Channelling abilities require that you hold down the key for a duration. Many of these abilities have a mana or resource cost while channelling. Death, immobilization, and silence all interrupt channelling. Taking damage also interrupts most channelling.", ref sorted_items);
            AddHelpTextPanel("AFK", "Away From Keyboard (AFK) triggers if you do not use any controls for " + MPlayer.AFK_SECONDS + " seconds. When AFK, you cannot gain or lose XP.", ref sorted_items);
            AddHelpTextPanel("In Combat", "In Combat triggers for " + MPlayer.IN_COMBAT_SECONDS + " seconds each time you take or deal damage.", ref sorted_items);
            AddHelpTextPanel("Class", "A class consists of a unique pattern of attribute scaling, a specific effect of the " + power + " attribute, and a collection of abilities and passives. Classes are organized into unlock tiers and playstyle branches. For example, there is a branch of classes dedicated to projectiles (ranged, throwing, magic, certain melee items, and even other mods' damage types so long as its a projectile).\n\n" + 
                "With the exception of the Minion branch, any class can work well with any weapon type.", ref sorted_items);
            AddHelpTextPanel("Death Penalty", "When you die, any active class(es) lose up to " + (Systems.XP.DEATH_PENALTY_PERCENT * 100) + "% of the XP required for the next level. This does not occur if you were AFK.", ref sorted_items);
            AddHelpTextPanel(power + " Scaling", power + " is the generic damage attribute, but its effects are different for each class. When using a subclass, the scaling is merged. This merging will not increase the scaling beyond what a single class could achieve, but does make the bonuses applied more flexible.", ref sorted_items);
            AddHelpTextPanel("Experience (XP)", "XP is earned by defeating monsters and consuming orbs. Monster XP value is based on their life, defense, and damage.\n\n" +
                "Earning XP while you are max level stores the XP for when you next unlock a class. When unlocking a class, the stored XP is transfered with a " + (Systems.XP.EXTRA_XP_POOL_MULTIPLIER * 100) + "% penalty.", ref sorted_items);
            AddHelpTextPanel("Resources", "Some classes have an additional resource that is displayed in the HUD between the XP bars and ability cooldowns. Each of these is unique so you will have to read the passive to understand what it does.", ref sorted_items);
            AddHelpTextPanel("Status", "This mod does not use the vanilla Buff system because there is a limit to the number of Buffs that can be on a target and it would be easy to reach that limit with this mod. Instead, this mod uses a custom status system. Some statuses are displayed with the Buffs in the top left corner, but these are not Buffs and do not count towards the Buff limit.", ref sorted_items);
            AddHelpTextPanel("Ability Level", "Some abilities scale with level as your progress. This is done through Ability Level, which starts at 1 when you unlock the ability and then increases with each level. The Ability Level of tier II abilities continues to increase with levels in related tier III classes. When two tier III classes from the same branch are active, the Ability Level of the common tier II abilities is set to whichever value is higher.", ref sorted_items);
            AddHelpTextPanel("Interface", "Left click and drag to move any interface windows. Some windows have command buttons in the top right corner while the cursor is hovering over the window. There is a hotkey that can be set to open/close the main interace.", ref sorted_items);
            AddHelpTextPanel("Damage Bonus Types", "Increase/Decrease are additive multipliers identical to the bonuses found on equipment. For example, if you have +10% and +40% then your final damage will be 50% higher (150% total).\n\n" +
                "More/Less are true multipliers. These multiply the final damage and therefore scale further with any increases/decreases. For example, if you have 50% increased damage and 30% more damage then you will deal 195% damage (150% * 1.3). Another example, if you have 30% more damage and 30% less damage then you will deal 100% damage.\n\n" + 
                "When the type of bonus is unspecified, it is an increase/decrease. The More/Less type is uncommon.", ref sorted_items);
            AddHelpTextPanel("Custom Weapon Types", "Weapon types added by other mods ARE affected by this mod.", ref sorted_items);

            help = sorted_items.Values.ToList();

            //create list of settings
            sorted_items.Clear();
            AddSettingsToggle(ExperienceAndClasses.LOCAL_MPLAYER.show_xp, ref sorted_items);
            AddSettingsToggle(ExperienceAndClasses.LOCAL_MPLAYER.show_ability_fail_messages, ref sorted_items);
            AddSettingsToggle(ExperienceAndClasses.LOCAL_MPLAYER.show_classes_button, ref sorted_items);

            settings = sorted_items.Values.ToList();

            state.Append(panel);
        }

        private void AddHelpTextPanel(string title, string text, ref SortedDictionary<string, UIElement> items) {
            items.Add(title, new HelpTextPanel(title, TEXT_SCALE_ITEMS, true, text, title, true, true, WIDTH_ITEM));
        }

        private void AddSettingsToggle(Utilities.Containers.Setting setting, ref SortedDictionary<string, UIElement> items) {
            items.Add(setting.NAME, new SettingsToggle(setting, TEXT_SCALE_ITEMS, TEXT_SCALE_ITEMS + 0.1f, WIDTH_ITEM, this));
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

        public void OpenSettings() {
            //close main ui
            UIMain.Instance.Visibility = false;

            //set help
            panel.SetTitle("Settings", TEXT_SCALE_TITLE);
            scroll.SetItems(settings);

            //center
            panel.Left.Set((Main.screenWidth - panel.Width.Pixels) / 2f, 0f);
            panel.Top.Set((Main.screenHeight - panel.Height.Pixels) / 2f, 0f);

            //set visible
            Visibility = true;
        }
    }
}
