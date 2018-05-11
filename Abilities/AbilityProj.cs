using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Abilities
{
    public class DustMakerProj : ModProjectile
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
                    SpreadDust(player.position, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 3, 5, 2, 150, AbilityMain.COLOUR_CLASS_TYPE[(int)projectile.ai[1]]);
                    break;
                case MODE.heal:
                    SpreadDust(projectile.position, DustID.AncientLight, 5, AbilityMain.Cleric_Active_Heal.range, 3, 150, Color.Red, true);
                    break;
                default:
                    break;
            }

            //done
            projectile.Kill();
        }

        private static void SpreadDust(Vector2 position, int dust_type, int loop_count, float velocity, float scale = 1, int alpha = 255, Color colour = default(Color), bool remove_gravity = false)
        {
            //NOTE: Math.Cos and Math.Sin were crashing for some reason so a better implementation was not possible
            if (loop_count < 3)
            {
                loop_count = 3;
            }
            float inc = 2f / (loop_count - 1);
            int dust_index;
            float velocity_use;
            for (float x = -1; x <= +1; x += inc)
            {
                for (float y = -1; y <= +1; y += inc)
                {
                    if (x == -1 || x == 1 || y == -1 || y == 1)
                    {
                        velocity_use = velocity / (float)Math.Sqrt(Math.Pow(x,2) + Math.Pow(y, 2));

                        dust_index = Dust.NewDust(position, 0, 0, dust_type, x * velocity_use, y * velocity_use, alpha, colour, scale);
                        if (remove_gravity)
                        {
                            Main.dust[dust_index].noGravity = true;
                            Main.dust[dust_index].velocity = new Vector2(x * velocity_use, y * velocity_use); //needs to be reset when setting gravity
                        }
                    }
                }
            }
        }
    }

    public class AbilityProj
    {

        //public class HealProj
        //{

        //    //add abstract class to reduce redundancy

        //    public class Initial : HomingProj
        //    {
        //        public override void SetDefaults()
        //        {
        //            velocity_max = 30f;
        //            projectile.friendly = true;
        //            mode = MODES.POSITION;
        //            direct = true;
        //        }
        //        public override void AI()
        //        {
        //            base.AI();
        //            Dust.NewDust(projectile.position, projectile.width, projectile.height, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 0, 0, 50, Color.White, 0.25f);
        //            if (projectile.Distance(target) < 20)
        //            {
        //                if (Main.LocalPlayer.whoAmI == projectile.owner)
        //                {
        //                    Projectile.NewProjectile(target, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<Heal>(), projectile.damage, 0, projectile.owner, 0);
        //                }
        //                projectile.Kill();
        //            }
        //        }
        //    }

        //    //dual purpose heal/dmg

        //    protected class Heal : HomingProj
        //    {
        //        public override void SetDefaults()
        //        {
        //            velocity_max = 30f;
        //            projectile.friendly = true;
        //            mode = MODES.PLAYER;
        //            direct = true;
        //        }
        //        public override void AI()
        //        {
        //            base.AI();
        //            Dust.NewDust(projectile.position, projectile.width, projectile.height, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 0, 0, 50, Color.White, 0.25f);
        //            if (projectile.Distance(target) < 20)
        //            {
        //                //do effect

        //                projectile.Kill();
        //            }
        //        }
        //    }
        //}



        public abstract class HomingProj : ModProjectile
        {
            public enum MODES : byte
            {
                POSITION,
                NPC,
                PLAYER,
            }

            public Vector2 target;
            public float velocity_adjust = 0.5f;
            public float velocity_max = 5f;
            public bool initialized = false;
            public bool direct = false;
            public int targetIndex = 0;
            public MODES mode = MODES.POSITION;

            public override void AI()
            {
                if (!initialized)
                {
                    if (mode == MODES.POSITION)
                    {
                        target = new Vector2(projectile.ai[0], projectile.ai[1]);
                    }
                    else
                    {
                        targetIndex = (int)projectile.ai[0];
                    }
                    initialized = true;
                }
                else
                {
                    if (mode == MODES.NPC)
                    {
                        NPC npc = Main.npc[targetIndex];
                        if (npc.active)
                        {
                            target = npc.position;
                        }
                        else
                        {
                            projectile.Kill();
                            return;
                        }
                    }
                    else if (mode == MODES.PLAYER)
                    {
                        Player player = Main.player[targetIndex];
                        if (player.active)
                        {
                            target = player.position;
                        }
                        else
                        {
                            projectile.Kill();
                            return;
                        }
                    }

                    HomeOnto(projectile, target, velocity_adjust, velocity_max, direct);
                }
            }
        }

        public static void HomeOnto(Projectile projectile, Vector2 target, float velocity_adjust, float velocity_max, bool direct)
        {
            if (!direct)
            {
                int direction;

                direction = projectile.position.X < target.X ? 1 : -1;
                projectile.velocity.X += direction * velocity_adjust;
                if (projectile.velocity.X > velocity_max) projectile.velocity.X = velocity_max;
                if (projectile.velocity.X < -velocity_max) projectile.velocity.X = -velocity_max;

                direction = projectile.position.Y < target.Y ? 1 : -1;
                projectile.velocity.Y += direction * velocity_adjust;
                if (projectile.velocity.Y > velocity_max) projectile.velocity.Y = velocity_max;
                if (projectile.velocity.Y < -velocity_max) projectile.velocity.Y = -velocity_max;
            }
            else
            {
                Vector2 d = projectile.DirectionTo(target);
                projectile.velocity.X = d.X / (1 / velocity_max);
                projectile.velocity.Y = d.Y / (1 / velocity_max);
            }
        }
    }
}
