using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    public class MNPC : GlobalNPC {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public Utilities.Containers.StatusList Statuses { get; private set; }
        public List<Systems.Status> Statuses_DrawBack;
        public List<Systems.Status> Statuses_DrawFront;
        public NPC npc { get; private set; }
        public Utilities.Containers.Thing thing { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constructor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public MNPC() {
            Statuses = new Utilities.Containers.StatusList();
            Statuses_DrawBack = new List<Systems.Status>();
            Statuses_DrawFront = new List<Systems.Status>();
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Instance per entity to store pre-calculated xp, etc.
        /// </summary>
        public override bool InstancePerEntity { get { return true; } }

        public override GlobalNPC NewInstance(NPC npc) {
            this.npc = npc;
            thing = new Utilities.Containers.Thing(this);
            return base.NewInstance(npc);
        }

    }
}
