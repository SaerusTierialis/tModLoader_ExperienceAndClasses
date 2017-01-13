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
        /// <param name="experience_needed"></param>
        /// <returns></returns>
        public static bool CraftWithExp(double experience_needed)
        {
            MyPlayer myPlayer = Main.LocalPlayer.GetModPlayer<MyPlayer>(mod);

            if (myPlayer.GetExp() >= experience_needed) //have enough exp
            {
                //take exp
                if (experience_needed > 0)
                {
                    if (Main.netMode == 0)
                        myPlayer.SubtractExp(experience_needed);
                    else
                    {
                        //tell server to reduce experience
                        Methods.PacketSender.ClientTellAddExp(-experience_needed);
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
        /// <param name="target_orb_value"></param>
        public static void AddRecipes_ExpOrbConversion(ModItem target, int target_orb_value)
        {
            int value;
            string value_str;
            for (int i = 0; i < values.Length; i++)
            {
                value = values[i];

                if (value != 1 && target_orb_value != 1) continue; //only convert to and from 1s for now

                if (value != 1) value_str = "" + value;
                else value_str = "";

                if (value > target_orb_value)
                {
                    //convert DOWN
                    Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Experience" + value_str), 1 } }, target, value / target_orb_value);
                }
                else if (value < target_orb_value)
                {
                    //convert UP
                    Commons.QuckRecipe(mod, new int[,] { { mod.ItemType("Experience" + value_str), target_orb_value / value } }, target);
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
            double experience_required = 0;
            if (tier > 0 && tier < TIER_LEVEL_REQUIREMENTS.Length) experience_required = Methods.Experience.GetExpReqForLevel(TIER_LEVEL_REQUIREMENTS[tier], true);

            //use ClassRecipe as starting point
            ModRecipe recipe = new ClassRecipes2(mod, experience_required);

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
