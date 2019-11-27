using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class ClassSheet : ContainerTemplate {
        public ClassSheet(PSheet psheet) : base(psheet) {
            Data_Class = new ClassInfo[PlayerClass.Count];
            for (byte i = 0; i < PlayerClass.Count; i++) {
                Data_Class[i] = new ClassInfo(this, i);
            }

            //unlock defaults
            Data_Class[(byte)PlayerClass.IDs.None].Unlock(false);
            Data_Class[(byte)PlayerClass.IDs.Novice].Unlock(false);
        }

        public byte ID_Active_Primary { get; private set; } = 0;
        public byte ID_Active_Secondary { get; private set; } = 0;

        public ClassInfo Primary { get { return Data_Class[ID_Active_Primary]; } }
        public ClassInfo Secondary { get { return Data_Class[ID_Active_Secondary]; } }

        public bool Has_Subclass { get { return Secondary.Valid_Class; } }

        protected bool[] Data_Unlock = new bool[PlayerClass.Count];
        protected byte[] Data_Level = new byte[PlayerClass.Count];
        protected uint[] Data_XP = new uint[PlayerClass.Count];

        private readonly ClassInfo[] Data_Class;
        public class ClassInfo {
            public readonly ClassSheet CONTAINER;
            public ClassInfo(ClassSheet container, byte id) {
                CONTAINER = container;
                ID = id;
                Class = PlayerClass.LOOKUP[id];
            }

            /// <summary>
            /// Shortcut to PlayerClass.LOOKUP
            /// </summary>
            public readonly PlayerClass Class;

            public readonly byte ID;

            public bool Is_Primary { get { return (Valid_Class && ID == CONTAINER.ID_Active_Primary); } }
            public bool Is_Secondary { get { return (Valid_Class && ID == CONTAINER.ID_Active_Secondary); } }
            public bool Is_Active { get { return (Is_Primary || Is_Secondary); } }

            public bool Unlocked { get { return CONTAINER.Data_Unlock[ID]; } private set { CONTAINER.Data_Unlock[ID] = value; } }

            public byte Level {
                get {
                    return Math.Min(PlayerClass.MAX_TIER_LEVEL[Class.Tier], CONTAINER.Data_Level[ID]);
                }
                private set {
                    CONTAINER.Data_Level[ID] = value; }
            }

            public uint XP { get { return CONTAINER.Data_XP[ID]; } private set { CONTAINER.Data_XP[ID] = value; } }

            public uint XP_Level_Total { get; private set; } = 0;
            public uint XP_Level_Remaining { get; private set; } = 0;

            public byte Level_Effective { get {
                    if (Is_Secondary)
                        if (CONTAINER.Primary.Class.Tier > Class.Tier) {
                            //primary is higher tier, no limit
                            return Level;
                        }
                        else if (CONTAINER.Primary.Class.Tier < Class.Tier) {
                            //primary is lower tier, limit to 1
                            return 1;
                        }
                        else {
                            //primary is same tier, limit to half primary
                            return (byte)Math.Min(Level, Math.Ceiling(CONTAINER.Primary.Level_Effective / 2.0));
                        }
                    else
                        return Level;
                }
            }

            public bool Can_Gain_XP {
                get {
                    return (Valid_Class && !Maxed);
                }
            }

            public bool Valid_Class {
                get {
                    return (Class.Tier > 0) && (Class.Enabled);
                }
            }

            public bool Maxed { get {
                    return (Level >= Class.Max_Level);
                }
            }

            public void Unlock(bool announce = true) {
                if (!Unlocked && announce) {
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock", Class.Name), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }

                //set
                Unlocked = true;

                //set level to 1 if 0
                Level = Math.Max(Level, (byte)1);

                //update xp needed
                UpdateXPForLevel();

                //update UI
                Shortcuts.UpdateUIPSheet(CONTAINER.PSHEET);
            }

            public void AddXP(uint xp, bool allow_multipliers = true) {
                if (Valid_Class) {
                    XP = Utilities.Commons.SafeAdd(XP, xp);
                    LocalHandleXPChange();
                }
            }

            public void SubtractXP(uint xp) {
                if (Valid_Class) {
                    XP = Utilities.Commons.SafeSubtract(XP, xp);
                    LocalHandleXPChange();
                }
            }

            public void UpdateXPForLevel() {
                XP_Level_Total = Systems.XP.Requirements.GetXPReqClass(Class, Level);
                XP_Level_Remaining = Utilities.Commons.SafeSubtract(XP_Level_Total, XP);
            }

            private void LocalHandleXPChange() {
                UpdateXPForLevel();
                bool leveled = false;

                while (XP_Level_Remaining == 0 && (Level < Class.Max_Level)) {
                    Level = Utilities.Commons.SafeAdd(Level, 1);
                    XP = Utilities.Commons.SafeSubtract(XP, XP_Level_Total);
                    UpdateXPForLevel();
                    leveled = true;
                }

                if (leveled) {
                    if (Shortcuts.IS_CLIENT)
                        CONTAINER.SyncClass(ID);
                    else
                        Main.NewText(CONTAINER.GetLevelupMessage(ID), UI.Constants.COLOUR_MESSAGE_SUCCESS);

                    if ((Class.Tier < PlayerClass.MAX_TIER) && (Level == Class.Max_Level)) {
                        Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock_Class_Prereq_Met", new string('I', Class.Tier + 1), Class.Name, Level), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                    }

                    CONTAINER.OnClassOrLevelChange();
                }
                else {
                    Shortcuts.UpdateUIXP();
                }
            }
        }

        public bool Can_Gain_XP {
            get {
                return Primary.Can_Gain_XP || Secondary.Can_Gain_XP;
            }
        }

        public void SetDefaultClass() {
            //default class selection
            SetPrimary((byte)PlayerClass.IDs.Novice, false);
            SetSecondary((byte)PlayerClass.IDs.None, false);
        }

        public string GetLevelupMessage(byte id) {
            return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Levelup_Class", PSHEET.eacplayer.player.name, Data_Class[id].Level, Data_Class[id].Class.Name);
        }

        public ClassInfo GetClassInfo(byte id) {
            return Data_Class[id];
        }

        public void SetPrimary(byte id, bool sync = true, bool destroy_minions = false) {
            //set
            if (ID_Active_Primary == id) {
                //toggle off class
                ID_Active_Primary = ID_Active_Secondary;
                ID_Active_Secondary = (byte)PlayerClass.IDs.None;
            }
            else if (ID_Active_Secondary == id) {
                //swap primary and secondary
                ID_Active_Secondary = ID_Active_Primary;
                ID_Active_Primary = id;
            }
            else {
                ID_Active_Primary = id;
            }

            //changes
            OnClassOrLevelChange(destroy_minions);

            //sync?
            if (sync && Shortcuts.IS_CLIENT)
                SyncClass();
        }

        public void SetSecondary(byte id, bool sync = true, bool destroy_minions = false) {
            //redirect to primary if no primary
            if (!Primary.Valid_Class) {
                SetPrimary(id, sync, destroy_minions);
                return;
            }

            //set
            if (ID_Active_Secondary == id) {
                //toggle off class
                ID_Active_Secondary = (byte)PlayerClass.IDs.None;
            }
            else if (ID_Active_Primary == id) {
                if (!Secondary.Valid_Class) {
                    //invalid
                    return;
                }
                else {
                    //swap primary and secondary
                    ID_Active_Primary = ID_Active_Secondary;
                    ID_Active_Secondary = id;
                }
            }
            else {
                ID_Active_Secondary = id;
            }

            //changes
            OnClassOrLevelChange(destroy_minions);

            //sync?
            if (sync && Shortcuts.IS_CLIENT)
                SyncClass();
        }

        private void OnClassOrLevelChange(bool destroy_minions = false) {
            PSHEET.Attributes.UpdateFromClass();

            //destroy_minions
            if (destroy_minions) {
                PSHEET.eacplayer.LocalDestroyMinions();
            }

            //TODO - ability, passive, etc.

            //UI
            Shortcuts.UpdateUIPSheet(PSHEET);
        }

        public void LocalResetLevels() {
            ID_Active_Primary = (byte)PlayerClass.IDs.None;
            ID_Active_Secondary = (byte)PlayerClass.IDs.None;

            for (byte i = 0; i < PlayerClass.Count; i++) {
                if (Data_Level[i] > 1) {
                    Data_Level[i] = 1;
                }

                if (Data_XP[i] > 0) {
                    Data_XP[i] = 0;
                }

                Data_Class[i].AddXP(0, false);
            }
        }

        private void SyncClass(byte id_levelup = 0) {
            if (!PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("SyncClass called by non-local");
            }
            else {
                Utilities.PacketHandler.Class.Send(-1, Shortcuts.WHO_AM_I, Primary.ID, Primary.Level, Secondary.ID, Secondary.Level, id_levelup);
            }
        }

        public void ForceActive(byte primary_id, byte primary_level, byte secondary_id, byte secondary_level) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("ForceActive called by local");
            }
            else {
                //set
                ID_Active_Primary = primary_id;
                Data_Level[ID_Active_Primary] = primary_level;
                ID_Active_Secondary = secondary_id;
                Data_Level[ID_Active_Secondary] = secondary_level;

                //changes
                OnClassOrLevelChange();
            }
        }

        public int[] GetTierTotalLevels(bool require_gives_allocation = false) {
            int[] totals = new int[PlayerClass.MAX_TIER + 1];

            for (byte i=0; i< PlayerClass.Count; i++) {
                if (Data_Class[i].Valid_Class && (!require_gives_allocation || Data_Class[i].Class.Gives_Allocation_Attributes)) {
                    totals[Data_Class[i].Class.Tier] += Data_Class[i].Level;
                }
            }

            return totals;
        }

        public TagCompound Save(TagCompound tag) {
            //class data arrays
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Class_Unlock, Data_Unlock);
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Class_Level, Data_Level);
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Class_XP, Data_XP);

            //active classes
            tag.Add(TAG_NAMES.Class_Active_Primary, ID_Active_Primary);
            tag.Add(TAG_NAMES.Class_Active_Secondary, ID_Active_Secondary);

            return tag;
        }
        public void Load(TagCompound tag) {
            Data_Unlock = Utilities.Commons.TagLoadListAsArray<bool>(tag, TAG_NAMES.Class_Unlock, PlayerClass.Count);
            Data_Level = Utilities.Commons.TagLoadListAsArray<byte>(tag, TAG_NAMES.Class_Level, PlayerClass.Count);
            Data_XP = Utilities.Commons.TagLoadListAsArray<uint>(tag, TAG_NAMES.Class_XP, PlayerClass.Count);

            //update xp needed to level
            for (byte i = 0; i < PlayerClass.Count; i++) {
                Data_Class[i].UpdateXPForLevel();
            }

            //active classes
            ID_Active_Primary = Utilities.Commons.TagTryGet(tag, TAG_NAMES.Class_Active_Primary, (byte)PlayerClass.IDs.Novice);
            ID_Active_Secondary = Utilities.Commons.TagTryGet(tag, TAG_NAMES.Class_Active_Secondary, (byte)PlayerClass.IDs.None);

            //deactivate disabled classes
            if (!PlayerClass.LOOKUP[ID_Active_Secondary].Enabled) {
                ID_Active_Secondary = (byte)PlayerClass.IDs.None;
            }
            if (!PlayerClass.LOOKUP[ID_Active_Primary].Enabled) {
                ID_Active_Primary = ID_Active_Secondary;
                ID_Active_Secondary = (byte)PlayerClass.IDs.None;
            }

            //ensure that base and active classes are unlocked
            Data_Unlock[(byte)PlayerClass.IDs.None] = true;
            Data_Unlock[(byte)PlayerClass.IDs.Novice] = true;
            Data_Unlock[ID_Active_Primary] = true;
            Data_Unlock[ID_Active_Secondary] = true;

            //set attributes, abilities, etc.
            OnClassOrLevelChange();
        }
    }
}
