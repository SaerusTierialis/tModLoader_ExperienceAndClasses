using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using Terraria.Localization;
using Terraria.GameInput;
using System.Linq;

namespace ExperienceAndClasses
{
    public class MyPlayer : ModPlayer
    {
        public static double MAX_EXPERIENCE = Methods.Experience.GetExpReqForLevel(ExperienceAndClasses.MAX_LEVEL, true);

        //general
        public bool auth = false;
        public bool traceChar = false;

        public bool experienceNeedsUpdate = false;
        private long timeTickUpdate = 0;

        public double experience = -1;

        //these are now map-specific
        //public double experienceModifier = 1;
        //public int explvlcap = -1;
        //public int expdmgred = -1;
        //public bool ignoreCaps = false;

        public bool displayExp = false;

        public float UILeft = 400f;
        public float UITop = 100f;
        public bool UIShow = true;
        public bool UITrans = false;

        public List<Tuple<ModItem, string>> classTokensEquipped;

        //active abilities
        public long[] abilityCooldowns = new long[Abilities.NUMBER_OF_IDs];
        public int[] currentAbilityIDsPotential = Enumerable.Repeat(Abilities.ID_UNDEFINED, Abilities.NUMBER_OF_IDs).ToArray();
        public int currentAbilityPotentialsIndex = 0;
        public int[] currentAbilityIDs = Enumerable.Repeat(Abilities.ID_UNDEFINED, ExperienceAndClasses.MAXIMUM_NUMBER_OF_ABILITIES).ToArray();
        public int currentAbilityIndex = 0;
        public int abilityLevel = 0;
        public int latestAbilityFail = Abilities.RETURN_FAIL_UNDEFINED;
        public Boolean showFailMessages = true;

        //rogue
        public float percentMidas = 0;
        public int dodgeChancePct = 0;

        //assassin
        public double bonusCritPct = 0;
        public double openerBonusPct = 0;
        public int openerTime_msec = 0;
        public DateTime timeLastAttack = DateTime.MinValue;
        public int openerImmuneTime_msec = 0;
        public DateTime openerImmuneEnd = DateTime.MinValue;
        
        /// <summary>
        /// Returns experience total.
        /// </summary>
        /// <returns></returns>
        public double GetExp()
        {
            return experience;
        }

        /// <summary>
        /// Adds experience. For use in single-player or by server.
        /// </summary>
        /// <param name="xp"></param>
        public void AddExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            SetExp(GetExp() + xp);
        }

