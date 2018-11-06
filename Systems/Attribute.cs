using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.Systems {
    class Attribute {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum ATTRIBUTE_IDS : byte {
            Power,
            Vitality,
            Spirit,
            Agility,
            Precision,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        //this may be reordered, UI uses this order
        public ATTRIBUTE_IDS[] ATTRIBUTES_UI_ORDER = new ATTRIBUTE_IDS[] { ATTRIBUTE_IDS.Power, ATTRIBUTE_IDS.Vitality, ATTRIBUTE_IDS.Spirit, ATTRIBUTE_IDS.Agility, ATTRIBUTE_IDS.Precision };

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static Attribute[] ATTRIBUTE_LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static Attribute() {
            ATTRIBUTE_LOOKUP = new Attribute[(byte)ATTRIBUTE_IDS.NUMBER_OF_IDs];

            string name, desc;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public Attribute() {

        }
    }

    class PowerScaling {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public enum POWER_SCALING_TYPES : byte {
            NONE,
            MELEE,
            RANGED,
            MAGIC,
            THROWING,
            MINION,
            ALL,
            ROGUE,

            //insert here

            NUMBER_OF_IDs, //leave this last
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static PowerScaling[] POWER_SCALING_LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static PowerScaling() {
            POWER_SCALING_LOOKUP = new PowerScaling[(byte)POWER_SCALING_TYPES.NUMBER_OF_IDs];

            byte id_byte;
            string name;
            float melee, ranged, magic, throwing, minion;

            for (POWER_SCALING_TYPES id = 0; id < POWER_SCALING_TYPES.NUMBER_OF_IDs; id++) {
                id_byte = (byte)id;

                //defaults
                name = "";
                melee = 0f;
                ranged = 0f;
                magic = 0f;
                throwing = 0f;
                minion = 0f;

                switch (id) {
                    case POWER_SCALING_TYPES.NONE:
                        name = "None";
                        break;

                    case POWER_SCALING_TYPES.MELEE:
                        name = "Melee";
                        melee = 1f;
                        break;

                    case POWER_SCALING_TYPES.RANGED:
                        name = "Ranged";
                        ranged = 1f;
                        break;

                    case POWER_SCALING_TYPES.MAGIC:
                        name = "Magic";
                        magic = 1f;
                        break;

                    case POWER_SCALING_TYPES.THROWING:
                        name = "Throwing";
                        throwing = 1f;
                        break;

                    case POWER_SCALING_TYPES.MINION:
                        name = "Minion";
                        minion = 1f;
                        break;

                    case POWER_SCALING_TYPES.ALL:
                        name = "Melee, Ranged, Magic, Throwing, Minion";
                        melee = 1f;
                        ranged = 1f;
                        magic = 1f;
                        throwing = 1f;
                        minion = 1f;
                        break;

                    case POWER_SCALING_TYPES.ROGUE:
                        name = "Melee, Throwing";
                        melee = 1f;
                        throwing = 1f;
                        break;
                }

                POWER_SCALING_LOOKUP[id_byte] = new PowerScaling(name, melee, ranged, magic, throwing, minion);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public string Name { get; private set; }
        public float Melee { get; private set; }
        public float Ranged { get; private set; }
        public float Magic { get; private set; }
        public float Throwing { get; private set; }
        public float Minion { get; private set; }
        public PowerScaling(string name, float melee, float ranged, float magic, float throwing, float minion) {
            Name = name;
            Melee = melee;
            Ranged = ranged;
            Magic = magic;
            Throwing = throwing;
            Minion = minion;
        }
    }
}
