using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Status-Specific ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public string Name { get; protected set; } = "default_name";
        public ushort Capacity { get; protected set; } = 100;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars Generic ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public IDs ID { get; private set; } = IDs.NONE;
        public byte ID_num { get; private set; } = (byte)IDs.NONE;
        public ushort Amount = 0;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Resource(IDs id, ushort amount_default = 0) {
            ID = id;
            ID_num = (byte)ID;
            Amount = amount_default;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Resources ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public class Bloodforce : Resource {
            public Bloodforce() : base(IDs.Bloodforce, 50) {
                Name = "Bloodforce";
                Capacity = 100;
            }
        }

    }
}