        //take xp from player
        public void SubtractExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            SetExp(GetExp() - xp);
        }

        //set xp of player
        public void SetExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            //in the rare case that the player is not synced with the server, don't do anything
            double priorExp = GetExp();
            if (Main.netMode == 2 && priorExp == -1)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Failed to change the experience value for player #" +player.whoAmI+":"+player.name +" (player not yet synced)"), ExperienceAndClasses.MESSAGE_COLOUR_RED);
                return;
            }

            int priorLevel = Methods.Experience.GetLevel(GetExp());
            experience = xp;
            LimitExp();
            LevelUp(priorLevel);

            //if server, tell client
            if (Main.netMode == 2)
            {
                experienceNeedsUpdate = true;
                timeTickUpdate = DateTime.Now.Ticks + ExperienceAndClasses.TIME_TICKS_SYNC_EXP_AFTER_KILL;
            }
            else if (Main.netMode==0)
            {
                ExpMsg(experience - priorExp);
            }
        }

        /// <summary>
        /// Keep experience between zero and max.
        /// </summary>
        public void LimitExp()
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            if (experience < 0) experience = 0;
            if (experience > MAX_EXPERIENCE) experience = MAX_EXPERIENCE;
        }

        /// <summary>
        /// Displays level-up/down messages. For use in single-player or by server.
        /// </summary>
        /// <param name="priorLevel"></param>
        public void LevelUp(int priorLevel)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            int level = Methods.Experience.GetLevel(GetExp());
            if (level>priorLevel)
            {
                if (Main.netMode == 0) Main.NewText("You have reached level " + level + "!");
                    else NetMessage.BroadcastChatMessage( NetworkText.FromLiteral(player.name+" has reached level "+level+"!"), ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
            }
            else if (level<priorLevel)
            {
                if (Main.netMode == 0) Main.NewText("You has fallen to level " + level + "!");
                    else NetMessage.BroadcastChatMessage( NetworkText.FromLiteral(player.name + " has dropped to level " + level + "!"), ExperienceAndClasses.MESSAGE_COLOUR_RED);
            }
        }

        /// <summary>
        /// Displays the local "You have gained/lost x experience." message. Loss is always displayed.
        /// </summary>
        /// <param name="experienceChange"></param>
        public void ExpMsg(double experienceChange)
        {
            if (!Main.LocalPlayer.Equals(player)) return;

            if (experienceChange>0 && displayExp)
            {
                Main.NewText("You have earned " + Math.Round(experienceChange) + " experience.");
            }
            else if (experienceChange<0)
            {
                Main.NewText("You have lost " + Math.Round(experienceChange * -1) + " experience.");
            }
        }

        //save xp
        public override TagCompound Save()
        {
            UILeft = (mod as ExperienceAndClasses).myUI.getLeft();
            UITop = (mod as ExperienceAndClasses).myUI.getTop();

            return new TagCompound {
                {"experience", experience},
                {"display_exp", displayExp},
                {"UI_left", UILeft},
                {"UI_top", UITop},
                {"UI_show", UIShow},
                {"UI_trans", UITrans},
                {"traceChar", traceChar},
            };
        }

        //load xp
        public override void Load(TagCompound tag)
        {
            //load exp
            experience = Commons.TryGet<double>(tag, "experience", 0);
            if (experience < 0) experience = 0;
            if (experience > MAX_EXPERIENCE) experience = MAX_EXPERIENCE;

            //load exp message
            displayExp = Commons.TryGet<bool>(tag, "display_exp", false);

            //UI
            UILeft = Commons.TryGet<float>(tag, "UI_left", 400f);
            UITop = Commons.TryGet<float>(tag, "UI_top", 100f);
            UIShow = Commons.TryGet<bool>(tag, "UI_show", true);
            UITrans = Commons.TryGet<bool>(tag, "UI_trans", false);

            //trace
            traceChar = Commons.TryGet<bool>(tag, "traceChar", false);
        }

        public override void SetupStartInventory(IList<Item> items)
        {
            Item item = new Item();
            item.SetDefaults(mod.ItemType("ClassToken_Novice"));
            item.stack = 1;
            items.Add(item);

            base.SetupStartInventory(items);
        }

        public override void OnEnterWorld(Player player)
        {
            if (player.Equals(Main.LocalPlayer))
            {
                if (experience < 0) //occurs when a player who does not have the mod joins a server that uses the mod
                {
                    experience = 0;
                    player.PutItemInInventory(mod.ItemType("ClassToken_Novice"));
                }

                UI.MyUI.visible = true;
                (mod as ExperienceAndClasses).myUI.setTrans(UITrans);
                (mod as ExperienceAndClasses).myUI.setPosition(UILeft, UITop);
                (mod as ExperienceAndClasses).myUI.updateValue(GetExp());

                //settings
                if (Main.netMode == 0)
                {
                    //Main.NewText("Require Auth: " + ExperienceAndClasses.requireAuth);
                    //Main.NewText("Experience Rate: " + (ExperienceAndClasses.globalExpModifier * 100)+"%");
                    //Main.NewText("Ignore Class Caps: " + ExperienceAndClasses.globalIgnoreCaps);

                    //if (ExperienceAndClasses.globalLevelCap > 0)
                    //    Main.NewText("Level Cap: " + ExperienceAndClasses.globalLevelCap);
                    // else
                    //    Main.NewText("Level Cap: disabled");

                    //if (ExperienceAndClasses.globalClassDamageReduction > 0)
                    //    Main.NewText("Reduce Class Damage: " + ExperienceAndClasses.globalClassDamageReduction + "%");
                    //else
                    //    Main.NewText("Reduce Class Damage: disabled");
                    Methods.ChatCommands.CommandDisplaySettings();
                }
            }

            base.OnEnterWorld(player);
        }

        public override void PreUpdate()
        {
            //empty current class list
            classTokensEquipped = new List<Tuple<ModItem, string>>();

            //empty current ability list
            currentAbilityIDsPotential = Enumerable.Repeat(Abilities.ID_UNDEFINED, Abilities.NUMBER_OF_IDs).ToArray();
            currentAbilityPotentialsIndex = 0;

            //default var bonuses
            bonusCritPct = 0;
            openerBonusPct = 0;
            openerTime_msec = 0;
            openerImmuneTime_msec = 0;
            percentMidas = 0;
            dodgeChancePct = 0;

            base.PreUpdate();
        }

        public override void PostUpdateEquips()
        {
            //apply class effects
            int numberClasses = classTokensEquipped.Count;
            foreach (var i in classTokensEquipped)
            {
                Items.Helpers.ClassTokenEffects(mod, player, i.Item1, i.Item2, true, this, numberClasses);
            }

            //calculate level for abilities
            abilityLevel = (int)Math.Floor((double)Methods.Experience.GetLevel(GetExp()) / numberClasses);

            //limit abilities based on level requirements
            currentAbilityIDs = Enumerable.Repeat(Abilities.ID_UNDEFINED, ExperienceAndClasses.MAXIMUM_NUMBER_OF_ABILITIES).ToArray();
            int index = 0;
            int id;
            for (int i=0; i<currentAbilityPotentialsIndex; i++)
            {
                id = currentAbilityIDsPotential[i];
                if ((abilityLevel >= Abilities.LEVEL_REQUIREMENT[id]) && (index < ExperienceAndClasses.MAXIMUM_NUMBER_OF_ABILITIES))
                {
                    currentAbilityIDs[index] = id;
                    index++;
                }
            }
            currentAbilityIndex = index;

            base.PostUpdateEquips();
        }

        public override void PostUpdate()
        {
            //things to do if this is you
            if (player.Equals(Main.LocalPlayer))
            {
                //update UI if local single-player
                if(Main.netMode==0) (mod as ExperienceAndClasses).myUI.updateValue(GetExp());

                //UI visibility
                if (UIShow)
                {
                    UI.MyUI.visible = true;
                }
                else
                {
                    UI.MyUI.visible = false;
                }
            }

            if (Main.netMode == 2 && experienceNeedsUpdate)
            {
                if (DateTime.Now.Ticks > timeTickUpdate)
                {
                    Methods.PacketSender.ServerForceExperience(mod, player);
                    experienceNeedsUpdate = false;
                }
            }

            base.PostUpdate();
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (!pvp && (Main.netMode==0 || Main.netMode==2))
            {
                int level = Methods.Experience.GetLevel(GetExp());

                double maxLoss = Methods.Experience.GetExpReqForLevel(level + 1, false) * 0.1;
                double expSoFar = Methods.Experience.GetExpTowardsNextLevel(GetExp());

                double expLoss = maxLoss;
                if (expSoFar < maxLoss)
                {
                    expLoss = expSoFar;
                }
                expLoss = Math.Floor(expLoss);

                SubtractExp(expLoss); //notifies client if server

                if (Main.netMode==0)
                {
                    (mod as ExperienceAndClasses).myUI.updateValue(GetExp());
                }
            }

            base.Kill(damage, hitDirection, pvp, damageSource);
        }

        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
            //on-hit midas
            if (Main.rand.Next(100) < (percentMidas * 100)) target.AddBuff(Terraria.ID.BuffID.Midas, 300);

            //Assassin special attack
            DateTime now = DateTime.Now;
            bool ready = timeLastAttack.AddMilliseconds(openerTime_msec).CompareTo(now) <= 0;
            if (openerBonusPct>0 && item.melee && (ready || target.life==target.lifeMax))
            {
                //if ready, add phase
                if (ready)
                {
                    player.AddBuff(mod.BuffType<Buffs.Buff_OpenerPhase>(), 1);
                    openerImmuneEnd = now.AddMilliseconds(openerImmuneTime_msec);
                }

                //bonus opener damage
                damage = (int)Math.Round((double)damage * (1 + openerBonusPct), 0);

                //crit opener?
                if (bonusCritPct > 0) damage = (int)Math.Round((double)damage * (1 + (bonusCritPct*3)), 0);
            }
            else
            {
                //bonus crit damage (Assassin)
                if (item.melee && bonusCritPct > 0) damage = (int)Math.Round((double)damage * (1 + bonusCritPct), 0);
            }

            //record time
            timeLastAttack = now;

            //remove buff
            int buffInd = player.FindBuffIndex(mod.BuffType<Buffs.Buff_OpenerAttack>());
            if (buffInd != -1) player.DelBuff(buffInd);

            //base
            base.ModifyHitNPC(item, target, ref damage, ref knockback, ref crit);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            //on-hit midas
            if (Main.rand.Next(100) < (percentMidas * 100)) target.AddBuff(Terraria.ID.BuffID.Midas, 300);

            //Assassin special attack for yoyo
            DateTime now = DateTime.Now;
            Item item = Main.player[proj.owner].HeldItem;
            bool ready = timeLastAttack.AddMilliseconds(openerTime_msec).CompareTo(now) <= 0;
            if (openerBonusPct > 0 && Items.Helpers.HeldYoyo(player) && (ready || target.life == target.lifeMax))
            {
                //if ready, add phase
                if (ready)
                {
                    player.AddBuff(mod.BuffType<Buffs.Buff_OpenerPhase>(), 2);
                    openerImmuneEnd = now.AddMilliseconds(openerImmuneTime_msec);
                }

                //bonus opener damage (50% YOYO PENALTY)
                damage = (int)Math.Round((double)damage * (1 + (openerBonusPct/2)), 0);

                //crit opener?
                if (bonusCritPct > 0) damage = (int)Math.Round((double)damage * (1 + (bonusCritPct * 3)), 0);
            }

            //record time
            timeLastAttack = now;

            //flex time for point-blank melee weapons that have a projectile
            if (openerBonusPct > 0 && !Items.Helpers.HeldYoyo(player)) timeLastAttack.AddMilliseconds(-50);

            //remove buff
            int buffInd = player.FindBuffIndex(mod.BuffType<Buffs.Buff_OpenerAttack>());
            if (buffInd != -1) player.DelBuff(buffInd);

            //base
            base.ModifyHitNPCWithProj(proj, target, ref damage, ref knockback, ref crit, ref hitDirection);
        }

        public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            int buffInd = player.FindBuffIndex(mod.BuffType<Buffs.Buff_OpenerAttack>());
            if (buffInd != -1)
            {
                if (Main.rand.Next(5) == 0 && drawInfo.shadow == 0f)
                {
                    int dust = Dust.NewDust(drawInfo.position - new Vector2(2f, 2f), player.width + 4, player.height + 4, mod.DustType("Dust_OpenerAttack"), player.velocity.X * 0.4f, player.velocity.Y * 0.4f, 100, default(Color), 3f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 1.8f;
                    Main.dust[dust].velocity.Y -= 0.5f;
                    Main.playerDrawDust.Add(dust);
                }
            }
            base.DrawEffects(drawInfo, ref r, ref g, ref b, ref a, ref fullBright);
        }

        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            //% dodge chance (shadow dodge and ninja dodge both use 80 ticks so I used that here)
            if (Main.LocalPlayer.Equals(player))
            {
                if (dodgeChancePct > 0 && Main.rand.Next(100) < dodgeChancePct)
                {
                    player.immune = true;
                    player.immuneTime = 80;
                    player.NinjaDodge();
                    NetMessage.SendData(62, -1, -1, null, player.whoAmI, 2f, 0f, 0f, 0, 0, 0); //might not work anymore
                    return false;
                }
            }

            return base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);
        }

        public override void ProcessTriggers(TriggersSet triggersSet) //CLIENT-SIDE ONLY
        {
            if (ExperienceAndClasses.HOTKEY_ACTIVATE_ABILITY.JustPressed)
            {
                latestAbilityFail = Abilities.RETURN_FAIL_UNDEFINED;
                showFailMessages = true;
            }

            if (ExperienceAndClasses.HOTKEY_ACTIVATE_ABILITY.Current)
            {
                //check modifieriers
                int modifer = 0;
                if (ExperienceAndClasses.HOTKEY_MODIFIER_4.Current)
                {
                    modifer = 4;
                }
                else if (ExperienceAndClasses.HOTKEY_MODIFIER_3.Current)
                {
                    modifer = 3;
                }
                else if (ExperienceAndClasses.HOTKEY_MODIFIER_2.Current)
                {
                    modifer = 2;
                }
                else if (ExperienceAndClasses.HOTKEY_MODIFIER_1.Current)
                {
                    modifer = 1;
                }

                int abilityID = currentAbilityIDs[modifer - 1];
                int outcome = Abilities.RETURN_FAIL_UNDEFINED;
                if (modifer == 0 || (abilityID == Abilities.ID_UNDEFINED))
                    return;
                else
                {
                    outcome = Abilities.DoAbility(this, abilityID, abilityLevel);

                    if (outcome == Abilities.RETURN_SUCCESS) showFailMessages = false;

                    if (showFailMessages && (outcome != latestAbilityFail))
                    {
                        latestAbilityFail = outcome;
                        Abilities.DoReturnMessage(latestAbilityFail, abilityID);
                    }
                }
            }
        }
    }
}
