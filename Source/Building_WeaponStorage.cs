using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Verse;

namespace WeaponStorage
{
    public class Building_WeaponStorage : Building_Storage, IStoreSettingsParent
    {
        private readonly LinkedList<ThingWithComps> storedWeapons = new LinkedList<ThingWithComps>();

        private Map CurrentMap { get; set; }

        public bool AllowAdds { get; set; }

        private bool includeInTradeDeals = true;
        public bool IncludeInTradeDeals { get { return this.includeInTradeDeals; } }

        public Building_WeaponStorage()
        {
            this.AllowAdds = true;
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
                r.AddWeaponStorage(this);
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

        public override void DeSpawn()
        {
            try
            {
                this.Dispose();
                base.DeSpawn();
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
                if (this.storedWeapons != null)
                {
                    foreach (ThingWithComps t in this.storedWeapons)
                    {
                        this.DropThing(t, false);
                    }
                    this.storedWeapons.Clear();
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
                r.RemoveWeaponStorage(this);
            }
        }

        public int Count
        {
            get
            {
                return this.storedWeapons.Count;
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
                this.DropWeapons(this.storedWeapons, false);
                this.storedWeapons.Clear();
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

            if (!this.storedWeapons.Contains((ThingWithComps)newItem))
            {
                // Must go after 'contains' check. In the case of 'drop on floor' Notify_ReceiveThing gets called before the weapon is removed from the list
                if (newItem.Spawned)
                    newItem.DeSpawn();

                this.AddWeapon((ThingWithComps)newItem);
            }
        }

        internal void AddWeapons(IEnumerable<ThingWithComps> weapons)
        {
            if (weapons == null)
                return;

            foreach (ThingWithComps w in weapons)
            {
                this.AddWeapon(w);
            }
        }

        internal bool AddWeapon(ThingWithComps weapon, bool fromWorldComp = false)
        {
            if (weapon != null)
            {
                if (this.settings.AllowedToAccept(weapon))
                {
                    if (weapon.Spawned)
                    {
                        weapon.DeSpawn();
                    }

                    if (!this.storedWeapons.Contains(weapon))
                    {
                        this.AddToSortedList(weapon);
                    }
                    return true;
                }

                // Not Allowed
                if (!fromWorldComp && WorldComp.Add(weapon))
                {
                    return true;
                }

                if (!weapon.Spawned)
                {
                    BuildingUtil.DropThing(weapon, this, this.CurrentMap, false);
                }
            }
            return false;
        }

        private void AddToSortedList(ThingWithComps weapon)
        {
            string weaponDefLabel = weapon.def.label;
            bool found = false;
            for (LinkedListNode<ThingWithComps> n = this.storedWeapons.First; n != null; n = n.Next)
            {
                string nDefLabel = n.Value.def.label;
                if (nDefLabel.Equals(weaponDefLabel))
                {
                    found = true;
                    QualityCategory weaponQuality;
                    QualityCategory currentQuality;
                    if (weapon.TryGetQuality(out weaponQuality) &&
                        n.Value.TryGetQuality(out currentQuality))
                    {
                        if ((weaponQuality > currentQuality) ||
                            (weaponQuality == currentQuality &&
                             weapon.HitPoints >= n.Value.HitPoints))
                        {
                            this.storedWeapons.AddBefore(n, weapon);
                            return;
                        }
                    }
                }
                else if (weaponDefLabel.CompareTo(nDefLabel) < 0)
                {
                    this.storedWeapons.AddBefore(n, weapon);
                    return;
                }
                else if (found)
                {
                    this.storedWeapons.AddBefore(n, weapon);
                    return;
                }
            }
            this.storedWeapons.AddLast(weapon);
        }

        internal int GetWeaponCount(ThingDef def)
        {
            int count = 0;
            foreach (ThingWithComps twc in this.storedWeapons)
            {
                if (twc.def == def)
                {
                    ++count;
                }
            }
            return count;
        }

        internal bool TryGetFilteredWeapons(Bill bill, ThingFilter filter, out List<ThingWithComps> gotten)
        {
            gotten = null;
            foreach (ThingWithComps weapon in this.storedWeapons)
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
            return gotten != null;
        }

        internal void ReclaimWeapons()
        {
            List<ThingWithComps> l = BuildingUtil.FindThingsOfTypeNextTo<ThingWithComps>(base.Map, base.Position, 1);
            if (l.Count > 0)
            {
                foreach (ThingWithComps t in l)
                {
                    this.AddWeapon(t);
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
                this.temp = new List<ThingWithComps>(this.storedWeapons);
            }

            Scribe_Collections.Look(ref this.temp, "storedWeapons", LookMode.Deep, new object[0]);
            Scribe_Values.Look<bool>(ref this.includeInTradeDeals, "includeInTradeDeals", true, false);

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.storedWeapons.Clear();
                if (this.temp != null)
                {
                    foreach (ThingWithComps t in this.temp)
                    {
                        this.AddToSortedList(t);
                    }
                }
                this.temp.Clear();
                this.temp = null;
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
            sb.Append(this.storedWeapons.Count);
            sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.IncludeInTradeDeals".Translate());
            sb.Append(": ");
            sb.Append(this.includeInTradeDeals.ToString());
            return sb.ToString();
        }

        public IEnumerable<ThingWithComps> StoredWeapons
        {
            get
            {
                return this.storedWeapons;
            }
        }

        public bool Remove(ThingWithComps weapon, bool forbidden = true)
        {
            try
            {
                if (this.DropThing(weapon, forbidden))
                {
                    return this.storedWeapons.Remove(weapon);
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
            return this.storedWeapons.Remove(thing);
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
                LinkedListNode<ThingWithComps> n = this.storedWeapons.First;
                while (n != null)
                {
                    var next = n.Next;
                    if (!base.settings.AllowedToAccept(n.Value))
                    {
                        removed.Add(n.Value);
                        this.storedWeapons.Remove(n);
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

            Command_Action a = new Command_Action();
            a.icon = UI.AssignUI.assignweaponsTexture;
            a.defaultDesc = "WeaponStorage.AssignWeaponsDesc".Translate();
            a.defaultLabel = "WeaponStorage.AssignWeapons".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action = delegate { Find.WindowStack.Add(new UI.AssignUI(this)); };
            ++groupKey;
            l.Add(a);

            a = new Command_Action();
            a.icon = UI.AssignUI.emptyTexture;
            a.defaultDesc = "WeaponStorage.EmptyDesc".Translate();
            a.defaultLabel = "WeaponStorage.Empty".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action =
                delegate
                {
                    this.Empty();
                };
            ++groupKey;
            l.Add(a);

            a = new Command_Action();
            a.icon = UI.AssignUI.collectTexture;
            a.defaultDesc = "WeaponStorage.CollectDesc".Translate();
            a.defaultLabel = "WeaponStorage.Collect".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action =
                delegate
                {
                    this.ReclaimWeapons();
                };
            a.groupKey = groupKey;
            ++groupKey;
            l.Add(a);

            a = new Command_Action();
            if (this.includeInTradeDeals)
            {
                a.icon = UI.AssignUI.yesSellTexture;
            }
            else
            {
                a.icon = UI.AssignUI.noSellTexture;
            }
            a.defaultDesc = "WeaponStorage.IncludeInTradeDealsDesc".Translate();
            a.defaultLabel = "WeaponStorage.IncludeInTradeDeals".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action =
                delegate
                {
                    this.includeInTradeDeals = !this.includeInTradeDeals;
                };
            a.groupKey = groupKey;
            ++groupKey;
            l.Add(a);

            return SaveStorageSettingsUtil.SaveStorageSettingsGizmoUtil.AddSaveLoadGizmos(l, "Weapon_Management", this.settings.filter);
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