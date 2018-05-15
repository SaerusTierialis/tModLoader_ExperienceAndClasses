using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
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
                    Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod).sanctuaries[sanc_index] = projectile;
                    if (Main.LocalPlayer.Equals(Main.player[projectile.owner]))
                    {
                        local_owner = true;
                    }
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
                    DateTime now = DateTime.Now;
                    if (now.Subtract(time_next_pulse).TotalSeconds >= AbilityMain.Cleric_Active_Sanctuary.pulse_seconds)
                    {
                        //timing
                        time_next_pulse = now;

                        //effects
                        AbilityMain.Cleric_Active_Sanctuary.Pulse(projectile);
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

        public class Cleric_Barrier : SyncingProjectile
        {
            public override void SetDefaults()
            {
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
                    damage = (int)(damage * AbilityMain.Cleric_Active_Heal.undead_bonus_multiplier);
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
                projectile.timeLeft = 100;
                base.SetDefaults();
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
                                player.GetModPlayer<MyPlayer>(mod).time_last_combat = DateTime.Now;
                                Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod).time_last_combat = DateTime.Now;
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
                                Main.player[projectile.owner].GetModPlayer<MyPlayer>(mod).time_last_combat = DateTime.Now;
                            }
                        }
                    }

                    //done
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

        //////public abstract class HomingProj : ModProjectile
        //////{
        //////    public enum MODES : byte
        //////    {
        //////        POSITION,
        //////        NPC,
        //////        PLAYER,
        //////    }

        //////    public Vector2 target;
        //////    public float velocity_adjust = 0.5f;
        //////    public float velocity_max = 5f;
        //////    public bool initialized = false;
        //////    public bool direct = false;
        //////    public int targetIndex = 0;
        //////    public MODES mode = MODES.POSITION;

        //////    public override void AI()
        //////    {
        //////        if (!initialized)
        //////        {
        //////            if (mode == MODES.POSITION)
        //////            {
        //////                target = new Vector2(projectile.ai[0], projectile.ai[1]);
        //////            }
        //////            else
        //////            {
        //////                targetIndex = (int)projectile.ai[0];
        //////            }
        //////            initialized = true;
        //////        }
        //////        else
        //////        {
        //////            if (mode == MODES.NPC)
        //////            {
        //////                NPC npc = Main.npc[targetIndex];
        //////                if (npc.active)
        //////                {
        //////                    target = npc.position;
        //////                }
        //////                else
        //////                {
        //////                    projectile.Kill();
        //////                    return;
        //////                }
        //////            }
        //////            else if (mode == MODES.PLAYER)
        //////            {
        //////                Player player = Main.player[targetIndex];
        //////                if (player.active)
        //////                {
        //////                    target = player.position;
        //////                }
        //////                else
        //////                {
        //////                    projectile.Kill();
        //////                    return;
        //////                }
        //////            }

        //////            HomeOnto(projectile, target, velocity_adjust, velocity_max, direct);
        //////        }
        //////    }
        //////}

        //////public static void HomeOnto(Projectile projectile, Vector2 target, float velocity_adjust, float velocity_max, bool direct)
        //////{
        //////    if (!direct)
        //////    {
        //////        int direction;

        //////        direction = projectile.position.X < target.X ? 1 : -1;
        //////        projectile.velocity.X += direction * velocity_adjust;
        //////        if (projectile.velocity.X > velocity_max) projectile.velocity.X = velocity_max;
        //////        if (projectile.velocity.X < -velocity_max) projectile.velocity.X = -velocity_max;

        //////        direction = projectile.position.Y < target.Y ? 1 : -1;
        //////        projectile.velocity.Y += direction * velocity_adjust;
        //////        if (projectile.velocity.Y > velocity_max) projectile.velocity.Y = velocity_max;
        //////        if (projectile.velocity.Y < -velocity_max) projectile.velocity.Y = -velocity_max;
        //////    }
        //////    else
        //////    {
        //////        Vector2 d = projectile.DirectionTo(target);
        //////        projectile.velocity.X = d.X / (1 / velocity_max);
        //////        projectile.velocity.Y = d.Y / (1 / velocity_max);
        //////    }
        //////}
    }

    public class DustMakerProj : ProjNoVisual
    {
        public enum MODE : byte
        {
            ability_cast,
            heal,
        }

        public override void AI()
        {
            //setup
            Player player = Main.player[projectile.owner];

            //creates ability on-use dust effect, shows for all clients but dust scatter is unqiue for each client
            switch ((MODE)projectile.ai[0])
            {
                case MODE.ability_cast:
                    SpreadDust(player.Center, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 3, 5, 2, 150, AbilityMain.COLOUR_CLASS_TYPE[(int)projectile.ai[1]]);
                    break;
                case MODE.heal:
                    SpreadDust(projectile.position, DustID.AncientLight, 10, AbilityMain.Cleric_Active_Heal.range/6, 3, 150, Color.Red, true, true);
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
        private static int global_player_count = 0;

        protected bool DO_TIMED_SYNC = true;
        protected ushort SYNC_EVERY_N_SECONDS = 120;
        private DateTime next_sync = DateTime.MinValue;

        private ulong sync_counter;

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
                if (global_player_count != Main.ActivePlayersCount)
                {
                    global_player_count = Main.ActivePlayersCount;
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
