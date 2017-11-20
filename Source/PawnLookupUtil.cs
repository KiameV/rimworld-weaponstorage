using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    class PawnLookupUtil
    {
        private static Dictionary<string, Pawn> pawnLookup = null;

        public static IEnumerable<Pawn> PlayerPawns
        {
            get
            {
                if (pawnLookup == null)
                {
                    Initialize();
                }
                return pawnLookup.Values;
            }
        }

        public static bool TryGetPawn(string thingId, out Pawn pawn)
        {
            return pawnLookup.TryGetValue(thingId, out pawn);
        }

        public static void Initialize()
        {
            pawnLookup = new Dictionary<string, Pawn>();
            foreach (Pawn p in PawnsFinder.AllMapsAndWorld_Alive)
            {
                if (p.Faction == Faction.OfPlayer && p.def.race.Humanlike)
                {
                    pawnLookup.Add(p.ThingID, p);
                }
            }
        }

        public static void Clear()
        {
            if (pawnLookup != null)
            {
                pawnLookup.Clear();
                pawnLookup = null;
            }
        }
    }
}
