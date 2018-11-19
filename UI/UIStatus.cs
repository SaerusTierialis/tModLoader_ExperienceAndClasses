using Terraria;

namespace ExperienceAndClasses.UI {

    //UI for displaying statuses

    class UIStatus : UIStateCombo {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Singleton ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public static readonly UIStatus Instance = new UIStatus();

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public const float BUFF_SIZE = 32f;
        public const float BUFF_HORIZONTAL_SPACING = 6f;
        public const float BUFF_VERTICAL_SPACING = 20f;

        private const byte COLUMNS = 11;
        private const byte ROWS = 5; //max number of statuses displayed is (ROWS-2)*COLUMNS
        private const byte SLOTS = ROWS * COLUMNS;

        private const float LEFT = 32;
        private const float TOP = 76;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        private StatusIcon[] icons;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        protected override void InitializeState() {
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
            int number_buffs = 0;
            foreach (int i in Main.LocalPlayer.buffType) {
                if (i > 0)
                    number_buffs++;
            }
            int number_statuses = 2;

            for (byte i = 0; i < icons.Length; i++) {
                if (i < number_buffs) {
                    icons[i].active = false;
                }
                else if (i < (number_buffs+ number_statuses)) {
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
