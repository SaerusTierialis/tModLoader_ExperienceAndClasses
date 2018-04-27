using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses
{
    class Abilities
    {
        public enum Return : byte
        {
            SUCCESS,
            FAIL_NOT_IMPLEMENTRD,
            FAIL_EXTERNAL,
            FAIL_MANA,
            FAIL_COOLDOWN,
            FAIL_REQUIREMENTS,
            FAIL_STATUS,
            FAIL_LINE_OF_SIGHT,
        }

        public enum ID : ushort //support >255 just in case
        {
            UNDEFINED,
            CLERIC_ACTIVE_HEAL,
            CLERIC_ACTIVE_SANCTUARY,
            CLERIC_ACTIVE_DIVINE_INTERVENTION,
            CLERIC_ACTIVE_PARAGON,

            //add here

            NUMBER_OF_IDs //leave this last
        }

        /* ~~~~~~~~~~~~ Initialize Ability List ~~~~~~~~~~~~ */
        public static Dictionary<ID, Ability> AbilityLookup = new Dictionary<ID, Ability>((int)ID.NUMBER_OF_IDs);
        public static void Initialize()
        {
            //cleric actives
            AbilityLookup.Add(ID.CLERIC_ACTIVE_HEAL, new CLERIC_ACTIVE_HEAL());
            AbilityLookup.Add(ID.CLERIC_ACTIVE_SANCTUARY, new CLERIC_ACTIVE_SANCTUARY());
            AbilityLookup.Add(ID.CLERIC_ACTIVE_DIVINE_INTERVENTION, new CLERIC_ACTIVE_DIVINE_INTERVENTION());
            AbilityLookup.Add(ID.CLERIC_ACTIVE_PARAGON, new CLERIC_ACTIVE_PARAGON());
        }

        /* ~~~~~~~~~~~~ Abilities ~~~~~~~~~~~~ */
        public class CLERIC_ACTIVE_HEAL : Ability
        {
            public CLERIC_ACTIVE_HEAL()
            {
                name = "Heal";
                name_short = "Heal";
                description = "";
                cost_mana_percent = 0.35f;
                cooldown_seconds = 3;
            }
        }

        public class CLERIC_ACTIVE_SANCTUARY : Ability
        {
            public CLERIC_ACTIVE_SANCTUARY()
            {
                name = "Sanctuary";
                name_short = "Sanc";
                description = "";
                cost_mana_percent = 0.90f;
                cooldown_seconds = 120;
            }
        }

        public class CLERIC_ACTIVE_DIVINE_INTERVENTION : Ability
        {
            public CLERIC_ACTIVE_DIVINE_INTERVENTION()
            {
                name = "Divine Intervention";
                name_short = "DI";
                description = "";
                cost_mana_percent = 0.50f;
                cooldown_seconds = 20;
            }
        }

        public class CLERIC_ACTIVE_PARAGON : Ability
        {
            public CLERIC_ACTIVE_PARAGON()
            {
                name = "Paragon";
                name_short = "Para";
                description = "";
                cost_mana_percent = 0.50f;
                cooldown_seconds = 300;
            }
        }

        /* ~~~~~~~~~~~~ Ability Template ~~~~~~~~~~~~ */
        public abstract class Ability
        {
            //descriptives
            protected static string name = "undefined";
            protected static string name_short = "undefined";
            protected static string description = "undefined";

            //coodlown tracking
            protected static bool cooldown_active = false;
            protected static DateTime cooldown_time_end = DateTime.MinValue;

            //costs
            protected static int cost_mana_base = 0;
            protected static float cost_mana_percent = 0f;
            protected static float cost_mana_reduction_cap = 0.8f;
            protected static double cooldown_seconds = 0;
            protected static bool requires_sight_cursor = false;

            //on-use effects
            protected static bool visual_do = false;
            protected static Color visual_colour = default(Color);
            protected static double prevent_weapon_milliseconds = 400;

            //encapsulate whatever needs external access
            protected static bool OnCooldown { get => cooldown_active; set => cooldown_active = value; }
            protected static DateTime TimeOffCooldown { get => cooldown_time_end; set => cooldown_time_end = value; }

            public Return Use()
            {
                //pre-checks
                Return return_value = UseChecks();
                if (return_value != Return.SUCCESS) return return_value;

                //do effect (override UseEffects)
                return_value = UseEffects();
                if (return_value != Return.SUCCESS) return return_value;

                //on-use effects
                if (visual_do) DoVisual(Main.LocalPlayer);
                if (prevent_weapon_milliseconds > 0) ExperienceAndClasses.localMyPlayer.PreventItemUse(prevent_weapon_milliseconds);

                //take costs
                return_value = UseCosts();
                return return_value;
            }

            protected Return UseChecks()
            {
                return Return.SUCCESS;
            }

            protected virtual Return UseEffects()
            {
                return Return.SUCCESS;
            }

            protected virtual Return UseCosts()
            {
                return Return.SUCCESS;
            }

            public int GetManaCost(int level = 1)
            {
                int manaCost = (int)((cost_mana_base + (cost_mana_percent * Main.LocalPlayer.statManaMax2)) * Main.LocalPlayer.manaCost);

                if (manaCost < 0) manaCost = 0;
                if (manaCost > Main.LocalPlayer.statManaMax2) manaCost = Main.LocalPlayer.statManaMax2;

                return manaCost;
            }

            public float GetCooldownSecs(int level = 1)
            {
                return 0f;
            }

            protected void DoVisual(Player player)
            {
                //maybe use a projectile instead for easy syncing
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, ExperienceAndClasses.mod.DustType("Dust_AbilityGeneric"), Main.rand.NextFloat(-5, +5), Main.rand.NextFloat(-5, +5), 150, ExperienceAndClasses.MESSAGE_COLOUR_YELLOW);
                    Main.playerDrawDust.Add(dust);
                }
            }

        }



        //public static int CalculateManaCost(MyPlayer myPlayer, int abilityID, int level = 1)
        //{
        //    //base cost
        //    Player player = myPlayer.player;
        //    int manaCost = (int)((MANA_COST[abilityID] + (player.statManaMax2 * MANA_COST_PERCENT[abilityID])) * player.manaCost);

        //    //any ability-specific adjustments go here

        //    //limits
        //    if (manaCost < 0) manaCost = 0;
        //    if (manaCost > player.statManaMax2) manaCost = player.statManaMax2;

        //    //return
        //    return manaCost;
        //}

        //public static float CalculateCooldownSecs(MyPlayer myPlayer, int abilityID, int level = 1)
        //{
        //    //base cost
        //    float cooldownSecs = COOLDOWN_SECS[abilityID];

        //    //any ability-specific adjustments go here

        //    //return
        //    return cooldownSecs;
        //}

        ///// <summary>
        ///// This is called ONLY by the client using the ability
        ///// </summary>
        ///// <param name="myPlayer"></param>
        ///// <param name="abilityID"></param>
        ///// <param name="level"></param>
        ///// <returns></returns>
        //public static int DoAbility(MyPlayer myPlayer, int abilityID, int level, bool alternateEffect)
        //{
        //    //check if ID is potentially valid
        //    if ((abilityID >= NUMBER_OF_IDs) || (abilityID < 0))
        //        return RETURN_FAIL_UNDEFINED;

        //    /* ~~~~~~~~~~~~ Setup ~~~~~~~~~~~~ */
        //    Player player = myPlayer.player;
        //    long timeNow = DateTime.Now.Ticks;
        //    long timeAllow = myPlayer.abilityCooldowns[abilityID];
        //    int manaCost = CalculateManaCost(myPlayer, abilityID, level);
        //    float cooldownSecs = CalculateCooldownSecs(myPlayer, abilityID, level);
        //    double attackDelay = ATTACK_DELAY[abilityID];

        //    Vector2 myPosition = player.position;

        //    /* ~~~~~~~~~~~~ Generic Checks ~~~~~~~~~~~~ */

        //    //don't allow server calls or calls from other clients
        //    if (Main.netMode == 2 || !Main.LocalPlayer.Equals(player)) return RETURN_FAIL_EXTERNAL;

        //    //check for invalid statuses
        //    if (player.frozen || player.dead) return RETURN_FAIL_STATUS;

        //    //check mana cost
        //    if (player.statMana < manaCost) return RETURN_FAIL_MANA;

        //    //check cooldown
        //    if (timeNow < timeAllow) return RETURN_FAIL_COOLDOWN;

        //    /* ~~~~~~~~~~~~ Ability-Specific Checks ~~~~~~~~~~~~ */



        //    /* ~~~~~~~~~~~~ Ability-Specific Effects ~~~~~~~~~~~~ */
        //    //keep in mind that all clients will execute this!
        //    switch (abilityID)
        //    {
        //        //case ID_CLERIC_ACTIVE_HEAL:
        //        //    // RadiusEffect(myPosition, true, true, true, false, false, 1000f, level * 2, 0.5f);
        //        //    // RadiusEffect(myPosition, false, false, false, true, true, 1000f, 0, 0, 0, 0, 10f);
        //        //    //Projectile.NewProjectile(player.position.X, player.position.Y, 0, 0, ProjectileID.LostSoulFriendly, level * 2, 0, player.whoAmI);
        //        //    break;

        //        //case ID_CLERIC_ACTIVE_SANCTUARY:
        //        //    break;

        //        //case ID_CLERIC_ACTIVE_DIVINE_INTERVENTION:
        //        //    break;

        //        //case ID_CLERIC_ACTIVE_RESSURECTION:
        //        //    break;




        //        default:
        //            //undefined ability

        //            //return RETURN_FAIL_UNDEFINED;
        //            Main.NewText(NAME[abilityID] + " Placeholder", ExperienceAndClasses.MESSAGE_COLOUR_RED);
        //            CombatText.NewText(player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_RED, NAME[abilityID] + " Placeholder");
        //            break;
        //    }

        //    /* ~~~~~~~~~~~~ Costs ~~~~~~~~~~~~ */
        //    player.statMana -= manaCost;
        //    if (player.statMana < 0)
        //    {
        //        player.statMana = 0;
        //    }
        //    if (cooldownSecs > 0) ON_COOLDOWN[abilityID] = true;
        //    myPlayer.abilityCooldowns[abilityID] = timeNow + (long)(cooldownSecs * TimeSpan.TicksPerSecond);

        //    /* ~~~~~~~~~~~~ Animation ~~~~~~~~~~~~ */
        //    if (attackDelay > 0)
        //        myPlayer.PreventItemUse(attackDelay);

        //    /* ~~~~~~~~~~~~ Success ~~~~~~~~~~~~ */
        //    return RETURN_SUCCESS;
        //}

        ///// <summary>
        ///// Applies effect in radius self. Can apply buffs, healing, or damage.
        ///// </summary>
        //public static void RadiusEffect(Vector2 center, bool affectSelf, bool affectPlayerFriendly, bool affectNPCFriendly, bool affectPlayerHostile, bool affectNPCHostile, float radius = 1000f,
        //    float healAmount = 0, float selfHealMultiplier = 1f, float manaAmount = 0, float selfManaMultiplier = 1f, float damageAmount = 0, int buffID = -1, int buffDurationTicks = 0, bool requireLineOfSight = false)
        //{
        //    //return if there is nothing to do
        //    if (healAmount == 0 && damageAmount == 0 && buffID == -1) return;
        //    if (!affectPlayerFriendly && !affectPlayerHostile && !affectNPCFriendly && !affectNPCHostile) return;

        //    //cast
        //    int heal = (int)healAmount;
        //    int healSelf = (int)(healAmount * selfHealMultiplier);
        //    int damage = (int)damageAmount;
        //    int mana = (int)manaAmount;
        //    int manaSelf = (int)(manaAmount * selfManaMultiplier);

        //    //inits
        //    Player sourcePlayer = Main.LocalPlayer;
        //    int selfIndex = sourcePlayer.whoAmI;
        //    int amount, lifeCurrent = 0, lifeMax = 0, manaCurrent = 0, manaMax = 0;
        //    Player player = null;
        //    NPC npc = null;
        //    bool forPlayer = true; //if false, then for npc

        //    //action to apply affects
        //    var apply = new Action(() => {
        //        //buff
        //        if (buffID >= 0 && buffDurationTicks > 0)
        //        {
        //            if (forPlayer && Main.LocalPlayer.Equals(player))
        //                player.AddBuff(buffID, buffDurationTicks);
        //            else if (Main.LocalPlayer.Equals(player))
        //                npc.AddBuff(buffID, buffDurationTicks);
        //        }

        //        //heal
        //        if (heal > 0)
        //        {
        //            if (forPlayer && player.Equals(sourcePlayer))
        //                amount = healSelf;
        //            else
        //                amount = heal;

        //            if (forPlayer)
        //            {
        //                lifeCurrent = player.statLife;
        //                lifeMax = player.statLifeMax2;
        //            }
        //            else
        //            {
        //                lifeCurrent = npc.life;
        //                lifeMax = npc.lifeMax;
        //            }


        //            if ((lifeMax - lifeCurrent) < amount) amount = lifeMax - lifeCurrent;

        //            if (amount > 0)
        //            {
        //                if (forPlayer)
        //                {
        //                    player.statLife += amount;
        //                    player.HealEffect(amount);
        //                    if ((Main.netMode == 1) && (player.whoAmI != selfIndex))
        //                    {
        //                        NetMessage.SendData(MessageID.HealEffect, -1, selfIndex, null, player.whoAmI, (float)amount, 0.0f, 0.0f);
        //                    }
        //                }
        //                else
        //                {
        //                    npc.HealEffect(amount);
        //                    npc.life += amount;
        //                }
        //            }
        //        }

        //        //mana
        //        if (mana > 0 && forPlayer)
        //        {
        //            if (player.Equals(sourcePlayer))
        //                amount = manaSelf;
        //            else
        //                amount = mana;

        //            manaCurrent = player.statMana;
        //            manaMax = player.statManaMax2;

        //            if ((manaMax - manaCurrent) < amount) amount = manaMax - manaCurrent;

        //            if (amount > 0 && Main.LocalPlayer.Equals(player))
        //            {
        //                player.ManaEffect(amount);
        //                player.statMana += amount;
        //            }
        //        }

        //        //damage
        //        if (damage > 0)
        //        {
        //            //TO DO: needs to send net message if npc is killed
        //            if (forPlayer)
        //                player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByPlayer(sourcePlayer.whoAmI), damage, 0, true);
        //            else
        //            {
        //                if (Main.netMode == 1)
        //                {
        //                    NetMessage.SendData(MessageID.StrikeNPC, -1, -1, null, npc.whoAmI, damage, 0f, 0);
        //                }
        //                else
        //                {
        //                    sourcePlayer.ApplyDamageToNPC(npc, damage, 0, 0, false); //multiplayer sync would be automatic, but npc death does not sync
        //                }
        //            }
        //        }
        //    });

        //    //cycle players
        //    forPlayer = true;
        //    if (affectSelf || affectPlayerFriendly || affectPlayerHostile)
        //    {

        //        //optimize self-only
        //        int indexMin, indexMax;
        //        if (affectSelf && !affectPlayerFriendly && !affectPlayerHostile)
        //        {
        //            indexMin = indexMax = sourcePlayer.whoAmI;
        //        }
        //        else
        //        {
        //            indexMin = 0;
        //            indexMax = 255;
        //        }

        //        //loop
        //        bool friendlyTeam = false;
        //        bool bothHostile = false;
        //        bool isSelf = false;
        //        for (int playerIndex = indexMin; playerIndex <= indexMax; playerIndex++)
        //        {
        //            player = Main.player[playerIndex];

        //            if (requireLineOfSight && !Collision.CanHit(sourcePlayer.position, 0, 0, player.position, player.width, player.height))
        //                continue;

        //            if (sourcePlayer.team != 0 && player.team == sourcePlayer.team)
        //                friendlyTeam = true;
        //            else
        //                friendlyTeam = false;

        //            if (sourcePlayer.hostile && player.hostile)
        //                bothHostile = true;
        //            else
        //                bothHostile = false;

        //            isSelf = player.Equals(sourcePlayer);

        //            if (player.active && player.Distance(center) < radius && ((!isSelf && affectPlayerHostile && bothHostile && !friendlyTeam) || (!isSelf && affectPlayerFriendly && (!bothHostile || friendlyTeam)) || (isSelf && affectSelf)))
        //            {
        //                apply();
        //            }
        //        }
        //    }

        //    //cycle npc
        //    forPlayer = false;
        //    NPC[] npcs = Main.npc;
        //    if (affectNPCFriendly || affectNPCHostile)
        //    {
        //        for (int npc_index = 0; npc_index < npcs.Length; npc_index++)
        //        {
        //            npc = npcs[npc_index];

        //            if (requireLineOfSight && !Collision.CanHit(sourcePlayer.position, 0, 0, npc.position, npc.width, npc.height))
        //                continue;

        //            if (npc.active && npc.Distance(center) < radius && npc.lifeMax > 5 && ((npc.friendly && affectNPCFriendly) || (!npc.friendly && affectNPCHostile)))
        //            {
        //                apply();
        //            }
        //        }
        //    }
        //}

        //public static void SendReturnMessage(int returnValue, int abilityID)
        //{
        //    string abilityName = NAME[abilityID];
        //    switch (returnValue)
        //    {
        //        case Abilities.RETURN_FAIL_MANA:
        //            Main.NewText(abilityName + ": " + Abilities.MESSAGE_FAIL_MANA, ExperienceAndClasses.MESSAGE_COLOUR_RED, true);
        //            break;
        //        case Abilities.RETURN_FAIL_COOLDOWN:
        //            Main.NewText(abilityName + ": " + Abilities.MESSAGE_FAIL_COOLDOWN, ExperienceAndClasses.MESSAGE_COLOUR_RED, true);
        //            break;
        //        case Abilities.RETURN_FAIL_STATUS:
        //        case Abilities.RETURN_FAIL_REQUIREMENTS:
        //            Main.NewText(abilityName + ": " + Abilities.MESSAGE_FAIL_GENERIC, ExperienceAndClasses.MESSAGE_COLOUR_RED, true);
        //            break;
        //        default:
        //            //no message
        //            break;
        //    }
        //}
    }
}
