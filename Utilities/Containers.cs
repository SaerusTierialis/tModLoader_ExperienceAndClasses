using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceAndClasses.Utilities {
    public class Containers {
        public struct Loaded_UI_Data {
            public readonly float LEFT, TOP;
            public readonly bool AUTO;

            public Loaded_UI_Data(float left = 0f, float top = 0f, bool auto = true) {
                LEFT = left;
                TOP = top;
                AUTO = auto;
            }
        }
    }
}
