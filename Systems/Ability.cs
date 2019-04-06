using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace ExperienceAndClasses.Systems {
    public abstract class Ability {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public enum IDs : ushort {
            Block,


            NUMBER_OF_IDs, //leave this second to last
            NONE, //leave this last
        }

        public enum USE_RESULT : byte {
            SUCCESS,
            FAIL_NOT_ENOUGH_MANA,
            FAIL_NOT_ENOUGH_RESOURCE,
            FAIL_ON_COOLDOWN,
            FAIL_LINE_OF_SIGH,
            FAIL_NO_TARGET,
            FAIL_BUFF,
            FAIL_STATUS,
            FAIL_DEAD,
            FAIL_CHANNELLING,
            FAIL_SPECIFIC,
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Auto-Populated Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// singleton instanstances for packet-recieving (do NOT attach these to targets)
        /// </summary>
        public static Ability[] LOOKUP { get; private set; }

        static Ability() {
            LOOKUP = new Ability[(ushort)Ability.IDs.NUMBER_OF_IDs];
            for (byte i = 0; i < LOOKUP.Length; i++) {
                LOOKUP[i] = Utilities.Commons.CreateObjectFromName<Ability>(Enum.GetName(typeof(IDs), i));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Status-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Specific_Name { get; protected set; } = "default_name";

        public string Specific_Description { get; protected set; } = "default_description";



        public int Specific_Mana_Cost_Flat { get; protected set; } = 0;

        public float Specific_Mana_Cost_Percent { get; protected set; } = 0f;

        public bool Specific_Mana_Apply_Reduction { get; protected set; } = true;



        public Systems.Resource Specific_Resource { get; protected set; } = null;

        public int Specific_Resource_Cost_Flat { get; protected set; } = 0;

        public float Specific_Resource_Cost_Percent { get; protected set; } = 0f;



        public float Specific_Cooldown_Seconds { get; protected set; } = 0f;

        public bool Specified_Cooldown_Apply_Reduction { get; protected set; } = true;



        public List<int> Specific_Antirequisite_Buff_IDs { get; protected set; } = new List<int>();

        public List<Systems.Status> Specific_Antirequisite_Statuses { get; protected set; } = new List<Status>();



        public bool Specific_Can_Be_Used_While_Channelling { get; protected set; } = false;



        public Systems.Class.IDs Specific_Required_Class_ID { get; protected set; } = Systems.Class.IDs.None;

        public byte Specific_Required_Class_Level { get; protected set; } = 0;



        public float Specific_Base_Range { get; protected set; } = 0;

        public float Specific_Base_Magnitude { get; protected set; } = 0;



        public float Specific_Level_Multiplier_Range { get; protected set; } = 0;

        public float Specific_Level_Multiplier_Magnitude { get; protected set; } = 0;



        public float[] Specific_Attribute_Multiplier_Magnitude { get; protected set; } = Enumerable.Repeat(1f, (byte)Systems.Attribute.IDs.NUMBER_OF_IDs).ToArray();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic (between activations) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;

        public ushort ID_num { get; private set; } = (ushort)IDs.NONE;

        public DateTime Time_Cooldown_End { get; protected set; } = DateTime.MinValue;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic (within activation) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private int mana_cost;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Core Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Ability(IDs id) {
            ID = id;
            ID_num = (ushort)id;

            //statuses that lock out ability use
            Specific_Antirequisite_Buff_IDs.Add(BuffID.Silenced);
            Specific_Antirequisite_Buff_IDs.Add(BuffID.Stoned);
            Specific_Antirequisite_Buff_IDs.Add(BuffID.Frozen);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Public Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void Activate() {
            //check if activation is allowed
        }

        public bool CanUse {
            get {
                return TryUse() == USE_RESULT.SUCCESS;
            }
        }

        public int ManaCost {
            get {
                return 0;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Private Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private USE_RESULT TryUse() {
            //mana

            //resource

            //cooldown

            //line of sight

            /*
            FAIL_NOT_ENOUGH_MANA,
            FAIL_NOT_ENOUGH_RESOURCE,
            FAIL_ON_COOLDOWN,
            FAIL_LINE_OF_SIGH,
            FAIL_NO_TARGET,
            FAIL_BUFF,
            FAIL_STATUS,
            FAIL_DEAD,
            FAIL_CHANNELLING,
            FAIL_SPECIFIC,
            */

            //can use!
            return USE_RESULT.SUCCESS;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Private Override Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

            /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Warrior ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public class Block : Ability {
            public Block() : base(IDs.Block) {

            }
        }

    }
}
