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
            Data_Class = new ClassInfo[Count];
            for (byte i = 0; i < Count; i++) {
                Data_Class[i] = new ClassInfo(this, i);
            }

            //unlock defaults
            Data_Class[(byte)PlayerClass.IDs.None].Unlock();
            Data_Class[(byte)PlayerClass.IDs.Novice].Unlock();

            //default class selection
            SetPrimary((byte)PlayerClass.IDs.Novice, false);
            SetSecondary((byte)PlayerClass.IDs.None, false);
        }

        public static byte Count { get { return (byte)PlayerClass.IDs.NUMBER_OF_IDs; } }

        public byte ID_Active_Primary { get; private set; } = 0;
        public byte ID_Active_Secondary { get; private set; } = 0;

        public ClassInfo Primary { get { return Data_Class[ID_Active_Primary]; } }
        public ClassInfo Secondary { get { return Data_Class[ID_Active_Secondary]; } }

        protected bool[] Data_Unlock = new bool[Count];
        protected byte[] Data_Level = new byte[Count];
        protected uint[] Data_XP = new uint[Count];

        private readonly ClassInfo[] Data_Class;
        public class ClassInfo {
            private readonly ClassSheet CONTAINER;
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

            public bool Is_Primary { get { return ID == CONTAINER.ID_Active_Primary; } }
            public bool Is_Secondary { get { return ID == CONTAINER.ID_Active_Secondary; } }

            public bool Unlocked { get { return CONTAINER.Data_Unlock[ID]; } private set { CONTAINER.Data_Unlock[ID] = value; } }
            public byte Level { get { return CONTAINER.Data_Level[ID]; } private set { CONTAINER.Data_Level[ID] = value; } }
            public uint XP { get { return CONTAINER.Data_XP[ID]; } private set { CONTAINER.Data_XP[ID] = value; } }

            public uint XP_Level_Total { get; private set; } = 0;
            public uint XP_Level_Remaining { get; private set; } = 0;

            public bool Valid_Class {
                get {
                    return Class.Tier > 0;
                }
            }

            public void Unlock(bool announce = false) {
                if (!Unlocked && announce) {
                    Main.NewText(Language.GetTextValue("Mods.ExperienceAndClasses.Common.Unlock") + " " + Class.Name + "!", UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }

                //set
                Unlocked = true;

                //set level to 1 if 0
                Level = Math.Max(Level, (byte)1);

                //update xp needed
                UpdateXPForLevel();
            }

            public void AddXP(uint xp, bool from_combat = true) {
                //TODO - non-combat

                XP = Utilities.Commons.SafeAdd(XP, xp);
                LocalHandleXPChange();
            }

            public void SubtractXP(uint xp) {
                XP = Utilities.Commons.SafeSubtract(XP, xp);
                LocalHandleXPChange();
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

                    CONTAINER.OnClassOrLevelChange();
                }
            }
        }

        public string GetLevelupMessage(byte id) {
            return Language.GetTextValue("Mods.ExperienceAndClasses.Common.Levelup_Class", PSHEET.eacplayer.player.name, Data_Class[id].Level, Data_Class[id].Class.Name);
        }

        public void SetPrimary(byte id, bool sync = true) {
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
            OnClassOrLevelChange();

            //sync?
            if (sync && Shortcuts.IS_CLIENT)
                SyncClass();
        }

        public void SetSecondary(byte id, bool sync = true) {
            //set
            if (ID_Active_Secondary == id) {
                //toggle off class
                ID_Active_Secondary = (byte)PlayerClass.IDs.None;
            }
            else if (ID_Active_Primary == id) {
                //swap primary and secondary
                ID_Active_Primary = ID_Active_Secondary;
                ID_Active_Secondary = id;
            }
            else {
                ID_Active_Secondary = id;
            }

            //changes
            OnClassOrLevelChange();

            //sync?
            if (sync && Shortcuts.IS_CLIENT)
                SyncClass();
        }

        private void OnClassOrLevelChange() {
            PSHEET.Attributes.UpdateFromClass();

            //TODO - ability, passive, etc.
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

        public TagCompound Save(TagCompound tag) {
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Class_Unlock, Data_Unlock);
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Class_Level, Data_Level);
            tag = Utilities.Commons.TagAddArrayAsList(tag, TAG_NAMES.Class_XP, Data_XP);

            return tag;
        }
        public void Load(TagCompound tag) {
            Data_Unlock = Utilities.Commons.TagLoadListAsArray<bool>(tag, TAG_NAMES.Class_Unlock, Count);
            Data_Level = Utilities.Commons.TagLoadListAsArray<byte>(tag, TAG_NAMES.Class_Level, Count);
            Data_XP = Utilities.Commons.TagLoadListAsArray<uint>(tag, TAG_NAMES.Class_XP, Count);

            //update xp needed to level
            for (byte i = 0; i < Count; i++) {
                Data_Class[i].UpdateXPForLevel();
            }

            //set attributes, abilities, etc.
            OnClassOrLevelChange();
        }
    }
}
