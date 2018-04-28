using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items
{
    public static class Helpers
    {
        /* General */
        public static readonly int LAST_AT_LEVEL = 100; //highest "At Level X" bonus
        //public static readonly List<int> VANITY_SLOTS = Enumerable.Range(13, 18).ToList();

        /* Aura */
        public static readonly float AURA_DISTANCE = 1000f;
        public static readonly int AURA_UPDATE_MSEC = 500;
        public static readonly int AURA_UPDATE_BUFF_TICKS = 50;

        /* Timing */
        public static readonly long TIME_START = new DateTime(2018, 1, 1).Ticks;

        public static readonly int TIME_IND_AURA = 0;
        public static readonly int TIME_IND_AURA_ICHOR = 1;
        public static readonly int TIME_IND_AURA_LIFE = 2;
        public static readonly int TIME_IND_AURA_DAMAGE = 3;
        public static readonly int TIME_IND_SELF_LIFE = 4;
        public static readonly int TIME_IND_SELF_MANA = 5;
        public static readonly int NUMBER_TIME_IND = 6;

        public static double[,] timeNext = new double[256, NUMBER_TIME_IND]; //this is a poor use of memory, but moving it to MyPlayer caused several issues

        //pre-defined bonus values
        public static readonly int[] OPENER_ATTACK_IMMUNE_MSEC = new int[] { 500, 750, 1000, 1250 };

        /// <summary>
        /// Returns true if the specified effect is ready. Timing is handled in intervals to ensure that effects occur in sync across clients.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="timeInd"></param>
        /// <param name="timeInterval"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public static bool TimeReady(int playerIndex, int timeInd, int timeInterval, bool update)
        {
            //get time in msec
            TimeSpan timePassed = new TimeSpan(DateTime.Now.Ticks - TIME_START);
            double time_msec = timePassed.TotalMilliseconds;

            //if first use
            if (timeNext[playerIndex, timeInd] == 0)
            {
                timeNext[playerIndex, timeInd] = Math.Floor(time_msec / timeInterval) + 1;
            }

            //check
            double target = timeNext[playerIndex, timeInd] * timeInterval;
            if (time_msec > target)
            {
                if (update) timeNext[playerIndex, timeInd] = Math.Floor(time_msec / timeInterval) + 1;
                return true;
            }
            else
                return false;
        }

        public static bool HeldYoyo(Player player)
        {
            Item item = player.HeldItem;
            if ((item.melee || item.thrown) && item.channel) return true;
                else return false;
        }

        /// <summary>
        /// Applies aura affect around self. Can apply buffs, healing, or damage.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="affectSelf"></param>
        /// <param name="affectPlayerFriendly"></param>
        /// <param name="affectPlayerHostile"></param>
        /// <param name="affectNPCFriendly"></param>
        /// <param name="affectNPCHostile"></param>
        /// <param name="healAmount"></param>
        /// <param name="selfHealMultiplier"></param>
        /// <param name="damageAmount"></param>
        /// <param name="buffID"></param>
        /// <param name="buffDurationTicks"></param>
        public static void AuraEffect(Player self, bool affectSelf, bool affectPlayerFriendly, bool affectNPCFriendly, bool affectPlayerHostile, bool affectNPCHostile,
            float healAmount = 0, float selfHealMultiplier=1f, float manaAmount = 0, float selfManaMultiplier=1f, float damageAmount=0, int buffID=-1, int buffDurationTicks=0)
        {
            //this is reached by server and all players

            //return if there is nothing to do
            if (healAmount == 0 && damageAmount == 0 && buffID == -1) return;
            if (!affectPlayerFriendly && !affectPlayerHostile && !affectNPCFriendly && !affectNPCHostile) return;

            //cast
            int heal = (int)healAmount;
            int healSelf = (int)(healAmount * selfHealMultiplier);
            int damage = (int)damageAmount;
            int mana = (int)manaAmount;
            int manaSelf = (int)(manaAmount * selfManaMultiplier);

            //init
            int amount, lifeCurrent=0, lifeMax=0, manaCurrent=0, manaMax=0;

            //
            Player player = null;
            NPC npc = null;
            bool forPlayer = true; //if false, then for npc

            //action to apply affects
            var apply = new Action(() => {
                //buff
                if (buffID >= 0 && buffDurationTicks > 0)
                {
                    if (forPlayer) player.AddBuff(buffID, buffDurationTicks);
                        else npc.AddBuff(buffID, buffDurationTicks);
                }

                //heal
                if (heal > 0)
                {
                    if (forPlayer && player.Equals(self)) amount = healSelf;
                        else amount = heal;

                    if (forPlayer)
                    {
                        lifeCurrent = player.statLife;
                        lifeMax = player.statLifeMax2;
                    }
                    else
                    {
                        lifeCurrent = npc.life;
                        lifeMax = npc.lifeMax;
                    }
                    

                    if ((lifeMax - lifeCurrent) < amount) amount = lifeMax - lifeCurrent;

                    if (amount > 0)
                    {
                        if (forPlayer)
                        {
                            if (Main.LocalPlayer.Equals(player)) player.HealEffect(amount);
                            player.statLife += amount;
                        }
                        else
                        {
                            npc.HealEffect(amount);
                            npc.life += amount;
                        }
                    }
                }

                //mana
                if (mana > 0 && forPlayer)
                {
                    if (player.Equals(self)) amount = manaSelf;
                    else amount = mana;

                    manaCurrent = player.statMana;
                    manaMax = player.statManaMax2;

                    if ((manaMax - manaCurrent) < amount) amount = manaMax - manaCurrent;

                    if (amount > 0)
                    {
                        if (Main.LocalPlayer.Equals(player)) player.ManaEffect(amount);
                        player.statMana += amount;
                    }
                }

                //damage
                if (damage > 0)
                {
                    if (forPlayer) player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByPlayer(self.whoAmI), damage, 0, true);
                    else self.ApplyDamageToNPC(npc, damage, 0, 0, false);
                }
            });

            //cycle players
            forPlayer = true;
            if (affectSelf || affectPlayerFriendly || affectPlayerHostile)
            {

                //optimize self-only
                int indexMin, indexMax;
                if (affectSelf && !affectPlayerFriendly && !affectPlayerHostile)
                {
                    indexMin = indexMax = self.whoAmI;
                }
                else
                {
                    indexMin = 0;
                    indexMax = 255;
                }

                //loop
                bool friendlyTeam = false;
                bool bothHostile = false;
                bool isSelf = false;
                for (int playerIndex = indexMin; playerIndex <= indexMax; playerIndex++)
                {
                    player = Main.player[playerIndex];

                    if (self.team != 0 && player.team == self.team) friendlyTeam = true;
                        else friendlyTeam = false;

                    if (self.hostile && player.hostile) bothHostile = true;
                        else bothHostile = false;

                    isSelf = player.Equals(self);

                    if (player.active && player.Distance(self.position) < AURA_DISTANCE && ((!isSelf && affectPlayerHostile && bothHostile && !friendlyTeam) || (!isSelf && affectPlayerFriendly && (!bothHostile || friendlyTeam)) || (isSelf && affectSelf)))
                    {
                        apply();
                    }
                }
            }

            //cycle npc
            forPlayer = false;
            NPC[] npcs = Main.npc;
            if (affectNPCFriendly || affectNPCHostile)
            {
                for (int npc_index = 0; npc_index < npcs.Length; npc_index++)
                {
                    npc = npcs[npc_index];
                    if (npc.active && npc.Distance(self.position) < AURA_DISTANCE && npc.lifeMax > 5 && ((npc.friendly && affectNPCFriendly) || (!npc.friendly && affectNPCHostile)))
                    {
                        apply();
                    }
                }
            }
        }

        /// <summary>
        /// For class tokens. Populates tooltip. Applies class effects if apply_effects is true and item is in a valid slot.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="item"></param>
        /// <param name="job"></param>
        /// <param name="applyEffects"></param>
        /// <param name="myPlayer"></param>
        public static string ClassTokenEffects(Mod mod, Player player, ModItem item, string job, bool applyEffects, MyPlayer myPlayer = null, bool isEquipped=false)
        {
            //auto-generate class bonuses (var names match player attributes)
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
            float light = 0f;
            float dodgeChancePct = 0f;
            int dodgeChancePct_CAP = 30;
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
            int assassinAttackPhase0_LEVEL = -1;
            int assassinAttackPhase1_LEVEL = -1;
            int assassinAttackPhase2_LEVEL = -1;
            int assassinAttackPhase3_LEVEL = -1;
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
                    meleeDamage = 0.01f;
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
                    meleeDamage = 0.0025f;
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
                    break;
                case "Gunner":
                    statLifeMax2 = 0.5f;
                    arrowDamage = -0.015;
                    rangedDamage = 0.015f;
                    rangedCrit = 0.5f;
                    rangedCrit_CAP = 20;
                    ammoCost80_LEVEL = 20;
                    ammoCost75_LEVEL = 40;
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
                    dodgeChancePct = 0.34f;
                    dodgeChancePct_CAP = 10;
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
                    dodgeChancePct = 0.34f;
                    dodgeChancePct_CAP = 20;
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
                    assassinAttackPhase0_LEVEL = 1;
                    assassinAttackPhase1_LEVEL = 40;
                    assassinAttackPhase2_LEVEL = 80;
                    assassinAttackPhase3_LEVEL = 100;
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
                    dodgeChancePct = 0.34f;
                    dodgeChancePct_CAP = 30;
                    pctChanceMidas = 0.01f;
                    noFallDmg_LEVEL = 15;
                    immune_Confused_LEVEL = 20;
                    immune_Darkness_LEVEL = 20;
                    onHitPetal_LEVEL = 30;
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
                    light = 0.0075f;
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
            if (myPlayer == null) myPlayer = player.GetModPlayer<MyPlayer>(mod);

            //experience and ignore class caps
            double experience = 0;
            bool ignoreCaps = false;
            if (applyEffects | isEquipped)
            {
                experience = myPlayer.GetExp();
                ignoreCaps = ExperienceAndClasses.worldIgnoreCaps;
            }

            //get effective level
            int level = myPlayer.effectiveLevel;

            //note if cap level
            string multiclass = "";
            if (myPlayer.levelCapped)
            {
                multiclass += ", Level Capped By Map";
            }

            //note if level reduced by multiclassing
            if (myPlayer.numberClasses > 1)
            {
                multiclass += ", Multiclass Penalty";
            }

            //reapply aura buff indicators?
            bool auraUpdate = false;
            if (applyEffects && TimeReady(player.whoAmI, TIME_IND_AURA, AURA_UPDATE_MSEC, true)) auraUpdate = true;

            /* Reduction From expdmgred */
            string reduction = "";
            int dmgred = ExperienceAndClasses.worldClassDamageReduction;
            if (dmgred > 0)
            {
                float reduction_multiplier = (100f - (float)dmgred) / 100f;

                //reduce damage
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
            string bonuses = "CURRENT BONUSES (Level " + level + multiclass + "):";
            int intBonus;
            float floatBonus;
            double doubleBonus;
            DateTime now = DateTime.Now;

            /* PER LEVEL BONUSES */
            desc += "\nSCALING BONUSES" + reduction + ":";

            //max life
            intBonus = (int)(statLifeMax2 * level);
            if (intBonus > 0)
            {
                if (applyEffects) player.statLifeMax2 += intBonus;
                bonuses += "\n+" + intBonus + " health";
            }
            if (statLifeMax2 > 0) desc += "\n+" + statLifeMax2 + " health";

            //life regen
            intBonus = (int)(lifeRegen * level);
            if (intBonus > 0)
            {
                if (applyEffects) player.lifeRegen += intBonus;
                bonuses += "\n+" + intBonus + " health regen";
            }
            if (lifeRegen > 0) desc += "\n+" + lifeRegen + " health regen";

            //max mana 
            intBonus = (int)(statManaMax2 * level);
            if (intBonus > 0)
            {
                //if ((player.statManaMax2 + intBonus) > 400) intBonus = 400 - player.statManaMax2;
                if (applyEffects) player.statManaMax2 += intBonus;
                bonuses += "\n+" + intBonus + " mana";
            }
            if (statManaMax2 > 0) desc += "\n+" + statManaMax2 + " mana (cannot exceed 400 mana total)";

            //mana regen delay bonus 
            intBonus = (int)(manaRegenDelayBonus * level);
            if (intBonus > 0)
            {
                if (intBonus > manaRegenDelayBonus_CAP && !ignoreCaps) intBonus = manaRegenDelayBonus_CAP;
                if (applyEffects) player.manaRegenDelayBonus += intBonus;
                bonuses += "\n+" + intBonus + " mana regen delay bonus";
            }
            if (manaRegenDelayBonus > 0)
            {
                desc += "\n+" + manaRegenDelayBonus + " mana regen delay bonus";
                if (!ignoreCaps) desc += " (max " + manaRegenDelayBonus_CAP + ")";
            }

            //defense
            intBonus = (int)(defense * level);
            if (intBonus > 0)
            {
                if (applyEffects) player.statDefense += intBonus;
                bonuses += "\n+" + intBonus + " defense";
            }
            if (defense > 0) desc += "\n+" + defense + " defense";

            //aggro
            intBonus = (int)(aggro * level);
            if (intBonus > 0)
            {
                if (applyEffects) player.aggro += intBonus;
                bonuses += "\n+" + intBonus + " aggro";
            }
            if (aggro > 0) desc += "\n+" + aggro + " aggro";

            //jump speed boost
            floatBonus = jumpSpeedBoost * level;
            if (floatBonus > 0)
            {
                if (floatBonus > jumpSpeedBoost_CAP && !ignoreCaps) floatBonus = jumpSpeedBoost_CAP;
                if (applyEffects) player.jumpSpeedBoost += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% jump speed";
            }
            if (jumpSpeedBoost > 0)
            {
                desc += "\n+" + (jumpSpeedBoost * 100) + "% jump speed";
                if (!ignoreCaps) desc += " (max " + (jumpSpeedBoost_CAP * 100) + "%)";
            }

            //move speed
            floatBonus = moveSpeed * level;
            if (floatBonus > 0)
            {
                if (floatBonus > moveSpeed_CAP && !ignoreCaps) floatBonus = moveSpeed_CAP;
                if (applyEffects) player.moveSpeed += floatBonus;
                bonuses += "\n+" + floatBonus + " move speed";
            }
            if (moveSpeed > 0)
            {
                desc += "\n+" + moveSpeed + " move speed";
                if (!ignoreCaps) desc += " (max " + moveSpeed_CAP + ")";
            }

            //melee damage
            floatBonus = meleeDamage * level;
            if (floatBonus > 0)
            {
                if (applyEffects) player.meleeDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% melee damage";
            }
            if (meleeDamage > 0) desc += "\n+" + (meleeDamage * 100) + "% melee damage";

            //melee crit
            intBonus = (int)(meleeCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > meleeCrit_CAP && !ignoreCaps) intBonus = meleeCrit_CAP;
                if (applyEffects) player.meleeCrit += intBonus;
                bonuses += "\n+" + intBonus + "% melee crit";
            }
            if (meleeCrit > 0)
            {
                desc += "\n+" + meleeCrit + "% melee crit";
                if (!ignoreCaps) desc += " (max " + meleeCrit_CAP + "%)";
            }

            //melee speed
            floatBonus = (meleeSpeed * level);
            if (floatBonus > 0)
            {
                //limit yo-yo to 200% speed
                if (HeldYoyo(player))
                {
                    float meleeSpeedCurrent = player.meleeSpeed;
                    if ((meleeSpeedCurrent + floatBonus) > 2f) floatBonus = 2f - meleeSpeedCurrent;
                }

                if (applyEffects) player.meleeSpeed += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% melee speed";
            }
            if (meleeSpeed > 0)
            {
                desc += "\n+" + (meleeSpeed * 100) + "% melee speed";
            }

            //throwing damage
            floatBonus = thrownDamage * level;
            if (floatBonus > 0)
            {
                if (applyEffects) player.thrownDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% throwing damage";
            }
            if (thrownDamage > 0) desc += "\n+" + (thrownDamage * 100) + "% throwing damage";

            //throw velocity
            floatBonus = thrownVelocity * level;
            if (floatBonus > 0)
            {
                if (applyEffects) player.thrownVelocity += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% throwing velocity";
            }
            if (thrownVelocity > 0) desc += "\n+" + (thrownVelocity * 100) + "% throwing velocity";

            //throw crit
            intBonus = (int)(thrownCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > thrownCrit_CAP && !ignoreCaps) intBonus = thrownCrit_CAP;
                if (applyEffects) player.thrownCrit += intBonus;
                bonuses += "\n+" + intBonus + "% throwing crit";
            }
            if (thrownCrit > 0)
            {
                desc += "\n+" + thrownCrit + "% throwing crit";
                if (!ignoreCaps) desc += " (max " + thrownCrit_CAP + "%)";
            }

            //ranged damage
            floatBonus = rangedDamage * level;
            if (floatBonus > 0)
            {
                if (applyEffects) player.rangedDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% ranged damage";
            }
            if (rangedDamage > 0) desc += "\n+" + (rangedDamage * 100) + "% ranged damage";

            //arrow damage
            doubleBonus = arrowDamage * level;
            if (doubleBonus != 0)
            {
                if (applyEffects && doubleBonus > 0) player.arrowDamage += (float)doubleBonus;
                if (applyEffects && doubleBonus < 0) player.arrowDamage -= (float)(-1 * doubleBonus);
                if (applyEffects && player.arrowDamage < 0) player.arrowDamage = 0;
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
                if (intBonus > rangedCrit_CAP && !ignoreCaps) intBonus = rangedCrit_CAP;
                if (applyEffects) player.rangedCrit += intBonus;
                bonuses += "\n+" + intBonus + "% ranged crit";
            }
            if (rangedCrit > 0)
            {
                desc += "\n+" + rangedCrit + "% ranged crit";
                if (!ignoreCaps) desc += " (max " + rangedCrit_CAP + "%)";
            }

            //magic damage
            floatBonus = magicDamage * level;
            if (floatBonus > 0)
            {
                if (applyEffects) player.magicDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% magic damage";
            }
            if (magicDamage > 0) desc += "\n+" + (magicDamage * 100) + "% magic damage";

            //mana used
            floatBonus = manaCost * level;
            if (floatBonus > 0)
            {
                if (floatBonus > manaCost_CAP && !ignoreCaps) floatBonus = manaCost_CAP;
                if (applyEffects) player.manaCost -= floatBonus;
                bonuses += "\n-" + (floatBonus * 100) + "% mana used";
            }
            if (manaCost > 0)
            {
                desc += "\n-" + (manaCost * 100) + "% mana used";
                if (!ignoreCaps) desc += " (max " + (manaCost_CAP * 100) + "%)";
            }

            //magic crit
            intBonus = (int)(magicCrit * level);
            if (intBonus > 0)
            {
                if (intBonus > magicCrit_CAP && !ignoreCaps) intBonus = magicCrit_CAP;
                if (applyEffects) player.magicCrit += intBonus;
                bonuses += "\n+" + intBonus + "% magic crit";
            }
            if (magicCrit > 0)
            {
                desc += "\n+" + magicCrit + "% magic crit";
                if (!ignoreCaps) desc += " (max " + magicCrit_CAP + "%)";
            }

            //minion damage
            floatBonus = minionDamage * level;
            if (floatBonus > 0)
            {
                if (applyEffects) player.minionDamage += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% minion damage";

            }
            if (minionDamage > 0) desc += "\n+" + (minionDamage * 100) + "% minion damage";

            //max minions
            intBonus = (int)(maxMinions * level) + (int)maxMinions_flat;
            if (intBonus > 0)
            {
                if (intBonus > (maxMinions_CAP + (int)maxMinions_flat) && !ignoreCaps) intBonus = (maxMinions_CAP + (int)maxMinions_flat);
                if (applyEffects) player.maxMinions += intBonus;
                bonuses += "\n+" + intBonus + " additional minion";
                if (intBonus > 1)
                {
                    bonuses += "s";
                }

                floatBonus = (float)(intBonus - (int)maxMinions_flat) * minionDamage_PENALTYPER; //MM penalty
                floatBonus = (float)Math.Round(floatBonus, 2);
                if (floatBonus > 0.9f) floatBonus = 0.9f;
                if (floatBonus > 0f)
                {
                    player.minionDamage *= (1 - floatBonus);
                    bonuses += "\n-" + (floatBonus * 100) + "% minion damage";
                }
            }
            if (maxMinions > 0)
            {
                desc += "\n+" + maxMinions + " additional minion";
                if (maxMinions > 1) desc += "s";
                if (!ignoreCaps) desc += " (max " + maxMinions_CAP + ")";
            }
            if (minionDamage_PENALTYPER > 0)
            {
                desc += "\n-" + (minionDamage_PENALTYPER * 100) + "% minion damage per bonus minion (excludes any from unlocked bonuses)";
            }

            //minion knockback
            floatBonus = minionKB * level;
            if (floatBonus > 0)
            {
                if (floatBonus > minionKB_CAP && !ignoreCaps) floatBonus = minionKB_CAP;
                if (applyEffects) player.minionKB += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% minion knockback";
            }
            if (minionKB > 0)
            {
                desc += "\n+" + (minionKB * 100) + "% minion knockback";
                if (!ignoreCaps) desc += " (max " + (minionKB_CAP * 100) + "%)";
            }

            //light
            floatBonus = light * level;
            if (floatBonus > 1f) floatBonus = 1f;
            if (floatBonus > 0)
            {
                if (applyEffects) Lighting.AddLight(player.position, 0.2f + floatBonus, 0.2f + floatBonus, 0.1f + floatBonus);
                bonuses += "\n+" + (floatBonus*100) + "% light";
            }
            if (light > 0) desc += "\n+" + (light*100) + "% light";

            //dodgeChancePct
            intBonus = (int)Math.Floor(dodgeChancePct * level);
            if (intBonus > 0)
            {
                if (intBonus > dodgeChancePct_CAP) intBonus = dodgeChancePct_CAP;
                bonuses += "\n+" + intBonus + "% dodge (might be further capped)";
                if (!ignoreCaps && (myPlayer.dodgeChancePct + intBonus) > dodgeChancePct_CAP) intBonus = dodgeChancePct_CAP - myPlayer.dodgeChancePct;
                if (applyEffects) myPlayer.dodgeChancePct += intBonus;
            }
            if (dodgeChancePct>0f)
            {
                desc += "\n+" + dodgeChancePct + "% dodge";
                if (!ignoreCaps) desc += " (max " + dodgeChancePct_CAP + "%)";
            }

            //chance to inflict Midas on hit
            floatBonus = pctChanceMidas * level;
            if (floatBonus >= 1f) floatBonus = 1f;
            if (floatBonus > 0)
            {
                if (applyEffects) myPlayer.percentMidas += floatBonus;
                bonuses += "\n+" + (floatBonus * 100) + "% chance to inflict Midas Debuff";
            }
            if (pctChanceMidas > 0) desc += "\n+" + (pctChanceMidas * 100) + "% chance to inflict Midas Debuff";

            /* AT LEVEL X BONUSES */
            desc += "\nUNLOCKED BONUSES:";

            //immune to fall damage
            if (noFallDmg_LEVEL != -1 && level >= noFallDmg_LEVEL)
            {
                if (applyEffects) player.noFallDmg = true;
                bonuses += "\nimmune to fall damage";
            }


            //no knockback
            if (noKnockback_LEVEL != -1 && level >= noKnockback_LEVEL)
            {
                if (applyEffects) player.noKnockback = true;
                bonuses += "\nimmune to knockback";
            }

            //silence immunity
            if (immune_Silence_LEVEL != -1 && level >= immune_Silence_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Silenced] = true;
                bonuses += "\nimmune to silence";
            }

            //curse immunity
            if (immune_Cursed_LEVEL != -1 && level >= immune_Cursed_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Cursed] = true;
                bonuses += "\nimmune to curse";
            }

            //bleed immunity
            if (immune_Bleeding_LEVEL != -1 && level >= immune_Bleeding_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Bleeding] = true;
                bonuses += "\nimmune to bleeding";
            }

            //confused immunity
            if (immune_Confused_LEVEL != -1 && level >= immune_Confused_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Confused] = true;
                bonuses += "\nimmune to confusion";
            }

            //darkness immunity
            if (immune_Darkness_LEVEL != -1 && level >= immune_Darkness_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Darkness] = true;
                bonuses += "\nimmune to darkness";
            }

            //poisoned immunity
            if (immune_Poisoned_LEVEL != -1 && level >= immune_Poisoned_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Poisoned] = true;
                bonuses += "\nimmune to poison";
            }

            //slow immunity
            if (immune_Slow_LEVEL != -1 && level >= immune_Slow_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Slow] = true;
                bonuses += "\nimmune to slow";
            }

            //weak immunity
            if (immune_Weak_LEVEL != -1 && level >= immune_Weak_LEVEL)
            {
                if (applyEffects) player.buffImmune[Terraria.ID.BuffID.Weak] = true;
                bonuses += "\nimmune to weak";
            }

            //on hit regen
            if (onHitRegen_LEVEL != -1 && level >= onHitRegen_LEVEL)
            {
                if (applyEffects) player.onHitRegen = true;
                bonuses += "\nhits trigger health regeneration";
            }

            //find treasure
            if (findTreasure_LEVEL != -1 && level >= findTreasure_LEVEL)
            {
                if (applyEffects) player.findTreasure = true;
                bonuses += "\ncan spot treasure";
            }

            //petals on hit
            if (onHitPetal_LEVEL != -1 && level >= onHitPetal_LEVEL)
            {
                if (applyEffects) player.onHitPetal = true;
                bonuses += "\nlaunches petals on hit (~30dmg, piercing)";
            }

            //dodge on hit
            if (onHitDodge_LEVEL != -1 && level >= onHitDodge_LEVEL)
            {
                if (applyEffects) player.onHitDodge = true;
                bonuses += "\ngrants dodges on hit";
            }

            //throw ammo 33%
            if (thrownCost33_LEVEL != -1 && level >= thrownCost33_LEVEL)
            {
                if (applyEffects) player.thrownCost33 = true;
                bonuses += "\n33% less throwing items used";
            }

            //throw ammo 50%
            if (thrownCost50_LEVEL != -1 && level >= thrownCost50_LEVEL)
            {
                if (applyEffects) player.thrownCost50 = true;
                bonuses += "\n50% less throwing items used";
            }

            //20% less ammo
            if (ammoCost80_LEVEL != -1 && level >= ammoCost80_LEVEL)
            {
                if (applyEffects) player.ammoCost80 = true;
                bonuses += "\n20% less ammo/arrows used";
            }

            //25% less ammo
            if (ammoCost75_LEVEL != -1 && level >= ammoCost75_LEVEL)
            {
                if (applyEffects) player.ammoCost75 = true;
                bonuses += "\n25% less ammo/arrows used";
            }

            //archery
            if (archery_LEVEL != -1 && level >= archery_LEVEL)
            {
                if (applyEffects) player.archery = true;
                bonuses += "\narchery bonus (20% arrow speed/damage)";
            }

            //scope
            if (scope_LEVEL != -1 && level >= scope_LEVEL)
            {
                if (applyEffects) player.scope = true;
                bonuses += "\nscope enabled";
            }

            //10% melee crit damage
            if (meleeCritDmg30Pct_LEVEL != -1 && level >= meleeCritDmg30Pct_LEVEL)
            {
                if (applyEffects && myPlayer.bonusCritPct < 0.3) myPlayer.bonusCritPct = 0.3;
                bonuses += "\n30% bonus melee critical damage (90% on Opener Attacks)";
            }
            else if (meleeCritDmg20Pct_LEVEL != -1 && level >= meleeCritDmg20Pct_LEVEL)
            {
                if (applyEffects && myPlayer.bonusCritPct < 0.2) myPlayer.bonusCritPct = 0.2;
                bonuses += "\n20% bonus melee critical damage (60% on Opener Attacks)";
            }
            else if (meleeCritDmg10Pct_LEVEL != -1 && level >= meleeCritDmg10Pct_LEVEL)
            {
                if (applyEffects && myPlayer.bonusCritPct < 0.1) myPlayer.bonusCritPct = 0.1;
                bonuses += "\n10% bonus melee critical damage (30% on Opener Attacks)";
            }

            //Assassin attack (bonus damage if target has full health or no attack has been made recently)
            if (assassinAttack_LEVEL != -1 && level >= assassinAttack_LEVEL)
            {
                floatBonus = assassinAttack_FLAT + (level * assassinAttack);
                if (applyEffects)
                {
                    myPlayer.openerBonusPct = floatBonus;
                    myPlayer.openerTime_msec = assassinAttack_TIME_MSEC;

                    //buff icon
                    if (myPlayer.timeLastAttack.AddMilliseconds(myPlayer.openerTime_msec).CompareTo(now) <= 0)
                    {
                        player.AddBuff(mod.BuffType("Buff_OpenerAttack"), 50);
                    }
                }
                bonuses += "\nopener attacks deal " + (floatBonus * 100) + "% damage";
            }

            //opener attack iframe
            intBonus = -1;
            if (assassinAttackPhase3_LEVEL != -1 && level >= assassinAttackPhase3_LEVEL)
            {
                intBonus = 3;
            }
            else if (assassinAttackPhase2_LEVEL != -1 && level >= assassinAttackPhase2_LEVEL)
            {
                intBonus = 2;
            }
            else if (assassinAttackPhase1_LEVEL != -1 && level >= assassinAttackPhase1_LEVEL)
            {
                intBonus = 1;
            }
            else if (assassinAttackPhase0_LEVEL != -1 && level >= assassinAttackPhase0_LEVEL)
            {
                intBonus = 0;
            }
            if (intBonus > -1)
            {
                if (applyEffects && myPlayer.openerImmuneTime_msec < OPENER_ATTACK_IMMUNE_MSEC[intBonus]) myPlayer.openerImmuneTime_msec = OPENER_ATTACK_IMMUNE_MSEC[intBonus];
                bonuses += "\nopener attacks grant "+(OPENER_ATTACK_IMMUNE_MSEC[intBonus] / 1000f) + " second immunity (must be off cooldown)";
            }

            //periodic party healing
            if (periodicPartyHeal_LEVEL != -1 && level >= periodicPartyHeal_LEVEL)
            {
                float healAmount = (periodicPartyHeal * level); //- ((periodicPartyHeal_LEVEL-1)* periodicPartyHeal);
                if (applyEffects)
                {
                    //apply indicator
                    if (auraUpdate) AuraEffect(player, true, true, true, false, false, 0, 0, 0, 0, 0, mod.BuffType("Aura_Life"), AURA_UPDATE_BUFF_TICKS);

                    //do heal
                    if (TimeReady(player.whoAmI, TIME_IND_AURA_LIFE, periodicPartyHeal_TIME_MSEC, true)) AuraEffect(player, true, true, true, false, false, healAmount, 0.5f);
                }
                bonuses += "\noccasionally heals allies for " + (int)healAmount + " life (half for self)";
            }

            //periodic ichor aura
            if (periodicIchorAura_LEVEL != -1 && level >= periodicIchorAura_LEVEL)
            {
                if (applyEffects)
                {
                    if (TimeReady(player.whoAmI, TIME_IND_AURA_ICHOR, periodicIchorAura_TIME_MSEC, true))
                    {
                        AuraEffect(player, false, false, false, true, true, 0, 0, 0, 0, 0, Terraria.ID.BuffID.Ichor, (int)periodicIchorAura_DUR);
                    }
                }
                bonuses += "\noccasionally inflicts ichor on nearby enemies";
            }

            //periodic dmg aura
            if (periodicDmgAura_LEVEL != -1 && level >= periodicDmgAura_LEVEL)
            {
                float damageAmount = (periodicDmgAura * level); // - ((periodicDmgAura_LEVEL-1) * periodicDmgAura);
                if (applyEffects)
                {
                    if (TimeReady(player.whoAmI, TIME_IND_AURA_DAMAGE, periodicDmgAura_TIME_MSEC, true))
                    {
                        AuraEffect(player, false, false, false, true, true, 0, 0, 0, 0, damageAmount);
                    }
                }
                bonuses += "\noccasionally harms nearby enemies for " + (int)damageAmount;
            }

            //defense aura
            if ((defenseAura1_LEVEL != -1 && level >= defenseAura1_LEVEL) || (defenseAura2_LEVEL != -1 && level >= defenseAura2_LEVEL) || (defenseAura3_LEVEL != -1 && level >= defenseAura3_LEVEL))
            {
                int buff = 0;
                intBonus = 0;
                if (defenseAura3_LEVEL != -1 && level >= defenseAura3_LEVEL)
                {
                    buff = mod.BuffType<Buffs.Aura_Defense3>();
                    intBonus = Buffs.Aura_Defense3.bonus_defense;
                }
                else if (defenseAura2_LEVEL != -1 && level >= defenseAura2_LEVEL)
                {
                    buff = mod.BuffType<Buffs.Aura_Defense2>();
                    intBonus = Buffs.Aura_Defense2.bonus_defense;
                }
                else
                {
                    buff = mod.BuffType<Buffs.Aura_Defense1>();
                    intBonus = Buffs.Aura_Defense1.bonus_defense;
                }
                
                if (applyEffects && auraUpdate)
                {
                    AuraEffect(player, true, true, true, false, false, 0, 0, 0, 0, 0, buff, AURA_UPDATE_BUFF_TICKS);
                }
                bonuses += "\nincrease the defense of nearby allies by " + intBonus;
            }

            //periodic life % gain
            if (periodicLifePercent_LEVEL != -1 && level >= periodicLifePercent_LEVEL)
            {
                float regenAmount = (periodicLifePercent * level); //- ((periodicLifePercent_LEVEL-1) * periodicLifePercent);
                if (regenAmount > periodicLifePercent_CAP) regenAmount = periodicLifePercent_CAP;
                regenAmount = regenAmount * player.statLifeMax2;

                if (applyEffects && TimeReady(player.whoAmI, TIME_IND_SELF_LIFE, periodicLifePercent_TIME_MSEC, true))
                {
                    AuraEffect(player, true, false, false, false, false, regenAmount);
                }
                bonuses += "\noccasionally heals for " + (int)regenAmount + " life";
            }

            //periodic mana % gain
            if (applyEffects && periodicManaPercent_LEVEL != -1 && level >= periodicManaPercent_LEVEL)
            {
                float regenAmount = (periodicManaPercent * level); //- ((periodicManaPercent_LEVEL - 1) * periodicManaPercent);
                if (regenAmount > periodicManaPercent_CAP) regenAmount = periodicManaPercent_CAP;
                regenAmount = regenAmount * player.statManaMax2;

                if (TimeReady(player.whoAmI, TIME_IND_SELF_MANA, periodicManaPercent_TIME_MSEC, true))
                {
                    AuraEffect(player, true, false, false, false, false, 0, 0, regenAmount);
                }
                bonuses += "\noccasionally regenerates " + (int)regenAmount + " mana";
            }

            /* DESCRIPTION OF AT LEVEL X IN ORDER */

            //FLAT BONUSES

            //flat bonnus minon(s)
            if (maxMinions_flat > 0)
            {
                desc += "\nLevel 1: +" + maxMinions_flat + " additional minion";
                if (intBonus > 1)
                {
                    desc += "s";
                }
            }

            //LEVEL X Bonuses
            for (int lvl = 0; lvl <= LAST_AT_LEVEL; lvl++)
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

                if (onHitRegen_LEVEL == lvl) desc += "\nLevel " + lvl + ": grants health regeneration on hit";
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

                if (assassinAttack_LEVEL == lvl) desc += "\nLevel " + lvl + ": opener attacks deal " + (assassinAttack_FLAT * 100) + "%+(" + (assassinAttack * 100) + "*level)% damage";

                if (assassinAttackPhase0_LEVEL==lvl) desc += "\nLevel " + lvl + ": opener attacks grant " + (OPENER_ATTACK_IMMUNE_MSEC[0] / 1000f) + " second immunity (must be off cooldown)";
                if (assassinAttackPhase1_LEVEL == lvl) desc += "\nLevel " + lvl + ": opener attacks grant " + (OPENER_ATTACK_IMMUNE_MSEC[1] / 1000f) + " second immunity (must be off cooldown)";
                if (assassinAttackPhase2_LEVEL == lvl) desc += "\nLevel " + lvl + ": opener attacks grant " + (OPENER_ATTACK_IMMUNE_MSEC[2] / 1000f) + " second immunity (must be off cooldown)";
                if (assassinAttackPhase3_LEVEL == lvl) desc += "\nLevel " + lvl + ": opener attacks grant " + (OPENER_ATTACK_IMMUNE_MSEC[3] / 1000f) + " second immunity (must be off cooldown)";

                if (periodicPartyHeal_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally heals allies (half for self)";
                if (periodicIchorAura_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally inflicts ichor on nearby enemies";
                if (periodicDmgAura_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally harms enemies";
                if (defenseAura1_LEVEL == lvl) desc += "\nLevel " + lvl + ": increase the defense of nearby allies by " + Buffs.Aura_Defense1.bonus_defense + " (non-stacking)";
                if (defenseAura2_LEVEL == lvl) desc += "\nLevel " + lvl + ": increase the defense of nearby allies by " + Buffs.Aura_Defense2.bonus_defense + " (non-stacking)";
                if (defenseAura3_LEVEL == lvl) desc += "\nLevel " + lvl + ": increase the defense of nearby allies by " + Buffs.Aura_Defense3.bonus_defense + " (non-stacking)";
                if (periodicLifePercent_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally regenerates health (scales with max hp and level)";
                if (periodicManaPercent_LEVEL == lvl) desc += "\nLevel " + lvl + ": occasionally regenerates mana (scales with max mp and level)";
            }

            //double exp_have = ExperienceAndClasses.GetExpTowardsNextLevel(experience);
            //double exp_need = ExperienceAndClasses.GetExpReqForLevel(level+1,false);
            //bonuses += "\n\nExp to next level: " + exp_have + "/" + exp_need + " (" + Math.Round((double)100 * exp_have / exp_need, 2) + "%)";

            //create tooltip
            if (applyEffects | isEquipped) desc += "\n\n" + bonuses;
            //item.toolTip2 = desc;
            //((ClassToken_Novice)item).desc2 = desc;
            return desc;
        }
    }
}
