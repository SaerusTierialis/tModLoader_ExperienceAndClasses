using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Utilities {
    class Commons {
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
        /// Checks whether target version is earlier then reference. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static bool VersionIsOlder(int[] target, int[] reference) {
            int max_length = Math.Max(target.Length, reference.Length);
            int t, r;
            for (int i = 0; i < max_length; i++) {
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

        /// <summary>
        /// Create Object by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="parent_type"></param>
        /// <returns></returns>
        public static T CreateObjectFromName<T>(string name, Type parent_type = null) {
            if (parent_type == null)
                return (T)(Assembly.GetExecutingAssembly().CreateInstance(typeof(T).FullName + "+" + name));
            else {
                return (T)(Assembly.GetExecutingAssembly().CreateInstance(parent_type.FullName + "+" + name));
            }
        }

        /// <summary>
        /// Returns true if tile at position is not solid
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool PositionNonSolidTile(Vector2 position) {
            Point point = position.ToTileCoordinates();
            return (Main.tile[point.X, point.Y].collisionType != 1);
        }

        public static double Clamp(double value, double min, double max) {
            return Math.Max(min, Math.Min(max, value));
        }

    }
}
