using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using System;

namespace WeaponStorage
{
    class WorldComp : WorldComponent
    {
        public static List<AssignedWeaponContainer> AssignedWeapons = new List<AssignedWeaponContainer>();

        public static LinkedList<Building_WeaponStorage> WeaponStoragesToUse { get; private set; }

        public WorldComp(World world) : base(world)
        {
            foreach (AssignedWeaponContainer c in AssignedWeapons)
            {
                c.Weapons.Clear();
            }
            AssignedWeapons.Clear();

            if (WeaponStoragesToUse == null)
            {
                WeaponStoragesToUse = new LinkedList<Building_WeaponStorage>();
            }
            else
            {
                WeaponStoragesToUse.Clear();
            }
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
            if (!WeaponStoragesToUse.Contains(ws))
            {
                WeaponStoragesToUse.AddLast(ws);
            }
        }

        public static bool Add(ThingWithComps t)
        {
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
            {
                if (ws.AddWeapon(t))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Remove(Building_WeaponStorage ws)
        {
            WeaponStoragesToUse.Remove(ws);
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

        public static void SortWeaponStoragesToUse()
        {
            LinkedList<Building_WeaponStorage> l = new LinkedList<Building_WeaponStorage>();
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
            {
                bool added = false;
                for (LinkedListNode<Building_WeaponStorage> n = WeaponStoragesToUse.First; n != null; n = n.Next)
                {
                    if (ws.settings.Priority > n.Value.settings.Priority)
                    {
                        l.AddBefore(n, ws);
                    }
                }
                if (!added)
                {
                    l.AddLast(ws);
                }
            }
            WeaponStoragesToUse.Clear();
            WeaponStoragesToUse = l;
        }
    }
}