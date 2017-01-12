using System;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.NPCs
{
    public class MyGlobalNPC : GlobalNPC
    {

        /// <summary>
        /// Returns the unrounded base experience for NPC. Returns 0 for invalid NPC.
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static double CalcBaseExp(NPC npc)
        {
            //no exp from statue, critter, or friendly
            if (npc.SpawnedFromStatue || npc.lifeMax<=5 || npc.friendly) return 0f;

            float experience = 0; ;
            if (npc.defDefense == 1000)
                experience = (npc.lifeMax / 100f) * (1f + (npc.defDamage / 25f));
            else
                experience = (npc.lifeMax / 100f) * (1f + (npc.defDefense / 10f)) * (1f + (npc.defDamage / 25f));

            return experience;
        }

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
                                    Main.NewText("You have found a Boss Orb!");
                                }
                                else if (Main.netMode == 2)
                                {
                                    NetMessage.SendData(25, -1, -1, Main.player[playerIndex].name + " has found a Boss Orb!", 255, 233, 36, 91, 0);
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
                            if (myPlayer.has_looted_monster_orb) chance = 150;
                                else chance = 75;
                            if (Main.rand.Next(chance) == 0) // 1/200 (1/75 for first orb)
                            {
                                if (!myPlayer.has_looted_monster_orb)
                                {
                                    //records orb loot
                                    myPlayer.has_looted_monster_orb = true;

                                    if (Main.netMode == 2)
                                    {
                                        //server tells client to record this
                                        (mod as ExperienceAndClasses).PacketSend_ServerFirstAscensionOrb(Main.player[playerIndex].whoAmI);
                                    }
                                }
                                //announce
                                if (Main.netMode == 0)
                                {
                                    Main.NewText("You have found an Ascension Orb!");
                                }
                                else if (Main.netMode == 2)
                                {
                                    NetMessage.SendData(25, -1, -1, player.name + " has found an Ascension Orb!", 255, 4, 195, 249, 0);
                                }
                                //item
                                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, mod.ItemType("Monster_Orb"));
                            }
                        }
                    }
                }

                /*~~~~~~~~~~~~~~~~~~~~~~Experience~~~~~~~~~~~~~~~~~~~~~~*/
                //get exp
                double experience = CalcBaseExp(npc);

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
                        if ((myPlayer.experience_modifier>0) && (npc.boss || (player.Distance(npc.position) <= 5000f))) //&& myPlayer.has_class)
                        {
                            //10% bonus for well fed
                            expGive = experience;
                            if (player.wellFed) expGive *= 1.1f;

                            //apply rate bonus
                            if (Main.netMode == 0)
                                expGive *= myPlayer.experience_modifier; //single-player
                            else
                                expGive *= ExperienceAndClasses.global_exp_modifier; //server

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
