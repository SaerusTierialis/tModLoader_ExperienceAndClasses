using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using static Terraria.ModLoader.ModContent;

namespace ExperienceAndClasses {
    public class EACPlayer : ModPlayer {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private const double MINUTES_BETWEEN_FULL_SUNC = 120;

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

            /// <summary>
            /// Set during init
            /// </summary>
            public bool Is_Local = false;

            /// <summary>
            /// Time until local player becomes afk (local only)
            /// </summary>
            public DateTime time_become_AFK = DateTime.MaxValue;

            /// <summary>
            /// Time until local player is no longer in combat (local only)
            /// </summary>
            public DateTime time_end_in_combat = DateTime.MinValue;

            /// <summary>
            /// Time until local player should send a full sync (local only)
            /// </summary>
            public DateTime time_full_sync = DateTime.MaxValue;

            /// <summary>
            /// Item animation number on prior cycle (for tracking item use)
            /// </summary>
            public int prior_item_animation_number = 0;

            /// <summary>
            /// List of minions including sentries. Includes each part of multi-part minions. Updates on CheckMinions().
            /// </summary>
            public List<Projectile> minions = new List<Projectile>();

            /// <summary>
            /// List of minions including only those that take minion slots. Updates on CheckMinions().
            /// </summary>
            public List<Projectile> slot_minions = new List<Projectile>();
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

