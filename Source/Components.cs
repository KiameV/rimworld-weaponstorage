using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using WeaponStorage.UI;

namespace WeaponStorage
{
    public class WorldComp : WorldComponent
    {
        public static List<Building_WeaponStorage> WeaponStoragesToUse = new List<Building_WeaponStorage>();

        private static Dictionary<Pawn, AssignedWeaponContainer> AssignedWeapons = new Dictionary<Pawn, AssignedWeaponContainer>();

        public static List<SharedWeaponFilter> SharedWeaponFilter = new List<SharedWeaponFilter>();

        public static Stack<ThingWithComps> WeaponsToDrop = new Stack<ThingWithComps>();

        public static ThingDef WeaponStorageDef { get; private set; }

        static WorldComp() { WeaponStorageDef = null; }

        public WorldComp(World world) : base(world)
        {
            if (WeaponStorageDef == null)
            {
                ThingDef d = null;
                List<ThingDef> weapons = new List<ThingDef>();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.defName.Equals("WeaponStorage"))
                    {
                        d = def;
                        WeaponStorageDef = def;
                    }
                    else if (def.IsWeapon)
                    {
                        weapons.Add(def);
                    }
                }

                bool allows;
                foreach (ThingDef w in weapons)
                {
                    d.building.fixedStorageSettings.filter.SetAllow(w, true);
                    allows = true;
                    if (w.defName.Equals("Beer") ||
                        w.defName.Equals("WoodLog"))
                    {
                        allows = false;
                    }
                    d.building.defaultStorageSettings.filter.SetAllow(w, allows);
                }
                d.building.fixedStorageSettings.filter.RecalculateDisplayRootCategory();
                d.building.defaultStorageSettings.filter.RecalculateDisplayRootCategory();

                if (WeaponStorageDef == null)
                    Log.Error("Unabled to find WeaponStorageDef");
            }

            foreach (AssignedWeaponContainer c in AssignedWeapons.Values)
            {
                c.Clear();
            }
            AssignedWeapons.Clear();

            SharedWeaponFilter.Clear();

