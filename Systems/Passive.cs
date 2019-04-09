using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    public abstract class Passive {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        /// <summary>
        /// includes ability unlocking, resource unlocking, ability altering, and passive effects (i.e., toggles status)
        /// </summary>
        public enum IDs : ushort {
            Warrior_BlockPerfect,
            Warrior_MoraleBoost,
            BloodKnight_Resoruce_Bloodforce,

            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        public enum PASSIVE_TYPE : byte {
            RESOURCE_UNLOCK,
            RESOURCE_UPGRADE,
            ABILITY_UPGRADE,
            BONUS,
            OTHER,
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

        public PASSIVE_TYPE Specific_Type { get; protected set; } = PASSIVE_TYPE.ABILITY_UPGRADE;

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

        protected Systems.Ability.IDs specific_ability = Systems.Ability.IDs.NONE;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;
        public ushort ID_num { get; private set; } = (ushort)IDs.NONE;
        public Texture2D Texture { get; private set; } = null;
        public bool Unlocked { get; private set; } = false;
        protected string ability_name = "";
        public string Tooltip { get; private set; } = "";

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
            if (Specific_Type == PASSIVE_TYPE.ABILITY_UPGRADE) {
                if (specific_ability != Systems.Ability.IDs.NONE) {
                    Texture = Systems.Ability.LOOKUP[(ushort)specific_ability].Texture;
                }
                else {
                    Texture = Utilities.Textures.TEXTURE_ABILITY_DEFAULT;
                }
            }
            else if ((Specific_Type == PASSIVE_TYPE.RESOURCE_UNLOCK) || (Specific_Type == PASSIVE_TYPE.RESOURCE_UPGRADE)) {
                if (specific_resource == Systems.Resource.IDs.NONE) {
                    Texture = Systems.Resource.LOOKUP[(byte)specific_resource].Texture;
                }
                else {
                    Texture = Utilities.Textures.TEXTURE_RESOURCE_DEFAULT;
                }
            }
            else if (specific_texture_path == null) {
                Texture = Utilities.Textures.TEXTURE_PASSIVE_DEFAULT;
            }
            else {
                Texture = ModLoader.GetTexture(specific_texture_path);
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

        public void UpdateTooltip() {
            //todo Tooltip
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Templates ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class AbilityUgrade : Passive {
            public AbilityUgrade(IDs id, Systems.Ability.IDs ability_id) : base(id) {
                Specific_Type = PASSIVE_TYPE.ABILITY_UPGRADE;
                ability_name = Systems.Ability.LOOKUP[(ushort)ability_id].Specific_Name;
                specific_ability = ability_id;
            }
        }

        /// <summary>
        /// defaults the name to resource's name
        /// </summary>
        public class ResourceUnlock : Passive {
            public ResourceUnlock(IDs id, Systems.Resource.IDs resource_id) : base(id) {
                Specific_Name = Systems.Resource.LOOKUP[(byte)resource_id].Specific_Name;
                Specific_Type = PASSIVE_TYPE.RESOURCE_UNLOCK;
                specific_resource = resource_id;
            }
        }

        public class ResourceUpdate : Passive {
            public ResourceUpdate(IDs id, Systems.Resource.IDs resource_id) : base(id) {
                Specific_Type = PASSIVE_TYPE.RESOURCE_UPGRADE;
                specific_resource = resource_id;
            }
        }

        public class Bonus : Passive {
            public Bonus(IDs id) : base(id) {
                Specific_Type = PASSIVE_TYPE.BONUS;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Warrior ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Warrior_BlockPerfect : AbilityUgrade {
            public const float DURATION_SECONDS = 0.5f;

            public Warrior_BlockPerfect() : base(IDs.Warrior_BlockPerfect, Systems.Ability.IDs.Warrior_Block) {
                Specific_Name = "Perfect Block";
                specific_description = "TODO";
                Specific_Required_Class_ID = Systems.Class.IDs.Warrior;
                Specific_Required_Class_Level = 10;
            }
        }

        public class Warrior_MoraleBoost : AbilityUgrade {
            public Warrior_MoraleBoost() : base(IDs.Warrior_MoraleBoost, Systems.Ability.IDs.Warrior_Block) {
                Specific_Name = "Morale Boost";
                specific_description = "TODO";
                Specific_Required_Class_ID = Systems.Class.IDs.Warrior;
                Specific_Required_Class_Level = 40;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Blood Knight ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class BloodKnight_Resoruce_Bloodforce : ResourceUnlock {
            public BloodKnight_Resoruce_Bloodforce() : base(IDs.BloodKnight_Resoruce_Bloodforce, Systems.Resource.IDs.Bloodforce) {
                specific_description = "TODO";
                Specific_Required_Class_ID = Systems.Class.IDs.BloodKnight;
                Specific_Required_Class_Level = 100;
            }
        }

    }
}
