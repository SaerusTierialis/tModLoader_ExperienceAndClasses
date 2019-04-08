using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Passive {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// includes ability unlocking, resource unlocking, ability altering, and passive effects (i.e., toggles status)
        /// </summary>
        public enum IDs : ushort {
            Warrior_BlockPerfect,
            BloodKnight_Resoruce_Bloodforce,

            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Passive[] LOOKUP { get; private set; }

        static Passive() {
            LOOKUP = new Passive[(ushort)IDs.NUMBER_OF_IDs];
            for (ushort i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Passive>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Status-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Specific_Name { get; protected set; } = "default_name";
        protected string specific_description = "default_description";

        protected string specific_texture_path = null;

        /// <summary>
        /// default is none
        /// </summary>
        public Systems.Class.IDs Specific_Required_Class_ID { get; protected set; } = Systems.Class.IDs.None;
        public byte Specific_Required_Class_Level { get; protected set; } = 0;

        /// <summary>
        /// Status to automatically add when the passive is unlocked. The status must be an AutoPassive. | default is NONE
        /// </summary>
        protected Systems.Status.IDs specific_status = Systems.Status.IDs.NONE;

        /// <summary>
        /// Having this status automatically enables this resource | default is NONE
        /// </summary>
        protected Systems.Resource.IDs specific_resource = Systems.Resource.IDs.NONE;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;
        public ushort ID_num { get; private set; } = (ushort)IDs.NONE;
        public Texture2D Texture { get; private set; } = null;
        public bool Unlocked { get; private set; } = false;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Core Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Passive(IDs id) {
            ID = id;
            ID_num = (ushort)id;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Run once during init
        /// </summary>
        public void LoadTexture() {
            if (specific_texture_path != null) {
                Texture = ModLoader.GetTexture(specific_texture_path);
            }
            else {
                Texture = Utilities.Textures.TEXTURE_PASSIVE_DEFAULT;
            }
        }

        /// <summary>
        /// Check if is correct (also sets Unlocked)
        /// </summary>
        public bool CorrectClass() {
            bool correct_class = false;
            Unlocked = false;

            //check primary
            Systems.Class c = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary;
            if (c.ID == Specific_Required_Class_ID) {
                correct_class = true;
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective >= Specific_Required_Class_Level) {
                    Unlocked = true;
                }
            }
            else {
                c = c.Prereq;
                while (c != null) {
                    if (c.ID == Specific_Required_Class_ID) {
                        correct_class = true;
                        Unlocked = true;
                        break;
                    }
                    c = c.Prereq;
                }
            }

            //check secondary
            c = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary;
            if (c.ID == Specific_Required_Class_ID) {
                correct_class = true;
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective >= Specific_Required_Class_Level) {
                    Unlocked = true;
                }
            }
            else {
                c = c.Prereq;
                while (c != null) {
                    if (c.ID == Specific_Required_Class_ID) {
                        correct_class = true;
                        Unlocked = true;
                        break;
                    }
                    c = c.Prereq;
                }
            }
            //either was correct class
            return correct_class;
        }

        public void Apply() {
            EnableResource();
            AddStatus();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Private Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void EnableResource() {
            if (specific_resource != Systems.Resource.IDs.NONE) {
                ExperienceAndClasses.LOCAL_MPLAYER.Resources.Add(specific_resource, Systems.Resource.LOOKUP[(byte)specific_resource]);
            }
        }

        private void AddStatus() {
            if (specific_status != Systems.Status.IDs.NONE) {
                Utilities.Containers.Thing self = ExperienceAndClasses.LOCAL_MPLAYER.thing;
                if (!self.HasStatus(specific_status)) {
                    Systems.Status.LOOKUP[(ushort)specific_status].AddAutoPassive(self);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Warrior ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Warrior_BlockPerfect : Passive {
            public const float DURATION_SECONDS = 5f;

            public Warrior_BlockPerfect() : base(IDs.Warrior_BlockPerfect) {
                Specific_Name = "Perfect Block";
                specific_description = "TODO";
                Specific_Required_Class_ID = Systems.Class.IDs.Warrior;
                Specific_Required_Class_Level = 10;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Blood Knight ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class BloodKnight_Resoruce_Bloodforce : Passive {
            public BloodKnight_Resoruce_Bloodforce() : base(IDs.BloodKnight_Resoruce_Bloodforce) {
                Specific_Name = "Bloodforce";
                specific_description = "TODO";
                Specific_Required_Class_ID = Systems.Class.IDs.BloodKnight;
                Specific_Required_Class_Level = 1;
                specific_resource = Systems.Resource.IDs.Bloodforce;
            }
        }

    }
}
