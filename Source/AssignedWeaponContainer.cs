using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    public class AssignedWeaponContainer : IExposable
    {
        public static List<AssignedWeaponContainer> AssignedWeapons = new List<AssignedWeaponContainer>();

        public static bool TryGetAssignedWeapons(Pawn pawn, out AssignedWeaponContainer assignedWeaponContainer)
        {
            if (pawn != null)
            {
                foreach (AssignedWeaponContainer c in AssignedWeapons)
                {
                    if (c.PawnId.Equals(pawn.ThingID))
                    {
                        assignedWeaponContainer = c;
                        return true;
                    }
                }
            }
            assignedWeaponContainer = null;
            return false;
        }

        public static void Set(AssignedWeaponContainer assignedWeapons)
        {
            for (int i = 0; i < AssignedWeapons.Count; ++i)
            {
                if (AssignedWeapons[i].PawnId.Equals(assignedWeapons.PawnId))
                {
                    AssignedWeapons[i] = assignedWeapons;
                    return;
                }
            }
            AssignedWeapons.Add(assignedWeapons);
        }

        public static void Remove(Pawn pawn)
        {
            for (int i = 0; i < AssignedWeapons.Count; ++i)
            {
                if (AssignedWeapons[i].PawnId.Equals(pawn.ThingID))
                {
                    AssignedWeapons.RemoveAt(i);
                    break;
                }
            }
        }


        public string PawnId = "";
        //public Pawn Pawn;
        public List<ThingWithComps> Weapons = new List<ThingWithComps>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.PawnId, "pawn", "", true);
            Scribe_Collections.Look(ref this.Weapons, "weapons", LookMode.Deep, new object[0]);
        }
    }
}
