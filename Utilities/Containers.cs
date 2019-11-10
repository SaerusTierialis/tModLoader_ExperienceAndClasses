using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace ExperienceAndClasses.Utilities.Containers {
    public class LevelSortedPassives : List<Systems.Passive> {
        private const int DEFAULT_CAPACITY = 100;

        public LevelSortedPassives() : base(DEFAULT_CAPACITY) { }
        public LevelSortedPassives(int capacity) : base(capacity) { }

        /// <summary>
        /// Adds status sorting by end time
        /// </summary>
        /// <param name="status"></param>
        public new void Add(Systems.Passive passive) {
            //is full?
            if (Count == Capacity) {
                Logger.Error("A passive cannot be added because there are not enough slots!");
                return;
            }

            //insert before first status that is equal/later
            byte tier_prior, tier_new;
            tier_new = Systems.PlayerClass.LOOKUP[passive.ID_num].Tier;
            for (int i = 0; i < Count; i++) {
                tier_prior = Systems.PlayerClass.LOOKUP[this[i].ID_num].Tier;
                if ((tier_prior > tier_new) || ((tier_prior == tier_new) && (this[i].Required_Class_Level >= passive.Required_Class_Level))) {
                    Insert(i, passive);
                    return;
                }
            }
            //default to insert at end
            base.Add(passive);
        }

        public bool Contains(Systems.Passive.IDs id) {
            foreach (Systems.Passive passive in this) {
                if (passive.ID == id) {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsUnlocked(Systems.Passive.IDs id) {
            foreach (Systems.Passive passive in this) {
                if ((passive.ID == id) && passive.Unlocked) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Can contain a player or an npc.
    /// Maintains a SortedDictionary of all things sorted by their Index.
    /// Index is identical for clients/server so this can be references in sync
    /// </summary>
    public class Entity {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static SortedDictionary<ushort, Entity> Entities { get; private set; } = new SortedDictionary<ushort, Entity>();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public readonly bool Is_Player;
        private readonly EACPlayer eacplayer;
        public readonly bool Is_Npc;
        private readonly EACNPC eacnpc;

        /// <summary>
        /// A reference index which is identical across clients/server. This Thing is Thing[Index].
        /// </summary>
        public readonly ushort Index;

        /// <summary>
        /// In singleplayer, all things are local.
        /// 
        /// To the server, all NPC are local.
        /// To the clients, their own player is local (not NPCs).
        /// </summary>
        public readonly bool Local;

        public static Entity GetThingPlayer(int player_index) {
            return Entities[GetIndexPlayer(player_index)];
        }

        public static Entity GetThingNPC(int npc_index) {
            return Entities[GetIndexNPC(npc_index)];
        }

        private static ushort GetIndexPlayer(int player_index) {
            return (ushort)player_index;
        }

        private static ushort GetIndexNPC(int npc_index) {
            return (ushort)(Main.maxPlayers + npc_index);
        }

        public Entity(EACPlayer eacplayer) {
            Is_Player = true;
            Is_Npc = false;

            this.eacplayer = eacplayer;
            eacnpc = null;

            Index = GetIndexPlayer(eacplayer.player.whoAmI);

            if ((Shortcuts.IS_SINGLEPLAYER) || (Shortcuts.IS_CLIENT && (whoAmI == Main.LocalPlayer.whoAmI))) {
                //singleplayer OR this is the local player on a client
                Local = true;
            }
            else {
                Local = false;
            }

            Add();
        }

        public Entity(EACNPC eacnpc) {
            Is_Player = false;
            Is_Npc = true;

            eacplayer = null;
            this.eacnpc = eacnpc;

            Index = GetIndexNPC(eacnpc.npc.whoAmI);

            if (!Shortcuts.IS_CLIENT) {
                //singleplayer OR this is an npc on the server
                Local = true;
            }
            else {
                Local = false;
            }

            Add();
        }

        /// <summary>
        /// Add to Things
        /// </summary>
        private void Add() {
            if (Entities.ContainsKey(Index)) {
                Entities[Index] = this;
            }
            else {
                Entities.Add(Index, this);
            }
        }

        public EACPlayer EACPlayer {
            get {
                if (eacplayer == null) {
                    Logger.Error("Attempted to get EACPlayer from non-player entity!");
                    return null;
                }
                else {
                    return eacplayer;
                }
            }
        }

        public EACNPC EACNPC {
            get {
                if (eacnpc == null) {
                    Logger.Error("Attempted to get EACNPC from non-NPC entity!");
                    return null;
                }
                else {
                    return eacnpc;
                }
            }
        }

        private void Remove() {
            Entities.Remove(Index);
        }

        /// <summary>
        /// Returns the player or npc index (player.whoAmI or npc.whoAmI)
        /// </summary>
        public int whoAmI {
            get {
                if (Is_Player) {
                    return eacplayer.player.whoAmI;
                }
                else {
                    int who = eacnpc.npc.whoAmI;
                    if (who < 0) {
                        Remove();
                    }
                    return who;
                }
            }
        }

        /// <summary>
        /// Returns whether the thing is alive. If an NPC is found to be dead, it is removed from Things.
        /// </summary>
        public bool Dead {
            get {
                if (Is_Player) {
                    return eacplayer.player.dead;
                }
                else {
                    bool dead = !eacnpc.npc.active;
                    if (dead) {
                        Remove();
                    }
                    return dead;
                }
            }
        }

        /// <summary>
        /// Returns whether the thing is active. If not, the thing is removed from Things.
        /// </summary>
        public bool Active {
            get {
                bool active;
                if (Is_Player) {
                    active = eacplayer.player.active;
                }
                else {
                    active = eacnpc.npc.active;
                }
                if (!active) {
                    Remove();
                }
                return active;
            }
        }

        /// <summary>
        /// Checks if the things are the same thing :)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Entity other) {
            return (Index == other.Index);
        }

        public void Heal(uint amount) {
            //TODO
        }

        public void Hurt(uint amount, Entity source) {
            //TODO
        }

        public bool IsHostileTo(Entity other) {
            return !IsFriendlyTo(other);
        }

        public bool IsFriendlyTo(Entity other) {
            if (Is_Player && other.Is_Player) {
                //both players
                Player us = eacplayer.player;
                Player them = other.eacplayer.player;
                if (us.hostile && them.hostile && ((us.team == 0) || (us.team != them.team)) && (whoAmI != them.whoAmI)) {
                    //pvp hostile
                    //both have pvp enabled, not on same team (or is on no team), not the sample player
                    return false;
                }
                else {
                    return true;
                }
            }
            else if (Is_Player && other.Is_Npc) {
                //this is player, that is npc
                return other.EACNPC.npc.friendly;
            }
            else if (Is_Npc && other.Is_Player) {
                //this is npc, that is player
                return EACNPC.npc.friendly;
            }
            else {
                //both npc
                //both hostile or both friendly
                return (EACNPC.npc.friendly == other.EACNPC.npc.friendly);
            }
        }

        public Vector2 Position {
            get {
                if (Is_Player) {
                    return eacplayer.player.position;
                }
                else {
                    return eacnpc.npc.position;
                }
            }
        }

        public float DistanceTo(Vector2 position) {
            return Vector2.Distance(Position, position);
        }

        public bool HasSightOf(Vector2 position) {
            return Collision.CanHit(Position, 0, 0, position, 0, 0);
        }

        public bool HasBuff(int buff_id) {
            if (Is_Player) {
                return eacplayer.player.HasBuff(buff_id);
            }
            else if (Is_Npc) {
                return eacnpc.npc.HasBuff(buff_id);
            }
            else {
                Utilities.Logger.Error("Unknown entity type");
                return false;
            }
        }

        public string Name {
            get {
                if (Is_Player) {
                    return eacplayer.player.name;
                }
                else if (Is_Npc) {
                    return eacnpc.npc.GetFullNetName().ToString();
                }
                else {
                    Utilities.Logger.Error("Unknown entity type");
                    return "Unknown";
                }
            }
        }
    }
}
