using System;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.NPCs
{
    public class MyGlobalNPC : GlobalNPC
    {
        public override bool CheckDead(NPC npc)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            //no exp for critters, statues, friendly, or anything that is too far away
            if (Main.netMode!=1 && !npc.friendly && npc.lifeMax>5 && !npc.SpawnedFromStatue && (npc.boss || (Main.player[npc.FindClosestPlayer()].Distance(npc.position) <= 5000f)))
            {
                //declare
                Player player;
                MyPlayer myPlayer;

                /*~~~~~~~~~~~~~~~~~~~~~~Boss and Ascension Orbs~~~~~~~~~~~~~~~~~~~~~~*/
                if (npc.boss)
                {
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        if (Main.player[playerIndex].active)
                        {
                            if (Main.rand.Next(4) == 0) //25%
                            {
                                //announce
                                if (Main.netMode == 0)
                                {
                                    Main.NewText("A Boss Orb has dropped!");
                                }
                                else if (Main.netMode == 2)
                                {
                                    NetMessage.SendData(25, -1, -1,  "A Boss Orb has dropped for "+ Main.player[playerIndex].name+"!", 255, 233, 36, 91, 0);
                                }
                                //item
                                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("Boss_Orb"));
                            }
                        }
                    }
                }
                else //if (CalcBaseExp(npc) >= 2f) //non-boss with base exp >1
                {
                    int chance;
                    for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                    {
                        player = Main.player[playerIndex];
                        if (player.active && player.Distance(npc.position) <= 5000f)
                        {
                            myPlayer = player.GetModPlayer<MyPlayer>(mod);
                            if (myPlayer.hasLootedMonsterOrb) chance = 150;
                                else chance = 75;
                            if (Main.rand.Next(chance) == 0) // 1/200 (1/75 for first orb)
                            {
                                if (!myPlayer.hasLootedMonsterOrb)
                                {
                                    //records orb loot
                                    myPlayer.hasLootedMonsterOrb = true;

                                    if (Main.netMode == 2)
                                    {
                                        //server tells client to record this
                                        Methods.PacketSender.ServerFirstAscensionOrb(mod, Main.player[playerIndex].whoAmI);
                                    }
                                }
                                //announce
                                if (Main.netMode == 0)
                                {
                                    Main.NewText("An Ascension Orb has dropped!");
                                }
                                else if (Main.netMode == 2)
                                {
                                    NetMessage.SendData(25, -1, -1, "An Ascension Orb has dropped for "+ player.name+"!", 255, 4, 195, 249, 0);
                                }
                                //item
                                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("Monster_Orb"));
                            }
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Experience~~~~~~~~~~~~~~~~~~~~~~*/
                //get exp
                double experience = Helpers.CalcBaseExp(npc);

                //cycle all players
                double expGive;
                for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                {
                    //if player active...
                    if (Main.player[playerIndex].active)
                    {
                        //get player-npc distance
                        player = Main.player[playerIndex];
                        myPlayer = player.GetModPlayer<MyPlayer>(mod);

                        //generous distance cutoff (a few screens)
                        if ((myPlayer.experienceModifier>0) && (npc.boss || (player.Distance(npc.position) <= 5000f)))
                        {
                            //10% bonus for well fed
                            expGive = experience;
                            if (player.wellFed) expGive *= 1.1f;

                            //apply rate bonus
                            if (Main.netMode == 0)
                                expGive *= myPlayer.experienceModifier; //single-player
                            else
                                expGive *= ExperienceAndClasses.globalExpModifier; //server

                            //min 1 exp
                            if (expGive < 1f) expGive = 1f;

                            //floor
                            expGive = Math.Floor(expGive);

                            //give exp
                            if (npc.boss || !player.dead)
                            {
                                //exp
                                myPlayer.AddExp((int)expGive); //player.QuickSpawnItem(mod.ItemType("Experience"), (int)expGive);

                                //if this is you...
                                if (player.Equals(Main.LocalPlayer))
                                {
                                    //update UI if earning experience while dead
                                    if (player.dead)
                                        (mod as ExperienceAndClasses).myUI.updateValue(myPlayer.GetExp());
                                }
                            }
                        }
                    }
                }
            }
            return base.CheckDead(npc);
        }
    }
}
