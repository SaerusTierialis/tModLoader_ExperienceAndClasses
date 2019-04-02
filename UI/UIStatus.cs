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
        public static SortedList<float, Systems.Status> status_to_draw;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
            needs_redraw_complete = false;
            needs_redraw_times_only = false;
            status_to_draw = new SortedList<float, Systems.Status>();

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
            //TODO - redraw (complete and times only)

            int number_buffs = 0;
            foreach (int i in Main.LocalPlayer.buffType) {
                if (i > 0)
                    number_buffs++;
            }

            //TODO count status
            int number_statuses = 0;

            for (byte i = 0; i < icons.Length; i++) {
                if (i < number_buffs) {
                    icons[i].active = false;
                }
                else if (i < (number_buffs+ number_statuses)) {
                    //TODO set status
                    icons[i].active = true;
                    icons[i].Update();
                }
                else {
                    icons[i].active = false;
                }
            }

        }

    }
}
