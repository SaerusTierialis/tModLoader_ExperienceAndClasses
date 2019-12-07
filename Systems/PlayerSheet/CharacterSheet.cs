using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses.Systems.PlayerSheet {
    public class CharacterSheet : ContainerTemplate {
        public CharacterSheet(PSheet psheet) : base(psheet) { }

        public byte Level { get; private set; } = 1;
        public uint XP { get; private set; } = 0;

        public uint XP_Level_Total { get; private set; } = 0;
        public uint XP_Level_Remaining { get; private set; } = 0;

        /// <summary>
        /// True while player is AFK
        /// | sync server
        /// </summary>
        public bool AFK { get; private set; } = false;

        /// <summary>
        /// True while in combat
        /// | sync ALL
        /// </summary>
        public bool In_Combat { get; private set; } = false;

        /// <summary>
        /// Track boss kill
        /// | local only
        /// </summary>
        public bool Defeated_WOF { get; private set; } = false;

        /// <summary>
        /// Has unlocked subclass system
        /// | local only
        /// </summary>
        public bool Secondary_Unlocked { get; private set; } = false;

        public void ForceLevel(byte level) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("ForceLevel called by local");
            }
            else {
                Level = level;
            }
        }

        public void SetAFK(bool afk) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                //show local message
                if (afk) {
                    Main.NewText(Shortcuts.GetCommonText("AFK_Start"), UI.Constants.COLOUR_MESSAGE_ERROR);
                }
                else {
                    Main.NewText(Shortcuts.GetCommonText("AFK_End"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }

                //sync change
                if (Shortcuts.IS_CLIENT && (afk != AFK)) {
                    Utilities.PacketHandler.AFK.Send(-1, Shortcuts.WHO_AM_I, afk);
                }
            }

            //set
            AFK = afk;
        }

        public void SetInCombat(bool combat_state) {
            if (PSHEET.eacplayer.Fields.Is_Local) {
                //sync change
                if (Shortcuts.IS_CLIENT && (combat_state != In_Combat)) {
                    Utilities.PacketHandler.InCombat.Send(-1, Shortcuts.WHO_AM_I, combat_state);
                }
            }

            //set
            In_Combat = combat_state;
        }

        public void DefeatWOF() {
            if (PSHEET.eacplayer.Fields.Is_Local && !Defeated_WOF) {
                Defeated_WOF = true;
                Main.NewText(Shortcuts.GetCommonText("Unlock_WOF"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                if (PlayerClass.CanUnlockTier3(PSHEET)) {
                    Main.NewText(Shortcuts.GetCommonText("Unlock_T3"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
                }
            }
        }

        public void UnlockSecondary() {
            if (!Secondary_Unlocked) {
                Secondary_Unlocked = true;
                Main.NewText(Shortcuts.GetCommonText("Unlock_Multiclass"), UI.Constants.COLOUR_MESSAGE_SUCCESS);
            }
        }

        public void LocalAddXP(uint xp) {
            XP = Utilities.Commons.SafeAdd(XP, xp);
            LocalHandleXPChange();
        }

        public void LocalSubtractXP(uint xp) {
            XP = Utilities.Commons.SafeSubtract(XP, xp);
            LocalHandleXPChange();
        }

        private void UpdateXPForLevel() {
            XP_Level_Total = Systems.XP.Requirements.GetXPReqCharacter(Level);
            XP_Level_Remaining = Utilities.Commons.SafeSubtract(XP_Level_Total, XP);
        }

        private void LocalHandleXPChange() {
            UpdateXPForLevel();
            bool leveled = false;

            while (XP_Level_Remaining == 0 && (Level < Systems.XP.MAX_LEVEL)) {
                Level = Utilities.Commons.SafeAdd(Level, 1);
                XP = Utilities.Commons.SafeSubtract(XP, XP_Level_Total);
                UpdateXPForLevel();
                leveled = true;
            }

            if (leveled) {
                if (Shortcuts.IS_CLIENT)
                    SyncCharacterLevel(true);
                else
                    Main.NewText(GetLevelupMessage(), UI.Constants.COLOUR_MESSAGE_SUCCESS);

                PSHEET.Attributes.LocalUpdateAttributePoints();

                Shortcuts.UpdateUIPSheet(PSHEET);
            }
            else {
                Shortcuts.UpdateUIXP();
            }
        }

        private void SyncCharacterLevel(bool is_levelup = false) {
            if (!PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("SyncCharacterLevel called by non-local");
            }
            else {
                Utilities.PacketHandler.CharLevel.Send(-1, Shortcuts.WHO_AM_I, Level, is_levelup);
            }
        }

        public string GetLevelupMessage() {
            return Shortcuts.GetCommonText("Levelup_Character", PSHEET.eacplayer.player.name, Level);
        }

        public void LocalResetLevel() {
            if (!PSHEET.eacplayer.Fields.Is_Local) {
                Utilities.Logger.Error("LocalResetLevel called by non-local");
            }
            else {
                Level = 1;
                XP = 0;
                UpdateXPForLevel();
            }
        }

        public TagCompound Save(TagCompound tag) {
            tag.Add(TAG_NAMES.Character_Level, Level);
            tag.Add(TAG_NAMES.Character_XP, XP);
            tag.Add(TAG_NAMES.WOF, Defeated_WOF);
            tag.Add(TAG_NAMES.UNLOCK_SUBCLASS, Secondary_Unlocked);
            return tag;
        }
        public void Load(TagCompound tag) {
            Level = Utilities.Commons.TagTryGet<byte>(tag, TAG_NAMES.Character_Level, 1);
            XP = Utilities.Commons.TagTryGet<uint>(tag, TAG_NAMES.Character_XP, 1);
            Defeated_WOF = Utilities.Commons.TagTryGet<bool>(tag, TAG_NAMES.WOF, false);
            Secondary_Unlocked = Utilities.Commons.TagTryGet<bool>(tag, TAG_NAMES.UNLOCK_SUBCLASS, false);

            UpdateXPForLevel();
        }
    }
}
