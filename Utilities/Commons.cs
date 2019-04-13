using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace ExperienceAndClasses.Utilities {
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
            if (buildOn == null) {
                recipe = new ModRecipe(mod);
            }
            else {
                recipe = buildOn;
            }

            //where to craft (use MaxValue to skip)
            if (where != ushort.MaxValue) {
                recipe.AddTile(where);
            }

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
        /// Returns a string representation of an item's recipe for UI or any other purpose
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="multiline"></param>
        /// <returns></returns>
        public static string GetRecipeString(Recipe recipe, bool multiline=false) {
            string str = "";

            RecipeGroup group;
            bool first = true;
            string name;

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

                    //get group name if applicable
                    name = recipe.requiredItem[i].Name;
                    foreach (int group_id in recipe.acceptedGroups) {
                        if (RecipeGroup.recipeGroups.TryGetValue(group_id, out group)) {
                            if (group.ContainsItem(recipe.requiredItem[i].type)) {
                                name = group.GetText.Invoke();
                            }
                        }
                    }

                    str += "x" + recipe.requiredItem[i].stack + " " + name;
                }
            }

            return str;
        }

        /// <summary>
        /// Try to get from tag, else default to specified value.
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
        }

        /// <summary>
        /// Log error message and display for server and all players
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message) {
            message = message + " (please report)";
            if (Utilities.Netmode.IS_SERVER) {
                message = "ERROR from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_ERROR);
            }
            else {
                if (Utilities.Netmode.IS_CLIENT) {
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

        /// <summary>
        /// Display message for server and all players
        /// </summary>
        /// <param name="message"></param>
        public static void Trace(string message) {
            if (Utilities.Netmode.IS_SERVER) {
                message = "TRACE from Server: " + message;
                Console.WriteLine(message);
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(message), UI.Constants.COLOUR_MESSAGE_TRACE);
            }
            else {
                if (Utilities.Netmode.IS_CLIENT) {
                    message = "TRACE from Player" + Main.LocalPlayer.whoAmI + ": " + message;
                    Main.NewText("Sending " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                    PacketHandler.Broadcast.Send(-1, (byte)Main.LocalPlayer.whoAmI, message);
                }
                else {
                    Main.NewText("TRACE: " + message, UI.Constants.COLOUR_MESSAGE_TRACE);
                }
            }
        }

        /// <summary>
        /// Checks whether target version is earlier then reference. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
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

        public static T CreateObjectFromName<T>(string name, Type partent_type = null) {
            if (partent_type == null)
                return (T)(Assembly.GetExecutingAssembly().CreateInstance(typeof(T).FullName + "+" + name));
            else {
                return (T)(Assembly.GetExecutingAssembly().CreateInstance(partent_type.FullName + "+" + name));
            }
        }

        public static bool IsNonMinionProjectileWeapon_ExceptMeleeWithHitANDProj(Item item) {
            if ((item.shoot > 0) && (item.damage > 0) && !IsMinionItem(item) && !(item.melee && !item.noMelee)) {
                //shoots some kind of projectile, does damage, isn't a minion item, and it IS NOT a melee weapon that deals melee hits
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsNonMinionProjectileWeapon_OnlyMeleeWithHitANDProj(Item item) {
            if ((item.shoot > 0) && (item.damage > 0) && !IsMinionItem(item) && (item.melee && !item.noMelee)) {
                //shoots some kind of projectile, does damage, isn't a minion item, and it IS a melee weapon that deals melee hits but also has a proj
                return true;
            }
            else {
                return false;
            }
        }

        public static bool IsMinionItem(Item item) {
            return (item.sentry || item.summon || item.DD2Summon);
        }
    }
}