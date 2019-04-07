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
        public NPC npc { get; private set; }
        public Utilities.Containers.Thing thing { get; private set; }

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

        public override void ResetEffects(NPC npc) {
            base.ResetEffects(npc);
            //thing.ProcessStatuses(); //TODO fix issue
        }


    }
}
