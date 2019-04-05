using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses {
    public class MPlayer : ModPlayer {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const long TICKS_PER_FULL_SYNC = TimeSpan.TicksPerMinute * 2;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Vars ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static DateTime time_next_full_sync;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (non-syncing, non-save/load) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Thing can be a player or an NPC and is used by the Status and Ability systems.
        /// </summary>
        public Utilities.Containers.Thing thing { get; private set; }

        /// <summary>
        /// Cannot attack, use items, or use abilities while channeling. Set false at the beginning of update and then set to true by sources of channel on each cycle.
        /// </summary>
        public bool channelling;

        public bool Is_Local_Player { get; private set; }

        /// <summary>
        /// Set true for local player during OnEnterWorld. Set true for non-local players when first full sync is recieved.
        /// </summary>
        public bool initialized;

        /// <summary>
        /// Base values from current classes
        /// </summary>
        public int[] Attributes_Class { get; private set; }

        /// <summary>
        /// Bonus points from status
        /// </summary>
        public int[] Attributes_Status { get; private set; }

        /// <summary>
        /// Sync + Status (set during ApplyAttributes)
        /// </summary>
        public int[] Attributes_Final { get; private set; }

        /// <summary>
        /// Available allocation points
        /// </summary>
        public int Allocation_Points_Unallocated { get; private set; }

        /// <summary>
        /// Spent allocation points
        /// </summary>
        public int Allocation_Points_Spent { get; private set; }

        /// <summary>
        /// Total allocation points
        /// </summary>
        private int Allocation_Points_Total;

        public float heal_damage; //TODO
        public float dodge_chance; //TODO
        public float ability_delay_reduction; //TODO
        public float use_speed_melee, use_speed_ranged, use_speed_magic, use_speed_throwing, use_speed_minion, use_speed_tool;
        public float tool_power;

        /// <summary>
        /// List of minions including sentries. Includes each part of multi-part minions. Updates on CheckMinions().
        /// </summary>
        public List<Projectile> minions { get; private set;  }

        /// <summary>
        /// List of minions including only those that take minion slots. Updates on CheckMinions().
        /// </summary>
        public List<Projectile> slot_minions { get; private set; }

        /// <summary>
        /// Container of statuses on this player
        /// </summary>
        public Utilities.Containers.StatusList Statuses { get; private set; }
        public List<Systems.Status> Statuses_DrawBack;
        public List<Systems.Status> Statuses_DrawFront;

        public List<Systems.Passive.IDs> Passives { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (saved/loaded) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //Class_Primary and Class_Secondary also save/load (specifically the .ID)

        /// <summary>
        /// Mod version loaded from
        /// </summary>
        public int[] Load_Version { get; private set; }

        /// <summary>
        /// Track wall of flesh progress for tier 3 unlock
        /// </summary>
        public bool Defeated_WOF { get; private set; } //TODO use this to limit tier 3 unlock

        /// <summary>
        /// Can have a secondary class active
        /// </summary>
        public bool Allow_Secondary;

        /// <summary>
        /// Allocated points
        /// </summary>
        public int[] Attributes_Allocated { get; private set; }

        public Utilities.Containers.LoadedUIData loaded_ui_main, loaded_ui_hud;

        /// <summary>
        /// Show xp gain overhead
        /// </summary>
        public bool show_xp;

        public byte[] Class_Levels { get; private set; }
        public uint[] Class_XP { get; private set; }
        public bool[] Class_Unlocked { get; private set; }

        /// <summary>
        /// Earning XP when all active classes are maxed stores the extra here
        /// </summary>
        public uint Extra_XP;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Systems.Class Class_Primary { get; private set; }
        public Systems.Class Class_Secondary { get; private set; }
        public byte Class_Primary_Level_Effective { get; private set; }
        public byte Class_Secondary_Level_Effective { get; private set; }

        /// <summary>
        /// A value summarizing overall progress used to scale orb drops and value
        /// </summary>
        public int Progression { get; private set; }

        /// <summary>
        /// Class + Allocated
        /// </summary>
        public int[] Attributes_Sync { get; private set; }

        public bool AFK { get; private set; } //TODO local set
        public bool IN_COMBAT { get; private set; } //TODO local set

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// instanced arrays must be initialized here (also called during cloning, etc)
        /// </summary>
        public override void Initialize() {
            //for targeting
            thing = new Utilities.Containers.Thing(this);

            //defaults
            Is_Local_Player = false;
            Allow_Secondary = false;
            initialized = false;
            Load_Version = new int[3];
            AFK = false;
            IN_COMBAT = false;
            Defeated_WOF = false;
            show_xp = true;
            minions = new List<Projectile>();
            slot_minions = new List<Projectile>();
            Statuses = new Utilities.Containers.StatusList();
            Statuses_DrawBack = new List<Systems.Status>();
            Statuses_DrawFront = new List<Systems.Status>();
            Progression = 0;
            Extra_XP = 0;
            Passives = new List<Systems.Passive.IDs>();
            channelling = false;

            //ui
            loaded_ui_main = new Utilities.Containers.LoadedUIData();
            loaded_ui_hud = new Utilities.Containers.LoadedUIData();

            //default level/xp/unlock
            Class_Levels = new byte[(byte)Systems.Class.IDs.NUMBER_OF_IDs];
            Class_XP = new uint[(byte)Systems.Class.IDs.NUMBER_OF_IDs];
            Class_Unlocked = new bool[(byte)Systems.Class.IDs.NUMBER_OF_IDs];

            //default unlocks
            Class_Levels[(byte)Systems.Class.IDs.Novice] = 1;
            Class_Unlocked[(byte)Systems.Class.IDs.New] = true;
            Class_Unlocked[(byte)Systems.Class.IDs.None] = true;
            Class_Unlocked[(byte)Systems.Class.IDs.Novice] = true;

            //default class selection
            Class_Primary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.Novice];
            Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];

            //initialize attributes
            Attributes_Class = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Allocated = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Sync = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Status = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Attributes_Final = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];
            Allocation_Points_Unallocated = 0;
            Allocation_Points_Spent = 0;
            Allocation_Points_Total = 0;

            //stats
            heal_damage = 1f;
            dodge_chance = 0f;
            use_speed_melee = use_speed_ranged = use_speed_magic = use_speed_throwing = use_speed_minion = use_speed_tool = 1f;
            ability_delay_reduction = 1f;
            tool_power = 1f;
        }

        /// <summary>
        /// player enters (not other players)
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player) {
            base.OnEnterWorld(player);
            if (!Utilities.Netmode.IS_SERVER) {
                //this is the current local player
                Is_Local_Player = true;
                ExperienceAndClasses.LOCAL_MPLAYER = this;

                //singleplayer or client?
                Utilities.Netmode.UpdateNetmode();

                //start timer for next full sync
                time_next_full_sync = DateTime.Now.AddTicks(TICKS_PER_FULL_SYNC);

                //grab UI-state combos to display
                ExperienceAndClasses.UIs = new UI.UIStateCombo[] { UI.UIStatus.Instance, UI.UIHUD.Instance, UI.UIMain.Instance, UI.UIInfo.Instance };

                //(re)initialize ui
                foreach (UI.UIStateCombo ui in ExperienceAndClasses.UIs) {
                    ui.Initialize();
                }

                //temp: show bars and status //TODO something here?
                UI.UIHUD.Instance.Visibility = true;
                UI.UIStatus.Instance.Visibility = true;

                //apply ui auto
                ExperienceAndClasses.SetUIAutoStates();

                //update class info
                LocalUpdateAll();

                //initialized
                initialized = true;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PreUpdate() {
            base.PreUpdate();
            if (initialized) {
                //defaults before update
                heal_damage = 1f;
                dodge_chance = 0f;
                use_speed_melee = use_speed_ranged = use_speed_magic = use_speed_throwing = use_speed_minion = use_speed_tool = 1f;
                ability_delay_reduction = 1f;
                tool_power = 1f;
                channelling = false; //TODO prevent attack/item use/ability use
            }

            //Systems.Status.Heal.Add(player, this, 10);
        }

        public override void PostUpdateEquips() {
            base.PostUpdateEquips();
            if (initialized) {
                ApplyStatuses();
                ApplyAttributes();
            }
        }

        public override void PostUpdate() {
            base.PostUpdate();

            //timed events...
            DateTime now = DateTime.Now;

            //server/singleplayer
            if (!Utilities.Netmode.IS_CLIENT) {

                

            }

            //local events
            if (Is_Local_Player) {
                //ui //TODO only update if something changed
                UI.UIStatus.Instance.Update();
                
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ XP & Class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Force the local player's classes (no checks) and update.
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public static void LocalForceClasses(Systems.Class primary, Systems.Class secondary) {
            ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary = primary;
            ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary = secondary;
            LocalUpdateAll();
        }

        /// <summary>
        /// Force one of the local player's classes (no checks) and update.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="is_primary"></param>
        public static void LocalForceClass(Systems.Class c, bool is_primary) {
            if (is_primary) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary = c;
            }
            else {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary = c;
            }
            LocalUpdateAll();
        }

        public static void LocalUpdateAll() {
            //prevent secondary without primary class (move secondary to primary)
            if ((ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num == (byte)Systems.Class.IDs.New) || (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num == (byte)Systems.Class.IDs.None)) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary;
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //any "new" class should be set
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num == (byte)Systems.Class.IDs.New) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.Novice];
            }
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num == (byte)Systems.Class.IDs.New) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //clear secondary if not allowed
            if (!ExperienceAndClasses.LOCAL_MPLAYER.Allow_Secondary) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //effective levels
            LocalCalculateEffectiveLevels();

            //base class attributes
            float sum_primary, sum_secondary;
            Systems.Class c;
            for (byte id = 0; id < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; id++) {
                sum_primary = 0;
                sum_secondary = 0;

                c = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary;
                while ((c != null) && (c.Tier > 0)) {
                    sum_primary += (c.Attribute_Growth[id] * Math.Min(ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[c.ID_num], c.Max_Level));
                    c = c.Prereq;
                }

                c = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary;
                while ((c != null) && (c.Tier > 0)) {
                    sum_secondary += (c.Attribute_Growth[id] * Math.Min(ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[c.ID_num], c.Max_Level));
                    c = c.Prereq;
                }

                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective > 0) {
                    ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Class[id] = (int)Math.Floor((sum_primary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS * Systems.Attribute.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_PRIMARY) +
                                                            (sum_secondary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS * Systems.Attribute.SUBCLASS_PENALTY_ATTRIBUTE_MULTIPLIER_SECONDARY));
                }
                else {
                    ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Class[id] = (int)Math.Floor(sum_primary / Systems.Attribute.ATTRIBUTE_GROWTH_LEVELS);
                }
            }

            //allocated attribute points
            ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Total = Systems.Attribute.LocalAllocationPointTotal();
            ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Spent = Systems.Attribute.LocalAllocationPointSpent();
            ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Unallocated = ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Total - ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Spent;

            //sum attributes
            LocalAttributesCalculateSync();
            ExperienceAndClasses.LOCAL_MPLAYER.CalculateAttributesFinal();

            //calclate progression value
            LocalProgressionUpdate();

            //update UI
            UI.UIMain.Instance.UpdateClassInfo();
            UI.UIHUD.Instance.Update();

            //update class features
            LocalUpdateClassInfo();
        }

        public static void LocalUpdateClassInfo() {
            //TODO
        }

        public static void LocalCalculateEffectiveLevels() {
            //set current levels as default
            ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective = ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.ID_num];
            ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective = ExperienceAndClasses.LOCAL_MPLAYER.Class_Levels[ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.ID_num];

            //level cap primary
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective > ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Max_Level) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective = ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Max_Level;
            }

            //subclass secondary effective level penalty
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier > ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier) {
                //subclass of higher tier limited to lv1
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective = 1;
            }
            else if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier == ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary.Tier) {
                //subclass of same tier limited to half primary
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective = (byte)Math.Min(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective, ExperienceAndClasses.LOCAL_MPLAYER.Class_Primary_Level_Effective / 2);

                //prevent effective level 0 if using two level 1s of same tier
                if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Tier > 0)
                    ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective = Math.Max(ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective, (byte)1);
            }//subclass of lower tier has no penalty

            //level cap secondary
            if (ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective > ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Max_Level) {
                ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary_Level_Effective = ExperienceAndClasses.LOCAL_MPLAYER.Class_Secondary.Max_Level;
            }
        }

        public static void LocalDefeatWOF() {
            if (!ExperienceAndClasses.LOCAL_MPLAYER.Defeated_WOF) {
                ExperienceAndClasses.LOCAL_MPLAYER.Defeated_WOF = true;
                Main.NewText("Tier III Requirement Met: Defeat Wall of Flesh", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                if (Systems.Class.LocalCanUnlockTier3()) {
                    Main.NewText("You can now unlock tier III classes!", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Minions ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void CheckMinions() {
            minions = new List<Projectile>();
            slot_minions = new List<Projectile>();
            foreach (Projectile p in Main.projectile) {
                if (p.active && (p.minion || p.sentry) && (p.owner == player.whoAmI)) {
                    minions.Add(p);
                    if (p.minionSlots > 0) {
                        slot_minions.Add(p);
                    }
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (ExperienceAndClasses.HOTKEY_UI.JustPressed) {
                UI.UIMain.Instance.Visibility = !UI.UIMain.Instance.Visibility;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Combat ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
            base.Hurt(pvp, quiet, damage, hitDirection, crit);
            if (channelling) {
                Statuses.RemoveChannelling();
                channelling = false;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Syncing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void clientClone(ModPlayer clientClone) {
            MPlayer clone = clientClone as MPlayer;

            clone.Class_Primary = Class_Primary;
            clone.Class_Secondary = Class_Secondary;
            clone.Class_Primary_Level_Effective = Class_Primary_Level_Effective;
            clone.Class_Secondary_Level_Effective = Class_Secondary_Level_Effective;

            Attributes_Sync.CopyTo(clone.Attributes_Sync, 0);

            clone.Progression = Progression;

            clone.AFK = AFK;
            clone.IN_COMBAT = IN_COMBAT;
        }

        /// <summary>
        /// look for changes to sync + send any changes via packet
        /// </summary>
        /// <param name="clientPlayer"></param>
        public override void SendClientChanges(ModPlayer clientPlayer) {
            DateTime now = DateTime.Now;
            if (now.CompareTo(time_next_full_sync) >= 0) {
                //full sync
                time_next_full_sync = now.AddTicks(TICKS_PER_FULL_SYNC);
                FullSync();
            }
            else {
                //partial sync
                MPlayer clone = clientPlayer as MPlayer;
                Byte me = (byte)player.whoAmI;

                //class and class levels
                if ((clone.Class_Primary.ID_num != Class_Primary.ID_num) || (clone.Class_Secondary.ID_num != Class_Secondary.ID_num) ||
                    (clone.Class_Primary_Level_Effective != Class_Primary_Level_Effective) || (clone.Class_Secondary_Level_Effective != Class_Secondary_Level_Effective)) {
                    Utilities.PacketHandler.ForceClass.Send(-1, me, Class_Primary.ID_num, Class_Primary_Level_Effective, Class_Secondary.ID_num, Class_Secondary_Level_Effective);
                }

                //final attribute
                for (byte i=0; i<(byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    if (clone.Attributes_Sync[i] != Attributes_Sync[i]) {
                        Utilities.PacketHandler.SyncAttribute.Send(-1, me, Attributes_Sync);
                        break;
                    }
                }

                //measure of character progression
                if (clone.Progression != Progression) {
                    Utilities.PacketHandler.Progression.Send(-1, me, Progression);
                }

                //afk
                if (clone.AFK != AFK) {
                    Utilities.PacketHandler.AFK.Send(-1, me, AFK);
                }

                //combat
                if (clone.IN_COMBAT != IN_COMBAT) {
                    Utilities.PacketHandler.InCombat.Send(-1, me, IN_COMBAT);
                }

            }
        }

        /// <summary>
        /// full sync (called to share current players with new players + new player with current players)
        /// </summary>
        /// <param name="toWho"></param>
        /// <param name="fromWho"></param>
        /// <param name="newPlayer"></param>
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            base.SyncPlayer(toWho, fromWho, newPlayer);
            FullSync();
        }

        /// <summary>
        /// sync all neccessary mod vars
        /// </summary>
        private void FullSync() {
            //send one packet with everything needed
            Utilities.PacketHandler.ForceFull.Send(-1, (byte)player.whoAmI, Class_Primary.ID_num, Class_Primary_Level_Effective, Class_Secondary.ID_num, Class_Secondary_Level_Effective, Attributes_Sync, AFK, IN_COMBAT, Progression);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Sync Force Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Set the class and level (effective level) of another player.
        /// Attributes etc. are synced separately.
        /// </summary>
        /// <param name="primary_id"></param>
        /// <param name="primary_level"></param>
        /// <param name="secondary_id"></param>
        /// <param name="secondary_level"></param>
        public void NonLocalSyncClass(byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            if (Is_Local_Player) {
                Utilities.Commons.Error("Cannot force class packet for local player");
                return;
            }

            Class_Primary = Systems.Class.LOOKUP[primary_id];
            Class_Primary_Level_Effective = primary_level;
            Class_Secondary = Systems.Class.LOOKUP[secondary_id];
            Class_Secondary_Level_Effective = secondary_level;
        }

        /// <summary>
        /// Sets Attributes_Sync of a non-local player, then recalculates Attributes_Final
        /// </summary>
        /// <param name="attributes"></param>
        public void NonLocalSyncAttributes(int[] attributes) {
            if (Is_Local_Player) {
                Utilities.Commons.Error("Cannot force attribute packet for local player");
                return;
            }

            for (byte i = 0; i < attributes.Length; i++) {
                Attributes_Sync[i] = attributes[i];
            }

            CalculateAttributesFinal();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Attributes ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Calculates final attribute (sync + status) and applies effects
        /// </summary>
        private void ApplyAttributes() {
            CalculateAttributesFinal();
            for (byte i=0; i<(byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                Systems.Attribute.LOOKUP[i].ApplyEffect(this, Attributes_Final[i]);
            }
        }

        private void CalculateAttributesFinal() {
            for (byte i = 0; i < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                Attributes_Final[i] = Attributes_Sync[i] + Attributes_Status[i];
            }
        }

        public void LocalAttributeAllocationAddPoint(byte id) {
            if (!Is_Local_Player) {
                Utilities.Commons.Error("Cannot set attribute allocation for non-local player");
                return;
            }
            
            int adjustment = +1;

            if ((Attributes_Allocated[id] < 0 && adjustment > 0) || (Allocation_Points_Unallocated < 0 && adjustment < 0) || 
                (((adjustment < 0) || (Allocation_Points_Unallocated >= Systems.Attribute.AllocationPointCost(Attributes_Allocated[id]))) && ((Attributes_Allocated[id] + adjustment) >= 0))) {
                Attributes_Allocated[id] += adjustment;
                LocalUpdateAll();
            }
        }

        public static void LocalAttributeReset() {
            //item cost
            int cost = Systems.Attribute.LocalCalculateResetCost();
            int type = Systems.Attribute.RESET_COST_ITEM.item.type;
            int held = ExperienceAndClasses.LOCAL_MPLAYER.player.CountItem(type);

            //do reset
            if (held >= cost) {
                //consume
                for (int i = 0; i < cost; i++)
                    ExperienceAndClasses.LOCAL_MPLAYER.player.ConsumeItem(type);

                //reset
                for (byte i = 0; i < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; i++) {
                    ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[i] = 0;
                }
                LocalUpdateAll();
            }
        }

        /// <summary>
        /// Calculates class + allocation
        /// </summary>
        public static void LocalAttributesCalculateSync() {
            for (byte id = 0; id < (byte)Systems.Attribute.IDs.NUMBER_OF_IDs; id++) {
                ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Sync[id] = ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Class[id] + ExperienceAndClasses.LOCAL_MPLAYER.Attributes_Allocated[id];
            }
        }

        //dexterity
        public override float UseTimeMultiplier(Item item) {
            if (item.melee)
                return base.UseTimeMultiplier(item) * use_speed_melee;

            if (item.ranged)
                return base.UseTimeMultiplier(item) * use_speed_ranged;

            if (item.magic)
                return base.UseTimeMultiplier(item) * use_speed_magic;

            if (item.thrown)
                return base.UseTimeMultiplier(item) * use_speed_throwing;

            if (item.summon || item.sentry)
                return base.UseTimeMultiplier(item) * use_speed_minion;

            else if (item.hammer>0 || item.axe>0 || item.pick>0 || item.fishingPole>0)
                return base.UseTimeMultiplier(item) * use_speed_tool;

            else
                return base.UseTimeMultiplier(item);
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Drawing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void DrawEffects(PlayerDrawInfo drawInfo, bool is_behind) {
            /*
            Player drawPlayer = drawInfo.drawPlayer;
            //Mod mod = ModLoader.GetMod("ExperienceAndClasses");
            //ExamplePlayer modPlayer = drawPlayer.GetModPlayer<ExamplePlayer>();
            //Texture2D texture = mod.GetTexture("NPCs/Lock");

            if (drawPlayer.statLife < 50) {
                return;
            }

            Texture2D texture = Main.flameTexture;

            int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X - texture.Width / 2f);
            int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y - texture.Height / 2f);

            DrawData data = new DrawData(texture, new Vector2(drawX, drawY), Color.Green);
            Main.playerDrawData.Add(data);
            */

            MPlayer mplayer = drawInfo.drawPlayer.GetModPlayer<MPlayer>();
            if (is_behind) {
                List<Systems.Status> statuses = mplayer.Statuses_DrawBack;
                foreach (Systems.Status status in statuses) {
                    //TODO
                    //status.DrawEffectBack(drawInfo);
                }
            }
            else {
                List<Systems.Status> statuses = mplayer.Statuses_DrawFront;
                foreach (Systems.Status status in statuses) {
                    //TODO
                    //status.DrawEffectFront(drawInfo);
                }
            }
        }

        public static readonly PlayerLayer MiscEffectsBehind = new PlayerLayer("ExperienceAndClasses", "MiscEffectsBack", PlayerLayer.MiscEffectsBack, delegate (PlayerDrawInfo drawInfo) {
            DrawEffects(drawInfo, true);
        });

        public static readonly PlayerLayer MiscEffects = new PlayerLayer("ExperienceAndClasses", "MiscEffects", PlayerLayer.MiscEffectsFront, delegate (PlayerDrawInfo drawInfo) {
            DrawEffects(drawInfo, false);
        });

        public override void ModifyDrawLayers(List<PlayerLayer> layers) {
            MiscEffectsBehind.visible = true;
            layers.Insert(0, MiscEffectsBehind);
            MiscEffects.visible = true;
            layers.Add(MiscEffects);
        }


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override TagCompound Save() {
            //must be byte[], int[], or List<>
            List<int> attributes_allocated = new List<int>();
            foreach (int value in Attributes_Allocated) {
                attributes_allocated.Add(value);
            }
            List<bool> class_unlocked = new List<bool>();
            foreach (bool value in Class_Unlocked) {
                class_unlocked.Add(value);
            }
            List<uint> class_xp = new List<uint>();
            foreach (uint value in Class_XP) {
                class_xp.Add(value);
            }
            List<byte> class_level = new List<byte>();
            foreach (byte value in Class_Levels) {
                class_level.Add(value);
            }

            //version
            Version version = ExperienceAndClasses.MOD.Version;
            int[] version_array = new int[] { version.Major, version.Minor, version.Build };

            //ui positions
            float ui_main_left, ui_main_top, ui_hud_left, ui_hud_top;
            bool ui_main_auto, ui_hud_auto;
            if (UI.UIMain.Instance.panel != null) {
                ui_main_left = UI.UIMain.Instance.panel.GetLeft();
                ui_main_top = UI.UIMain.Instance.panel.GetTop();
                ui_main_auto = UI.UIMain.Instance.panel.Auto;

                ui_hud_left = UI.UIHUD.Instance.panel.GetLeft();
                ui_hud_top = UI.UIHUD.Instance.panel.GetTop();
                ui_hud_auto = UI.UIHUD.Instance.panel.Auto;
            }
            else {
                ui_main_left = UI.Constants.DEFAULT_UI_MAIN_LEFT;
                ui_main_top = UI.Constants.DEFAULT_UI_MAIN_TOP;
                ui_main_auto = UI.Constants.DEFAULT_UI_MAIN_AUTO;

                ui_hud_left = UI.Constants.DEFAULT_UI_HUD_LEFT;
                ui_hud_top = UI.Constants.DEFAULT_UI_HUD_TOP;
                ui_hud_auto = UI.Constants.DEFAULT_UI_HUD_AUTO;
            }

            return new TagCompound {
                {"eac_version", version_array },
                {"eac_ui_class_left", ui_main_left },
                {"eac_ui_class_top", ui_main_top },
                {"eac_ui_class_auto", ui_main_auto },
                {"eac_ui_hud_left", ui_hud_left },
                {"eac_ui_hud_top", ui_hud_top },
                {"eac_ui_hud_auto", ui_hud_auto },
                {"eac_class_unlock", class_unlocked },
                {"eac_class_xp", class_xp },
                {"eac_class_level", class_level },
                {"eac_class_current_primary", Class_Primary.ID_num },
                {"eac_class_current_secondary", Class_Secondary.ID_num },
                {"eac_class_subclass_unlocked", Allow_Secondary },
                {"eac_attribute_allocation", attributes_allocated },
                {"eac_wof", Defeated_WOF },
                {"eac_settings_show_xp", show_xp},
                {"eac_extra_xp", Extra_XP},
            };
        }

        public override void Load(TagCompound tag) {
            //this is the local mplayer (at least for a moment)
            MPlayer backup = ExperienceAndClasses.LOCAL_MPLAYER;
            ExperienceAndClasses.LOCAL_MPLAYER = this;

            //get version in case needed
            Load_Version = Utilities.Commons.TryGet<int[]>(tag, "eac_version", new int[3]);

            //has killed wof
            Defeated_WOF = Utilities.Commons.TryGet<bool>(tag, "eac_wof", Defeated_WOF);

            //subclass unlocked
            Allow_Secondary = Utilities.Commons.TryGet<bool>(tag, "eac_class_subclass_unlocked", Allow_Secondary);

            //extra xp pool
            Extra_XP = Utilities.Commons.TryGet<uint>(tag, "eac_extra_xp", Extra_XP);

            //settings
            show_xp = Utilities.Commons.TryGet<bool>(tag, "eac_settings_show_xp", show_xp);

            //current classes
            Class_Primary = Systems.Class.LOOKUP[Utilities.Commons.TryGet<byte>(tag, "eac_class_current_primary", Class_Primary.ID_num)];
            Class_Secondary = Systems.Class.LOOKUP[Utilities.Commons.TryGet<byte>(tag, "eac_class_current_secondary", Class_Secondary.ID_num)];

            //class still enabled?
            if (!Class_Secondary.Allowed) {
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }
            if (!Class_Primary.Allowed) {
                Class_Primary = Class_Secondary;
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //class unlocked
            List<bool> class_unlock_loaded = Utilities.Commons.TryGet<List<bool>>(tag, "eac_class_unlock", new List<bool>());
            for (byte i = 0; i < class_unlock_loaded.Count; i++) {
                Class_Unlocked[i] = class_unlock_loaded[i];
            }

            //class level
            List<byte> class_level_loaded = Utilities.Commons.TryGet<List<byte>>(tag, "eac_class_level", new List<byte>());
            for (byte i = 0; i < class_level_loaded.Count; i++) {
                Class_Levels[i] = class_level_loaded[i];
            }

            //class xp
            List<uint> class_xp_loaded = Utilities.Commons.TryGet<List<uint>>(tag, "eac_class_xp", new List<uint>());
            for (byte i = 0; i < class_xp_loaded.Count; i++) {
                Class_XP[i] = class_xp_loaded[i];
            }

            //fix any potential issues...
            for (byte id = 0; id < (byte)Systems.Class.IDs.NUMBER_OF_IDs; id++) {

                //if unlocked...
                if (Class_Unlocked[id]) {
                    //level should be at least one
                    if (Class_Levels[id] < 1) {
                        Class_Levels[id] = 1;
                    }

                    //level up if required xp changed
                    Systems.Class.LOOKUP[id].LocalCheckDoLevelup(false);
                }

            }

            //if not allowed secondary, set none
            if (!Allow_Secondary) {
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //if selected class is now locked for some reason, select no class
            if ((!Class_Unlocked[Class_Primary.ID_num]) || (!Class_Unlocked[Class_Secondary.ID_num])) {
                Class_Primary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
                Class_Secondary = Systems.Class.LOOKUP[(byte)Systems.Class.IDs.None];
            }

            //allocated attributes
            List<int> attribute_allocation = Utilities.Commons.TryGet<List<int>>(tag, "eac_attribute_allocation", new List<int>());
            for(byte i = 0; i < attribute_allocation.Count; i++) {
                if (Systems.Attribute.LOOKUP[i].Active) {
                    Attributes_Allocated[i] = attribute_allocation[i];
                }
            }

            //UI data
            loaded_ui_main = new Utilities.Containers.LoadedUIData(
                Utilities.Commons.TryGet<float>(tag, "eac_ui_class_left", UI.Constants.DEFAULT_UI_MAIN_LEFT),
                Utilities.Commons.TryGet<float>(tag, "eac_ui_class_top", UI.Constants.DEFAULT_UI_MAIN_TOP),
                Utilities.Commons.TryGet<bool>(tag, "eac_ui_class_auto", UI.Constants.DEFAULT_UI_MAIN_AUTO));

            loaded_ui_hud = new Utilities.Containers.LoadedUIData(
                Utilities.Commons.TryGet<float>(tag, "eac_ui_hud_left", UI.Constants.DEFAULT_UI_HUD_LEFT),
                Utilities.Commons.TryGet<float>(tag, "eac_ui_hud_top", UI.Constants.DEFAULT_UI_HUD_TOP),
                Utilities.Commons.TryGet<bool>(tag, "eac_ui_hud_auto", UI.Constants.DEFAULT_UI_HUD_AUTO));

            //if this is a client loading (or singleplayer), then this will become the local mplayer again when entering the world
            ExperienceAndClasses.LOCAL_MPLAYER = backup;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Misc ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public void SetAFK(bool afk) {
            AFK = afk;
            if (Is_Local_Player) {
                if (AFK) {
                    Main.NewText("You are now AFK. You will not gain or lose XP.", UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Main.NewText("You are no longer AFK. You can gain and lose XP again.", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
            }
        }

        public void SetInCombat(bool in_combat) {
            IN_COMBAT = in_combat;
        }

        private static void LocalProgressionUpdate() {
            //calculate
            int progression = ExperienceAndClasses.LOCAL_MPLAYER.Allocation_Points_Total;
            //set
            ExperienceAndClasses.LOCAL_MPLAYER.SetProgression(progression);
        }

        public void SetProgression(int player_progression) {
            Progression = player_progression;
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Status ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private void ApplyStatuses() {
            //reset attributes from status
            Attributes_Status = new int[(byte)Systems.Attribute.IDs.NUMBER_OF_IDs];

            //process statuses
            Systems.Status.ProcessStatuses(Statuses);
        }
        
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Ability ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /*
        public void Heal(int amount_life, int amount_mana) {
            if (Is_Local_Player) {
                Main.NewText("do " + amount_life + " " + amount_mana);

                //life
                amount_life = Math.Min(amount_life, player.statLifeMax2 - player.statLife);
                if (amount_life > 0) {
                    player.statLife += amount_life;
                    player.HealEffect(amount_life, true);
                }

                //mana
                amount_mana = Math.Min(amount_mana, player.statManaMax2 - player.statMana);
                if (amount_mana > 0) {
                    player.statMana += amount_mana;
                    player.ManaEffect(amount_mana);
                }
            }
            else {
                Utilities.PacketHandler.Heal.Send((byte)player.whoAmI, (byte)Main.LocalPlayer.whoAmI, amount_life, amount_mana);
            }
        }
        */

    }
}
