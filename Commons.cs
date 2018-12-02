using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

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
        public static Recipe QuckRecipe(Mod mod, int[,] ingredients, ModItem result, int numResult = 1, ModRecipe buildOn = null, ushort where = TileID.WorkBenches) {
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
            return recipe;
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

        public static string GetRecipeString(Recipe recipe, bool multiline=false) {
            string str = "";

            bool first = true;
            for (uint i = 0; i < recipe.requiredItem.Length; i++) {
                if (recipe.requiredItem[i].type > 0) {
                    if (!first) {
                        if (multiline) {
                            str += "\n";
                        }
                        else {
                            str += " + ";
                        }
                    }
                    else {
                        first = false;
                    }
                    str += "x" + recipe.requiredItem[i].stack + " " + recipe.requiredItem[i].Name;
                }
            }

            return str;
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
            //new method does not detect if type is wrong
            if ((tag != null) && (tag.ContainsKey(key))) {
                try {
                    return tag.Get<T>(key);
                }
                catch {
                    return defaultValue;
                }
            }
            else {
                return defaultValue;
            }

            /*
             * This method no longer works because tag get methods no long throw exceptions when name or type is wrong
             * 
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
            */
        }

        public static void Error(string message) {
            message = message + " (please report)";
            if (ExperienceAndClasses.IS_SERVER) {
                message = "ERROR from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_ERROR);
            }
            else {
                if (ExperienceAndClasses.IS_CLIENT) {
                    message = "ERROR from Player" + Main.LocalPlayer.whoAmI + ": " + message;
                    Main.NewText("Sending " + message, UI.Constants.COLOUR_MESSAGE_ERROR);
                    PacketHandler.Broadcast.Send(-1, (byte)Main.LocalPlayer.whoAmI, message);
                }
                else {
                    Main.NewText("ERROR: " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                }
            }
            ErrorLogger.Log(message);
        }

        public static void Trace(string message) {
            if (ExperienceAndClasses.IS_SERVER) {
                message = "TRACE from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_TRACE);
            }
            else {
                if (ExperienceAndClasses.IS_CLIENT) {
                    message = "TRACE from Player" + Main.LocalPlayer.whoAmI + ": " + message;
                    Main.NewText("Sending " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                    PacketHandler.Broadcast.Send(-1, (byte)Main.LocalPlayer.whoAmI, message);
                }
                else {
                    Main.NewText("TRACE: " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                }
            }
        }

        public static bool VersionIsOlder(int[] target, int[] reference) {
            int max_length = Math.Max(target.Length, reference.Length);
            int t, r;
            for (int i=0; i<max_length; i++) {
                if (target.Length > i) {
                    t = target[i];
                }
                else {
                    t = 0;
                }

                if (reference.Length > i) {
                    r = reference[i];
                }
                else {
                    r = 0;
                }

                if (t < r) {
                    //older
                    return true;
                }
                else if (t > r) {
                    //newer
                    return false;
                }
                //else this digit is the same, continue
            }

            //default to false (target is equal or more recent)
            return false;
        }

        public static void CenterUIElement(UIElement element, float x, float y) {
            element.Left.Set(x - (element.Width.Pixels / 2f), 0f);
            element.Top.Set(y - (element.Height.Pixels / 2f), 0f);
        }

    }
}