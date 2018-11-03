using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses {
    public static class Commons {
        /// <summary>
        /// Creates and finalizes a recipe. Ingredients must be formatted new int[,] { {id1, num1}, {id2, num2}, ... }. Can build on an existing recipe.
        /// NOTE: Duplicate items in a recipe cause a bug where only one stack is checked/needed. The method below can be used to solve this.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="ingredients"></param>
        /// <param name="result"></param>
        /// <param name="numResult"></param>
        /// <param name="where"></param>
        public static void QuckRecipe(Mod mod, int[,] ingredients, ModItem result, int numResult = 1, ModRecipe buildOn = null, ushort where = TileID.WorkBenches) {
            //recipe
            ModRecipe recipe;
            if (buildOn == null) recipe = new ModRecipe(mod);
            else recipe = buildOn;

            //where to craft (use MaxValue to skip)
            if (where != ushort.MaxValue) recipe.AddTile(where);

            //add ingredients
            if (ingredients.GetLength(1) == 2) {
                for (int i = 0; i < ingredients.GetLength(0); i++) {
                    recipe.AddIngredient(ingredients[i, 0], ingredients[i, 1]);
                }
            }

            //result
            recipe.SetResult(result, numResult);

            //complete
            recipe.AddRecipe();
        }

        /// <summary>
        /// Combines duplicate items and checks if the player has enough. Workaround for duplicate item recipe bug.
        /// Returns true if the player has enough of the item.
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public static bool EnforceDuplicatesInRecipe(ModRecipe recipe) {
            List<int> types = new List<int>();
            List<int> stacks = new List<int>();

            Item[] ingedients = recipe.requiredItem;
            int ind;
            for (int i = 0; i < ingedients.Length; i++) {
                ind = types.IndexOf(ingedients[i].type);
                if (ind >= 0) {
                    stacks[ind] += ingedients[i].stack;
                }
                else {
                    types.Add(ingedients[i].type);
                    stacks.Add(ingedients[i].stack);
                }
            }
            for (int i = 0; i < types.Count; i++) {
                if (Main.LocalPlayer.CountItem(types[i], stacks[i]) < stacks[i]) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Try to get from tag, else default to specified value. Supports int, float, double, bool, long, and string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tag"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T TryGet<T>(TagCompound tag, string key, T defaultValue) {
            try {
                T val;
                Type type = typeof(T);
                if (type == typeof(int)) val = (T)Convert.ChangeType(tag.GetInt(key), type);
                else if (type == typeof(float)) val = (T)Convert.ChangeType(tag.GetFloat(key), type);
                else if (type == typeof(double)) val = (T)Convert.ChangeType(tag.GetDouble(key), type);
                else if (type == typeof(bool)) val = (T)Convert.ChangeType(tag.GetBool(key), type);
                else if (type == typeof(long)) val = (T)Convert.ChangeType(tag.GetLong(key), type);
                else if (type == typeof(string)) val = (T)Convert.ChangeType(tag.GetString(key), type);
                else if (type == typeof(byte)) val = (T)Convert.ChangeType(tag.GetByte(key), type);
                else if (type == typeof(byte[])) val = (T)Convert.ChangeType(tag.GetByteArray(key), type);
                else if (type == typeof(int[])) val = (T)Convert.ChangeType(tag.GetIntArray(key), type);
                else throw new Exception();

                return val;
            }
            catch {
                return defaultValue;
            }
        }

        public static void Error(string message) {
            message = message + " (please report)";
            if (ExperienceAndClasses.IS_SERVER) {
                message = "Error from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), ExperienceAndClasses.COLOUR_MESSAGE_ERROR);
            }
            else {
                message = "Error from Player" + Main.LocalPlayer.whoAmI + ": " + message;
                Main.NewText(message, ExperienceAndClasses.COLOUR_MESSAGE_ERROR);
                if (ExperienceAndClasses.IS_CLIENT) PacketSender.SendBroadcastTrace((byte)Main.LocalPlayer.whoAmI, message);
            }
            ErrorLogger.Log(message);
        }

        public static void Trace(string message) {
            if (ExperienceAndClasses.IS_SERVER) {
                message = "TRACE from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), ExperienceAndClasses.COLOUR_MESSAGE_TRACE);
            }
            else {
                message = "TRACE from Player" + Main.LocalPlayer.whoAmI + ": " + message;
                Main.NewText(message, ExperienceAndClasses.COLOUR_MESSAGE_TRACE);
                if (ExperienceAndClasses.IS_CLIENT) PacketSender.SendBroadcastTrace((byte)Main.LocalPlayer.whoAmI, message);
            }
        }
    }
}