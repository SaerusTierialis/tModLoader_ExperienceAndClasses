using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExperienceAndClasses.NPCs
{
    public class MyGlobalNPC : GlobalNPC
    {
        public override void NPCLoot(NPC npc) //bool CheckDead(NPC npc)
        {
            //singleplayer and server-side only
            //no exp or loot for critters, statues, friendly, or anything that is too far away (unless it's a boss)
            if (Main.netMode != 1 && !npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue && (npc.boss || (Main.netMode == 2) || (Main.LocalPlayer.Distance(npc.position) <= ExperienceAndClasses.RANGE_EXP_AND_ASCENSION_ORB)))
            {
                //declare
                Player player;
                MyPlayer myPlayer;

                /*~~~~~~~~~~~~~~~~~~~~~~Sort out which players qualify~~~~~~~~~~~~~~~~~~~~~~*/

                //store prior interactions
                bool[] interactionsBefore = npc.playerInteraction;

                //will have 3 sets of interactions
                bool[] interactionsExp = interactionsBefore;
                bool[] interactionsBossOrb = new bool[interactionsExp.Length];
                bool[] interactionsMonsterOrb = new bool[interactionsExp.Length];

                //default to no drop
                bool droppedBossOrb = false;
                bool droppedMonsterOrb = false;
                
                //check qualifications and loot
                if (Main.netMode == 0)
                {
                    //always qualify in singleplayer
                    interactionsExp[Main.LocalPlayer.whoAmI] = true;
                    if (npc.boss && (Main.rand.Next(1000) < (int)(ExperienceAndClasses.PERCENT_CHANCE_BOSS_ORB * 10)))
                    {
                        interactionsBossOrb[Main.LocalPlayer.whoAmI] = true;
                        droppedBossOrb = true;
                    }
                    if (Main.rand.Next(1000) < (int)(ExperienceAndClasses.PERCENT_CHANCE_ASCENSION_ORB * 10))
                    {
                        interactionsMonsterOrb[Main.LocalPlayer.whoAmI] = true;
                        droppedMonsterOrb = true;
                    }
                }
                else if (Main.netMode == 2)
                {
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        player = Main.player[playerIndex];
                        if (Main.player[playerIndex].active)
                        {
                            //qualify if prior interaction or if boss or if nearby
                            if (npc.boss || player.Distance(npc.position) <= ExperienceAndClasses.RANGE_EXP_AND_ASCENSION_ORB)
                            {
                                interactionsExp[playerIndex] = true;
                            }
                            //unqualify for exp and orbs if afk
                            myPlayer = player.GetModPlayer<MyPlayer>(mod);
                            if (myPlayer.afk)
                            {
                                interactionsExp[playerIndex] = false;
                            }
                            if (interactionsExp[playerIndex] && npc.boss && (Main.rand.Next(1000) < (int)(ExperienceAndClasses.PERCENT_CHANCE_BOSS_ORB * 10)))
                            {
                                interactionsBossOrb[playerIndex] = true;
                                droppedBossOrb = true;
                            }
                            if (interactionsExp[playerIndex] && Main.rand.Next(1000) < (int)(ExperienceAndClasses.PERCENT_CHANCE_ASCENSION_ORB * 10))
                            {
                                interactionsMonsterOrb[playerIndex] = true;
                                droppedMonsterOrb = true;
                            }
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Boss and Ascension Orbs (singleplayer or server-side)~~~~~~~~~~~~~~~~~~~~~~*/

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

                /*~~~~~~~~~~~~~~~~~~~~~~Experience~~~~~~~~~~~~~~~~~~~~~~*/

                //set interaction
                npc.playerInteraction = interactionsExp;

                //calculate base exp
                double experience = Helpers.CalcBaseExp(npc);

                //reward all qualified players
                double expGive;
                for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                {
                    if (Main.player[playerIndex].active && npc.playerInteraction[playerIndex])
                    {
                        player = Main.player[playerIndex];
                        myPlayer = player.GetModPlayer<MyPlayer>(mod);
                        expGive = experience;

                        //10% bonus for well fed
                        if (player.wellFed) expGive *= 1.1f;

                        //apply rate bonus
                        expGive *= ExperienceAndClasses.mapExpModifier;

                        //min 1 exp
                        if (expGive < 1f) expGive = 1f;

                        //round down
                        expGive = Math.Floor(expGive);

                        //exp
                        myPlayer.AddExp((int)expGive);

                        //if singleplayer...
                        if (Main.netMode == 0)
                        {
                            //update UI if earning experience while dead (prevents visual bug)
                            //if (player.dead)
                            //    (mod as ExperienceAndClasses).uiExp.Update();
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Restore Interactions~~~~~~~~~~~~~~~~~~~~~~*/

                npc.playerInteraction = interactionsBefore;

            }
            //return base.CheckDead(npc);
        }
    }
}
