using System.Collections.Generic;
using Terraria;

namespace ExperienceAndClasses.UI {

    //UI for displaying statuses

    public class UIStatus : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIStatus Instance = new UIStatus();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        //Terraria buff positions
        private const float LEFT = 32; //don't change this
        private const float TOP = 76; //don't change this
        public const float BUFF_SIZE = 32f; //don't change this
        public const float BUFF_HORIZONTAL_SPACING = 6f; //don't change this
        public const float BUFF_VERTICAL_SPACING = 20f; //don't change this
        private const byte COLUMNS = 11; //don't change this

        private const byte ROWS = 5; //max number of statuses displayed is ((ROWS*COLUMNS) - #buffs)
        private const byte SLOTS = ROWS * COLUMNS;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private static StatusIcon[] icons;
        public static bool needs_redraw_complete;
        public static bool needs_redraw_times_only;

        /// <summary>
        /// sorted by float duration remaining
        /// </summary>
        public static Utilities.Containers.TimeSortedStatusList status_to_draw = new Utilities.Containers.TimeSortedStatusList(SLOTS);

        private static int number_buffs_prior = 0;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            needs_redraw_complete = true;
            needs_redraw_times_only = false;

            icons = new StatusIcon[SLOTS];
            for (byte i = 0; i < icons.Length; i++) icons[i] = new StatusIcon();

            byte row = 0, col = 0;
            foreach(StatusIcon icon in icons) {
                icon.SetPosition(LEFT + (col * (BUFF_SIZE + BUFF_HORIZONTAL_SPACING)), TOP + (row * (BUFF_SIZE + BUFF_VERTICAL_SPACING)));
                state.Append(icon);

                col++;
                if (col >= COLUMNS) {
                    row++;
                    col = 0;
                }
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public void Update() {
            StatusIcon icon;

            //number of buffs changed?
            int number_buffs = Main.LocalPlayer.CountBuffs();
            bool number_buff_changed = (number_buffs_prior != number_buffs);
            number_buffs_prior = number_buffs;

            //TODO - needs_redraw_times_only at set intervals

            //need to remake list of status to show?
            if (needs_redraw_complete) {
                status_to_draw.Clear();
                List<Systems.Status> instances_applied = ExperienceAndClasses.LOCAL_MPLAYER.thing.Statuses.GetAllApply();
                Systems.Status.IDs id_skip = Systems.Status.IDs.NONE;
                foreach (Systems.Status status in instances_applied) {
                    //default to not in ui
                    status.was_in_ui = false;

                    //add to ui? (set was_in_ui if added)
                    if (status.ID != id_skip) {
                        switch (status.Specific_UI_Type) {
                            case Systems.Status.UI_TYPES.NONE:
                                //do nothing and skip any other instances of this status
                                id_skip = status.ID;
                                break;
                            case Systems.Status.UI_TYPES.ALL_APPLY:
                                //show all instances applied
                                status_to_draw.Add(status);
                                status.was_in_ui = true;
                                break;
                            case Systems.Status.UI_TYPES.ONE:
                                //show just first instance applied
                                status_to_draw.Add(status);
                                status.was_in_ui = true;
                                id_skip = status.ID; //skip other instances of this status
                                break;

                            default:
                                Utilities.Commons.Error("Unsupported UI_TYPES: " + status.Specific_UI_Type);
                                break;
                        }
                    }
                }
            }

            //update positions of icons (and text)
            if (needs_redraw_complete || number_buff_changed) {
                //stop hover text if any
                UIInfo.Instance.EndTextChildren(state);

                //clear all icons
                foreach (StatusIcon i in icons) {
                    i.active = false;
                }

                //set icon statuses
                int counter = number_buffs;
                foreach (Systems.Status status in status_to_draw) {
                    icon = icons[counter++];
                    icon.active = true;
                    icon.SetStatus(status);
                    icon.Update();
                }
            }
            else if (needs_redraw_times_only) {
                //just update text
                foreach (StatusIcon i in icons) {
                    if (i.active) {
                        i.Update();
                    }
                }
            }

            //don't need to update anymore
            needs_redraw_complete = false;
            needs_redraw_times_only = false;

        }

    }
}
