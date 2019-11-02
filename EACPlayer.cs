using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    class EACPlayer : ModPlayer {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Fields ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public FieldsContainer Fields { get; private set; }
        /// <summary>
        /// A container to store fields with defaults in a way that is easy to (re)initialize
        /// </summary>
        public class FieldsContainer {
            /// <summary>
            /// Set true when local player enters world and when other players are first synced
            /// </summary>
            public bool initialized = false;

            /// <summary>
            /// Client password for multiplayer authentication
            /// | Not synced between clients
            /// </summary>
            public string password = "";

            public bool Is_Local = false; 
        }

        /// <summary>
        /// Character sheet containing classes, attributes, etc.
        /// </summary>
        public Systems.CharacterSheet CSheet = new Systems.CharacterSheet();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Init ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public override void Initialize() {
            Fields = new FieldsContainer();
            CSheet = new Systems.CharacterSheet();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public override void OnEnterWorld(Player player) {
            //Update netmode
            Shortcuts.UpdateNetmode();

            //set local player
            Shortcuts.LocalPlayerSet(this);

            //Set world password when entering in singleplayer, send password to server when entering multiplayer
            Systems.Password.UpdateLocalPassword();

            //TODO - sync class etc.
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load(TagCompound tag) {
            base.Load(tag);
            CSheet.Load(tag);
        }

        public override TagCompound Save() {
            TagCompound tag = base.Save();
            if (tag == null)
                tag = new TagCompound();
            return CSheet.Save(tag);
        }

    }
}
