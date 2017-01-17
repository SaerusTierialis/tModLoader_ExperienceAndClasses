using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Recipes
{
    public static class Helpers
    {
        static Mod mod = ModLoader.GetMod("ExperienceAndClasses");

        /// <summary>
        /// Returns true if the player had enough experience, else false.
        /// If this returns false in a OnCraft, you must "Main.mouseItem.stack--;" to prevent exploit.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="experienceNeeded"></param>
        /// <returns></returns>
        public static bool CraftWithExp(double experienceNeeded)
        {
            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);

            if (myPlayer.GetExp() >= experienceNeeded) //have enough exp
            {
                //take exp
                if (experienceNeeded > 0)
                {
                    if (Main.netMode == 0)
                        myPlayer.SubtractExp(experienceNeeded);
                    else
                    {
                        //tell server to reduce experience
                        Methods.PacketSender.ClientTellAddExp(-experienceNeeded);
                    }
                }

                //success
                return true;
            }
            else
            {
                //fail
                return false;
            }
        }
    }
}
