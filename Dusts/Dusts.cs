using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Dusts
{
    public class Dust_OpenerAttack : Dusts
    {
        public override void OnSpawn(Dust dust)
        {
            dust.velocity.Y = Main.rand.Next(-10, 6) * 0.1f;
            dust.velocity.X *= 0.3f;
            dust.scale *= 0.7f;
        }
    }

    public class Dust_AbilityGeneric : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.scale = 2f;
        }
    }



    /* TEMPLATES */
    public abstract class Dusts : ModDust
    {
        public override bool MidUpdate(Dust dust)
        {
            if (!dust.noGravity)
            {
                dust.velocity.Y += 0.05f;
            }
            if (!dust.noLight)
            {
                float strength = dust.scale * 1.4f;
                if (strength > 1f)
                {
                    strength = 1f;
                }
                Lighting.AddLight(dust.position, 0.1f * strength, 0.2f * strength, 0.7f * strength);
            }
            return false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return new Color(lightColor.R, lightColor.G, lightColor.B, 10);
        }
    }
}