            if (WeaponStoragesToUse == null)
                WeaponStoragesToUse = new List<Building_WeaponStorage>();
            WeaponStoragesToUse.Clear();
        }

        public static void Add(Building_WeaponStorage ws)
        {
            if (ws == null || ws.Map == null)
            {
                Log.Error("Cannot add WeaponStorage that is either null or has a null map.");
                return;
            }

            if (!WeaponStoragesToUse.Contains(ws))
                WeaponStoragesToUse.Add(ws);
        }

        public static bool Add(ThingWithComps t)
        {
            if (t != null)
            {
                foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
                {
                    if (ws.AddWeapon(t))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CanAdd(ThingWithComps t)
        {
            if (t != null)
            {
                foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
                {
                    if (ws.CanAdd(t))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool TryRemoveWeapon(ThingDef def, SharedWeaponFilter filter, bool includeBioencoded, out ThingWithComps weapon)
        {
            if (def != null)
            {
                if (CombatExtendedUtil.TryRemoveAmmo(def, 1, out Thing t))
                {
                    weapon = t as ThingWithComps;
                    if (weapon != null)
                        return true;
                }

                foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
                {
                    if (ws.TryRemoveWeapon(def, filter, includeBioencoded, out weapon))
                        return true;
                }
            }
            weapon = null;
            return false;
        }

        public static bool Drop(ThingWithComps w)
        {
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
                if (BuildingUtil.DropThing(w, ws, ws.Map))
                {
                    return true;
                }

            return false;
        }

        public static List<Building_WeaponStorage> GetWeaponStorages()
        {
            List<Building_WeaponStorage> l = new List<Building_WeaponStorage>(WeaponStoragesToUse.Count);
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
            {
                if (ws.Spawned)
                {
                    l.Add(ws);
                }
            }
            return l;
        }

        public static IEnumerable<Building_WeaponStorage> GetWeaponStorages(Map map)
        {
            if (WeaponStoragesToUse != null)
            {
                foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
                {
                    if (map == null ||
                        (ws.Spawned && ws.Map == map))
                    {
                        yield return ws;
                    }
                }
            }
        }

        public static bool HasStorages()
        {
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
            {
                if (ws.Spawned)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasStorages(Map map)
        {
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
            {
                if (ws.Spawned && ws.Map == map)
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

        public static void Remove(Map map)
        {
            for (int i = WeaponStoragesToUse.Count - 1; i >= 0; --i)
            {
                if (WeaponStoragesToUse[i].Map == map)
                {
                    WeaponStoragesToUse.RemoveAt(i);
                }
            }
        }

        private List<AssignedWeaponContainer> tmp = null;
        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                tmp = new List<AssignedWeaponContainer>(AssignedWeapons.Values);
            }

            Scribe_Collections.Look(ref tmp, "assignedWeapons", LookMode.Deep, new object[0]);
            Scribe_Collections.Look(ref SharedWeaponFilter, "sharedWeaponFilter", LookMode.Deep, new object[0]);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                foreach (AssignedWeaponContainer a in tmp)
                {
                    if (!Settings.EnableAssignWeapons)
                    {
                        if (a.Weapons != null)
                        {
                            foreach (ThingWithComps w in a.Weapons)
                            {
                                if (!Add(w))
                                {
                                    WeaponsToDrop.Push(w);
                                }
                            }
                        }
                    }
                    else if (a.Pawn == null || a.Pawn.Dead)
                    {
                        Log.Warning("Unable to load pawn [" + a.Pawn + "]. Re-storing assigned weapons");
                        if (a.Weapons != null)
                        {
                            foreach (ThingWithComps w in a.Weapons)
                            {
                                if (!Add(w))
                                {
                                    WeaponsToDrop.Push(w);
                                }
                            }
                        }
                    }
                    else
                    {
                        AssignedWeapons.Add(a.Pawn, a);
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving ||
                Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (tmp != null)
                {
                    tmp.Clear();
                    tmp = null;
                }
                if (SharedWeaponFilter == null)
                    SharedWeaponFilter = new List<SharedWeaponFilter>();
            }
        }

        public static void SortWeaponStoragesToUse()
        {
            WeaponStoragesToUse.Sort((l, r) => l.settings.Priority.CompareTo(r.settings.Priority));
        }

        public static bool CreateOrGetAssignedWeapons(Pawn pawn, out AssignedWeaponContainer aw)
        {
            if (!Settings.EnableAssignWeapons)
            {
                aw = null;
                return false;
            }

            if (!AssignedWeapons.TryGetValue(pawn, out aw))
            {
                aw = new AssignedWeaponContainer();
                AssignedWeapons.Add(pawn, aw);
            }
            return true;
        }

        public static void AddAssignedWeapons(Pawn pawn, AssignedWeaponContainer aw)
        {
            if (Settings.EnableAssignWeapons)
                AssignedWeapons[pawn] = aw;
        }

        public static bool TryGetAssignedWeapons(Pawn pawn, out AssignedWeaponContainer aw)
        {
            if (!Settings.EnableAssignWeapons)
            {
                aw = null;
                return false;
            }
            return AssignedWeapons.TryGetValue(pawn, out aw);
        }

        public static bool RemoveAssignedWeapons(Pawn pawn)
        {
            return AssignedWeapons.Remove(pawn);
        }

        public static void InitializeAssignedWeapons()
        {
            if (Settings.EnableAssignWeapons && AssignedWeapons.Count == 0)
            {
                foreach (var p in Util.GetPawns(true))
                {
                    AssignedWeaponContainer a = new AssignedWeaponContainer() { Pawn = p.Pawn };
                    if (p.Pawn.equipment.Primary != null)
                        a.Add(p.Pawn.equipment.Primary);
                    AssignedWeapons.Add(p.Pawn, a);
                }
            }
        }

        public static IEnumerable<AssignedWeaponContainer> AssignedWeaponContainers { 
            get
            {
                if (!Settings.EnableAssignWeapons)
                    return new List<AssignedWeaponContainer>(0);
                return AssignedWeapons.Values;
            }
        }
    }
}