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
        public class AbilityVisual : ModProjectile
        {
            public override void AI()
            {
                //creates ability on-use dust effect, shows for all clients but dust scatter is unqiue for each client
                Player player = Main.player[projectile.owner];
                Color colour = AbilityMain.COLOUR_CLASS_TYPE[(int)projectile.ai[0]];
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), Main.rand.NextFloat(-5, +5), Main.rand.NextFloat(-5, +5), 150, colour);
                    Main.playerDrawDust.Add(dust);
                }
                projectile.Kill();
            }
        }

        public class HealProj
        {

            //add abstract class to reduce redundancy

            public class Initial : HomingProj
            {
                public override void SetDefaults()
                {
                    velocity_max = 30f;
                    projectile.friendly = true;
                    mode = MODES.POSITION;
                    direct = true;
                }
                public override void AI()
                {
                    base.AI();
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 0, 0, 50, Color.White, 0.25f);
                    if (projectile.Distance(target) < 20)
                    {
                        if (Main.LocalPlayer.whoAmI == projectile.owner)
                        {
                            Projectile.NewProjectile(target, new Vector2(0f), ExperienceAndClasses.mod.ProjectileType<Heal>(), projectile.damage, 0, projectile.owner, 0);
                        }
                        projectile.Kill();
                    }
                }
            }

            //dual purpose heal/dmg

            protected class Heal : HomingProj
            {
                public override void SetDefaults()
                {
                    velocity_max = 30f;
                    projectile.friendly = true;
                    mode = MODES.PLAYER;
                    direct = true;
                }
                public override void AI()
                {
                    base.AI();
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, ExperienceAndClasses.mod.DustType<Dusts.Dust_AbilityGeneric>(), 0, 0, 50, Color.White, 0.25f);
                    if (projectile.Distance(target) < 20)
                    {
                        //do effect

                        projectile.Kill();
                    }
                }
            }
        }



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