        public override void OnEnterWorld(Player player) {
            //Update time
            Shortcuts.UpdateTime();

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
            //TODO - send other?

            Fields.time_full_sync = DateTime.Now.AddMinutes(MINUTES_BETWEEN_FULL_SUNC);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PreUpdateBuffs() {
            base.PreUpdateBuffs();

            //reinit stats, attributes, etc.
            PSheet.PreUpdate();
        }

        public override void PostUpdateEquips() {
            base.PostUpdateEquips();

            //apply attributes etc.
            PSheet.PostUpdate();
        }

        public override void PostUpdate() {
            base.PostUpdate();

            if (Fields.Is_Local) {
                ConfigServer config = Shortcuts.GetConfigServer;

                //afk (must be in post, not pre)
                if (config.AFKEnabled) {
                    //become afk?
                    if (!PSheet.Character.AFK && (Shortcuts.Now.CompareTo(Fields.time_become_AFK) > 0)) {
                        PSheet.Character.SetAFK(true);
                    }
                }
                else {
                    //stop afk?
                    if (PSheet.Character.AFK) {
                        NotAFK();
                    }
                }

                //in combat
                if (PSheet.Character.In_Combat && (Shortcuts.Now.CompareTo(Fields.time_end_in_combat) > 0)) {
                    PSheet.Character.SetInCombat(false);
                }

                //full sync
                if (Shortcuts.Now.CompareTo(Fields.time_full_sync) > 0) {
                    FullSync();
                }

            }

            //detect any item use
            bool used_item = (player.itemAnimation > Fields.prior_item_animation_number) && (!player.HeldItem.channel || player.channel);
            Fields.prior_item_animation_number = player.itemAnimation;
            if (used_item) {
                UsedItem(player.HeldItem);
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Use Items/Weapons ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Called by server + all clients
        /// Called only at start of channelling
        /// </summary>
        /// <param name="item"></param>
        private void UsedItem(Item item) {
            
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Items ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ModifyWeaponDamage(Item item, ref float add, ref float mult, ref float flat) {
            base.ModifyWeaponDamage(item, ref add, ref mult, ref flat);
            Systems.Combat.ModifyItemDamge(this, item, ref add, ref mult, ref flat);
        }

        public override float UseTimeMultiplier(Item item) {
            return Systems.Combat.ModifyItemUseTime(this, item, base.UseTimeMultiplier(item));
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Damage Taken ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// called locally and by server
        /// </summary>
        /// <param name="pvp"></param>
        /// <param name="quiet"></param>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        /// <param name="crit"></param>
        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
            base.Hurt(pvp, quiet, damage, hitDirection, crit);
            TriggerInCombat();
        }

        /// <summary>
        /// called by all if damage could possible occur (clients call repeatedly when another player is dead on a monster, etc.)
        /// </summary>
        /// <param name="pvp"></param>
        /// <param name="quiet"></param>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        /// <param name="crit"></param>
        /// <param name="customDamage"></param>
        /// <param name="playSound"></param>
        /// <param name="genGore"></param>
        /// <param name="damageSource"></param>
        /// <returns></returns>
        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource) {
            //check if hit
            bool hit = base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);

            //dodge (local check)
            if (hit && Fields.Is_Local && (Main.rand.NextFloat(0f, 1f) < PSheet.Stats.Dodge)) {
                player.ShadowDodge();
                return false;
            }

            return hit;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Damage Dealt (local) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit) {
            Systems.Combat.LocalModifyDamageDealt(this, new Systems.Combat.DamageSource(item), ref damage, ref crit);
            base.ModifyHitNPC(item, target, ref damage, ref knockback, ref crit);
        }

        public override void ModifyHitPvp(Item item, Player target, ref int damage, ref bool crit) {
            Systems.Combat.LocalModifyDamageDealt(this, new Systems.Combat.DamageSource(item), ref damage, ref crit);
            base.ModifyHitPvp(item, target, ref damage, ref crit);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            Systems.Combat.LocalModifyDamageDealt(this, new Systems.Combat.DamageSource(proj), ref damage, ref crit, true, player.Distance(target.position));
            base.ModifyHitNPCWithProj(proj, target, ref damage, ref knockback, ref crit, ref hitDirection);
        }

        public override void ModifyHitPvpWithProj(Projectile proj, Player target, ref int damage, ref bool crit) {
            Systems.Combat.LocalModifyDamageDealt(this, new Systems.Combat.DamageSource(proj), ref damage, ref crit, true, player.Distance(target.position));
            base.ModifyHitPvpWithProj(proj, target, ref damage, ref crit);
        }

        /// <summary>
        /// called locally only
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="victim"></param>
        public override void OnHitAnything(float x, float y, Entity victim) {
            TriggerInCombat();
            base.OnHitAnything(x, y, victim);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Death ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
            base.Kill(damage, hitDirection, pvp, damageSource);

            //ends in-combat
            PSheet.Character.SetInCombat(false);

            //death penalty
            if (Fields.Is_Local)
                Systems.XP.Adjustments.LocalDeathPenalty();
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

        public void CheckMinions() {
            Fields.minions = new List<Projectile>();
            Fields.slot_minions = new List<Projectile>();
            foreach (Projectile p in Main.projectile) {
                if (p.active && (p.minion || p.sentry) && (p.owner == player.whoAmI)) {
                    Fields.minions.Add(p);
                    if (p.minionSlots > 0) {
                        Fields.slot_minions.Add(p);
                    }
                }
            }
        }

        public void LocalDestroyMinions() {
            if (Fields.Is_Local) {
                CheckMinions();
                if (Fields.minions.Count > 0) {
                    Main.NewText("Your minions have been despawned.", UI.Constants.COLOUR_MESSAGE_ERROR);
                    foreach (Projectile p in Fields.minions) {
                        p.Kill();
                    }
                }
            }
            else {
                Utilities.Logger.Error("LocalDestroyMinions called by non-local");
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
            Fields.time_become_AFK = Shortcuts.Now.AddSeconds(Shortcuts.GetConfigServer.AFKSeconds);
        }

        private void TriggerInCombat() {
            if (!PSheet.Character.In_Combat) {
                PSheet.Character.SetInCombat(true);
            }
            Fields.time_end_in_combat = Shortcuts.Now.AddSeconds(Systems.Combat.SECONDS_IN_COMBAT);
        }

    }
}
