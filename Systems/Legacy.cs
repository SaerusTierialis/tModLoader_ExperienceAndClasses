using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.ModContent;

/// <summary>
/// Handles conversion from legacy versions (pre 2.0). Also maintains amount of legacy xp available.
/// </summary>
namespace ExperienceAndClasses.Systems {
    public static class Legacy {
        /// <summary>
        /// Must use the ModPlayer name "MyPlayer"
        /// </summary>
        public class MyPlayer : ModPlayer {
            private double legacy_xp, legacy_xp_spent, legacy_xp_available;

            public override void Initialize() {
                legacy_xp = 0;
                legacy_xp_spent = 0;
                legacy_xp_available = 0;
            }
            public override void Load(TagCompound tag) {
                legacy_xp = Utilities.Commons.TryGet<double>(tag, "experience", 0);
                legacy_xp_spent = Utilities.Commons.TryGet<double>(tag, "experience_spent", 0);
            }
            public override TagCompound Save() {
                return new TagCompound {
                    {"experience", legacy_xp },
                    {"experience_spent", legacy_xp_spent },
                };
            }
            public override void OnEnterWorld(Player player) {
                //convert legacy items into legacy xp/new items
                ConvertLegacyItems();

                //update amount of legacy_xp that is available to revamp
                UpdateXPAvailable();

                //update initial state of UIMain button
                UI.UIMain.Instance.UpdatePreRevampXPButtonVisible();
            }

            private void UpdateXPAvailable() {
                legacy_xp_available = legacy_xp - legacy_xp_spent;
            }

            public double GetXPAvailable() {
                return legacy_xp_available;
            }

            public bool SpendXP(double amount) {
                if (legacy_xp_available >= amount) {
                    legacy_xp_spent += amount;
                    UpdateXPAvailable();
                    return true;
                }
                else {
                    //not enough
                    return false;
                }
            }

            /// <summary>
            /// Search inventory for legacy items (tokens, orbs, etc.) and convert them to new items/legacy_xp as long as there is enough inventory space. The xp is added to the legacy value so it is also available to older versions.
            /// </summary>
            /// <param name="player"></param>
            public void ConvertLegacyItems() {
                double prior_legacy_xp;
                for (int loop = 0; loop <= 1; loop++) { //loop twice in case first loop clears up enough space for other conversions
                    foreach (Item item in player.inventory) {
                        if (item.type > 0 && item.Name.Equals("Unloaded Item")) {

                            TagCompound tag = item.modItem.Save();
                            string mod = Utilities.Commons.TryGet<String>(tag, "mod", "");

                            if (mod.Equals("ExperienceAndClasses")) {

                                string name = Utilities.Commons.TryGet<String>(tag, "name", "");

                                double xp = 0;
                                ModItem[] items = new ModItem[0];
                                bool found = true;

                                switch (name) {
                                    case "Monster_Orb":
                                        items = new ModItem[] { GetInstance<Items.Orb_Monster>() };
                                        break;

                                    case "Boss_Orb":
                                        items = new ModItem[] { GetInstance<Items.Orb_Boss>() };
                                        break;

                                    case "Experience":
                                        xp = 1;
                                        break;

                                    case "Experience100":
                                        xp = 100;
                                        break;

                                    case "Experience1000":
                                        xp = 1000;
                                        break;

                                    case "Experience10000":
                                        xp = 10000;
                                        break;

                                    case "Experience100000":
                                        xp = 100000;
                                        break;

                                    case "Experience1000000":
                                        xp = 1000000;
                                        break;

                                    case "ClassToken_Novice":
                                        //give nothing
                                        break;

                                    case "ClassToken_Cleric":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Hunter":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Hybrid":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Mage":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Rogue":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Squire":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Summoner":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>() };
                                        break;

                                    case "ClassToken_Archer":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Assassin":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Berserker":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Gunner":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_HybridII":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_MinionMaster":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Mystic":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Ninja":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Ranger":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Sage":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Saint":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_SoulBinder":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Tank":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    case "ClassToken_Warrior":
                                        items = new ModItem[] { GetInstance<Items.Unlock_Tier2>(), GetInstance<Items.Unlock_Tier3>() };
                                        break;

                                    default:
                                        found = false;
                                        break;
                                }
                                if (found) { //found something to convert
                                             //how many to convert
                                    int num = player.CountItem(item.type);

                                    //how many slots are needed...
                                    int slots_needed = items.Length - 1;

                                    //search for stackables
                                    if (slots_needed > 0) {
                                        foreach (ModItem i in items) {
                                            for (int j = 0; j < Main.realInventory; j++) {
                                                if ((i.item.type == player.inventory[j].type) && ((num + player.inventory[j].stack) <= i.item.maxStack)) {
                                                    slots_needed--;
                                                }
                                            }
                                        }
                                    }

                                    //if there are still slots needed, look for empty slots
                                    if (slots_needed > 0) {
                                        for (int i = 0; i < Main.realInventory; i++) {
                                            if (player.inventory[i].type == 0) {
                                                slots_needed--;
                                            }
                                        }
                                    }

                                    //if there is room, convert!
                                    if (slots_needed <= 0) {
                                        //remove item
                                        for (int i = 0; i < num; i++) {
                                            player.ConsumeItem(item.type);
                                        }

                                        //add xp (check for overflow) for use in revamp versions
                                        prior_legacy_xp = legacy_xp;
                                        legacy_xp += (xp * num);
                                        if (legacy_xp < prior_legacy_xp) {
                                            legacy_xp = double.MaxValue;
                                        }

                                        //add item
                                        foreach (ModItem i in items) {
                                            for (int j = 0; j < num; j++) {
                                                player.PutItemInInventory(i.item.type);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
