using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    class WorldComp : WorldComponent
    {
        public static List<AssignedWeaponContainer> AssignedWeapons = new List<AssignedWeaponContainer>();
        private static List<Building_WeaponStorage> weaponStorages = new List<Building_WeaponStorage>();

        public WorldComp(World world) : base(world)
        {
            foreach (AssignedWeaponContainer c in AssignedWeapons)
            {
                c.Weapons.Clear();
            }
            AssignedWeapons.Clear();
        }

        public static void Add(AssignedWeaponContainer assignedWeapons)
        {
#if DEBUG
            Log.Warning("WeaponStorage.TryAdd for " + assignedWeapons.PawnId);
#endif
            AssignedWeaponContainer c;
            if (!TryGetAssignedWeapons(assignedWeapons.PawnId, out c))
            {
                AssignedWeapons.Add(assignedWeapons);
            }
            else
            {
                c.Weapons = assignedWeapons.Weapons;
            }
        }

        public static void Add(Building_WeaponStorage ws)
        {
            if (!weaponStorages.Contains(ws))
            {
                weaponStorages.Add(ws);
            }
        }

        public static IEnumerable<Building_WeaponStorage> WeaponStorages
        {
            get
            {
                return weaponStorages;
            }
        }

        public static void Remove(Building_WeaponStorage ws)
        {
            weaponStorages.Remove(ws);
        }

        public static bool TryGetAssignedWeapons(string pawnId, out AssignedWeaponContainer assignedWeaponContainer)
        {
            foreach (AssignedWeaponContainer c in AssignedWeapons)
            {
                if (c.PawnId.Equals(pawnId))
                {
                    assignedWeaponContainer = c;
                    return true;
                }
            }
            assignedWeaponContainer = null;
            return false;
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

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref AssignedWeapons, "assignedWeapons", LookMode.Deep, new object[0]);
        }
    }
}
