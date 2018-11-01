﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Systems {
    static class Classes {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants (and readonly) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //DO NOT CHANGE THE ORDER OF IDs
        public enum ID : byte {
            New, //newly created chracters will momentarily have this class before being set to novice
            None, //no class selected (intentionally)
            Novice,
            Warrior,
            Ranger,
            Mage,
            Rogue,
            Summoner,
            Cleric,
            Hybrid,
            Knight,
            Berserker,
            Guardian,
            Trickshot,
            Engineer,
            Sniper,
            Mystic,
            Sage,
            Assassin,
            Ninja,
            Hivemind,
            SoulBinder,
            Saint,
            HybridPrime,

            //insert new class IDs here

            NUMBER_OF_IDs, //leave this last
        }

        //which classes to show in ui and where
        public static ID[,] class_locations = new ID[5,7];

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Treated like readonly ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static Class[] CLASS_LOOKUP { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Populate Lookup ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        static Classes() {
            CLASS_LOOKUP = new Class[(int)ID.NUMBER_OF_IDs];

            string name, desc;
            byte tier;
            Texture2D texture;
            ID id_prereq;

            for (Systems.Classes.ID id = 0; id < Systems.Classes.ID.NUMBER_OF_IDs; id++) {
                //defaults
                name = "Unknown" + id;
                desc = "";
                tier = 0;
                id_prereq = ID.New;
                texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Novice");

                //specifics
                switch (id) {
                    case ID.None:
                        name = "None";
                        break;

                    case ID.Novice:
                        name = "Novice";
                        desc = "TODO";
                        tier = 1;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Novice");
                        class_locations[0, 3] = id;
                        break;

                    case ID.Warrior:
                        name = "Warrior";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Warrior");
                        class_locations[1, 0] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Ranger:
                        name = "Ranger";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Ranger");
                        class_locations[1, 1] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Mage:
                        name = "Mage";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Mage");
                        class_locations[1, 2] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Rogue:
                        name = "Rogue";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Rogue");
                        class_locations[1, 3] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Summoner:
                        name = "Summoner";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Summoner");
                        class_locations[1, 4] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Cleric:
                        name = "Cleric";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Cleric");
                        class_locations[1, 5] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Hybrid:
                        name = "Hybrid";
                        desc = "TODO";
                        tier = 2;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Hybrid");
                        class_locations[1, 6] = id;
                        id_prereq = ID.Novice;
                        break;

                    case ID.Knight:
                        name = "Knight";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Knight");
                        class_locations[2, 0] = id;
                        id_prereq = ID.Warrior;
                        break;

                    case ID.Berserker:
                        name = "Berserker";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Berserker");
                        class_locations[3, 0] = id;
                        id_prereq = ID.Warrior;
                        break;

                    case ID.Guardian:
                        name = "Guardian";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Guardian");
                        class_locations[4, 0] = id;
                        id_prereq = ID.Warrior;
                        break;

                    case ID.Sniper:
                        name = "Sniper";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Sniper");
                        class_locations[2, 1] = id;
                        id_prereq = ID.Ranger;
                        break;

                    case ID.Trickshot:
                        name = "Trickshot";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Trickshot");
                        class_locations[3, 1] = id;
                        id_prereq = ID.Ranger;
                        break;

                    case ID.Engineer:
                        name = "Engineer";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Engineer");
                        class_locations[4, 1] = id;
                        id_prereq = ID.Ranger;
                        break;

                    case ID.Mystic:
                        name = "Mystic";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Mystic");
                        class_locations[2, 2] = id;
                        id_prereq = ID.Mage;
                        break;

                    case ID.Sage:
                        name = "Sage";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Sage");
                        class_locations[3, 2] = id;
                        id_prereq = ID.Mage;
                        break;

                    case ID.Assassin:
                        name = "Assassin";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Assassin");
                        class_locations[2, 3] = id;
                        id_prereq = ID.Rogue;
                        break;

                    case ID.Ninja:
                        name = "Ninja";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Ninja");
                        class_locations[3, 3] = id;
                        id_prereq = ID.Rogue;
                        break;

                    case ID.SoulBinder:
                        name = "Soul Binder";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_SoulBinder");
                        class_locations[2, 4] = id;
                        id_prereq = ID.Summoner;
                        break;

                    case ID.Hivemind:
                        name = "Hivemind";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Hivemind");
                        class_locations[3, 4] = id;
                        id_prereq = ID.Summoner;
                        break;

                    case ID.Saint:
                        name = "Saint";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_Saint");
                        class_locations[2, 5] = id;
                        id_prereq = ID.Cleric;
                        break;

                    case ID.HybridPrime:
                        name = "Hybrid Prime";
                        desc = "TODO";
                        tier = 3;
                        texture = ModLoader.GetTexture("ExperienceAndClasses/Items/ClassToken_HybridPrime");
                        class_locations[2, 6] = id;
                        id_prereq = ID.Hybrid;
                        break;
                }

                //add
                CLASS_LOOKUP[(byte)id] = new Class(id, name, desc, tier, texture, id_prereq);
            }
        }
    }

    class Class {
        public Classes.ID ID { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public byte Tier { get; private set; }
        public Texture2D Texture { get; private set; }
        public Classes.ID ID_Prereq { get; private set; }

        public Class(Classes.ID id, string name, string description, byte tier, Texture2D texture, Classes.ID id_prereq) {
            ID = id;
            Name = name;
            Description = description;
            Tier = tier;
            Texture = texture;
            ID_Prereq = id_prereq;
        }
    }
}
