using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    public class EACPlayer : ModPlayer {
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
        public Systems.PSheet PSheet { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Init ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public override void Initialize() {
            Fields = new FieldsContainer();
            PSheet = new Systems.PSheet(this);
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sync ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            base.SyncPlayer(toWho, fromWho, newPlayer);
            FullSync();
        }

        private void FullSync() {
            Utilities.PacketHandler.FullSync.Send(this);
            //TODO - send statuses
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PreUpdate() {
            base.PreUpdate();

            PSheet.PreUpdate();
        }

        public override void PostUpdate() {
            base.PostUpdate();

            PSheet.PostUpdate();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Load(TagCompound tag) {
            base.Load(tag);
            PSheet.Load(tag);
        }

        public override TagCompound Save() {
            TagCompound tag = base.Save();
            if (tag == null)
                tag = new TagCompound();
            return PSheet.Save(tag);
        }

    }
}
