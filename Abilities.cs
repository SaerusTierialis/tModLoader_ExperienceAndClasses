using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses
{
    public class Abilities
    {
        /* ~~~~~~~~~~~~ Cosntants ~~~~~~~~~~~~ */
        protected const int ACTIVE_PREVENT_ATTACK_MILLISECONDS = 400;
        protected static readonly Color COLOUR_SUPPORT = new Color(255, 255, 100); //new Color(239, 239, 239);

        /* ~~~~~~~~~~~~ Values ~~~~~~~~~~~~ */
        public enum RETURN : byte
        {
            UNUSED,
            SUCCESS,
            FAIL_NOT_IMPLEMENTRD,
            FAIL_EXTERNAL_CALL,
            FAIL_MANA,
            FAIL_COOLDOWN,
            FAIL_REQUIREMENTS,
            FAIL_STATUS,
            FAIL_LINE_OF_SIGHT,
        }

        public enum ABILITY_TYPE : byte
        {
            NOT_IMPLEMENTED,
            ACTIVE,
            UPGRADE,
            PASSIVE,
        }

        public enum ID : ushort //order does not matter except where specified
        {
            UNDEFINED, //leave this first

            Cleric_Active_Heal,
            Cleric_Active_Sanctuary,
            Cleric_Active_DivineIntervention,
            Cleric_Active_Paragon,

            //when adding here, make that that a class of the same name is added below

            NUMBER_OF_IDs, //leave this last
        }

        /* ~~~~~~~~~~~~ List of Abilities ~~~~~~~~~~~~ */
        //contains the one and only instance of each ability
        //not actually a "list" but an ID-indexed array
        //auto-populates from ID
        //will CRASH on mod startup if there is no corresponding class for an ID
        //using this list is a pretty cumbersome design choice but it allowed for various efficiencies

        public static Ability[] AbilityLookup = new Ability[(int)ID.NUMBER_OF_IDs];
        public static void Initialize()
        {
            string[] IDs = Enum.GetNames(typeof(ID));
            for (byte i = 0; i < AbilityLookup.Length; i++)
            {
                AbilityLookup[i] = (Ability)(Assembly.GetExecutingAssembly().CreateInstance(typeof(Abilities).FullName + "+" + IDs[i]));
            }
        }

        /* ~~~~~~~~~~~~ Abilities (includes all active, upgrade, passive, proc, etc) ~~~~~~~~~~~~ */
        //singleton implementation

        public class Cleric_Active_Heal : Ability
        {
            public Cleric_Active_Heal()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Heal";
                name_short = "Heal";
                description = "";
                cost_mana_percent = 0.35f;
                cooldown_seconds = 3;
                dust_colour = COLOUR_SUPPORT;
            }
            protected override RETURN UseEffects()
            {
                return RETURN.SUCCESS;
            }
        }

        public class Cleric_Active_Sanctuary : Ability
        {
            public Cleric_Active_Sanctuary()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Sanctuary";
                name_short = "Sanc";
                description = "";
                cost_mana_percent = 0.90f;
                cooldown_seconds = 120;
                dust_colour = COLOUR_SUPPORT;
            }
        }

        public class Cleric_Active_DivineIntervention : Ability
        {
            public Cleric_Active_DivineIntervention()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Divine Intervention";
                name_short = "DI";
                description = "";
                cost_mana_percent = 0.50f;
                cooldown_seconds = 20;
                dust_colour = COLOUR_SUPPORT;
            }
        }

        public class Cleric_Active_Paragon : Ability
        {
            public Cleric_Active_Paragon()
            {
                ability_type = ABILITY_TYPE.ACTIVE;
                name = "Paragon";
                name_short = "Para";
                description = "";
                cost_mana_percent = 0.50f;
                cooldown_seconds = 300;
                dust_colour = COLOUR_SUPPORT;
            }
        }

        public class UNDEFINED : Ability { }

        /* ~~~~~~~~~~~~ Ability Abstract ~~~~~~~~~~~~ */
        //singleton implementation
        //nothing should be static in here

        public abstract class Ability
        {
            //type of ability
            protected ABILITY_TYPE ability_type = ABILITY_TYPE.NOT_IMPLEMENTED;

            //toggle on for constant passives for efficiency
            protected bool skip_checks_and_costs = false;

            //descriptives
            protected string name = "undefined";
            protected string name_short = "undefined";
            protected string description = "undefined";

            //coodlown tracking
            protected bool cooldown_active = false;
            protected DateTime cooldown_time_end = DateTime.MinValue;

            //costs
            protected int cost_mana_base = 0;
            protected float cost_mana_percent = 0f;
            protected float cost_mana_reduction_cap = 0.8f;
            protected double cooldown_seconds = 0;
            protected bool requires_sight_cursor = false;

            //on-use effects
            protected Color dust_colour = default(Color);
            protected bool active_prevents_attack = true;

            //encapsulate whatever needs external access (better formats wouldn't compile in tModLoader - exit code 1)
            public string GetName()
            {
                return name;
            }
            public string GetNameShort()
            {
                return name_short;
            }
            public bool OnCooldown(bool changeValue=false, bool newValue=false)
            {
                if (changeValue)
                    cooldown_active = newValue;
                return cooldown_active;
            }

            public RETURN Use(byte level=1, bool alternate=false)
            {
                //not to be used by server, all abilities are client-side
                if (Main.netMode == 2) return RETURN.FAIL_EXTERNAL_CALL;

                //store outcome
                RETURN return_value;

                //pre-checks
                if (!skip_checks_and_costs)
                {
                    return_value = UseChecks(level, alternate);
                    if (return_value != RETURN.SUCCESS) return return_value;
                }

                //do effect (override UseEffects)
                return_value = UseEffects();
                if (return_value != RETURN.SUCCESS) return return_value;

                //active on-use effects
                if (IsTypeActive())
                {
                    CastDust();
                    if (active_prevents_attack)
                        ExperienceAndClasses.localMyPlayer.PreventItemUse(ACTIVE_PREVENT_ATTACK_MILLISECONDS);
                }

                //take costs
                if (!skip_checks_and_costs)
                {
                    return_value = UseCosts();
                }

                //return final result
                return return_value;
            }

            protected virtual RETURN UseChecks(byte level=1, bool alternate=false)
            {
                //check for invalid statuses
                if (Main.LocalPlayer.frozen || Main.LocalPlayer.dead) return RETURN.FAIL_STATUS;

                //check mana cost
                if (Main.LocalPlayer.statMana < GetManaCost(level, alternate)) return RETURN.FAIL_MANA;

                //check cooldown
                if (cooldown_active) return RETURN.FAIL_COOLDOWN;

                return RETURN.SUCCESS;
            }

            protected virtual RETURN UseEffects()
            {
                return RETURN.FAIL_NOT_IMPLEMENTRD;
            }

            protected virtual RETURN UseCosts(byte level = 1, bool alternate = false)
            {
                Main.LocalPlayer.statMana -= GetManaCost(level, alternate);
                if (Main.LocalPlayer.statMana < 0)
                {
                    Main.LocalPlayer.statMana = 0;
                }
                double cd = GetCooldownSecs(level);
                if (cd > 0)
                {
                    cooldown_active = true;
                    cooldown_time_end = DateTime.Now.AddSeconds(cd);
                }
                return RETURN.SUCCESS;
            }

            public int GetManaCost(byte level = 1, bool alternate = false)
            {
                int manaCost = (int)((cost_mana_base + (cost_mana_percent * Main.LocalPlayer.statManaMax2)) * Main.LocalPlayer.manaCost); //apply cost_mana_reduction_cap

                if (manaCost < 0) manaCost = 0;
                if (manaCost > Main.LocalPlayer.statManaMax2) manaCost = Main.LocalPlayer.statManaMax2;

                return manaCost;
            }

            public virtual double GetCooldownSecs(byte level = 1)
            {
                return cooldown_seconds;
            }

            public float GetCooldownRemainingSeconds()
            {
                return (float)(cooldown_time_end.Subtract(DateTime.Now).TotalMilliseconds) / 1000;
            }

            protected void CastDust()
            {
                //maybe use a projectile instead for easy syncing
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(Main.LocalPlayer.position, Main.LocalPlayer.width, Main.LocalPlayer.height, ExperienceAndClasses.mod.DustType("Dust_AbilityGeneric"), Main.rand.NextFloat(-5, +5), Main.rand.NextFloat(-5, +5), 150, dust_colour);
                    Main.playerDrawDust.Add(dust);
                }
            }

            public virtual string CooldownUI(byte level, out float percent)
            {
                //calculate cd time remaining
                double timeRemain = GetCooldownRemainingSeconds();
                if (timeRemain < 0)
                    timeRemain = 0;

                //if time, set string
                string cooldownText = null;
                if (timeRemain > 0)
                    cooldownText = Math.Round(timeRemain, 1).ToString();

                //also calculate percentage complete
                double cd = GetCooldownSecs(level);
                percent = (float)((cd - timeRemain) / cd);

                return cooldownText;
            }

            public bool IsTypeActive()
            {
                return ability_type == ABILITY_TYPE.ACTIVE;
            }

        }

        //////    ////cleric actives
        //////    //AbilityLookup.Add(ID.CLERIC_ACTIVE_HEAL, new CLERIC_ACTIVE_HEAL());
        //////    //AbilityLookup.Add(ID.CLERIC_ACTIVE_SANCTUARY, new CLERIC_ACTIVE_SANCTUARY());
        //////    //AbilityLookup.Add(ID.CLERIC_ACTIVE_DIVINE_INTERVENTION, new CLERIC_ACTIVE_DIVINE_INTERVENTION());
        //////    //AbilityLookup.Add(ID.CLERIC_ACTIVE_PARAGON, new CLERIC_ACTIVE_PARAGON());
        //////}


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
