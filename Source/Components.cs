using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    public class WorldComp : WorldComponent
	{
		public static List<Building_WeaponStorage> WeaponStoragesToUse = new List<Building_WeaponStorage>();

		public static Dictionary<Pawn, AssignedWeaponContainer> AssignedWeapons = new Dictionary<Pawn, AssignedWeaponContainer>();

		public static List<SharedWeaponFilter> SharedWeaponFilter = new List<SharedWeaponFilter>();

		private static bool defInitialized = false;

        public WorldComp(World world) : base(world)
        {
            if (!defInitialized)
            {
                ThingDef d = null;
                List<ThingDef> weapons = new List<ThingDef>();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.defName.Equals("WeaponStorage"))
                    {
                        d = def;
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
                defInitialized = true;
            }

            foreach (AssignedWeaponContainer c in AssignedWeapons.Values)
            {
                c.Weapons.Clear();
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
                    return ws.AddWeapon(t);
                }
            }
            return false;
        }

		public static bool TryRemoveWeapon(ThingDef def, SharedWeaponFilter filter, out ThingWithComps weapon)
		{
			if (def != null)
			{
				foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
				{
					if (ws.TryRemoveWeapon(def, filter, out weapon))
						return true;
					break;
				}
			}
			weapon = null;
			return false;
		}

        public static bool Drop(ThingWithComps w)
        {
			foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
				if (BuildingUtil.DropThing(w, ws, ws.Map, false))
				{
					return true;
				}

            return false;
        }

        public static IEnumerable<Building_WeaponStorage> GetWeaponStorages()
        {
            foreach (Building_WeaponStorage ws in WeaponStoragesToUse)
            {
                if (ws.Spawned)
                {
                    yield return ws;
                }
            }
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
                    if (a.Pawn == null || a.Pawn.Dead)
                    {
                        Log.Warning("Unable to load pawn [" + a.Pawn + "]. Re-storing assigned weapons");
                        if (a.Weapons != null)
                        {
                            foreach(ThingWithComps w in a.Weapons)
                            {
                                if (!Add(w))
                                {
                                    Drop(w);
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
    }
}