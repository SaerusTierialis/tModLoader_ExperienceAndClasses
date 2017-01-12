using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;

namespace ExperienceAndClasses
{
    public class MyPlayer : ModPlayer
    {
        public static double MAX_EXPERIENCE = ExperienceAndClasses.GetExpReqForLevel(ExperienceAndClasses.MAX_LEVEL, true);

        public bool auth = false;

        public double experience = -1;
        public double experience_modifier = 1;

        public int explvlcap = -1;
        public int expdmgred = -1;

        public bool display_exp = false;
        public bool ignore_caps = false;

        public float UI_left = 400f;
        public float UI_top = 100f;
        public bool UI_show = true;
        public bool UI_trans = false;

        //public bool has_class = true;

        public double bonus_crit_pct = 0;
        public double opener_bonus_pct = 0;
        public int opener_time_msec = 0;
        public DateTime time_last_attack = DateTime.MinValue;

        public float percent_midas = 0;

        public bool has_looted_monster_orb = false;

        
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

            SetExp(experience + xp);
        }

        //take xp from player
        public void SubtractExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            SetExp(experience - xp);
        }

        //set xp of player
        public void SetExp(double xp)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            //in the rare case that the player is not synced with the server, don't do anything
            if (Main.netMode == 2 && experience == -1)
            {
                NetMessage.SendData(25, -1, -1, "Failed to change the experience value for player #"+player.whoAmI+":"+player.name +" (player not yet synced)", 255, 255, 0, 0, 0);
                return;
            }

            double priorExp = GetExp();
            int priorLevel = ExperienceAndClasses.GetLevel(GetExp());
            experience = xp;
            LimitExp();
            LevelUp(priorLevel);

            //if server, tell client
            if (Main.netMode == 2)
            {
                (mod as ExperienceAndClasses).PacketSend_ServerForceExperience(player);
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

            if (explvlcap!=-1)
            {
                double exp_cap = ExperienceAndClasses.GetExpReqForLevel(explvlcap, true);
                if (experience > exp_cap) experience = exp_cap;
            }
        }

        /// <summary>
        /// Displays level-up/down messages. For use in single-player or by server.
        /// </summary>
        /// <param name="prior_level"></param>
        public void LevelUp(int prior_level)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (Main.netMode == 1) return;

            int level = ExperienceAndClasses.GetLevel(GetExp());
            if (level>prior_level)
            {
                if (Main.netMode == 0) Main.NewText("You have reached level " + level + "!");
                    else NetMessage.SendData(25, -1, -1, player.name+" has reached level "+level+"!", 255, 0, 255, 0, 0);
            }
            else if (level<prior_level)
            {
                if (Main.netMode == 0) Main.NewText("You has fallen to level " + level + "!");
                    else NetMessage.SendData(25, -1, -1, player.name + " has dropped to level " + level + "!", 255, 255, 0, 0, 0);
            }
        }

        /// <summary>
        /// Displays the local "You have gained/lost x experience." message. Loss is always displayed.
        /// </summary>
        /// <param name="experience_change"></param>
        public void ExpMsg(double experience_change)
        {
            if (!Main.LocalPlayer.Equals(player)) return;

            if (experience_change>0 && display_exp)
            {
                Main.NewText("You have earned " + (int)experience_change + " experience.");
            }
            else if (experience_change<0)
            {
                Main.NewText("You have lost " + (int)(experience_change * -1) + " experience.");
            }
        }

        //save xp
        public override TagCompound Save()
        {
            UI_left = (mod as ExperienceAndClasses).myUI.getLeft();
            UI_top = (mod as ExperienceAndClasses).myUI.getTop();

            return new TagCompound {
                { "experience", experience},
                {"experience_modifier", experience_modifier},
                {"display_exp", display_exp},
                {"ignore_caps", ignore_caps},
                {"UI_left", UI_left},
                {"UI_top", UI_top},
                {"UI_show", UI_show},
                {"has_looted_monster_orb", has_looted_monster_orb},
                {"UI_trans", UI_trans},
                {"explvlcap", explvlcap},
                {"expdmgred", expdmgred}
            };
        }

        //load xp
        public override void Load(TagCompound tag)
        {
            //load exp
            experience = tag.TryGet<double>("experience", 0);
            if (experience < 0) experience = 0;
            if (experience > MAX_EXPERIENCE) experience = MAX_EXPERIENCE;

            //load exp rate
            experience_modifier = tag.TryGet<double>("experience_modifier", 1);

            //load exp message
            display_exp = tag.TryGet<bool>("display_exp", false);

            //load ignore caps
            ignore_caps = tag.TryGet<bool>("ignore_caps", false);

            //UI
            UI_left = tag.TryGet<float>("UI_left", 400f);
            UI_top = tag.TryGet<float>("UI_top", 100f);
            UI_show = tag.TryGet<bool>("UI_show", true);
            UI_trans = tag.TryGet<bool>("UI_trans", false);

            //has_looted_monster_orb
            has_looted_monster_orb = tag.TryGet<bool>("has_looted_monster_orb", false);

            //explvlcap
            explvlcap = tag.TryGet<int>("explvlcap", -1);

            //expdmgred
            expdmgred = tag.TryGet<int>("expdmgred", -1);
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
            if (explvlcap == 0) explvlcap = -1; //should fix an odd bug

            if (player.Equals(Main.LocalPlayer))
            {
                if (experience < 0) //occurs when a player who does not have the mod joins a server that uses the mod
                {
                    experience = 0;
                    player.PutItemInInventory(mod.ItemType("ClassToken_Novice"));
                }

                UI.MyUI.visible = true;
                (mod as ExperienceAndClasses).myUI.setTrans(UI_trans);
                (mod as ExperienceAndClasses).myUI.setPosition(UI_left, UI_top);
                (mod as ExperienceAndClasses).myUI.updateValue(GetExp());

                //settings
                if (Main.netMode == 0)
                {
                    Main.NewText("Require Auth: " + ExperienceAndClasses.require_auth);
                    Main.NewText("Experience Rate: " + (experience_modifier*100)+"%");
                    Main.NewText("Ignore Class Caps: " + ignore_caps);
                    if (explvlcap > 0) Main.NewText("Level Cap: " + explvlcap);
                        else Main.NewText("Level Cap: disabled");
                    if (expdmgred > 0) Main.NewText("Reduce Class Damamge: " + expdmgred + "%");
                        else Main.NewText("Reduce Class Damamge: disabled");
                }
            }

            base.OnEnterWorld(player);
        }

        public override void PreUpdate()
        {
            bonus_crit_pct = 0;
            opener_bonus_pct = 0;
            opener_time_msec = 0;
            percent_midas = 0;

            base.PreUpdate();
        }

        public override void PostUpdateEquips()
        {
            //class
            string job = ExperienceAndClasses.GetClass(player);

            /*
            if (job.Equals("No Class"))
            {
                has_class = false;
            }
            else
            {
                has_class = true;
            }
            */

            //UI
            if (player.Equals(Main.LocalPlayer))
            {
                if (UI_show)// && has_class)
                {
                    UI.MyUI.visible = true;
                }
                else
                {
                    UI.MyUI.visible = false;
                }
            }

            base.PostUpdateEquips();
        }

        public override void PostUpdate()
        {
            //things to do if this is you
            if (player.Equals(Main.LocalPlayer))
            {
                //update UI if local single-player
                if(Main.netMode==0) (mod as ExperienceAndClasses).myUI.updateValue(GetExp());
            }

            //
            base.PostUpdate();
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            /*~~~~~~~~~~~~~~~~~~~~~~Single Player and Server Only~~~~~~~~~~~~~~~~~~~~~~*/
            if (!pvp && (Main.netMode==0 || Main.netMode==2))
            {
                int level = ExperienceAndClasses.GetLevel(GetExp());

                double max_loss = ExperienceAndClasses.GetExpReqForLevel(level + 1, false) * 0.1;
                double exp_so_far = ExperienceAndClasses.GetExpTowardsNextLevel(GetExp());

                double exp_loss = max_loss;
                if (exp_so_far < max_loss)
                {
                    exp_loss = exp_so_far;
                }
                exp_loss = Math.Floor(exp_loss);

                SubtractExp(exp_loss); //notifies client if server

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
            if (Main.rand.Next(100) < (percent_midas * 100)) target.AddBuff(Terraria.ID.BuffID.Midas, 300);

            //Assassin special attack
            DateTime now = DateTime.Now;
            if (opener_bonus_pct>0 && item.melee && (time_last_attack.AddMilliseconds(opener_time_msec).CompareTo(now)<=0 || target.life==target.lifeMax))
            {
                //bonus opener damage
                damage = (int)Math.Round((double)damage * (1 + opener_bonus_pct), 0);

                //crit opener?
                if (bonus_crit_pct > 0) damage = (int)Math.Round((double)damage * (1 + (bonus_crit_pct*3)), 0);
            }
            else
            {
                //bonus crit damage (Assassin)
                if (item.melee && bonus_crit_pct > 0) damage = (int)Math.Round((double)damage * (1 + bonus_crit_pct), 0);
            }

            //record time
            time_last_attack = now;

            //remove buff
            int buffInd = player.FindBuffIndex(mod.BuffType("Buff_OpenerAttack"));
            if (buffInd != -1) player.DelBuff(buffInd);

            //base
            base.ModifyHitNPC(item, target, ref damage, ref knockback, ref crit);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            //on-hit midas
            if (Main.rand.Next(100) < (percent_midas * 100)) target.AddBuff(Terraria.ID.BuffID.Midas, 300);

            //Assassin special attack for yoyo
            DateTime now = DateTime.Now;
            Item item = Main.player[proj.owner].HeldItem;
            if (opener_bonus_pct > 0 && proj.melee && item.channel && (time_last_attack.AddMilliseconds(opener_time_msec).CompareTo(now) <= 0 || target.life == target.lifeMax))
            {
                //bonus opener damage (50% YOYO PENALTY)
                damage = (int)Math.Round((double)damage * (1 + (opener_bonus_pct/2)), 0);

                //crit opener?
                if (bonus_crit_pct > 0) damage = (int)Math.Round((double)damage * (1 + (bonus_crit_pct * 3)), 0);
            }

            //record time
            time_last_attack = now;

            //remove buff
            int buffInd = player.FindBuffIndex(mod.BuffType("Buff_OpenerAttack"));
            if (buffInd != -1) player.DelBuff(buffInd);

            //base
            base.ModifyHitNPCWithProj(proj, target, ref damage, ref knockback, ref crit, ref hitDirection);
        }

        public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            int buffInd = player.FindBuffIndex(mod.BuffType("Buff_OpenerAttack"));
            if (buffInd != -1)
            {
                if (Main.rand.Next(10) == 0 && drawInfo.shadow == 0f)
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

    }
}
