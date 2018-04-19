using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses
{
    class Abilities
    {
        public const int RETURN_SUCCESS = 1;
        public const int RETURN_FAIL_UNDEFINED = -1;
        public const int RETURN_FAIL_STATUS = -2;
        public const int RETURN_FAIL_MANA = -3;
        public const int RETURN_FAIL_COOLDOWN = -4;
        public const int RETURN_FAIL_REQUIREMENTS = -5;
        public const int RETURN_FAIL_REDUNDANT = -6;

        public const string MESSAGE_FAIL_MANA = "insufficient mana!";
        public const string MESSAGE_FAIL_COOLDOWN = "not yet ready!";
        public const string MESSAGE_FAIL_GENERIC = "cannot use at this time!";

        //IDs must be zero or positive (except for the undefined ID)
        public const int ID_UNDEFINED = -1;
        public const int ID_CLERIC_ACTIVE_HEAL = 0;
        public const int ID_CLERIC_ACTIVE_SANCTUARY = 1;
        public const int ID_CLERIC_ACTIVE_DIVINE_INTERVENTION = 2;
        public const int ID_CLERIC_ACTIVE_RESSURECTION = 3;

        public const int NUMBER_OF_IDs = 4; //don't forget to update this
        public static string[] NAME = new string[NUMBER_OF_IDs];
        public static string[] DESCRIPTION = new string[NUMBER_OF_IDs];
        public static int[] LEVEL_REQUIREMENT = new int[NUMBER_OF_IDs];
        public static float[] MANA_COST = new float[NUMBER_OF_IDs];
        public static float[] MANA_COST_PERCENT = new float[NUMBER_OF_IDs];
        public static float[] COOLDOWN_SECS = new float[NUMBER_OF_IDs];

        /* ~~~~~~~~~~~~ Ability Values ~~~~~~~~~~~~ */
        public static void Initialize()
        {
            int id;

            id = ID_CLERIC_ACTIVE_HEAL; //resize with level if graphic can be resized
            NAME[id] = "Heal";
            DESCRIPTION[id] = "placeholder";
            LEVEL_REQUIREMENT[id] = 10;
            MANA_COST_PERCENT[id] = 0.10f;
            COOLDOWN_SECS[id] = 0.5f;

            id = ID_CLERIC_ACTIVE_SANCTUARY;
            NAME[id] = "Sanctuary";
            DESCRIPTION[id] = "placeholder";
            LEVEL_REQUIREMENT[id] = 15;
            MANA_COST_PERCENT[id] = 0.90f;
            COOLDOWN_SECS[id] = 120f;

            id = ID_CLERIC_ACTIVE_DIVINE_INTERVENTION; //1sec dur, more if Cross necklace (longInvince???)
            NAME[id] = "Divine Intervention";
            DESCRIPTION[id] = "placeholder";
            LEVEL_REQUIREMENT[id] = 20;
            MANA_COST_PERCENT[id] = 0.50f;
            COOLDOWN_SECS[id] = 20f;

            id = ID_CLERIC_ACTIVE_RESSURECTION;
            NAME[id] = "Ressurection";
            DESCRIPTION[id] = "placeholder";
            LEVEL_REQUIREMENT[id] = 30;
            MANA_COST_PERCENT[id] = 0.90f;
            COOLDOWN_SECS[id] = 10f;
        }

        public static int CalculateManaCost(MyPlayer myPlayer, int abilityID, int level = 1)
        {
            //base cost
            Player player = myPlayer.player;
            int manaCost = (int)((MANA_COST[abilityID] + (player.statManaMax2 * MANA_COST_PERCENT[abilityID])) * player.manaCost);

            //any ability-specific adjustments go here

            //limits
            if (manaCost < 0) manaCost = 0;
            if (manaCost > player.statManaMax2) manaCost = player.statManaMax2;

            //return
            return manaCost;
        }

        public static float CalculateCooldownSecs(MyPlayer myPlayer, int abilityID, int level = 1)
        {
            //base cost
            float cooldownSecs = COOLDOWN_SECS[abilityID];

            //any ability-specific adjustments go here

            //return
            return cooldownSecs;
        }

        /// <summary>
        /// This is called ONLY by the client using the ability
        /// </summary>
        /// <param name="myPlayer"></param>
        /// <param name="abilityID"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int DoAbility(MyPlayer myPlayer, int abilityID, int level = 1)
        {
            //check if ID is potentially valid
            if ((abilityID >= NUMBER_OF_IDs) || (abilityID < 0))
                return RETURN_FAIL_UNDEFINED;

            /* ~~~~~~~~~~~~ Setup ~~~~~~~~~~~~ */
            Player player = myPlayer.player;
            long timeNow = DateTime.Now.Ticks;
            long timeAllow = myPlayer.abilityCooldowns[abilityID];
            int manaCost = CalculateManaCost(myPlayer, abilityID, level);
            float cooldownSecs = CalculateCooldownSecs(myPlayer, abilityID, level);

            Vector2 myPosition = player.position;

            /* ~~~~~~~~~~~~ Generic Checks ~~~~~~~~~~~~ */

            //check for invalid statuses
            if (player.frozen || player.dead) return RETURN_FAIL_STATUS;

            //check mana cost
            if (player.statMana < manaCost) return RETURN_FAIL_MANA;

            //check cooldown
            if (timeNow < timeAllow) return RETURN_FAIL_COOLDOWN;

            /* ~~~~~~~~~~~~ Ability-Specific Checks ~~~~~~~~~~~~ */



            /* ~~~~~~~~~~~~ Ability-Specific Effects ~~~~~~~~~~~~ */
            //keep in mind that all clients will execute this!
            switch (abilityID)
            {
                case ID_CLERIC_ACTIVE_HEAL:
                    // RadiusEffect(myPosition, true, true, true, false, false, 1000f, level * 2, 0.5f);
                    // RadiusEffect(myPosition, false, false, false, true, true, 1000f, 0, 0, 0, 0, 10f);
                    Projectile.NewProjectile(player.position.X, player.position.Y, 0, 0, ProjectileID.LostSoulFriendly, level * 2, 0, player.whoAmI);
                    break;





                default:
                    //undefined ability
                    return RETURN_FAIL_UNDEFINED;
            }

            /* ~~~~~~~~~~~~ Costs ~~~~~~~~~~~~ */
            player.statMana -= manaCost;
            if (player.statMana < 0)
            {
                player.statMana = 0;
            }

            myPlayer.abilityCooldowns[abilityID] = timeNow + (long)(cooldownSecs * TimeSpan.TicksPerSecond);

            /* ~~~~~~~~~~~~ Success ~~~~~~~~~~~~ */
            return RETURN_SUCCESS;
        }

        /// <summary>
        /// Applies effect in radius self. Can apply buffs, healing, or damage.
        /// </summary>
        public static void RadiusEffect(Vector2 center, bool affectSelf, bool affectPlayerFriendly, bool affectNPCFriendly, bool affectPlayerHostile, bool affectNPCHostile, float radius = 1000f,
            float healAmount = 0, float selfHealMultiplier = 1f, float manaAmount = 0, float selfManaMultiplier = 1f, float damageAmount = 0, int buffID = -1, int buffDurationTicks = 0)
        {
            //return if there is nothing to do
            if (healAmount == 0 && damageAmount == 0 && buffID == -1) return;
            if (!affectPlayerFriendly && !affectPlayerHostile && !affectNPCFriendly && !affectNPCHostile) return;

            //cast
            int heal = (int)healAmount;
            int healSelf = (int)(healAmount * selfHealMultiplier);
            int damage = (int)damageAmount;
            int mana = (int)manaAmount;
            int manaSelf = (int)(manaAmount * selfManaMultiplier);

            //inits
            Player sourcePlayer = Main.LocalPlayer;
            int selfIndex = sourcePlayer.whoAmI;
            int amount, lifeCurrent = 0, lifeMax = 0, manaCurrent = 0, manaMax = 0;
            Player player = null;
            NPC npc = null;
            bool forPlayer = true; //if false, then for npc

            //action to apply affects
            var apply = new Action(() => {
                //buff
                if (buffID >= 0 && buffDurationTicks > 0)
                {
                    if (forPlayer && Main.LocalPlayer.Equals(player))
                        player.AddBuff(buffID, buffDurationTicks);
                    else if (Main.LocalPlayer.Equals(player))
                        npc.AddBuff(buffID, buffDurationTicks);
                }

                //heal
                if (heal > 0)
                {
                    if (forPlayer && player.Equals(sourcePlayer))
                        amount = healSelf;
                    else
                        amount = heal;

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
                            player.statLife += amount;
                            player.HealEffect(amount);
                            if ((Main.netMode == 1) && (player.whoAmI != selfIndex))
                            {
                                NetMessage.SendData(MessageID.HealEffect, -1, selfIndex, null, player.whoAmI, (float)amount, 0.0f, 0.0f);
                            }
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
                    if (player.Equals(sourcePlayer))
                        amount = manaSelf;
                    else
                        amount = mana;

                    manaCurrent = player.statMana;
                    manaMax = player.statManaMax2;

                    if ((manaMax - manaCurrent) < amount) amount = manaMax - manaCurrent;

                    if (amount > 0 && Main.LocalPlayer.Equals(player))
                    {
                        player.ManaEffect(amount);
                        player.statMana += amount;
                    }
                }

                //damage
                if (damage > 0)
                {
                    //TO DO: needs to send net message if npc is killed
                    if (forPlayer)
                        player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByPlayer(sourcePlayer.whoAmI), damage, 0, true);
                    else
                    {
                        if (Main.netMode == 1)
                        {
                            NetMessage.SendData(MessageID.StrikeNPC, -1, -1, null, npc.whoAmI, damage, 0f, 0);
                        }
                        else
                        {
                            sourcePlayer.ApplyDamageToNPC(npc, damage, 0, 0, false); //multiplayer sync would be automatic, but npc death does not sync
                        }
                    }
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
                    indexMin = indexMax = sourcePlayer.whoAmI;
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

                    if (!Collision.CanHit(sourcePlayer.position, 0, 0, player.position, player.width, player.height))
                        continue;

                    if (sourcePlayer.team != 0 && player.team == sourcePlayer.team)
                        friendlyTeam = true;
                    else
                        friendlyTeam = false;

                    if (sourcePlayer.hostile && player.hostile)
                        bothHostile = true;
                    else
                        bothHostile = false;

                    isSelf = player.Equals(sourcePlayer);

                    if (player.active && player.Distance(center) < radius && ((!isSelf && affectPlayerHostile && bothHostile && !friendlyTeam) || (!isSelf && affectPlayerFriendly && (!bothHostile || friendlyTeam)) || (isSelf && affectSelf)))
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

                    if (!Collision.CanHit(sourcePlayer.position, 0, 0, npc.position, npc.width, npc.height))
                        continue;

                    if (npc.active && npc.Distance(center) < radius && npc.lifeMax > 5 && ((npc.friendly && affectNPCFriendly) || (!npc.friendly && affectNPCHostile)))
                    {
                        apply();
                    }
                }
            }
        }

        public static void DoReturnMessage(int returnValue, int abilityID)
        {
            string abilityName = NAME[abilityID];
            switch (returnValue)
            {
                case Abilities.RETURN_FAIL_MANA:
                    Main.NewText(abilityName + ": " + Abilities.MESSAGE_FAIL_MANA, ExperienceAndClasses.MESSAGE_COLOUR_RED, true);
                    break;
                case Abilities.RETURN_FAIL_COOLDOWN:
                    Main.NewText(abilityName + ": " + Abilities.MESSAGE_FAIL_COOLDOWN, ExperienceAndClasses.MESSAGE_COLOUR_RED, true);
                    break;
                case Abilities.RETURN_FAIL_STATUS:
                case Abilities.RETURN_FAIL_REQUIREMENTS:
                    Main.NewText(abilityName + ": " + Abilities.MESSAGE_FAIL_GENERIC, ExperienceAndClasses.MESSAGE_COLOUR_RED, true);
                    break;
                default:
                    //no message
                    break;
            }
        }
    }
}
