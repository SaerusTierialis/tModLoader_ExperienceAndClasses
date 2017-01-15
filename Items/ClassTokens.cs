using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items
{
    /* Novice */
    public class ClassToken_Novice : ClassToken
    {
        public ClassToken_Novice()
        {
            name = "Novice";
            tier = 1;
            desc = "Starter class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Squire */
    public class ClassToken_Squire : ClassToken
    {
        public ClassToken_Squire()
        {
            name = "Squire";
            tier = 2;
            desc = "Basic melee damage and life class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 10);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 } }, this, 1, recipe);
        }
    }

    /* Squire - Tank */
    public class ClassToken_Tank : ClassToken
    {
        public ClassToken_Tank()
        {
            name = "Tank";
            tier = 3;
            desc = "Tank class."+
                       "\n\nHas the highest life, defense, and aggro. Occasionally recovers"+
                       "\na percentage of maximum life.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Squire"), 1 }, { ItemID.StoneBlock, 999 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, { ItemID.StoneBlock, 999 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Squire - Warrior */
    public class ClassToken_Warrior : ClassToken
    {
        public ClassToken_Warrior()
        {
            name = "Warrior";
            tier = 3;
            desc = "Melee damage and life class."+
                       "\n\nHas the highest melee damage, and the second highest melee speed"+
                         "\nand life.";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe;

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Squire"), 1 } }, this, 1, recipe);

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 } }, this, 1, recipe);
        }
    }

    /* Squire - Berserker */
    public class ClassToken_Berserker : ClassToken
    {
        public ClassToken_Berserker()
        {
            name = "Berserker";
            tier = 3;
            desc = "Melee speed and agility class."+
                       "\n\nHas the highest melee speed as well as moderate life, agility,"+
                         "\nand melee damage.";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe;

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Squire"), 1 } }, this, 1, recipe);

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 } }, this, 1, recipe);
        }
    }

    /* Hunter */
    public class ClassToken_Hunter : ClassToken
    {
        public ClassToken_Hunter()
        {
            name = "Hunter";
            tier = 2;
            desc = "Basic ranged class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("Wood", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 } }, this, 1, recipe);
        }
    }

    /* Hunter - Archer */
    public class ClassToken_Archer : ClassToken
    {
        public ClassToken_Archer()
        {
            name = "Archer";
            tier = 3;
            desc = "Archery class."+
                       "\n\nFocuses on archery weapons (bow/crossbow). Gun weapons do not"+
                         "\nrecieve any bonuses.";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe;

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("Wood", 500);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hunter"), 1 } }, this, 1, recipe);

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("Wood", 500);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 } }, this, 1, recipe);
        }
    }

    /* Hunter - Ranger */
    public class ClassToken_Ranger : ClassToken
    {
        public ClassToken_Ranger()
        {
            name = "Ranger";
            tier = 3;
            desc = "Generic ranged class."+
                       "\n\nAn unspecialized ranged class. Equally well-suited to archery and"+
                         "\ngun weapons. Has slightly better survivability than Archer and"+
                         "\nGunner, but less damage.";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe;

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 50);
            recipe.AddRecipeGroup("Wood", 250);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hunter"), 1 } }, this, 1, recipe);

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 50);
            recipe.AddRecipeGroup("Wood", 250);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 } }, this, 1, recipe);
        }
    }

    /* Hunter - Gunner */
    public class ClassToken_Gunner : ClassToken
    {
        public ClassToken_Gunner()
        {
            name = "Gunner";
            tier = 3;
            desc = "Gunnery class." +
                       "\n\nFocuses on gun weapons. Archery weapons (bow/crossbow) do not" +
                         "\nrecieve any bonuses.";
        }
        public override void AddRecipes()
        {
            ModRecipe recipe;

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hunter"), 1 } }, this, 1, recipe);

            recipe = Recipes.Helpers.GetTokenRecipeBase(tier);
            recipe.AddRecipeGroup("IronBar", 100);
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 } }, this, 1, recipe);
        }
    }

    /* Mage */
    public class ClassToken_Mage : ClassToken
    {
        public ClassToken_Mage()
        {
            name = "Mage";
            tier = 2;
            desc = "Basic magic class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 }, { ItemID.FallenStar, 3 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Mage - Mystic */
    public class ClassToken_Mystic : ClassToken
    {
        public ClassToken_Mystic()
        {
            name = "Mystic";
            tier = 3;
            desc = "Magic damage class." +
                       "\n\nHas the highest magic damage, mana, mana regen, and mana cost" +
                         "\nreduction. This is the only class with magic crit. Occasionally" +
                         "\nrecovers a percentage of maximum mana.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Mage"), 1 }, {ItemID.FallenStar, 20} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, {ItemID.FallenStar, 20} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Mage - Sage */
    public class ClassToken_Sage : ClassToken
    {
        public ClassToken_Sage()
        {
            name = "Sage";
            tier = 3;
            desc = "Defensive magic class."+
                       "\n\nMagic damage and mana stats are second to the Mystic, but"+
                         "\nthe Sage has excellent life and defense. Occasionally" +
                         "\nrecovers a percentage of maximum mana. The Sage also produces"+
                         "\nan aura that boosts defense of nearby allies and further"+
                         "\nbolsters the Sage's defenses.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Mage"), 1 }, {ItemID.FallenStar, 10},
                {ItemID.StoneBlock, 500} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, {ItemID.FallenStar, 10},
                {ItemID.StoneBlock, 500} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Summoner */
    public class ClassToken_Summoner : ClassToken
    {
        public ClassToken_Summoner()
        {
            name = "Summoner";
            tier = 2;
            desc = "Basic minion class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 }, { mod.ItemType("Monster_Orb"), 1} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Summoner - SoulBinder */
    public class ClassToken_SoulBinder : ClassToken
    {
        public ClassToken_SoulBinder()
        {
            name = "Soul Binder";
            tier = 3;
            desc = "Minion quality class."+
                       "\n\nFocuses on quality of minions rather than quantity. Has"+
                         "\nslightly better life and defense than the Minion Master.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Summoner"), 1 }, { mod.ItemType("Monster_Orb"), 10 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, { mod.ItemType("Monster_Orb"), 10 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Summoner - MinionMaster */
    public class ClassToken_MinionMaster : ClassToken
    {
        public ClassToken_MinionMaster()
        {
            name = "Minion Master";
            tier = 3;
            desc = "Minion quantity class." +
                       "\n\nFocuses on quantity of minions rather than quality. Has" +
                         "\nslightly worse life and defense than the Soul Binder, but"+
                         "\nthis is offset by sheer numbers."+
                       "\n\nBe aware that many minions deal piecing damage and the game"+
                         "\nhas a limit on how often a single target can be hit by piecing"+
                         "\nattacks. It is possible to exceed this limit with these types"+
                         "\nof minions on a high level Minion Master, which reduces"+
                         "\neffective single target damage.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Summoner"), 1 }, {mod.ItemType("Monster_Orb"), 10} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, { mod.ItemType("Monster_Orb"), 10 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Rogue */
    public class ClassToken_Rogue : ClassToken
    {
        public ClassToken_Rogue()
        {
            name = "Rogue";
            tier = 2;
            desc = "Basic throwing, melee, and agility class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 }, { ItemID.GoldCoin, 1 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Rogue - Assassin */
    public class ClassToken_Assassin : ClassToken
    {
        public ClassToken_Assassin()
        {
            name = "Assassin";
            tier = 3;
            desc = "Melee critical and agility class."+
                       "\n\nHas the unique ability to make Opener Attacks, which rewards a"+
                         "\n\"poking\" playstyle."+
                       "\n\nOpener Attack: Occurs when making a melee attack against a target"+
                         "\nwith full life or when you have not landed a hit recently. A buff"+
                         "\nand visual will indicate when it has been long enough. Yo-yo gain"+
                         "\nonly half of the damage multiplier. Does not trigger on projectile"+
                         "\nmelee attacks such as boomerang or magic sword projectiles. Bonus"+
                         "\ncritical damage is tripled on Opener Attacks.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Rogue"), 1 }, { ItemID.PlatinumCoin, 1 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, { ItemID.PlatinumCoin, 1 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Rogue - Ninja */
    public class ClassToken_Ninja : ClassToken
    {
        public ClassToken_Ninja()
        {
            name = "Ninja";
            tier = 3;
            desc = "Throwing and agility class."+
                       "\n\nTo make throwing builds viable, Ninja has the highest"+
                         "\ndamage modifier of any class. Ninja also has excellent"+
                         "\nagility including the highest jump bonus.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Rogue"), 1}, { ItemID.PlatinumCoin, 1} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, { ItemID.PlatinumCoin, 1 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Cleric */
    public class ClassToken_Cleric : ClassToken
    {
        public ClassToken_Cleric()
        {
            name = "Cleric";
            tier = 2;
            desc = "Basic support class."+
                       "\n\nCan produce an Ichor Aura that occasionally inflicts"+
                         "\nIchor on all nearby enemies for a moment."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 }, { ItemID.LesserHealingPotion, 3} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Cleric - Saint */
    public class ClassToken_Saint : ClassToken
    {
        public ClassToken_Saint()
        {
            name = "Saint";
            tier = 3;
            desc = "Advanced support class." +
                       "\n\nCan produce a longer-lasting Ichor Aura as well as a" +
                         "\nLife Aura (healing) and Damage Aura (harm). The Saint" +
                         "\nalso has several immunities, mana cost reduction, and" +
                         "\ndecent life and defense.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Cleric"), 1 }, { ItemID.HeartLantern, 1},
                { ItemID.StarinaBottle, 1},{ ItemID.Campfire, 10} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));

            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Hybrid"), 1 }, { ItemID.HeartLantern, 1},
                { ItemID.StarinaBottle, 1},{ ItemID.Campfire, 10} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Hybrid */
    public class ClassToken_Hybrid : ClassToken
    {
        public ClassToken_Hybrid()
        {
            name = "Hybrid";
            tier = 2;
            desc = "Basic hybrid class."+
                       "\n\nCan advance to any Tier III class or to the well-rounded Hybrid II class."+
                       "\n\nClass advancement is available at level " + Recipes.Helpers.TIER_LEVEL_REQUIREMENTS[tier+1] + ".";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("ClassToken_Novice"), 1 }, { ItemID.DirtBlock, 200 } }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* Hybrid - Hybrid II */
    public class ClassToken_HybridII : ClassToken
    {
        public ClassToken_HybridII()
        {
            name = "Hybrid II";
            tier = 3;
            desc = "Advanced hybrid class."+
                         "\nA jack-of-all-trades with numerous bonuses and decent"+
                         "\nsurvivability.";
        }
        public override void AddRecipes()
        {
            Commons.QuckRecipe(mod, new int[,] { {mod.ItemType("ClassToken_Hybrid"), 1}, { ItemID.DirtBlock, 999} }, this, 1, Recipes.Helpers.GetTokenRecipeBase(tier));
        }
    }

    /* TEMPLATES */
    public abstract class ClassToken : ModItem
    {
        public static readonly string[] TIER_NAMES = new string[] { "?", "I", "II", "III" };
        public string name = "default";
        public int tier = 1;
        public string desc = "Class Token template. Not meant to be used as an in-game item.";

        public override void SetDefaults()
        {
            //tier string
            string tier_string = "?";
            if (tier > 0 && tier < TIER_NAMES.Length) tier_string = TIER_NAMES[tier];

            //basic properties
            item.name = "Class Token: " + name + " (Tier " + tier_string + ")";
            item.width = 36;
            item.height = 36;
            item.value = 0;
            item.rare = 10;
            item.accessory = true;

            //add class description
            item.toolTip = desc;

            //add class bonuses description
            if (name != "default") Helpers.ClassTokenEffects(Main.LocalPlayer, item, name, false, new MyPlayer());
        }
        public override bool CanEquipAccessory(Player player, int slot)
        {
            if (!Helpers.VALID_SLOTS_EQUIP.Contains(slot)) return false;
                else return base.CanEquipAccessory(player, slot);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            Helpers.ClassTokenEffects(player, item, name, true);
        }
    }
}