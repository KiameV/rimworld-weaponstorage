using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    class WorldComp : WorldComponent
    {
        public WorldComp(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref AssignedWeaponContainer.AssignedWeapons, "assignedWeapons", LookMode.Deep, new object[0]);
        }
    }
}
