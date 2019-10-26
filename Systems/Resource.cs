using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Resource {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public enum IDs : byte {
            Bloodforce,


            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Resource[] LOOKUP { get; private set; }

        static Resource() {
            LOOKUP = new Resource[(byte)IDs.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Resource>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Resource-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Specific_Name { get; protected set; } = "default_name";
        public ushort Specific_Capacity { get; protected set; } = 10;

        protected ushort specific_default_value = 0;

        protected string specific_texture_path = null;

        protected bool passive_value_change = false;
        protected float passive_value_change_seconds = 1f;
        protected float passive_value_change_percent_in_combat = 0f;
        protected ushort passive_value_change_flat_in_combat = 0;
        protected float passive_value_change_percent_out_of_combat = 0f;
        protected ushort passive_value_change_flat_out_of_combat = 0;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;
        public byte ID_num { get; private set; } = (byte)IDs.NONE;
        public ushort Value { get; private set; } = 0;
        public Texture2D Texture { get; private set; }
        public Color colour = Systems.PlayerClass.COLOUR_DEFAULT;
        protected DateTime time_next_update;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Resource(IDs id) {
            ID = id;
            ID_num = (byte)ID;
        }

        public void LoadTexture() {
            if (specific_texture_path != null) {
                Texture = ModLoader.GetTexture(specific_texture_path);
            }
            else {
                Texture = Utilities.Textures.TEXTURE_RESOURCE_DEFAULT;
            }
        }

        /// <summary>
        /// Call when a resource is added by a passive and was not already in use
        /// </summary>
        public void Initialize() {
            Value = specific_default_value;
            time_next_update = Shortcuts.Now.AddSeconds(passive_value_change_seconds);
            OnInitialize();
        }

        /// <summary>
        /// Call on each cycle
        /// </summary>
        public void TimedUpdate() {
            if (passive_value_change) {
                if (Shortcuts.Now.CompareTo(time_next_update) > 0) {

                    time_next_update = Shortcuts.Now.AddSeconds(passive_value_change_seconds);

                    float change = 0f;
                    if (Shortcuts.LOCAL_PLAYER.IN_COMBAT) {
                        change += passive_value_change_flat_in_combat;
                        change += (passive_value_change_percent_in_combat * Specific_Capacity);
                    }
                    else {
                        change += passive_value_change_flat_out_of_combat;
                        change += (passive_value_change_percent_out_of_combat * Specific_Capacity);
                    }
                    change = ModifyPassiveValueChange(change);

                    AdjustValue((int)change);
                }
            }

            OnUpdate();
        }

        /// <summary>
        /// Limits to 0-to-Capacity and updates UI if value changed
        /// </summary>
        /// <param name="change"></param>
        public void AdjustValue(int change) {
            ushort new_value = (ushort)(Value + change);
            if (new_value > Specific_Capacity) {
                new_value = Specific_Capacity;
            }

            if (Value != new_value) {
                Value = new_value;
                UI.UIHUD.Instance.UpdateResource();
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Called when a resource is added by a passive and was not already in use - called after defaulting Value and time_next_update
        /// </summary>
        public virtual void OnInitialize() { }

        /// <summary>
        /// Called on each cycle after passive value changes
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// Modify the amount of passive value change
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        public virtual float ModifyPassiveValueChange(float change) { return change; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Resources ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Bloodforce : Resource {
            public Bloodforce() : base(IDs.Bloodforce) {
                Specific_Name = "Bloodforce";
                Specific_Capacity = 100;
                passive_value_change = true;
            }

            public override float ModifyPassiveValueChange(float change) {
                if (Value > 50) {
                    return -1;
                }
                else if (Value < 50) {
                    return +1;
                }
                else {
                    return 0;
                }
            }
        }

    }
}
