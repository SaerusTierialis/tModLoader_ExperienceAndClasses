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

                //set npc-player interactions (will put them back after)
                if (Main.netMode == 0)
                {
                    npc.ApplyInteraction(Main.LocalPlayer.whoAmI);
                }
                else
                {
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        player = Main.player[playerIndex];
                        if (Main.player[playerIndex].active)
                        {
                            if (npc.boss || player.Distance(npc.position) <= ExperienceAndClasses.RANGE_EXP_AND_ASCENSION_ORB)
                            {
                                npc.ApplyInteraction(playerIndex);
                            }
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Boss and Ascension Orbs (singleplayer or server-side)~~~~~~~~~~~~~~~~~~~~~~*/

                //boss orb
                bool droppedBossOrb = false;
                if (npc.boss && (Main.rand.Next(1000) < (int)(ExperienceAndClasses.PERCENT_CHANCE_BOSS_ORB * 10)))
                {
                    droppedBossOrb = true;
                    npc.DropItemInstanced(npc.position, npc.Size, mod.ItemType("Boss_Orb"), 1, true);
                }

                //ascension orb
                bool droppedMonsterOrb = false;
                if (Main.rand.Next(1000) < (int)(ExperienceAndClasses.PERCENT_CHANCE_ASCENSION_ORB * 10))
                {
                    droppedMonsterOrb = true;
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
                        if (Main.player[playerIndex].active && npc.playerInteraction[playerIndex])
                        {
                            if (droppedBossOrb) NetMessage.SendChatMessageToClient(textBoss, ExperienceAndClasses.MESSAGE_COLOUR_BOSS_ORB, playerIndex);
                            if (droppedMonsterOrb) NetMessage.SendChatMessageToClient(textMonster, ExperienceAndClasses.MESSAGE_COLOUR_ASCENSION_ORB, playerIndex);
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Experience~~~~~~~~~~~~~~~~~~~~~~*/

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
                        expGive *= ExperienceAndClasses.globalExpModifier;

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
                            if (player.dead)
                                (mod as ExperienceAndClasses).myUI.updateValue(myPlayer.GetExp());
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
