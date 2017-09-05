using System;
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
                    if (c.Pawn.ThingID.Equals(pawn.ThingID))
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
                if (AssignedWeapons[i].Pawn.ThingID.Equals(assignedWeapons.Pawn.ThingID))
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
                if (AssignedWeapons[i].Pawn.ThingID.Equals(pawn.ThingID))
                {
                    AssignedWeapons.RemoveAt(i);
                    break;
                }
            }
        }



        public Pawn Pawn;
        public List<ThingWithComps> Weapons = new List<ThingWithComps>();

        public void ExposeData()
        {
            Scribe_References.Look(ref this.Pawn, "pawn", true);
            Scribe_Collections.Look(ref this.Weapons, "weapons", LookMode.Deep);
        }
    }
}
