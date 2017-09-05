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

            List<AssignedWeaponContainer> assignedWeapons = AssignedWeaponContainer.AssignedWeapons;

            Scribe_Collections.Look(ref assignedWeapons, "weaponSets", LookMode.Deep, new object[0]);

            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                if (assignedWeapons != null)
                {
                    for (int i = assignedWeapons.Count - 1; i >= 0; --i)
                    {
                        if (assignedWeapons[i].Pawn == null)
                        {
                            assignedWeapons.RemoveAt(i);
                        }
                    }
                }
                AssignedWeaponContainer.AssignedWeapons = assignedWeapons;
            }
        }
    }

    /*class GameComp : GameComponent
    {
        public GameComp() { }

        public GameComp(Game game) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Dictionary<string, Pawn> pawnIdToPawn = new Dictionary<string, Pawn>();
            foreach (Pawn p in PawnsFinder.AllMapsAndWorld_Alive)
            {
                if (p.Faction == Faction.OfPlayer &&
                    p.def.defName.Equals("Human") &&
                    !pawnIdToPawn.ContainsKey(p.ThingID))
                {
                    pawnIdToPawn.Add(p.ThingID, p);
                }
            }
            StoredApparelContainer.Initialize(pawnIdToPawn);
        }
    }*/
}
