using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses.NPCs
{
    public class MyGlobalNPC : GlobalNPC
    {
        public const double EATER_13_MULT = 1.801792115;
        public const double EATER_14_MULT = 1.109713024;
        public const double EATER_15_MULT = 0.647725809;

        public override void NPCLoot(NPC npc) //bool CheckDead(NPC npc)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~ Checks ~~~~~~~~~~~~~~~~~~~~~~*/
            //singleplayer and server-side only
            //no exp or loot for critters, statues, friendlies
            if (Main.netMode != 1 && !npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue)
            {

                /*~~~~~~~~~~~~~~~~~~~~~~ Defaults ~~~~~~~~~~~~~~~~~~~~~~*/

                //exp rate
                double xp_mult = ExperienceAndClasses.worldExpModifier;

                //chances for orbs
                float chanceBossFixed = ExperienceAndClasses.PERCENT_CHANCE_BOSS_ORB_BASE;
                float chanceBossVariable = ExperienceAndClasses.PERCENT_CHANCE_BOSS_ORB_VARIABLE;
                float chanceMonster;
                if (Main.expertMode)
                {
                    chanceMonster = ExperienceAndClasses.PERCENT_CHANCE_ASCENSION_ORB_EXPERT;
                }
                else
                {
                    chanceMonster = ExperienceAndClasses.PERCENT_CHANCE_ASCENSION_ORB;
                }

                //boss
                bool treat_as_boss = npc.boss;
                double scale_xp_in_boss_orb_drop_calc = 1;

                /*~~~~~~~~~~~~~~~~~~~~~~ Modifier for special cases ~~~~~~~~~~~~~~~~~~~~~~*/

                //eater of worlds (make all parts worth same exp so kill order doesn't matter)
                if ((npc.netID >= 13) && (npc.netID <= 15))
                {
                    //treat all parts as a boss using the boss xp (x50) but at 1/50 final rate
                    treat_as_boss = true;
                    chanceBossFixed /= 50;
                    chanceBossVariable /= 50;
                    chanceMonster /= 50;
                    scale_xp_in_boss_orb_drop_calc = 50;
                    //adjust all segments to give similar xp so that kill order doesn't affect xp
                    switch (npc.netID)
                    {
                        case 13://head
                            xp_mult *= EATER_13_MULT;
                            break;
                        case 14://body
                            xp_mult *= EATER_14_MULT; 
                            break;
                        case 15://tail
                            xp_mult *= EATER_15_MULT; 
                            break;
                        default:
                            break;
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~ Prepare ~~~~~~~~~~~~~~~~~~~~~~*/

                //calculate base exp
                double experience = Helpers.CalcBaseExp(npc) * xp_mult;
                double experience_boss_orb_calc = experience * scale_xp_in_boss_orb_drop_calc;

                //declare
                Player player;
                MyPlayer myPlayer;
                double expGive;
                float chanceBoss;

                //store prior interactions
                bool[] interactionsBefore = npc.playerInteraction;

                //will have use interactions to give instanced orb drops (default to false, no drop)
                bool[] interactionsBossOrb = new bool[interactionsBefore.Length];
                bool[] interactionsMonsterOrb = new bool[interactionsBefore.Length];

                //default to nothing droped
                bool droppedBossOrb = false;
                bool droppedMonsterOrb = false;

                /*~~~~~~~~~~~~~~~~~~~~~~ Process for each player ~~~~~~~~~~~~~~~~~~~~~~*/
                for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                {
                    //select
                    if (Main.netMode == 0)
                    {
                        player = Main.LocalPlayer;
                    }
                    else
                    {
                        player = Main.player[playerIndex];
                    }
                    myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);

                    //only reward if not afk
                    if (!myPlayer.afk)
                    {
                        //check if qualified, any one of these is fine:
                        //1. npc is a boss or counts as one
                        //2. singleplayer
                        //3. player has interacted with npc (hit, been hit by, etc)
                        //4. player is nearby
                        if (treat_as_boss || (Main.netMode == 0) || npc.playerInteraction[playerIndex] || player.Distance(npc.position) <= ExperienceAndClasses.RANGE_EXP_AND_ASCENSION_ORB)
                        {
                            /*~~~~~~~~~~~~~~~~~~~~~~ xp ~~~~~~~~~~~~~~~~~~~~~~*/

                            //base
                            expGive = experience;

                            //10% bonus for well fed
                            if (player.wellFed) expGive *= 1.1;

                            //min 1 exp
                            if (expGive < 1) expGive = 1;

                            //round down
                            expGive = Math.Floor(expGive);

                            //exp
                            myPlayer.AddExp((int)expGive);

                            /*~~~~~~~~~~~~~~~~~~~~~~ monster orb ~~~~~~~~~~~~~~~~~~~~~~*/
                            if (Main.rand.Next(1000) < (int)(chanceMonster * 10))
                            {
                                interactionsMonsterOrb[playerIndex] = true;
                                droppedMonsterOrb = true;
                            }

                            /*~~~~~~~~~~~~~~~~~~~~~~ boss orb ~~~~~~~~~~~~~~~~~~~~~~*/
                            //chance = fixed + (variable * factor)
                            //factor = log10( 1 + ((xp / current boss orb value)^4) ) * 4    ***max 100%
                            //description, heavily weighted towards giving boss orbs when the boss is worth a similar or higher amount of xp (has a minimum chance)
                            chanceBoss = chanceBossFixed + (chanceBossVariable * Math.Min(1f, (float)Math.Log10(1 + Math.Pow(experience_boss_orb_calc / myPlayer.GetBossOrbXP(), 4)) * 4));
                            if (treat_as_boss && (Main.rand.Next(1000) < (int)(chanceBoss * 10)))
                            {
                                interactionsBossOrb[playerIndex] = true;
                                droppedBossOrb = true;
                            }

                        }
                    }

                    //single player doesn't need to iterate more than once
                    if (Main.netMode == 0) break;
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Instanced Loot~~~~~~~~~~~~~~~~~~~~~~*/

                //boss orb
                npc.playerInteraction = interactionsBossOrb;
                if (droppedBossOrb)
                {
                    npc.DropItemInstanced(npc.position, npc.Size, mod.ItemType("Boss_Orb"), 1, true);
                }

                //ascension orb
                npc.playerInteraction = interactionsMonsterOrb;
                if (droppedMonsterOrb)
                {
                    npc.DropItemInstanced(npc.position, npc.Size, mod.ItemType("Monster_Orb"), 1, true);
                }

                //messages
                if (Main.netMode == 0)
                {
                    if (droppedBossOrb) Main.NewText("A Boss Orb has dropped for you!", ExperienceAndClasses.MESSAGE_COLOUR_BOSS_ORB);
                    if (droppedMonsterOrb) Main.NewText("An Ascension Orb has dropped for you!", ExperienceAndClasses.MESSAGE_COLOUR_ASCENSION_ORB);
                }
                else if (Main.netMode == 2)
                {
                    NetworkText textBoss = NetworkText.FromLiteral("A Boss Orb has dropped for you!");
                    NetworkText textMonster = NetworkText.FromLiteral("An Ascension Orb has dropped for you!");
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        if (Main.player[playerIndex].active)
                        {
                            if (droppedBossOrb && interactionsBossOrb[playerIndex]) NetMessage.SendChatMessageToClient(textBoss, ExperienceAndClasses.MESSAGE_COLOUR_BOSS_ORB, playerIndex);
                            if (droppedMonsterOrb && interactionsMonsterOrb[playerIndex]) NetMessage.SendChatMessageToClient(textMonster, ExperienceAndClasses.MESSAGE_COLOUR_ASCENSION_ORB, playerIndex);
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Restore Interactions~~~~~~~~~~~~~~~~~~~~~~*/

                npc.playerInteraction = interactionsBefore;

                //done







                ///*~~~~~~~~~~~~~~~~~~~~~~Sort out which players qualify~~~~~~~~~~~~~~~~~~~~~~*/


                ////check qualifications and loot
                //if (Main.netMode == 0)
                //{
                //    //always qualify in singleplayer
                //    myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);
                //    if (!myPlayer.afk || treat_as_boss) interactionsExp[Main.LocalPlayer.whoAmI] = true;
                //    if (treat_as_boss && (Main.rand.Next(1000) < (int)(chanceBossVariable * 10)))
                //    {
                //        interactionsBossOrb[Main.LocalPlayer.whoAmI] = true;
                //        droppedBossOrb = true;
                //    }
                //    if (!myPlayer.afk && (Main.rand.Next(1000) < (int)(chanceMonster * 10)))
                //    {
                //        interactionsMonsterOrb[Main.LocalPlayer.whoAmI] = true;
                //        droppedMonsterOrb = true;
                //    }
                //}
                //else if (Main.netMode == 2)
                //{
                //    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                //    {
                //        player = Main.player[playerIndex];
                //        if (Main.player[playerIndex].active)
                //        {
                //            //qualify if prior interaction or if boss or if nearby
                //            if (treat_as_boss || player.Distance(npc.position) <= ExperienceAndClasses.RANGE_EXP_AND_ASCENSION_ORB)
                //            {
                //                interactionsExp[playerIndex] = true;
                //            }
                //            //unqualify for exp and orbs if afk
                //            myPlayer = player.GetModPlayer<MyPlayer>(mod);
                //            if (myPlayer.afk)
                //            {
                //                interactionsExp[playerIndex] = false;
                //            }
                //            if (interactionsExp[playerIndex] && treat_as_boss && (Main.rand.Next(1000) < (int)(chanceBossVariable * 10)))
                //            {
                //                interactionsBossOrb[playerIndex] = true;
                //                droppedBossOrb = true;
                //            }
                //            if (interactionsExp[playerIndex] && Main.rand.Next(1000) < (int)(chanceMonster * 10))
                //            {
                //                interactionsMonsterOrb[playerIndex] = true;
                //                droppedMonsterOrb = true;
                //            }
                //        }
                //    }
                //}

                ///*~~~~~~~~~~~~~~~~~~~~~~Boss and Ascension Orbs (singleplayer or server-side)~~~~~~~~~~~~~~~~~~~~~~*/

                ////boss orb
                //npc.playerInteraction = interactionsBossOrb;
                //if (droppedBossOrb)
                //{
                //    npc.DropItemInstanced(npc.position, npc.Size, mod.ItemType("Boss_Orb"), 1, true);
                //}

                ////ascension orb
                //npc.playerInteraction = interactionsMonsterOrb;
                //if (droppedMonsterOrb)
                //{
                //    npc.DropItemInstanced(npc.position, npc.Size, mod.ItemType("Monster_Orb"), 1, true);
                //}

                ////messages
                //if (Main.netMode == 0)
                //{
                //    if (droppedBossOrb) Main.NewText("A Boss Orb has dropped for you!", ExperienceAndClasses.MESSAGE_COLOUR_BOSS_ORB);
                //    if (droppedMonsterOrb) Main.NewText("An Ascension Orb has dropped for you!", ExperienceAndClasses.MESSAGE_COLOUR_ASCENSION_ORB);
                //}
                //else if (Main.netMode == 2)
                //{
                //    NetworkText textBoss = NetworkText.FromLiteral("A Boss Orb has dropped for you!");
                //    NetworkText textMonster = NetworkText.FromLiteral("An Ascension Orb has dropped for you!");
                //    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                //    {
                //        if (Main.player[playerIndex].active)
                //        {
                //            if (droppedBossOrb && interactionsBossOrb[playerIndex]) NetMessage.SendChatMessageToClient(textBoss, ExperienceAndClasses.MESSAGE_COLOUR_BOSS_ORB, playerIndex);
                //            if (droppedMonsterOrb && interactionsMonsterOrb[playerIndex]) NetMessage.SendChatMessageToClient(textMonster, ExperienceAndClasses.MESSAGE_COLOUR_ASCENSION_ORB, playerIndex);
                //        }
                //    }
                //}

                ///*~~~~~~~~~~~~~~~~~~~~~~Experience~~~~~~~~~~~~~~~~~~~~~~*/

                ////set interaction
                //npc.playerInteraction = interactionsExp;

                ////calculate base exp
                ////double experience = Helpers.CalcBaseExp(npc);

                ////reward all qualified players
                //double expGive;
                //for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                //{
                //    if (Main.player[playerIndex].active && npc.playerInteraction[playerIndex])
                //    {
                //        player = Main.player[playerIndex];
                //        myPlayer = player.GetModPlayer<MyPlayer>(mod);
                //        expGive = experience;

                //        //10% bonus for well fed
                //        if (player.wellFed) expGive *= 1.1;

                //        //apply rate bonus
                //        expGive *= xp_mult;

                //        //min 1 exp
                //        if (expGive < 1) expGive = 1;

                //        //round down
                //        expGive = Math.Floor(expGive);

                //        //exp
                //        myPlayer.AddExp((int)expGive);
                //    }
                //}

                

            }
        }
    }
}
