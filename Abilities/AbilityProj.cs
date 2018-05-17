using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Abilities
{
    public class AbilityProj
    {
        public class Cleric_Sanctuary : SyncingProjectile
        {
            private int sanc_index = -1;
            private DateTime time_next_pulse = DateTime.MinValue;
            private bool local_owner = false;

            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.width = 151;
                projectile.height = 151;
                projectile.light = 1f;
                projectile.alpha = 220;
            }
            public override void AI()
            {
                //attach to owner
                if (sanc_index == -1)
                {
                    sanc_index = (int)projectile.ai[0];

                    if (Main.LocalPlayer.Equals(Main.player[projectile.owner])) //local
                    {
                        local_owner = true;

                        if ((ExperienceAndClasses.localMyPlayer.sanctuaries[sanc_index] != null) && !ExperienceAndClasses.localMyPlayer.sanctuaries[sanc_index].Equals(projectile))
                        {
                            ExperienceAndClasses.localMyPlayer.sanctuaries[sanc_index].Kill();
                        }
                    }

                    Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod).sanctuaries[sanc_index] = projectile;
                }

                //unlimited duration
                projectile.timeLeft = int.MaxValue;

                //remove any strays
                if (!Main.player[projectile.owner].active || ((projectile.owner == Main.LocalPlayer.whoAmI) && !ExperienceAndClasses.localMyPlayer.sanctuaries[sanc_index].Equals(projectile)))
                {
                    projectile.Kill();
                    return;
                }

                //effects
                if (local_owner)
                {
                    //kill if no longer have requirements
                    if (!ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)AbilityMain.ID.Cleric_Active_Sanctuary] ||
                        ((sanc_index > 0) && !ExperienceAndClasses.localMyPlayer.unlocked_abilities_current[(int)AbilityMain.ID.Saint_Upgrade_Sanctuary_Link]))
                    {
                        projectile.Kill();
                    }
                    else
                    {
                        DateTime now = DateTime.Now;
                        if (now.Subtract(time_next_pulse).TotalSeconds >= AbilityMain.Cleric_Active_Sanctuary.PULSE_SECONDS)
                        {
                            //timing
                            time_next_pulse = now;

                            //effects
                            AbilityMain.Cleric_Active_Sanctuary.Pulse(projectile);
                        }
                    }
                }
                
                //everything else
                base.AI();
            }
            public override void Kill(int timeLeft)
            {
                MyPlayer myPlayer = Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod);
                if (myPlayer.sanctuaries[sanc_index].Equals(projectile))
                {
                    myPlayer.sanctuaries[sanc_index] = null;
                }
                base.Kill(timeLeft);
            }
        }

        public class Status_Cleric_Blessing : ProjStatusVisual
        {
            public Status_Cleric_Blessing()
            {
                status_index = (int)ExperienceAndClasses.STATUSES.Blessing;
                position_type = ProjStatusVisual.POSITION.BOTTOM;
            }
            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.width = 40;
                projectile.height = 60;
                projectile.alpha = 200;
            }
        }

        public class Status_Cleric_Paragon : ProjStatusVisual
        {
            public Status_Cleric_Paragon()
            {
                status_index = (int)ExperienceAndClasses.STATUSES.Paragon;
                position_type = ProjStatusVisual.POSITION.ABOVE;
            }
            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.width = 20;
                projectile.height = 8;
                projectile.alpha = 100;
            }
        }

        public class Status_Cleric_Paragon_Renew : ProjStatusVisual
        {
            public Status_Cleric_Paragon_Renew()
            {
                status_index = (int)ExperienceAndClasses.STATUSES.Renew;
                position_type = ProjStatusVisual.POSITION.ABOVE;
            }
            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.width = 20;
                projectile.height = 8;
                projectile.alpha = 100;
            }
        }

        public class Cleric_Barrier : SyncingProjectile
        {
            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.height = 127;
                projectile.width = 12;
                projectile.penetrate = 3;
                projectile.friendly = true;
                projectile.alpha = 100;
                projectile.light = 0.3f;
                projectile.timeLeft = (int)TimeSpan.TicksPerSecond * 20;
            }

            public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
            {
                //direction
                RedirectToNPC(projectile, target);
                hitDirection = projectile.direction;

                //undead bonus
                if (AbilityMain.IsUndead(target))
                {
                    damage = (int)(damage * AbilityMain.Cleric_Active_Heal.UNDEAD_BONUS_MULTIPLIER);
                }
            }

            public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
            {
                RedirectToPlayer(projectile, target);
                base.ModifyHitPlayer(target, ref damage, ref crit);
            }

            public override void ModifyHitPvp(Player target, ref int damage, ref bool crit)
            {
                RedirectToPlayer(projectile, target);
                base.ModifyHitPvp(target, ref damage, ref crit);
            }

        }

        public class Misc_HealHurt : ProjNoVisual
        {
            //heal/hurt a player/npc
            //projectile.damage is the magnitude (+ for heal, - for hurt)
            //projectile.ai[0] is the mode
            //projectile.ai[1] is the target index
            //uses projectile knockback

            private bool has_run = false;

            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.timeLeft = 100;
            }

            public override bool? CanHitNPC(NPC target)
            {
                return false;
            }
            public override bool CanHitPlayer(Player target)
            {
                return false;
            }
            public override bool CanHitPvp(Player target)
            {
                return false;
            }

            public override void AI()
            {
                if (!has_run)
                {
                    bool is_player = projectile.ai[0] != 0;
                    int target = (int)projectile.ai[1];
                    int amount = projectile.damage;
                    int direction = 1;

                    bool sanctuary_heal = projectile.ai[0] == 2;

                    bool server_or_single = (Main.netMode != 1);

                    bool do_visual = false;
                    if ((is_player && (target == Main.LocalPlayer.whoAmI)) || (!is_player && (projectile.owner == Main.LocalPlayer.whoAmI)))
                    {
                        do_visual = true;
                    }

                    if (is_player)
                    {
                        //player
                        Player player = Main.player[target];
                        if (player.active && !player.dead)
                        {
                            if ((amount > 0) && !(!Main.player[projectile.owner].hostile && player.hostile))//don't let non-pvp players heal pvp players
                            {
                                int amount_valid = player.statLifeMax2 - player.statLife;
                                if (amount > amount_valid)
                                {
                                    amount = amount_valid;
                                }
                                if (do_visual)
                                {
                                    player.HealEffect(amount);
                                }
                                player.statLife += amount;
                                if (sanctuary_heal)
                                {
                                    player.GetModPlayer<MyPlayer>(mod).time_last_sanc_effect = DateTime.Now;
                                }
                            }
                            else if (amount < 0)
                            {
                                if (do_visual)
                                {
                                    amount *= -1;
                                    if (projectile.Center.X > player.Center.X)
                                    {
                                        direction = -1;
                                    }
                                    player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByPlayer(projectile.owner), amount, direction, true);
                                }
                                //player.GetModPlayer<MyPlayer>(mod).time_last_hit_taken = DateTime.Now;
                                //Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod).time_last_hit_taken = DateTime.Now;
                            }
                        }
                    }
                    else
                    {
                        //npc
                        NPC npc = Main.npc[target];
                        if (npc.active)
                        {
                            if (amount > 0)
                            {
                                int amount_valid = npc.lifeMax - npc.life;
                                if (amount > amount_valid)
                                {
                                    amount = amount_valid;
                                }
                                if (do_visual)
                                {
                                    npc.HealEffect(amount);
                                }
                                npc.life += amount;
                            }
                            else if (amount < 0)
                            {
                                if (server_or_single)
                                {
                                    amount *= -1;
                                    if (projectile.Center.X > npc.Center.X)
                                    {
                                        direction = -1;
                                    }
                                    Main.player[projectile.owner].ApplyDamageToNPC(npc, amount, projectile.knockBack, direction, false);
                                }
                                //Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod).time_last_hit_taken = DateTime.Now;
                            }
                        }
                    }

                    //done
                    has_run = true;
                }
            }
        }

        public class Misc_PlayerStatus : ProjNoVisual
        {
            //give a status to a player
            //projectile.damage is the player index
            //projectile.knockback is the magnitude
            //projectile.ai[0] is the status
            //projectile.ai[1] is the duration in seconds
            //
            //is velocity.X is not 0, force new values

            private bool has_run = false;

            public override void SetDefaults()
            {
                base.SetDefaults();
                projectile.timeLeft = 100;
            }

            public override bool? CanHitNPC(NPC target)
            {
                return false;
            }
            public override bool CanHitPlayer(Player target)
            {
                return false;
            }
            public override bool CanHitPvp(Player target)
            {
                return false;
            }

            public override void AI()
            {
                if (!has_run)
                {
                    int player_index = (int)projectile.damage;
                    float magnitude = projectile.knockBack;
                    byte status = (byte)projectile.ai[0];
                    double duration_seconds = projectile.ai[1];
                    bool force_mode = (projectile.velocity.X != 0);

                    if (Main.player[player_index].active && !Main.player[player_index].dead)
                    {
                        MyPlayer myPlayer = Main.player[player_index].GetModPlayer<MyPlayer>(mod);

                        if (duration_seconds > 0) //add status
                        {
                            DateTime now = DateTime.Now;
                            DateTime time_end = now.AddSeconds(duration_seconds);
                            if (force_mode || time_end.Subtract(myPlayer.status_end_time[status]).Ticks > 0)
                            {
                                myPlayer.status_end_time[status] = time_end;
                            }
                            if (force_mode || magnitude > myPlayer.status_magnitude[status])
                            {
                                myPlayer.status_magnitude[status] = magnitude;
                            }

                            if (myPlayer.show_status_messages && !myPlayer.status_active[status] && (myPlayer.player.whoAmI == Main.LocalPlayer.whoAmI))
                            {
                                //Main.NewText("Gained Effect: " + (ExperienceAndClasses.STATUSES)status + " (MAG: " + myPlayer.status_magnitude[status] + ", DUR: " + myPlayer.status_end_time[status].Subtract(now).TotalSeconds + ")", ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
                                Main.NewText("Gained Status: " + (ExperienceAndClasses.STATUSES)status, ExperienceAndClasses.MESSAGE_COLOUR_GREEN);
                            }

                            myPlayer.status_active[status] = true;
                            myPlayer.status_new[status] = true;
                        }
                        else //remove status
                        {
                            myPlayer.status_active[status] = false;
                            myPlayer.status_end_time[status] = DateTime.MinValue;
                            myPlayer.status_magnitude[status] = 0;

                            if (myPlayer.show_status_messages && (myPlayer.player.whoAmI == Main.LocalPlayer.whoAmI))
                            {
                                Main.NewText("Lost Status: " + (ExperienceAndClasses.STATUSES)status, ExperienceAndClasses.MESSAGE_COLOUR_RED);
                            }
                        }
                    }
                    
                    //do once
                    has_run = true;
                }
            }

        }

        private static void RedirectToPlayer(Projectile projectile, Player target)
        {
            projectile.direction = 1;
            if (projectile.Center.X > target.Center.X)
            {
                projectile.direction = -1;
            }
        }

        private static void RedirectToNPC(Projectile projectile, NPC target)
        {
            projectile.direction = 1;
            if (projectile.Center.X > target.Center.X)
            {
                projectile.direction = -1;
            }
        }
    }

    public class DustMakerProj : ProjNoVisual
    {
        public enum MODE : byte
        {
            ABILITY_CAST,
            HEAL,
            HEAL_RENEW,
            DIVINE_INTERVENTION,
        }

        public override void AI()
        {
            //setup
            Player player = Main.player[projectile.owner];

            //creates ability on-use dust effect, shows for all clients but dust scatter is unqiue for each client
            switch ((MODE)projectile.ai[0])
            {
                case MODE.ABILITY_CAST:
                    SpreadDust(player.Center, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 3, 5, 2, 150, AbilityMain.COLOUR_CLASS_TYPE[(int)projectile.ai[1]]);
                    break;
                case MODE.HEAL:
                    SpreadDust(projectile.position, DustID.AncientLight, 10, AbilityMain.Cleric_Active_Heal.RANGE / 6, 3, 150, Color.Red, true, true);
                    break;
                case MODE.HEAL_RENEW:
                    SpreadDust(projectile.position, DustID.AncientLight, 3, 10, 2, 150, Color.White, true, true);
                    break;
                case MODE.DIVINE_INTERVENTION:
                    SpreadDust(projectile.position, DustID.AncientLight, 10, projectile.ai[1] / 6, 3, 150, Color.Yellow, true, true);
                    break;
                default:
                    break;
            }

            //done
            projectile.Kill();
        }

        private static void SpreadDust(Vector2 position, int dust_type, int loop_count, float velocity, float scale = 1, int alpha = 255, Color colour = default(Color), bool remove_gravity = false, bool remove_light = false)
        {
            //Math.Cos and Math.Sin were crashing for some reason so a better implementation was not possible
            //Should handle large values of loop_count fairly well
            //Total number of dusts is ((loop_count * 4) - 4), velocities form a circle
            if (loop_count < 3)
            {
                loop_count = 3;
            }
            float inc = 2f / (loop_count - 1);
            int dust_index;
            float velocity_adjust, velocity_x, velocity_y;
            for (float step = -1; step <= +1; step += inc)
            {
                for (int mode = 1; mode <= 4; mode++)
                {
                    //4 directions
                    switch (mode)
                    {
                        case 1:
                            velocity_x = step;
                            velocity_y = -1;
                            break;
                        case 2:
                            velocity_x = step;
                            velocity_y = 1;
                            break;
                        case 3:
                            velocity_x = 1;
                            velocity_y = step;
                            break;
                        case 4:
                            velocity_x = -1;
                            velocity_y = step;
                            break;
                        default:
                            velocity_x = 1;
                            velocity_y = 1;
                            break;
                    }

                    //adjust for distance (so it is a circle instead of a square)
                    velocity_adjust = velocity / (float)Math.Sqrt(Math.Pow(velocity_x, 2) + Math.Pow(velocity_y, 2));
                    velocity_x *= velocity_adjust;
                    velocity_y *= velocity_adjust;

                    //do
                    dust_index = Dust.NewDust(position, 0, 0, dust_type, velocity_x, velocity_y, alpha, colour, scale);
                    if (remove_gravity)
                    {
                        Main.dust[dust_index].noGravity = true;
                        Main.dust[dust_index].velocity = new Vector2(velocity_x, velocity_y); //needs to be reset when setting gravity
                    }
                    if (remove_light)
                    {
                        Main.dust[dust_index].noLight = true;
                    }

                }
            }
        }
    }

    public abstract class ProjStatusVisual : SyncingProjectile
    {
        protected enum POSITION : byte
        {
            CENTER,
            ABOVE,
            BOTTOM,
        }

        protected int status_index;
        private bool run_already = false;
        protected MyPlayer owner_myPlayer;
        protected POSITION position_type = POSITION.CENTER;

        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool CanHitPlayer(Player target)
        {
            return false;
        }
        public override bool CanHitPvp(Player target)
        {
            return false;
        }

        public override void AI()
        {
            if (!run_already)
            {
                owner_myPlayer = Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod);
                if (Main.LocalPlayer.whoAmI == owner_myPlayer.player.whoAmI)
                {
                    if ((owner_myPlayer.status_visuals[status_index] != null) && !owner_myPlayer.status_visuals[status_index].Equals(projectile))
                    {
                        owner_myPlayer.status_visuals[status_index].Kill();
                    }
                }
                owner_myPlayer.status_visuals[status_index] = projectile;
                run_already = true;
            }

            projectile.timeLeft = int.MaxValue;

            if (!owner_myPlayer.status_active[status_index])
            {
                projectile.Kill();
            }

            Vector2 new_pos = Main.player[projectile.owner].Center;
            switch (position_type)
            {
                case (POSITION.CENTER):
                    new_pos.X -= projectile.width / 2;
                    new_pos.Y -= projectile.height / 2;
                    break;
                case (POSITION.ABOVE):
                    new_pos.X -= projectile.width / 2;
                    new_pos.Y -= (Main.LocalPlayer.height * 0.66f) + projectile.height;
                    break;
                case (POSITION.BOTTOM):
                    new_pos.X -= projectile.width / 2;
                    new_pos.Y -= (projectile.height - Main.LocalPlayer.height / 2);
                    break;
                default:
                    break;
            }
            projectile.position = new_pos;

            base.AI();
        }

        public override void Kill(int timeLeft)
        {
            if ((owner_myPlayer.status_visuals[status_index] != null) && owner_myPlayer.status_visuals[status_index].Equals(projectile))
            {
                owner_myPlayer.status_visuals[status_index] = null;
            }

            base.Kill(timeLeft);
        }
    }

    public abstract class ProjNoVisual : SyncingProjectile
    {
        public override string Texture
        {
            get
            {
                return mod.Name + "/Abilities/Placeholder";
            }
        }
    }

    public abstract class ProjCycleFrames : SyncingProjectile
    {
        protected int NUMBER_FRAMES = 1;
        protected int TICKS_PER_FRAME = 10;

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = NUMBER_FRAMES;
        }
        public override void AI()
        {
            if (++projectile.frameCounter >= TICKS_PER_FRAME)
            {
                projectile.frameCounter = 0;
                if (++projectile.frame >= NUMBER_FRAMES)
                {
                    projectile.frame = 0;
                }
            }
            base.AI();
        }
    }

    public abstract class SyncingProjectile : ModProjectile
    {
        //sync every N seconds and when a player joins/leaves (can be disabled with DO_SYNC == false)

        private static ulong global_sync_counter = 0;

        protected bool DO_TIMED_SYNC = true;
        protected ushort SYNC_EVERY_N_SECONDS = 120;
        private DateTime next_sync = DateTime.MinValue;

        private ulong sync_counter;

        public override void SetDefaults()
        {
            //default all mod projectiles to not hit anything
            projectile.friendly = false;
            projectile.hostile = false;
            projectile.tileCollide = false;
        }

        public SyncingProjectile()
        {
            sync_counter = global_sync_counter;
        }

        public override void AI()
        {
            base.AI();

            if (projectile.owner == Main.LocalPlayer.whoAmI)
            {
                //trigger sync when # of players changes (static)
                if (ExperienceAndClasses.sync_local_proj)
                {
                    ExperienceAndClasses.sync_local_proj = false;
                    if (global_sync_counter == ulong.MaxValue)
                    {
                        global_sync_counter = 0;
                    }
                    else
                    {
                        global_sync_counter++;
                    }
                }

                //instance sync
                if (sync_counter != global_sync_counter)
                {
                    sync_counter = global_sync_counter;
                    projectile.netUpdate = true;
                }
                else if (DO_TIMED_SYNC)
                {
                    DateTime now = DateTime.Now;
                    if (next_sync.CompareTo(now) <= 0)
                    {
                        next_sync = now.AddSeconds(SYNC_EVERY_N_SECONDS);
                        projectile.netUpdate = true;
                    }
                }
            }
        }

    }

}
