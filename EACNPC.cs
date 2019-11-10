using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ExperienceAndClasses {
    public class EACNPC : GlobalNPC {
        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Variables ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
        public NPC npc { get; private set; }
        public Utilities.Containers.Entity entity { get; private set; }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Overrides ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// Instance per entity to store pre-calculated xp, etc.
        /// </summary>
        public override bool InstancePerEntity { get { return true; } }

        public override GlobalNPC NewInstance(NPC npc) {
            this.npc = npc;
            entity = new Utilities.Containers.Entity(this);
            return base.NewInstance(npc);
        }

        public override void ResetEffects(NPC npc) {
            base.ResetEffects(npc);
            //thing.ProcessStatuses(); //TODO fix issue
        }
    }
}
