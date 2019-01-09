using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace WeaponStorage
{
    public class Building_WeaponStorage : Building_Storage, IStoreSettingsParent
    {
        public readonly Dictionary<ThingDef, LinkedList<ThingWithComps>> StoredWeapons = new Dictionary<ThingDef, LinkedList<ThingWithComps>>();

        private Map CurrentMap { get; set; }

        public bool AllowAdds { get; set; }

        private bool includeInTradeDeals = true;
        public bool IncludeInTradeDeals { get { return this.includeInTradeDeals; } }

		public bool IncludeInSharedWeapons = true;

		private List<Thing> forceAddedWeapons = null;

		public Building_WeaponStorage()
        {
            this.AllowAdds = true;
		}

		public bool HasWeapon(SharedWeaponFilter filter, ThingDef def)
		{
			/*Log.Error("Stored Weapons: " + this.StoredWeapons.Count);
			foreach (ThingDef d in this.StoredWeapons.Keys)
			{
				Log.Warning("    " + d.label);
			}*/
			if (this.StoredWeapons.TryGetValue(def, out LinkedList<ThingWithComps> l))
			{
				foreach (ThingWithComps t in l)
					if (filter.Allows(t))
						return true;
			}
			return false;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.CurrentMap = map;

            if (settings == null)
            {
                base.settings = new StorageSettings(this);
                base.settings.CopyFrom(this.def.building.defaultStorageSettings);
                base.settings.filter.SetDisallowAll();
            }
            this.UpdatePreviousStorageFilter();
            WorldComp.Add(this);

            foreach (Building_RepairWeaponStorage r in BuildingUtil.FindThingsOfTypeNextTo<Building_RepairWeaponStorage>(base.Map, base.Position, Settings.RepairAttachmentDistance))
            {
#if DEBUG_REPAIR
                Log.Warning("Adding Dresser " + this.Label + " to " + r.Label);
#endif
                r.Add(this);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            try
            {
                this.Dispose();
                base.Destroy(mode);
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Destroy\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            try
            {
                this.Dispose();
                base.DeSpawn(mode);
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".DeSpawn\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        private void Dispose()
        {
            try
            {
                if (this.StoredWeapons != null)
                {
					foreach (LinkedList<ThingWithComps> l in this.StoredWeapons.Values)
						foreach (ThingWithComps t in l)
						{
							this.DropThing(t, false);
						}
                    this.StoredWeapons.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Dispose\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }

            WorldComp.Remove(this);
            foreach (Building_RepairWeaponStorage r in BuildingUtil.FindThingsOfTypeNextTo<Building_RepairWeaponStorage>(base.Map, base.Position, Settings.RepairAttachmentDistance))
            {
#if DEBUG_REPAIR
                Log.Warning("Removing Dresser " + this.Label + " to " + r.Label);
#endif
                r.Remove(this);
            }
        }

		public bool TryRemoveWeapon(ThingDef def, SharedWeaponFilter filter, out ThingWithComps weapon)
		{
			if (this.StoredWeapons.TryGetValue(def, out LinkedList<ThingWithComps> l))
				for (LinkedListNode<ThingWithComps> n = l.First; n != null; n = n.Next)
					if (filter.Allows(n.Value))
					{
						weapon = n.Value;
						l.Remove(n);
						return true;
					}
			weapon = null;
			return false;
		}

		public int Count
        {
            get
            {
                return this.StoredWeapons.Count;
            }
        }

        private bool DropThing(Thing t, bool makeForbidden = true)
        {
            return BuildingUtil.DropThing(t, this, this.CurrentMap, makeForbidden);
        }

        private void DropWeapons<T>(IEnumerable<T> things, bool makeForbidden = true) where T : Thing
        {
            try
            {
                if (things != null)
                {
                    foreach (T t in things)
                    {
                        this.DropThing(t, makeForbidden);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    "ChangeDresser:Building_Dresser.DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public void Empty()
        {
            try
            {
                this.AllowAdds = false;
				foreach (IEnumerable<ThingWithComps> l in this.StoredWeapons.Values)
					this.DropWeapons(l, false);
                this.StoredWeapons.Clear();
            }
            finally
            {
                this.AllowAdds = true;
            }
        }

        public override void Notify_ReceivedThing(Thing newItem)
        {
            if (!this.AllowAdds)
            {
                if (!newItem.Spawned)
                {
                    DropThing(newItem);
                }
                return;
            }

            if (!(newItem is ThingWithComps) ||
                !((ThingWithComps)newItem).def.IsWeapon)
            {
                DropThing(newItem);
                return;
            }

            base.Notify_ReceivedThing(newItem);

            if (!this.Contains((ThingWithComps)newItem))
            {
                // Must go after 'contains' check. In the case of 'drop on floor' Notify_ReceiveThing gets called before the weapon is removed from the list
                if (newItem.Spawned)
                    newItem.DeSpawn();

                if (!this.AddWeapon(newItem as ThingWithComps) && 
                    !WorldComp.Add(newItem as ThingWithComps))
                {
                    BuildingUtil.DropThing(newItem, this, this.CurrentMap, true);
                }
            }
        }

		private bool Contains(ThingWithComps t)
		{
			if (t != null &&
				this.StoredWeapons.TryGetValue(t.def, out LinkedList<ThingWithComps> l))
				return l.Contains(t);
			return false;
		}

		/*internal void AddWeapons(IEnumerable<ThingWithComps> weapons)
        {
            if (weapons == null)
                return;

            foreach (ThingWithComps w in weapons)
            {
                this.AddWeapon(w);
            }
        }*/

		internal bool AddWeapon(ThingWithComps weapon)
        {
            if (weapon != null)
            {
                if (this.settings.AllowedToAccept(weapon))
                {
                    if (weapon.Spawned)
                    {
                        weapon.DeSpawn();
                    }

                    if (!this.Contains(weapon))
                    {
                        this.AddToSortedList(weapon);
                    }
                    return true;
                }
            }
            return false;
        }

        private void AddToSortedList(ThingWithComps weapon)
        {
            string weaponDefLabel = weapon.def.label;
			if (!this.StoredWeapons.TryGetValue(weapon.def, out LinkedList<ThingWithComps> l))
			{
				l = new LinkedList<ThingWithComps>();
				this.StoredWeapons[weapon.def] = l;
			}

			for (LinkedListNode<ThingWithComps> n = l.First; n != null; n = n.Next)
            {
				if (weapon.TryGetQuality(out QualityCategory weaponQuality) &&
					n.Value.TryGetQuality(out QualityCategory currentQuality))
				{
					if ((weaponQuality > currentQuality) ||
						(weaponQuality == currentQuality &&
							weapon.HitPoints >= n.Value.HitPoints))
					{
						l.AddBefore(n, weapon);
						return;
					}
				}
            }
            l.AddLast(weapon);
        }

        internal int GetWeaponCount(ThingDef expectedDef, QualityRange qualityRange, FloatRange hpRange, ThingFilter ingredientFilter)
        {
            int count = 0;
			foreach (IEnumerable<ThingWithComps> l in this.StoredWeapons.Values)
				foreach (ThingWithComps t in l)
					if (this.Allows(t, expectedDef, qualityRange, hpRange, ingredientFilter))
						++count;
            return count;
        }

        private bool Allows (Thing t, ThingDef expectedDef, QualityRange qualityRange, FloatRange hpRange, ThingFilter filter)
        {
#if DEBUG || DEBUG_DO_UNTIL_X
            Log.Warning("Building_WeaponStoreage.Allows Begin [" + t.Label + "]");
#endif
            if (t.def != expectedDef)
            {
#if DEBUG || DEBUG_DO_UNTIL_X
                Log.Warning("    Building_WeaponStoreage.Allows End Def Does Not Match [False]");
#endif
                return false;
            }
            if (expectedDef.useHitPoints &&
                hpRange != null &&
                hpRange.min != 0f && hpRange.max != 100f)
            {
                float num = (float)t.HitPoints / (float)t.MaxHitPoints;
                num = GenMath.RoundedHundredth(num);
                if (!hpRange.IncludesEpsilon(Mathf.Clamp01(num)))
                {
#if DEBUG || DEBUG_DO_UNTIL_X
                    Log.Warning("    Building_WeaponStoreage.Allows End Hit Points [False - HP]");
#endif
                    return false;
                }
            }
            if (qualityRange != null && qualityRange != QualityRange.All && t.def.FollowQualityThingFilter())
            {
                QualityCategory p;
                if (!t.TryGetQuality(out p))
                {
                    p = QualityCategory.Normal;
                }
                if (!qualityRange.Includes(p))
                {
#if DEBUG || DEBUG_DO_UNTIL_X
                    Log.Warning("    Building_WeaponStoreage.Allows End Quality [False - Quality]");
#endif
                    return false;
                }
            }

            if (filter != null && !filter.Allows(t.Stuff))
            {
#if DEBUG || DEBUG_DO_UNTIL_X
                    Log.Warning("StoredApparel.Allows End Quality [False - filters.Allows]");
#endif
                return false;
            }
#if DEBUG || DEBUG_DO_UNTIL_X
            Log.Warning("    Building_WeaponStoreage.Allows End [True]");
#endif
            return true;
        }

        internal bool TryGetFilteredWeapons(Bill bill, ThingFilter filter, out List<ThingWithComps> gotten)
        {
            gotten = null;
			foreach (KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv in this.StoredWeapons)
			{
				if (filter.Allows(kv.Key))
				{
					foreach (ThingWithComps weapon in kv.Value)
					{
						if (bill.IsFixedOrAllowedIngredient(weapon) && filter.Allows(weapon))
						{
							if (gotten == null)
							{
								gotten = new List<ThingWithComps>();
							}
							gotten.Add(weapon);
						}
					}
				}
			}
            return gotten != null;
        }

        internal void ReclaimWeapons(bool force = false)
        {
			if (base.Map == null)
				return;

            List<ThingWithComps> l = BuildingUtil.FindThingsOfTypeNextTo<ThingWithComps>(base.Map, base.Position, 1);
            if (l.Count > 0)
            {
                foreach (ThingWithComps t in l)
                {
					if (!this.AddWeapon(t) && 
						force &&
						t.Spawned && 
						t.def.IsWeapon && 
						!t.def.defName.Equals("WoodLog"))
					{
						t.DeSpawn();
						if (this.forceAddedWeapons == null)
							forceAddedWeapons = new List<Thing>(l.Count);
						this.forceAddedWeapons.Add(t);
					}
                }
                l.Clear();
            }
        }

        public void HandleThingsOnTop()
        {
            if (this.Spawned)
            {
                foreach (Thing t in base.Map.thingGrid.ThingsAt(this.Position))
                {
                    if (t != null && t != this && !(t is Blueprint) && !(t is Building))
                    {
                        if (!(t is ThingWithComps && this.AddWeapon((ThingWithComps)t)))
                        {
                            if (t.Spawned)
                            {
                                IntVec3 p = t.Position;
                                p.x = p.x + 1;
                                t.Position = p;
                                Log.Warning("Moving " + t.Label);
                            }
                        }
                    }
                }
            }
        }

        public List<ThingWithComps> temp = null;
        public override void ExposeData()
        {
            base.ExposeData();
            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.temp = new List<ThingWithComps>();
				foreach (IEnumerable<ThingWithComps> l in this.StoredWeapons.Values)
					this.temp.AddRange(l);
				if (this.forceAddedWeapons == null)
					this.forceAddedWeapons = new List<Thing>(0);
            }

            Scribe_Collections.Look(ref this.temp, false, "storedWeapons", LookMode.Deep, new object[0]);
            Scribe_Values.Look<bool>(ref this.includeInTradeDeals, "includeInTradeDeals", true, false);
			Scribe_Values.Look<bool>(ref this.IncludeInSharedWeapons, "includeInSharedWeapons", true, false);
			Scribe_Collections.Look(ref this.forceAddedWeapons, false, "forceAddedWeapons", LookMode.Deep, new object[0]);

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.StoredWeapons.Clear();
                if (this.temp != null)
                {
                    foreach (ThingWithComps t in this.temp)
                    {
                        this.AddToSortedList(t);
                    }
                }
            }
            
            if (Scribe.mode == LoadSaveMode.Saving ||
                Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                if (this.temp != null)
                {
                    this.temp.Clear();
                    this.temp = null;
                }

				if (this.forceAddedWeapons != null && this.forceAddedWeapons.Count == 0)
					this.forceAddedWeapons = null;
            }
        }

        public override string GetInspectString()
        {
            this.Tick();
            StringBuilder sb = new StringBuilder(base.GetInspectString());
            if (sb.Length > 0)
                sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.StoragePriority".Translate());
            sb.Append(": ");
            sb.Append(("StoragePriority" + base.settings.Priority).Translate());
            sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.Count".Translate());
            sb.Append(": ");
            sb.Append(this.StoredWeapons.Count);
            sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.IncludeInTradeDeals".Translate());
            sb.Append(": ");
            sb.Append(this.includeInTradeDeals.ToString());
            return sb.ToString();
        }

        public IEnumerable<ThingWithComps> AllWeapons
        {
            get
            {
				foreach (LinkedList<ThingWithComps> l in this.StoredWeapons.Values)
					foreach (ThingWithComps t in l)
						yield return t;
            }
		}

		/// <summary>
		/// METHOD SIGNATURE CANNOT BE CHANGED AS MENDING PATCH USES THIS METHOD
		/// </summary>
		public bool Remove(ThingWithComps weapon, bool forbidden = true)
        {
            try
            {
				StoredWeapons.TryGetValue(weapon.def, out LinkedList<ThingWithComps> weapons);
				weapons.Remove(weapon);

				if (weapon.Spawned ||
					this.DropThing(weapon, forbidden))
				{
					return true;
				}
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Remove(ThingWithComp)\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
            return false;
        }

        public bool RemoveNoDrop(ThingWithComps thing)
        {
#if DEBUG
            Log.Warning(this.GetType().Name + ".RemoveNoDrop " + thing.Label);
#endif
			if (this.StoredWeapons.TryGetValue(thing.def, out LinkedList<ThingWithComps> l))
				return l.Remove(thing);
			return false;
        }

        public override void TickLong()
        {
            if (this.Spawned && base.Map != null)
            {
                // Fix for an issue where apparel will appear on top of the dresser even though it's already stored inside
                this.HandleThingsOnTop();
            }

            if (!this.AreStorageSettingsEqual())
            {
                this.UpdatePreviousStorageFilter();

                WorldComp.SortWeaponStoragesToUse();

                List<ThingWithComps> removed = new List<ThingWithComps>();
				LinkedListNode<ThingWithComps> n;
				foreach (LinkedList<ThingWithComps> l in this.StoredWeapons.Values)
				{
					n = l.First;
					while (n != null)
					{
						var next = n.Next;
						if (!base.settings.AllowedToAccept(n.Value))
						{
							removed.Add(n.Value);
							l.Remove(n);
						}
						n = next;
					}

					foreach (ThingWithComps t in removed)
					{
						if (!WorldComp.Add(t))
						{
							this.DropThing(t, false);
						}
					}
				}
            }

			if (this.forceAddedWeapons != null && this.forceAddedWeapons.Count > 0)
			{
				foreach (Thing t in this.forceAddedWeapons)
					this.DropThing(t, false);
				this.forceAddedWeapons.Clear();
				this.forceAddedWeapons = null;
			}
        }
        #region Gizmos
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerable<Gizmo> enumerables = base.GetGizmos();

            List<Gizmo> l;
            if (enumerables != null)
                l = new List<Gizmo>(enumerables);
            else
                l = new List<Gizmo>(1);

            int groupKey = "WeaponStorage".GetHashCode();

            l.Add(new Command_Action()
            {
                icon = UI.AssignUI.assignweaponsTexture,
                defaultDesc = "WeaponStorage.AssignWeaponsDesc".Translate(),
                defaultLabel = "WeaponStorage.AssignWeapons".Translate(),
                activateSound = SoundDef.Named("Click"),
                action = delegate { Find.WindowStack.Add(new UI.AssignUI(this)); },
                groupKey = groupKey,
            });
            ++groupKey;

			l.Add(new Command_Action()
			{
				icon = UI.AssignUI.assignweaponsTexture,
				defaultDesc = "WeaponStorage.SharedWeaponsDesc".Translate(),
				defaultLabel = "WeaponStorage.SharedWeapons".Translate(),
				activateSound = SoundDef.Named("Click"),
				action = delegate { Find.WindowStack.Add(new UI.SharedWeaponsUI()); },
				groupKey = groupKey,
			});
			++groupKey;

			l.Add(new Command_Action()
            {
                icon = UI.AssignUI.emptyTexture,
                defaultDesc = "WeaponStorage.EmptyDesc".Translate(),
                defaultLabel = "WeaponStorage.Empty".Translate(),
                activateSound = SoundDef.Named("Click"),
                action = delegate { this.Empty(); },
                groupKey = groupKey,
            });
            ++groupKey;

            l.Add(new Command_Action()
            {
                icon = UI.AssignUI.collectTexture,
                defaultDesc = "WeaponStorage.CollectDesc".Translate(),
                defaultLabel = "WeaponStorage.Collect".Translate(),
                activateSound = SoundDef.Named("Click"),
                action = delegate { this.ReclaimWeapons(); },
                groupKey = groupKey,
            });
            ++groupKey;
            
            l.Add(new Command_Action()
            {
                icon = (this.includeInTradeDeals) ? UI.AssignUI.yesSellTexture : UI.AssignUI.noSellTexture,
                defaultDesc = "WeaponStorage.IncludeInTradeDealsDesc".Translate(),
                defaultLabel = "WeaponStorage.IncludeInTradeDeals".Translate(),
                activateSound = SoundDef.Named("Click"),
                action = delegate { this.includeInTradeDeals = !this.includeInTradeDeals; },
                groupKey = groupKey,
            });
            ++groupKey;

            return SaveStorageSettingsUtil.AddSaveLoadGizmos(l, "Weapon_Management", this.settings.filter);
        }
#endregion

		#region ThingFilters
        private ThingFilter previousStorageFilters = new ThingFilter();
        private FieldInfo AllowedDefsFI = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.Instance | BindingFlags.NonPublic);

		protected bool AreStorageSettingsEqual()
        {
            ThingFilter currentFilters = base.settings.filter;
            if (currentFilters.AllowedDefCount != this.previousStorageFilters.AllowedDefCount ||
                currentFilters.AllowedQualityLevels != this.previousStorageFilters.AllowedQualityLevels ||
                currentFilters.AllowedHitPointsPercents != this.previousStorageFilters.AllowedHitPointsPercents)
            {
                return false;
            }

            HashSet<ThingDef> currentAllowed = AllowedDefsFI.GetValue(currentFilters) as HashSet<ThingDef>;
            foreach (ThingDef previousAllowed in AllowedDefsFI.GetValue(this.previousStorageFilters) as HashSet<ThingDef>)
            {
                if (!currentAllowed.Contains(previousAllowed))
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdatePreviousStorageFilter()
        {
            ThingFilter currentFilters = base.settings.filter;

            this.previousStorageFilters.AllowedHitPointsPercents = currentFilters.AllowedHitPointsPercents;
            this.previousStorageFilters.AllowedQualityLevels = currentFilters.AllowedQualityLevels;

            HashSet<ThingDef> previousAllowed = AllowedDefsFI.GetValue(this.previousStorageFilters) as HashSet<ThingDef>;
            previousAllowed.Clear();
            previousAllowed.AddRange(AllowedDefsFI.GetValue(currentFilters) as HashSet<ThingDef>);
        }
		#endregion
    }
}