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

        static int[] values = { 1, 100, 1000, 10000, 100000, 1000000 };
        /// <summary>
        /// Add recipes for converting between Exp Orb values.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="target"></param>
        /// <param name="targetOrbValue"></param>
        public static void AddRecipes_ExpOrbConversion(ModItem target, int targetOrbValue)
        {
            int value;
            string valueStr;
            for (int i = 0; i < values.Length; i++)
            {
                value = values[i];

                if (value != 1 && targetOrbValue != 1) continue; //only convert to and from 1s for now

                if (value != 1) valueStr = "" + value;
                else valueStr = "";

                if (value > targetOrbValue)
                {
                    //convert DOWN
                    Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Experience" + valueStr), 1 } }, target, value / targetOrbValue);
                }
                else if (value < targetOrbValue)
                {
                    //convert UP
                    Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Experience" + valueStr), targetOrbValue / value } }, target);
                }
            }
        }

        public static int[] TIER_LEVEL_REQUIREMENTS = new int[] { 0, 0, 10, 25 }; //starts at tier 0 even though there is no tier 0
        /// <summary>
        /// Creates the base for a token recipe including experience and standard item requirements.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="tier"></param>
        /// <returns></returns>
        public static ModRecipe GetTokenRecipeBase(int tier)
        {
            //experience required
            double experienceRequired = 0;
            if (tier > 0 && tier < TIER_LEVEL_REQUIREMENTS.Length) experienceRequired = Methods.Experience.GetExpReqForLevel(TIER_LEVEL_REQUIREMENTS[tier], true);

            //use ClassRecipe as starting point
            ModRecipe recipe = new ClassRecipes2(mod, experienceRequired);

            //add standard item requirements (not class specific)
            switch (tier)
            {
                case 1:
                    //novice currently requires nothing
                    break;
                case 2:
                    recipe.AddIngredient(mod.ItemType("Monster_Orb"), 1);
                    break;
                case 3:
                    recipe.AddIngredient(mod.ItemType("Boss_Orb"), 5);
                    recipe.AddIngredient(mod.ItemType("Monster_Orb"), 50);
                    break;
                default:
                    break;
            }

            //ready to add specific item requirements and the result item
            return recipe;
        }
    }
}
