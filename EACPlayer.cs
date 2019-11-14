using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameInput;
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

            public DateTime AFK_Time = DateTime.MaxValue; 
        }

        /// <summary>
        /// Character sheet containing classes, attributes, etc.
        /// </summary>
        public Systems.PSheet PSheet { get; private set; }

        /// <summary>
        /// Entity can be a player or an NPC and is used by the Status and Ability systems.
        /// </summary>
        public Utilities.Containers.Entity Entity { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Init ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public override void Initialize() {
            Fields = new FieldsContainer();
            PSheet = new Systems.PSheet(this);
            Entity = new Utilities.Containers.Entity(this);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public override void OnEnterWorld(Player player) {
            //Update netmode
            Shortcuts.UpdateNetmode();

            //set local player
            Shortcuts.LocalPlayerSet(this);

            //Set world password when entering in singleplayer, send password to server when entering multiplayer
            Systems.Password.UpdateLocalPassword();

            //initialize UI
            Shortcuts.InitializeUIs();

            //reset afk time
            NotAFK();
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

            //Main.NewText("test=" + PSheet.Classes.Primary.Class.Name + " " + PSheet.Classes.Primary.Unlocked);

            if (Fields.Is_Local) {
                ConfigServer config = Shortcuts.GetConfigServer;

                //afk (must be in post, not pre)
                if (config.AFKEnabled) {
                    //become afk?
                    if (!PSheet.Character.AFK && (Shortcuts.Now.CompareTo(Fields.AFK_Time) > 0)) {
                        PSheet.Character.SetAFK(true);
                    }
                }
                else {
                    //stop afk?
                    if (PSheet.Character.AFK) {
                        NotAFK();
                    }
                }

            }
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

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Misc ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Attempt to send mana. Returns true on success.
        /// </summary>
        /// <param name="cost"></param>
        /// <param name="regen_delay"></param>
        /// <returns></returns>
        public bool UseMana(int cost, bool regen_delay = true) {
            //mana flower: use potion if it makes the difference
            if ((Main.LocalPlayer.statMana < cost) && Main.LocalPlayer.manaFlower) {
                Item mana_item = Main.LocalPlayer.QuickMana_GetItemToUse();
                if (mana_item != null) {
                    if ((Main.LocalPlayer.statMana + mana_item.healMana) >= cost) {
                        player.QuickMana();
                    }
                }
            }

            if (player.statMana >= cost) {
                //take mana (has enough)
                player.statMana -= cost;
                if (player.statMana < 0) player.statMana = 0;
                player.netMana = true;
                if (regen_delay) {
                    player.manaRegenDelay = Math.Min(200, player.manaRegenDelay + 50);
                }
                return true;
            }
            else {
                //not enough mana
                return false;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            //AFK
            if (triggersSet.KeyStatus.ContainsValue(true)) {
                NotAFK();
            }

            //Hotkey - UI
            if (Shortcuts.HOTKEY_UI.JustPressed) {
                if (UI.UIMain.Instance.Visibility) {
                    Main.PlaySound(Terraria.ID.SoundID.MenuClose);
                }
                else {
                    Main.PlaySound(Terraria.ID.SoundID.MenuOpen);
                }
                UI.UIHelp.Instance.Visibility = false;
                UI.UIMain.Instance.Visibility = !UI.UIMain.Instance.Visibility;
            }
        }

        private void NotAFK() {
            if (PSheet.Character.AFK) {
                PSheet.Character.SetAFK(false);
            }
            Fields.AFK_Time = Shortcuts.Now.AddSeconds(Shortcuts.GetConfigServer.AFKSeconds);
        }

    }
}
