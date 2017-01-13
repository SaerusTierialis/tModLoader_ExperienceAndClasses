using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items
{
    /* General Functins */
    public static class ClassTokenMethods
    {
        static Mod mod = ModLoader.GetMod("ExperienceAndClasses");

        //public static int TOKEN_LIMIT = 1;

        public const int LAST_AT_LEVEL = 100; //highest "At Level X" bonus
        public const float AURA_DISTANCE = 1000f;

        public const int AURA_UPDATE_MSEC = 500;
        public const int AURA_UPDATE_BUFF_TICKS = 50;

        public static long TIME_START = new DateTime(2017, 1, 1).Ticks;

        public const int TIME_IND_AURA = 0;
        public const int TIME_IND_AURA_ICHOR = 1;
        public const int TIME_IND_AURA_LIFE = 2;
        public const int TIME_IND_AURA_DAMAGE = 3;
        public const int TIME_IND_SELF_LIFE = 4;
        public const int TIME_IND_SELF_MANA = 5;
        public const int NUMBER_TIME_IND = 6;

        public static double[,] time_next = new double[256,NUMBER_TIME_IND]; //this is a poor use of memory, but moving it to MyPlayer causes several issues

        public static bool TimeReady(int player_index, int time_ind, int time_interval, bool update)
        {
            //get time in msec
            TimeSpan time_passed = new TimeSpan(DateTime.Now.Ticks - TIME_START);
            double time_msec = time_passed.TotalMilliseconds;

            //if first use
            if (time_next[player_index,time_ind] == 0)
            {
                time_next[player_index, time_ind] = Math.Floor(time_msec / time_interval) + 1;
            }

            //check
            double target = time_next[player_index, time_ind] * time_interval;
            if (time_msec > target)
            {
                if (update) time_next[player_index, time_ind] = Math.Floor(time_msec / time_interval) + 1;
                return true;
            }
            else
                return false;
        }

        /*
        public static bool CanWearMoreTokens(Player player, int slot)
        {
            //check if multiple classes are equipped
            Item[] equips = player.armor;
            int countTokens = 0;
            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].name.Contains("Class Token") && i!=slot) countTokens++;
            }
            return countTokens >= TOKEN_LIMIT;
        }
        */

        public static int CountClasses(Player player)
        {
            Item[] equips = player.armor;
            int countTokens = 0;
            for (int i = 0; i < equips.Length; i++)
            {
                if (equips[i].name.Contains("Class Token")) countTokens++;
            }
            return countTokens;
        }

        public static void AddDescAndEffects(Player player, Item item, string job, bool apply, MyPlayer myPlayer=null)
        {
            //auto-generate declarations/initializations
            float statLifeMax2 = 0f;
            float lifeRegen = 0f;
            float statManaMax2 = 0f;
            float manaRegenDelayBonus = 0f;
            int manaRegenDelayBonus_CAP = 25;
            float defense = 0f;
            float aggro = 0f;
            float jumpSpeedBoost = 0f;
            float jumpSpeedBoost_CAP = 4f;
            float moveSpeed = 0f;
            float moveSpeed_CAP = 500f;
            float meleeDamage = 0f;
            float meleeSpeed = 0f;
            float meleeCrit = 0f;
            int meleeCrit_CAP = 0;
            double arrowDamage = 0;
            float rangedDamage = 0f;
            float rangedCrit = 0f;
            int rangedCrit_CAP = 0;
            float thrownDamage = 0f;
            float thrownVelocity = 0f;
            float thrownCrit = 0f;
            int thrownCrit_CAP = 0;
            float magicDamage = 0f;
            float manaCost = 0f;
            float manaCost_CAP = 0.5f;
            float magicCrit = 0f;
            int magicCrit_CAP = 0;
            float minionDamage = 0f;
            float maxMinions = 0f;
            int maxMinions_CAP = 10;
            float minionDamage_PENALTYPER = 0f;
            float minionKB = 0f;
            float minionKB_CAP = 0.75f;
            float pctChanceMidas = 0f;
            float maxMinions_flat = 0f;
            int noKnockback_LEVEL = -1;
            int noFallDmg_LEVEL = -1;
            int immune_Silence_LEVEL = -1;
            int immune_Cursed_LEVEL = -1;
            int immune_Bleeding_LEVEL = -1;
            int immune_Confused_LEVEL = -1;
            int immune_Darkness_LEVEL = -1;
            int immune_Poisoned_LEVEL = -1;
            int immune_Slow_LEVEL = -1;
            int immune_Weak_LEVEL = -1;
            int archery_LEVEL = -1;
            int findTreasure_LEVEL = -1;
            int onHitDodge_LEVEL = -1;
            int onHitPetal_LEVEL = -1;
            int onHitRegen_LEVEL = -1;
            int thrownCost33_LEVEL = -1;
            int thrownCost50_LEVEL = -1;
            int ammoCost80_LEVEL = -1;
            int ammoCost75_LEVEL = -1;
            int scope_LEVEL = -1;
            int meleeCritDmg10Pct_LEVEL = -1;
            int meleeCritDmg20Pct_LEVEL = -1;
            int meleeCritDmg30Pct_LEVEL = -1;
            float assassinAttack = 0f;
            float assassinAttack_FLAT = 0f;
            int assassinAttack_TIME_MSEC = 2000;
            int assassinAttack_LEVEL = -1;
            float periodicPartyHeal = 0f;
            int periodicPartyHeal_TIME_MSEC = 5000;
            int periodicPartyHeal_LEVEL = -1;
            float periodicDmgAura = 0f;
            int periodicDmgAura_TIME_MSEC = 5000;
            int periodicDmgAura_LEVEL = -1;
            float periodicIchorAura_DUR = 100f;
            int periodicIchorAura_TIME_MSEC = 5000;
            int periodicIchorAura_LEVEL = -1;
            int defenseAura1_LEVEL = -1;
            int defenseAura2_LEVEL = -1;
            int defenseAura3_LEVEL = -1;
            float periodicLifePercent = 0f;
            int periodicLifePercent_TIME_MSEC = 5000;
            float periodicLifePercent_CAP = 0.2f;
            int periodicLifePercent_LEVEL = -1;
            float periodicManaPercent = 0f;
            int periodicManaPercent_TIME_MSEC = 5000;
            float periodicManaPercent_CAP = 0.2f;
            int periodicManaPercent_LEVEL = -1;

            //auto-generate job switch
            switch (job)
            {
                case "Novice":
                    statLifeMax2 = 0.5f;
                    findTreasure_LEVEL = 65;
                    break;
                case "Squire":
                    statLifeMax2 = 1f;
                    lifeRegen = 0.1f;
                    meleeDamage = 0.005f;
                    break;
                case "Warrior":
                    statLifeMax2 = 1.5f;
                    lifeRegen = 0.15f;
                    aggro = 0.125f;
                    meleeDamage = 0.0125f;
                    meleeSpeed = 0.005f;
                    meleeCrit = 0.2f;
                    meleeCrit_CAP = 10;
                    noKnockback_LEVEL = 40;
                    immune_Weak_LEVEL = 20;
                    break;
                case "Berserker":
                    statLifeMax2 = 1f;
                    lifeRegen = 0.1f;
                    aggro = 0.15f;
                    jumpSpeedBoost = 0.05f;
                    jumpSpeedBoost_CAP = 2f;
                    moveSpeed = 2f;
                    moveSpeed_CAP = 200f;
                    meleeDamage = 0.0075f;
                    meleeSpeed = 0.015f;
                    noKnockback_LEVEL = 50;
                    immune_Slow_LEVEL = 10;
                    immune_Weak_LEVEL = 30;
                    break;
                case "Tank":
                    statLifeMax2 = 2.5f;
                    lifeRegen = 0.2f;
                    defense = 0.25f;
                    aggro = 0.5f;
                    meleeDamage = 0.005f;
                    noKnockback_LEVEL = 20;
                    immune_Confused_LEVEL = 50;
                    immune_Poisoned_LEVEL = 10;
                    immune_Slow_LEVEL = 30;
                    onHitRegen_LEVEL = 40;
                    periodicLifePercent = 0.0015f;
                    periodicLifePercent_TIME_MSEC = 5000;
                    periodicLifePercent_CAP = 0.1f;
                    periodicLifePercent_LEVEL = 1;
                    break;
                case "Hunter":
                    statLifeMax2 = 0.5f;
                    rangedDamage = 0.0075f;
                    rangedCrit = 0.25f;
                    rangedCrit_CAP = 10;
                    break;
                case "Archer":
                    statLifeMax2 = 0.5f;
                    arrowDamage = 0.013;
                    rangedCrit = 0.5f;
                    rangedCrit_CAP = 25;
                    archery_LEVEL = 50;
                    ammoCost80_LEVEL = 20;
                    ammoCost75_LEVEL = 40;
                    scope_LEVEL = 70;
                    break;
                case "Gunner":
                    statLifeMax2 = 0.5f;
                    arrowDamage = -0.015;
                    rangedDamage = 0.015f;
                    rangedCrit = 0.5f;
                    rangedCrit_CAP = 20;
                    ammoCost80_LEVEL = 20;
                    ammoCost75_LEVEL = 40;
                    scope_LEVEL = 50;
                    break;
                case "Ranger":
                    statLifeMax2 = 0.75f;
                    lifeRegen = 0.075f;
                    defense = 0.05f;
                    rangedDamage = 0.01f;
                    rangedCrit = 0.5f;
                    rangedCrit_CAP = 20;
                    ammoCost80_LEVEL = 15;
                    ammoCost75_LEVEL = 35;
                    scope_LEVEL = 60;
                    break;
                case "Rogue":
                    statLifeMax2 = 0.5f;
                    jumpSpeedBoost = 0.05f;
                    jumpSpeedBoost_CAP = 2f;
                    moveSpeed = 2f;
                    moveSpeed_CAP = 200f;
                    meleeDamage = 0.0025f;
                    meleeCrit = 0.5f;
                    meleeCrit_CAP = 15;
                    thrownDamage = 0.01f;
                    thrownVelocity = 0.005f;
                    thrownCrit = 0.5f;
                    thrownCrit_CAP = 15;
                    pctChanceMidas = 0.0075f;
                    break;
                case "Assassin":
                    statLifeMax2 = 0.5f;
                    jumpSpeedBoost = 0.075f;
                    jumpSpeedBoost_CAP = 3f;
                    moveSpeed = 5f;
                    moveSpeed_CAP = 500f;
                    meleeDamage = 0.003f;
                    meleeCrit = 1f;
                    meleeCrit_CAP = 100;
                    pctChanceMidas = 0.01f;
                    noFallDmg_LEVEL = 15;
                    immune_Poisoned_LEVEL = 30;
                    meleeCritDmg10Pct_LEVEL = 20;
                    meleeCritDmg20Pct_LEVEL = 60;
                    meleeCritDmg30Pct_LEVEL = 90;
                    assassinAttack = 0.05f;
                    assassinAttack_FLAT = 5f;
                    assassinAttack_TIME_MSEC = 2000;
                    assassinAttack_LEVEL = 1;
                    break;
                case "Ninja":
                    statLifeMax2 = 0.5f;
                    jumpSpeedBoost = 0.1f;
                    jumpSpeedBoost_CAP = 4f;
                    moveSpeed = 4f;
                    moveSpeed_CAP = 400f;
                    thrownDamage = 0.02f;
                    thrownVelocity = 0.015f;
                    thrownCrit = 0.75f;
                    thrownCrit_CAP = 75;
                    pctChanceMidas = 0.01f;
                    noFallDmg_LEVEL = 15;
                    immune_Confused_LEVEL = 20;
                    immune_Darkness_LEVEL = 20;
                    onHitPetal_LEVEL = 40;
                    thrownCost33_LEVEL = 10;
                    thrownCost50_LEVEL = 35;
                    break;
                case "Mage":
                    statLifeMax2 = 0.5f;
                    statManaMax2 = 1f;
                    manaRegenDelayBonus = 0.15f;
                    magicDamage = 0.0075f;
                    manaCost = 0.005f;
                    manaCost_CAP = 0.15f;
                    break;
                case "Mystic":
                    statLifeMax2 = 0.5f;
                    statManaMax2 = 2f;
                    manaRegenDelayBonus = 0.3f;
                    magicDamage = 0.015f;
                    manaCost = 0.01f;
                    manaCost_CAP = 0.5f;
                    magicCrit = 0.15f;
                    magicCrit_CAP = 10;
                    periodicManaPercent = 0.003f;
                    periodicManaPercent_TIME_MSEC = 5000;
                    periodicManaPercent_CAP = 0.2f;
                    periodicManaPercent_LEVEL = 1;
                    break;
                case "Sage":
                    statLifeMax2 = 1f;
                    lifeRegen = 0.15f;
                    statManaMax2 = 1.5f;
                    manaRegenDelayBonus = 0.3f;
                    defense = 0.05f;
                    magicDamage = 0.01f;
                    manaCost = 0.0075f;
                    manaCost_CAP = 0.4f;
                    defenseAura1_LEVEL = 20;
                    defenseAura2_LEVEL = 50;
                    defenseAura3_LEVEL = 90;
                    periodicManaPercent = 0.0015f;
                    periodicManaPercent_TIME_MSEC = 5000;
                    periodicManaPercent_CAP = 0.1f;
                    periodicManaPercent_LEVEL = 30;
                    break;
                case "Summoner":
                    statLifeMax2 = 0.5f;
                    statManaMax2 = 0.5f;
                    minionDamage = 0.005f;
                    maxMinions = 0.1f;
                    maxMinions_CAP = 2;
                    break;
                case "Minion Master":
                    statLifeMax2 = 0.5f;
                    statManaMax2 = 1f;
                    maxMinions = 0.15f;
                    maxMinions_CAP = 15;
                    minionDamage_PENALTYPER = 0.01f;
                    minionKB = 0.005f;
                    minionKB_CAP = 0.4f;
                    maxMinions_flat = 2f;
                    break;
                case "Soul Binder":
                    statLifeMax2 = 0.75f;
                    lifeRegen = 0.075f;
                    statManaMax2 = 1f;
                    defense = 0.05f;
                    minionDamage = 0.015f;
                    minionKB = 0.01f;
                    minionKB_CAP = 0.75f;
                    break;
                case "Cleric":
                    statLifeMax2 = 0.75f;
                    lifeRegen = 0.075f;
                    statManaMax2 = 1f;
                    manaRegenDelayBonus = 0.1f;
                    manaCost = 0.005f;
                    manaCost_CAP = 0.1f;
                    immune_Silence_LEVEL = 20;
                    periodicIchorAura_DUR = 100f;
                    periodicIchorAura_TIME_MSEC = 5000;
                    periodicIchorAura_LEVEL = 10;
                    break;
                case "Saint":
                    statLifeMax2 = 1f;
                    lifeRegen = 0.1f;
                    statManaMax2 = 1.5f;
                    manaRegenDelayBonus = 0.2f;
                    manaCost = 0.005f;
                    manaCost_CAP = 0.4f;
                    immune_Silence_LEVEL = 1;
                    immune_Cursed_LEVEL = 50;
                    immune_Confused_LEVEL = 30;
                    immune_Darkness_LEVEL = 10;
                    onHitRegen_LEVEL = 40;
                    periodicPartyHeal = 1.5f;
                    periodicPartyHeal_TIME_MSEC = 5000;
                    periodicPartyHeal_LEVEL = 1;
                    periodicDmgAura = 3f;
                    periodicDmgAura_TIME_MSEC = 5000;
                    periodicDmgAura_LEVEL = 20;
                    periodicIchorAura_DUR = 200f;
                    periodicIchorAura_TIME_MSEC = 5000;
                    periodicIchorAura_LEVEL = 1;
                    defenseAura1_LEVEL = 60;
                    break;
                case "Hybrid":
                    statLifeMax2 = 0.75f;
                    statManaMax2 = 0.5f;
                    jumpSpeedBoost = 0.025f;
                    jumpSpeedBoost_CAP = 1.5f;
                    moveSpeed = 1f;
                    moveSpeed_CAP = 100f;
                    meleeDamage = 0.0025f;
                    meleeCrit = 0.15f;
                    meleeCrit_CAP = 5;
                    rangedDamage = 0.0025f;
                    rangedCrit = 0.15f;
                    rangedCrit_CAP = 5;
                    thrownDamage = 0.005f;
                    thrownCrit = 0.15f;
                    thrownCrit_CAP = 5;
                    magicDamage = 0.0025f;
                    minionDamage = 0.0025f;
                    break;
                case "Hybrid II":
                    statLifeMax2 = 1f;
                    lifeRegen = 0.075f;
                    statManaMax2 = 1f;
                    defense = 0.05f;
                    jumpSpeedBoost = 0.05f;
                    jumpSpeedBoost_CAP = 1.5f;
                    moveSpeed = 1.5f;
                    moveSpeed_CAP = 150f;
                    meleeDamage = 0.004f;
                    meleeCrit = 0.15f;
                    meleeCrit_CAP = 7;
                    rangedDamage = 0.005f;
                    rangedCrit = 0.15f;
                    rangedCrit_CAP = 7;
                    thrownDamage = 0.0075f;
                    thrownCrit = 0.15f;
                    thrownCrit_CAP = 7;
                    magicDamage = 0.005f;
                    manaCost = 0.005f;
                    manaCost_CAP = 0.25f;
                    minionDamage = 0.005f;
                    maxMinions = 0.075f;
                    maxMinions_CAP = 5;
                    pctChanceMidas = 0.005f;
                    noKnockback_LEVEL = 80;
                    noFallDmg_LEVEL = 50;
                    immune_Silence_LEVEL = 30;
                    immune_Cursed_LEVEL = 60;
                    break;
                default:
                    break;
            }

            //get the MyPlayer
            if(myPlayer==null) myPlayer = player.GetModPlayer<MyPlayer>(mod);

            //experience and ignore class caps
            double experience = 0;
            bool ignore_caps = false;
            if (apply)
            {

                experience = myPlayer.GetExp();
                ignore_caps = myPlayer.ignore_caps;
            }
            if (Main.netMode == 1) ignore_caps = ExperienceAndClasses.global_ignore_caps;

            //get level
            int level = Methods.Experience.GetLevel(experience);

            //reduce effective level if multiclassing
            int number_classes = CountClasses(player);
            string multiclass = "";
            if (number_classes > 1)
            {
                level = (int)Math.Floor((double)level / number_classes);
                multiclass = ", Multiclass Penalty";
            }

            
            //reapply aura buff indicators?
            bool aura_update = false;
            if (apply && TimeReady(player.whoAmI, TIME_IND_AURA, AURA_UPDATE_MSEC, true)) aura_update = true;

            /* Reduction From expdmgred */
            string reduction = "";
            int dmgred = myPlayer.expdmgred;
            if (dmgred != -1)
            {
                float reduction_multiplier = (100f - (float)dmgred) / 100f;

                //reduce damage floats
                meleeDamage *= reduction_multiplier;
                rangedDamage *= reduction_multiplier;
                if (arrowDamage > 0) arrowDamage *= reduction_multiplier;
                thrownDamage *= reduction_multiplier;
                magicDamage *= reduction_multiplier;
                minionDamage *= reduction_multiplier;

                //attack speed
                meleeSpeed *= reduction_multiplier;

                //crit
                meleeCrit *= reduction_multiplier;
                rangedCrit *= reduction_multiplier;
                thrownCrit *= reduction_multiplier;
                magicCrit *= reduction_multiplier;

                //reduction string
                reduction = " (" + dmgred + "% damage reduction)";
            }

            //apply bonuses...
            string desc = "";
            string bonuses = "CURRENT BONUSES (Level " + level + multiclass +"):";
            int intBonus;
            float floatBonus;
            double doubleBonus;
            DateTime now = DateTime.Now;

            /* PER LEVEL BONUSES */
            desc += "\nSCALING BONUSES"+ reduction+":";

            //max life
            intBonus = (int)(statLifeMax2 * level);
            if (intBonus > 0)
            {
                if (apply) player.statLifeMax2 += intBonus;
                bonuses += "\n+" + intBonus + " health";
            }
            if (statLifeMax2 > 0) desc += "\n+" + statLifeMax2 + " health";

            //life regen
            intBonus = (int)(lifeRegen * level);
            if (intBonus > 0)
            {
                if (apply) player.lifeRegen += intBonus;
                bonuses += "\n+" + intBonus + " health regen";
            }
            if (lifeRegen > 0) desc += "\n+" + lifeRegen + " health regen";

            //max mana 
            intBonus = (int)(statManaMax2 * level);
            if (intBonus > 0)
            {
                if ((player.statManaMax2 + intBonus) > 400) intBonus = 400 - player.statManaMax2;
                if (apply) player.statManaMax2 += intBonus;
                bonuses += "\n+" + intBonus + " mana";
            }
            if (statManaMax2 > 0) desc += "\n+" + statManaMax2 + " mana (cannot exceed 400 mana total)";

            //mana regen delay bonus 
            intBonus = (int)(manaRegenDelayBonus * level);
            if (intBonus > 0)
            {
                if (intBonus > manaRegenDelayBonus_CAP && !ignore_caps) intBonus = manaRegenDelayBonus_CAP;
                if (apply) player.manaRegenDelayBonus += intBonus;
                bonuses += "\n+" + intBonus + " mana regen delay bonus";
            }
            if (manaRegenDelayBonus > 0)
            {
                desc += "\n+" + manaRegenDelayBonus + " mana regen delay bonus";
                if (!ignore_caps) desc += " (max " + manaRegenDelayBonus_CAP + ")";
            }

            //defense
            intBonus = (int)(defense * level);
            if (intBonus > 0)
            {
                if (apply) player.statDefense += intBonus;
                bonuses += "\n+" + intBonus + " defense";
            }
            if (defense > 0) desc += "\n+" + defense + " defense";

            //aggro
            intBonus = (int)(aggro * level);
            if (intBonus > 0)
            {
                if (apply) player.aggro += intBonus;
                bonuses += "\n+" + intBonus + " aggro";
            }
            if (aggro > 0) desc += "\n+" + aggro + " aggro";

            //jump speed boost
            floatBonus = jumpSpeedBoost * level;
            if (floatBonus > 0)
            {
                if (floatBonus > jumpSpeedBoost_CAP && !ignore_caps) floatBonus = jumpSpeedBoost_CAP;
                if (apply) player.jumpSpeedBoost += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% jump speed";
            }
            if (jumpSpeedBoost > 0)
            {
                desc += "\n+" + (jumpSpeedBoost * 100) + "% jump speed";
                if (!ignore_caps) desc += " (max " + (jumpSpeedBoost_CAP * 100) + "%)";
            }

            //move speed
            floatBonus = moveSpeed * level;
            if (floatBonus > 0)
            {
                if (floatBonus > moveSpeed_CAP && !ignore_caps) floatBonus = moveSpeed_CAP;
                if (apply) player.moveSpeed += floatBonus;
                bonuses += "\n+" + floatBonus + " move speed";
            }
            if (moveSpeed > 0)
            {
                desc += "\n+" + moveSpeed + " move speed";
                if (!ignore_caps) desc += " (max " + moveSpeed_CAP + ")";
            }

            //melee damage
            floatBonus = meleeDamage * level;
            if (floatBonus > 0)
            {
                if (apply) player.meleeDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% melee damage";
            }
            if (meleeDamage > 0) desc += "\n+" + (meleeDamage * 100) + "% melee damage";

            //melee crit
            intBonus = (int)(meleeCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > meleeCrit_CAP && !ignore_caps) intBonus = meleeCrit_CAP;
                if (apply) player.meleeCrit += intBonus;
                bonuses += "\n+" + intBonus + "% melee crit";
            }
            if (meleeCrit > 0)
            {
                desc += "\n+" + meleeCrit + "% melee crit";
                if (!ignore_caps) desc += " (max " + meleeCrit_CAP + "%)";
            }

            //melee speed
            floatBonus = (meleeSpeed * level);
            if (floatBonus > 0)
            {
                if (player.HeldItem.channel && floatBonus > 0.7f) floatBonus = 0.7f;
                //if (floatBonus > meleeSpeed_CAP && !ignore_caps) floatBonus = meleeSpeed_CAP;
                if (apply) player.meleeSpeed += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% melee speed";
            }
            if (meleeSpeed > 0)
            {
                desc += "\n+" + (meleeSpeed * 100) + "% melee speed";
                //if (!ignore_caps) desc += " (max " + (meleeSpeed_CAP * 100) + "%)";
            }

            //throwing damage
            floatBonus = thrownDamage * level;
            if (floatBonus > 0)
            {
                if (apply) player.thrownDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% throwing damage";
            }
            if (thrownDamage > 0) desc += "\n+" + (thrownDamage * 100) + "% throwing damage";

            //throw velocity
            floatBonus = thrownVelocity * level;
            if (floatBonus > 0)
            {
                if (apply) player.thrownVelocity += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% throwing velocity";
            }
            if (thrownVelocity > 0) desc += "\n+" + (thrownVelocity * 100) + "% throwing velocity";

            //throw crit
            intBonus = (int)(thrownCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > thrownCrit_CAP && !ignore_caps) intBonus = thrownCrit_CAP;
                if (apply) player.thrownCrit += intBonus;
                bonuses += "\n+" + intBonus + "% throwing crit";
            }
            if (thrownCrit > 0)
            {
                desc += "\n+" + thrownCrit + "% throwing crit";
                if (!ignore_caps) desc += " (max " + thrownCrit_CAP + "%)";
            }

            //ranged damage
            floatBonus = rangedDamage * level;
            if (floatBonus > 0)
            {
                if (apply) player.rangedDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% ranged damage";
            }
            if (rangedDamage > 0) desc += "\n+" + (rangedDamage * 100) + "% ranged damage";

            //arrow damage
            doubleBonus = arrowDamage * level;
            if (doubleBonus != 0)
            {
                if (apply && doubleBonus>0) player.arrowDamage += (float)doubleBonus;
                if (apply && doubleBonus<0) player.arrowDamage -= (float)(-1 * doubleBonus);
                if (apply && player.arrowDamage < 0) player.arrowDamage = 0;
                bonuses += "\n";
                if (arrowDamage >= 0) bonuses += "+";
                    //else bonuses += "-";
                bonuses += (doubleBonus * 100) + "% arrow damage";
            }
            if (arrowDamage > 0) desc += "\n+" + (arrowDamage * 100) + "% arrow damage";
            if (arrowDamage < 0) desc += "\n-" + (arrowDamage * -100) + "% arrow damage (cannot reduce damage below zero)";

            //ranged crit
            intBonus = (int)(rangedCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > rangedCrit_CAP && !ignore_caps) intBonus = rangedCrit_CAP;
                if (apply) player.rangedCrit += intBonus;
                bonuses += "\n+" + intBonus + "% ranged crit";
            }
            if (rangedCrit > 0)
            {
                desc += "\n+" + rangedCrit + "% ranged crit";
                if (!ignore_caps) desc += " (max " + rangedCrit_CAP + "%)";
            }

            //magic damage
            floatBonus = magicDamage * level;
            if (floatBonus > 0)
            {
                if (apply) player.magicDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% magic damage";
            }
            if (magicDamage > 0) desc += "\n+" + (magicDamage * 100) + "% magic damage";

            //mana used
            floatBonus = manaCost * level;
            if (floatBonus > 0)
            {
                if (floatBonus > manaCost_CAP && !ignore_caps) floatBonus = manaCost_CAP;
                if (apply) player.manaCost -= floatBonus;
                bonuses += "\n-" + (floatBonus * 100) + "% mana used";
            }
            if (manaCost > 0)
            {
                desc += "\n-" + (manaCost * 100) + "% mana used";
                if (!ignore_caps) desc += " (max " + (manaCost_CAP * 100) + "%)";
            }

            //magic crit
            intBonus = (int)(magicCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > magicCrit_CAP && !ignore_caps) intBonus = magicCrit_CAP;
                if (apply) player.magicCrit += intBonus;
                bonuses += "\n+" + intBonus + "% magic crit";
            }
            if (magicCrit > 0)
            {
                desc += "\n+" + magicCrit + "% magic crit";
                if (!ignore_caps) desc += " (max " + magicCrit_CAP + "%)";
            }

            //minion damage
            floatBonus = minionDamage * level;
            if (floatBonus > 0)
            {
                if (apply) player.minionDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% minion damage";

            }
            if (minionDamage > 0) desc += "\n+" + (minionDamage * 100) + "% minion damage";

            //max minions
            intBonus = (int)(maxMinions * level) + (int)maxMinions_flat;
            if (intBonus > 0)
            {
                if (intBonus > (maxMinions_CAP + (int)maxMinions_flat) && !ignore_caps) intBonus = (maxMinions_CAP + (int)maxMinions_flat);
                if (apply) player.maxMinions += intBonus;
                bonuses += "\n+" + intBonus + " additional minion";
                if (intBonus>1)
                {
                    bonuses += "s";
                }

                floatBonus = (float)(intBonus - (int)maxMinions_flat) * minionDamage_PENALTYPER; //MM penalty
                floatBonus = (float)Math.Round(floatBonus, 2);
                if (floatBonus > 0.9f) floatBonus = 0.9f;
                if (floatBonus > 0f)
                {
                    player.minionDamage *= (1 - floatBonus);
                    bonuses += "\n-" + (floatBonus*100) + "% minion damage";
                }
            }
            if (maxMinions > 0)
            {
                desc += "\n+" + maxMinions + " additional minion";
                if (maxMinions > 1) desc += "s";
                if (!ignore_caps) desc += " (max " + maxMinions_CAP + ")";
            }
            if (minionDamage_PENALTYPER > 0)
            {
                desc += "\n-" + (minionDamage_PENALTYPER*100) + "% minion damage per bonus minion (excludes any from unlocked bonuses)";
            }

            //minion knockback
            floatBonus = minionKB * level;
            if (floatBonus > 0)
            {
                if (floatBonus > minionKB_CAP && !ignore_caps) floatBonus = minionKB_CAP;
                if (apply) player.minionKB += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% minion knockback";
            }
            if (minionKB > 0)
            {
                desc += "\n+" + (minionKB * 100) + "% minion knockback";
                if (!ignore_caps) desc += " (max " + (minionKB_CAP*100) + "%)";
            }

            //chance to inflict Midas on hit
            floatBonus = pctChanceMidas * level;
            if (floatBonus > 0)
            {
                if (apply) myPlayer.percent_midas = floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% chance to inflict Midas Debuff";
            }
            if (pctChanceMidas>0) desc += "\n+" + (pctChanceMidas*100) + "% chance to inflict Midas Debuff";

            /* AT LEVEL X BONUSES */
            desc += "\nUNLOCKED BONUSES:";

            //immune to fall damage
            if (noFallDmg_LEVEL != -1 && level >= noFallDmg_LEVEL)
            {
                if (apply) player.noFallDmg = true;
                bonuses += "\nimmune to fall damage";
            }


            //no knockback
            if (noKnockback_LEVEL != -1 && level >= noKnockback_LEVEL)
            {
                if (apply) player.noKnockback = true;
                bonuses += "\nimmune to knockback";
            }

            //silence immunity
            if (immune_Silence_LEVEL != -1 && level >= immune_Silence_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Silenced] = true;
                bonuses += "\nimmune to silence";
            }

            //curse immunity
            if (immune_Cursed_LEVEL != -1 && level >= immune_Cursed_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Cursed] = true;
                bonuses += "\nimmune to curse";
            }

            //bleed immunity
            if (immune_Bleeding_LEVEL != -1 && level >= immune_Bleeding_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Bleeding] = true;
                bonuses += "\nimmune to bleeding";
            }

            //confused immunity
            if (immune_Confused_LEVEL != -1 && level >= immune_Confused_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Confused] = true;
                bonuses += "\nimmune to confusion";
            }

            //darkness immunity
            if (immune_Darkness_LEVEL != -1 && level >= immune_Darkness_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Darkness] = true;
                bonuses += "\nimmune to darkness";
            }

            //poisoned immunity
            if (immune_Poisoned_LEVEL != -1 && level >= immune_Poisoned_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Poisoned] = true;
                bonuses += "\nimmune to poison";
            }

            //slow immunity
            if (immune_Slow_LEVEL != -1 && level >= immune_Slow_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Slow] = true;
                bonuses += "\nimmune to slow";
            }

            //weak immunity
            if (immune_Weak_LEVEL != -1 && level >= immune_Weak_LEVEL)
            {
                if (apply) player.buffImmune[Terraria.ID.BuffID.Weak] = true;
                bonuses += "\nimmune to weak";
            }

            //on hit regen
            if (onHitRegen_LEVEL != -1 && level >= onHitRegen_LEVEL)
            {
                if (apply) player.onHitRegen = true;
                bonuses += "\nhits trigger health regeneration";
            }

            //find treasure
            if (findTreasure_LEVEL != -1 && level >= findTreasure_LEVEL)
            {
                if (apply) player.findTreasure = true;
                bonuses += "\ncan spot treasure";
            }

            //petals on hit
            if (onHitPetal_LEVEL != -1 && level >= onHitPetal_LEVEL)
            {
                if (apply) player.onHitPetal = true;
                bonuses += "\nlaunches petals on hit (~30dmg, piercing)";
            }

            //dodge on hit
            if (onHitDodge_LEVEL != -1 && level >= onHitDodge_LEVEL)
            {
                if (apply) player.onHitDodge = true;
                bonuses += "\ngrants dodges on hit";
            }

            //throw ammo 33%
            if (thrownCost33_LEVEL != -1 && level >= thrownCost33_LEVEL)
            {
                if (apply) player.thrownCost33 = true;
                bonuses += "\n33% less throwing items used";
            }

            //throw ammo 50%
            if (thrownCost50_LEVEL != -1 && level >= thrownCost50_LEVEL)
            {
                if (apply) player.thrownCost50 = true;
                bonuses += "\n50% less throwing items used";
            }

            //20% less ammo
            if (ammoCost80_LEVEL != -1 && level >= ammoCost80_LEVEL)
            {
                if (apply) player.ammoCost80 = true;
                bonuses += "\n20% less ammo/arrows used";
            }

            //25% less ammo
            if (ammoCost75_LEVEL != -1 && level >= ammoCost75_LEVEL)
            {
                if (apply) player.ammoCost75 = true;
                bonuses += "\n25% less ammo/arrows used";
            }

            //archery
            if (archery_LEVEL != -1 && level >= archery_LEVEL)
            {
                if (apply) player.archery = true;
                bonuses += "\narchery bonus (20% arrow speed/damage)";
            }

            //scope
            if (scope_LEVEL != -1 && level >= scope_LEVEL)
            {
                if (apply) player.scope = true;
                bonuses += "\nscope enabled";
            }

            //10% melee crit damage
            if (meleeCritDmg30Pct_LEVEL != -1 && level >= meleeCritDmg30Pct_LEVEL)
            {
                if (apply) myPlayer.bonus_crit_pct = 0.3;
                bonuses += "\n30% bonus melee critical damage (90% on Opener Attacks)";
            }
            else if (meleeCritDmg20Pct_LEVEL != -1 && level >= meleeCritDmg20Pct_LEVEL)
            {
                if (apply) myPlayer.bonus_crit_pct = 0.2;
                bonuses += "\n20% bonus melee critical damage (60% on Opener Attacks)";
            }
            else if (meleeCritDmg10Pct_LEVEL != -1 && level >= meleeCritDmg10Pct_LEVEL)
            {
                if (apply) myPlayer.bonus_crit_pct = 0.1;
                bonuses += "\n10% bonus melee critical damage (30% on Opener Attacks)";
            }
            else
            {
                myPlayer.bonus_crit_pct = 0;
            }

            //Assassin attack (bonus damage if target has full health or no attack has been made recently)
            if (assassinAttack_LEVEL != -1 && level >= assassinAttack_LEVEL)
            {
                floatBonus = assassinAttack_FLAT + (level * assassinAttack);
                if (apply)
                {
                    myPlayer.opener_bonus_pct = floatBonus;
                    myPlayer.opener_time_msec = assassinAttack_TIME_MSEC;

                    //buff icon
                    if (myPlayer.time_last_attack.AddMilliseconds(myPlayer.opener_time_msec).CompareTo(now) <= 0)
                    {
                        player.AddBuff(mod.BuffType("Buff_OpenerAttack"), 50);
                    }
                }
                bonuses += "\nOpener Attacks deal " + (floatBonus*100) + "% damage";
            }
            else
            {
                myPlayer.opener_bonus_pct = 0f;
                myPlayer.opener_time_msec = 0;
            }

            //periodic party healing
            if (periodicPartyHeal_LEVEL != -1 && level >= periodicPartyHeal_LEVEL)
            {
                float healAmt = (periodicPartyHeal * level); //- ((periodicPartyHeal_LEVEL-1)* periodicPartyHeal);
                if (apply)
                {
                    //cycle all players
                    Player allie;
                    int life, lifeMax, amt;
                    float heal;
                    bool do_aura = false;

                    if (TimeReady(player.whoAmI, TIME_IND_AURA_LIFE, periodicPartyHeal_TIME_MSEC,true))
                    {
                        do_aura = true;
                    }
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        //if player active...
                        if (Main.player[playerIndex].active)
                        {
                            //get player-npc distance
                            allie = Main.player[playerIndex];

                            //generous distance cutoff (a few screens)
                            if (player.Distance(allie.position) < AURA_DISTANCE)
                            {
                                //add icon
                                if (aura_update) allie.AddBuff(mod.BuffType("Aura_Life"), AURA_UPDATE_BUFF_TICKS);

                                //heal
                                if (do_aura)
                                {
                                    life = allie.statLife;
                                    lifeMax = allie.statLifeMax2;
                                    if (player.Equals(allie))
                                    {
                                        heal = healAmt / 2;
                                    }
                                    else
                                    {
                                        heal = healAmt;
                                    }

                                    if (life < lifeMax)
                                    {
                                        amt = (int)heal;
                                        if ((lifeMax - life) < heal)
                                        {
                                            amt = (int)(lifeMax - allie.statLife);
                                        }
                                        if (Main.LocalPlayer.Equals(allie)) allie.HealEffect(amt); //if (player.Equals(Main.LocalPlayer)) 
                                        allie.statLife += amt;
                                        //Main.NewText(player.name + " healed " + allie.name + " for " + amt);
                                    }
                                }
                            }
                        }
                    }
                    if (do_aura)
                    {
                        NPC[] npcs = Main.npc;
                        for (int npcIndex = 0; npcIndex < npcs.Length; npcIndex++)
                        {
                            if (npcs[npcIndex].friendly && npcs[npcIndex].active)
                            {
                                if (npcs[npcIndex].life < npcs[npcIndex].lifeMax)
                                {
                                    amt = (int)healAmt;
                                    if ((npcs[npcIndex].life + amt) > npcs[npcIndex].lifeMax) amt = npcs[npcIndex].lifeMax - npcs[npcIndex].life;
                                    npcs[npcIndex].life += amt;
                                }
                            }
                        }
                    }
                }
                bonuses += "\noccasionally heals allies for " + (int)healAmt + " life (half for self)";
            }

            //periodic ichor aura
            if (periodicIchorAura_LEVEL != -1 && level >= periodicIchorAura_LEVEL)
            {
                if (apply)
                {
                    if (TimeReady(player.whoAmI, TIME_IND_AURA_ICHOR, periodicIchorAura_TIME_MSEC,true))
                    {
                        NPC[] npcs = Main.npc;
                        for (int npcIndex = 0; npcIndex < npcs.Length; npcIndex++)
                        {
                            if (!npcs[npcIndex].friendly && npcs[npcIndex].active && npcs[npcIndex].lifeMax > 5)
                            {
                                if (player.Distance(npcs[npcIndex].position) <= AURA_DISTANCE)
                                {
                                    npcs[npcIndex].AddBuff(Terraria.ID.BuffID.Ichor, (int)periodicIchorAura_DUR);
                                }
                            }

                        }
                    }
                }
                bonuses += "\noccasionally inflicts ichor on nearby enemies";
            }

            //periodic dmg aura
            if (periodicDmgAura_LEVEL != -1 && level >= periodicDmgAura_LEVEL)
            {
                float dmgAmt = (periodicDmgAura * level); // - ((periodicDmgAura_LEVEL-1) * periodicDmgAura);
                if (apply)
                {
                    if (TimeReady(player.whoAmI, TIME_IND_AURA_DAMAGE, periodicDmgAura_TIME_MSEC,true))
                    {
                        NPC[] npcs = Main.npc;
                        for (int npcIndex = 0; npcIndex < npcs.Length; npcIndex++)
                        {
                            if (!npcs[npcIndex].friendly && npcs[npcIndex].active && npcs[npcIndex].lifeMax > 5)
                            {
                                if (player.Distance(npcs[npcIndex].position) <= AURA_DISTANCE)
                                {
                                    npcs[npcIndex].StrikeNPC((int)dmgAmt, 0f, 0);
                                }
                            }

                        }
                    }
                }
                bonuses += "\noccasionally harms nearby enemies for " + (int)dmgAmt;
            }

            //defense aura
            int buffInd = 0;
            if ((defenseAura1_LEVEL != -1 && level >= defenseAura1_LEVEL) || (defenseAura2_LEVEL != -1 && level >= defenseAura2_LEVEL) || (defenseAura3_LEVEL != -1 && level >= defenseAura3_LEVEL))
            {
                buffInd = 0;
                if (defenseAura3_LEVEL!=-1 && level >= defenseAura3_LEVEL)
                {
                    buffInd = mod.BuffType("Aura_Defense3");
                    intBonus = Buffs.Aura_Defense3.bonus;
                }
                else if (defenseAura2_LEVEL!=-1 && level >= defenseAura2_LEVEL)
                {
                    buffInd = mod.BuffType("Aura_Defense2");
                    intBonus = Buffs.Aura_Defense2.bonus;
                }
                else
                {
                    buffInd = mod.BuffType("Aura_Defense1");
                    intBonus = Buffs.Aura_Defense1.bonus;
                }

                if (apply)
                {
                    //cycle all players
                    Player allie;
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        //if player active...
                        if (Main.player[playerIndex].active)
                        {
                            //get player-npc distance
                            allie = Main.player[playerIndex];

                            if (player.Distance(allie.position) < AURA_DISTANCE)
                            {
                                if ((buffInd == mod.BuffType("Aura_Defense1") && player.FindBuffIndex(mod.BuffType("Aura_Defense2")) == -1 && player.FindBuffIndex(mod.BuffType("Aura_Defense3")) == -1) ||
                                     (buffInd == mod.BuffType("Aura_Defense2") && player.FindBuffIndex(mod.BuffType("Aura_Defense3")) == -1) || 
                                     (buffInd == mod.BuffType("Aura_Defense3")))
                                {
                                    if (aura_update) allie.AddBuff(buffInd, AURA_UPDATE_BUFF_TICKS);
                                }
                            }
                        }
                    }
                }
                bonuses += "\nincrease the defense of nearby allies by " + intBonus;
            }

            //periodic life % gain
            if (periodicLifePercent_LEVEL != -1 && level >= periodicLifePercent_LEVEL)
            {
                float regenAmt = (periodicLifePercent * level); //- ((periodicLifePercent_LEVEL-1) * periodicLifePercent);
                if (regenAmt > periodicLifePercent_CAP) regenAmt = periodicLifePercent_CAP;
                int amt = 0;
                regenAmt = regenAmt * player.statLifeMax2;

                if (apply && TimeReady(player.whoAmI, TIME_IND_SELF_LIFE, periodicLifePercent_TIME_MSEC,true))
                {
                    if (player.statLife<player.statLifeMax2)
                    {
                        if ((player.statLifeMax2-player.statLife) < regenAmt)
                        {
                            amt = (int)(player.statLifeMax2 - player.statLife);
                        }
                        else
                        {
                            amt = (int)regenAmt;
                        }
                        if (player.Equals(Main.LocalPlayer)) player.HealEffect(amt);
                        player.statLife += amt;
                    }
                }
                bonuses += "\noccasionally heals for " + (int)regenAmt + " life";
            }

            //periodic mana % gain
            if (apply && periodicManaPercent_LEVEL != -1 && level >= periodicManaPercent_LEVEL)
            {
                float regenAmt = (periodicManaPercent * level); //- ((periodicManaPercent_LEVEL - 1) * periodicManaPercent);
                if (regenAmt > periodicManaPercent_CAP) regenAmt = periodicManaPercent_CAP;
                int amt = 0;
                regenAmt = regenAmt * player.statManaMax2;

                if (TimeReady(player.whoAmI, TIME_IND_SELF_MANA, periodicManaPercent_TIME_MSEC,true))
                {
                    if (player.statMana < player.statManaMax2)
                    {
                        if ((player.statManaMax2 - player.statMana) < regenAmt)
                        {
                            amt = (int)(player.statManaMax2 - player.statMana);
                        }
                        else
                        {
                            amt = (int)regenAmt;
                        }
                        if (player.Equals(Main.LocalPlayer)) player.ManaEffect(amt);
                        player.statMana += amt;
                    }
                }
                bonuses += "\noccasionally regenerates " + (int)regenAmt + " mana";
            }

            /* DESCRIPTION OF AT LEVEL X IN ORDER */

            //FLAT BONUSES

            //flat bonnus minon(s)
            if (maxMinions_flat > 0)
            {
                desc += "\nLv1: +" + maxMinions_flat + " additional minion";
                if (intBonus > 1)
                {
                    desc += "s";
                }
            }

            //LEVEL X Bonuses
            for (int lvl=0; lvl<=LAST_AT_LEVEL; lvl++)
            {
                if (noFallDmg_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to fall damage";
                if (noKnockback_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to knockback";

                if (immune_Silence_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to silence";
                if (immune_Cursed_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to curse";
                if (immune_Bleeding_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to bleed";
                if (immune_Confused_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to confusion";
                if (immune_Darkness_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to darkness";
                if (immune_Poisoned_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to poison";
                if (immune_Slow_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to slow";
                if (immune_Weak_LEVEL == lvl) desc += "\nLevel " + lvl + ": immune to weak";

                if (findTreasure_LEVEL == lvl) desc += "\nLevel " + lvl + ": can spot treasure";

                if(onHitRegen_LEVEL == lvl) desc += "\nLevel " + lvl + ": grants health regeneration on hit";
                if (onHitDodge_LEVEL == lvl) desc += "\nLevel " + lvl + ": grants dodges on hit";
                if (onHitPetal_LEVEL == lvl) desc += "\nLevel " + lvl + ": launches petals on hit (~30dmg, piercing)";

                if (thrownCost33_LEVEL == lvl) desc += "\nLevel " + lvl + ": 33% less throwing items used";
                if (thrownCost50_LEVEL == lvl) desc += "\nLevel " + lvl + ": 50% less throwing items used";

                if (ammoCost80_LEVEL == lvl) desc += "\nLevel " + lvl + ": 20% less ammo used";
                if (ammoCost75_LEVEL == lvl) desc += "\nLevel " + lvl + ": 25% less ammo used";

                if (archery_LEVEL == lvl) desc += "\nLevel " + lvl + ": archery bonus (20% arrow speed/damage)";
                if (scope_LEVEL == lvl) desc += "\nLevel " + lvl + ": scope enabled";

                if (meleeCritDmg10Pct_LEVEL == lvl) desc += "\nLevel " + lvl + ": 10% bonus melee critical damage (non-stacking)";
                if (meleeCritDmg20Pct_LEVEL == lvl) desc += "\nLevel " + lvl + ": 20% bonus melee critical damage (non-stacking)";
                if (meleeCritDmg30Pct_LEVEL == lvl) desc += "\nLevel " + lvl + ": 30% bonus melee critical damage (non-stacking)";

                if (assassinAttack_LEVEL == lvl) desc += "\nLevel " + lvl + ": Opener Attacks deal " + (assassinAttack_FLAT*100) + "+(" + (assassinAttack*100) + "/level)% damage";

                if (periodicPartyHeal_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally heals allies (half for self)";
                if (periodicIchorAura_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally inflicts ichor on nearby enemies";
                if (periodicDmgAura_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally harms enemies";
                if (defenseAura1_LEVEL == lvl) desc += "\nLevel " + lvl + ": increase the defense of nearby allies by " + Buffs.Aura_Defense1.bonus + " (non-stacking)";
                if (defenseAura2_LEVEL == lvl) desc += "\nLevel " + lvl + ": increase the defense of nearby allies by " + Buffs.Aura_Defense2.bonus + " (non-stacking)";
                if (defenseAura3_LEVEL == lvl) desc += "\nLevel " + lvl + ": increase the defense of nearby allies by " + Buffs.Aura_Defense3.bonus + " (non-stacking)";
                if (periodicLifePercent_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally regenerates health (scales with max hp and level)";
                if (periodicManaPercent_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally regenerates mana (scales with max mp and level)";
            }

            //double exp_have = ExperienceAndClasses.GetExpTowardsNextLevel(experience);
            //double exp_need = ExperienceAndClasses.GetExpReqForLevel(level+1,false);
            //bonuses += "\n\nExp to next level: " + exp_have + "/" + exp_need + " (" + Math.Round((double)100 * exp_have / exp_need, 2) + "%)";

            //create tooltip
            if (apply) desc += "\n\n" + bonuses;
            item.toolTip2 = desc;

        }
    }

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

    /* All Class Tokens are derived from this */
    public class ClassToken : ModItem
    {
        public string[] TIER_NAMES = new string[] { "?", "I", "II", "III" };
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
            if (name != "default") ClassTokenMethods.AddDescAndEffects(Main.LocalPlayer, item, name, false, new MyPlayer());
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            ClassTokenMethods.AddDescAndEffects(player, item, name, true);
        }
    }
}