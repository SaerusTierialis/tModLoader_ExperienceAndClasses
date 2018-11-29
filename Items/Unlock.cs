using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses.Items {
    public abstract class Unlock : MItem {
        private const int WIDTH = 32;
        private const int HEIGTH = 32;
        private const bool CONSUMABLE = false;

        protected Unlock(string name, string tooltip, string texture, int rarity) : base(name, tooltip, texture, CONSUMABLE, WIDTH, HEIGTH, rarity) {}
    }

    public class Unlock_Tier2 : Unlock {
        public const string NAME = "Token: Tier II Class";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Tier2";
        private const int RARITY = 8;

        public Unlock_Tier2() : base(NAME, TOOLTIP, TEXTURE, RARITY) {}
    }

    public class Unlock_Tier3 : Unlock {
        public const string NAME = "Token: Tier III Class";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Tier3";
        private const int RARITY = 10;

        public Unlock_Tier3() : base(NAME, TOOLTIP, TEXTURE, RARITY) { }
    }

    public class Unlock_Subclass : Unlock {
        public const string NAME = "Token: Multiclass";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Subclass";
        private const int RARITY = -12;

        public Unlock_Subclass() : base(NAME, TOOLTIP, TEXTURE, RARITY) { }
    }

    public class Unlock_Explorer : Unlock {
        public const string NAME = "Token: Explorer";
        private const string TOOLTIP = "TODP_tooltip";
        private const string TEXTURE = "ExperienceAndClasses/Textures/Item/Unlock_Explorer";
        private const int RARITY = -11;

        public Unlock_Explorer() : base(NAME, TOOLTIP, TEXTURE, RARITY) { }
    }
}
