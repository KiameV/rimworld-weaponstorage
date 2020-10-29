using RimWorld;
using SaveStorageSettingsUtil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using WeaponStorage.UI;

namespace WeaponStorage
{
    public class Building_WeaponStorage : Building_Storage, IStoreSettingsParent
    {
        public readonly Dictionary<ThingDef, LinkedList<ThingWithComps>> StoredWeapons = new Dictionary<ThingDef, LinkedList<ThingWithComps>>();
        public readonly Dictionary<ThingDef, LinkedList<ThingWithComps>> StoredBioEncodedWeapons = new Dictionary<ThingDef, LinkedList<ThingWithComps>>();

        private Map CurrentMap { get; set; }

        public bool AllowAdds = true;

        private bool includeInTradeDeals = true;
        public bool IncludeInTradeDeals { get { return this.includeInTradeDeals; } }

		public bool IncludeInSharedWeapons = true;

		private List<Thing> forceAddedWeapons = null;

        public string Name = "";

        public Building_WeaponStorage()
        {
            this.AllowAdds = true;
		}

		public bool HasWeapon(SharedWeaponFilter filter, ThingDef def)
		{
			if (this.StoredWeapons.TryGetValue(def, out LinkedList<ThingWithComps> l))
			{
                if (l == null)
                {
                    this.StoredWeapons.Remove(def);
                }
                else
                {
                    foreach (ThingWithComps t in l)
                        if (filter.Allows(t))
                            return true;
                }
            }
            // Do not include bio incoded here
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

            if (WorldComp.AssignedWeapons.Count == 0)
            {
                foreach(var p in Util.GetPawns(true))
                {
                    AssignedWeaponContainer a = new AssignedWeaponContainer() { Pawn = p.Pawn };
                    if (p.Pawn.equipment.Primary != null)
                        a.Add(p.Pawn.equipment.Primary);
                    WorldComp.AssignedWeapons.Add(p.Pawn, a);
                }
            }
        }

        public override string Label => (this.Name == "") ? base.Label : this.Name;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAdd(ThingWithComps t)
        {
            return t != null && base.settings.AllowedToAccept(t);
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
							this.DropThing(t);
						}
                    this.StoredWeapons.Clear();
                }
                if (this.StoredBioEncodedWeapons != null)
                {
                    foreach (LinkedList<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
                        foreach (ThingWithComps t in l)
                        {
                            this.DropThing(t);
                        }
                    this.StoredBioEncodedWeapons.Clear();
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

		public bool TryRemoveWeapon(ThingDef def, SharedWeaponFilter filter, bool includeBioencoded, out ThingWithComps weapon)
		{
            if (this.StoredWeapons.TryGetValue(def, out LinkedList<ThingWithComps> l))
            {
                for (LinkedListNode<ThingWithComps> n = l.First; n != null; n = n.Next)
                {
                    if (filter.Allows(n.Value))
                    {
                        weapon = n.Value;
                        l.Remove(n);
                        return true;
                    }
                }
            }
            if (includeBioencoded)
            {
                if (this.StoredBioEncodedWeapons.TryGetValue(def, out l))
                {
                    for (LinkedListNode<ThingWithComps> n = l.First; n != null; n = n.Next)
                    {
                        if (filter.Allows(n.Value))
                        {
                            weapon = n.Value;
                            l.Remove(n);
                            return true;
                        }
                    }
                }
            }
			weapon = null;
			return false;
		}

		public int Count
        {
            get
            {
                return this.StoredWeapons.Count + this.StoredBioEncodedWeapons.Count;
            }
        }

        private bool DropThing(Thing t)
        {
            return BuildingUtil.DropThing(t, this, this.CurrentMap);
        }

        private void DropWeapons<T>(IEnumerable<T> things) where T : Thing
        {
            try
            {
                if (things != null)
                {
                    foreach (T t in things)
                    {
                        this.DropThing(t);
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
					this.DropWeapons(l);
                foreach (IEnumerable<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
                    this.DropWeapons(l);
                CombatExtendedUtil.EmptyAmmo(this);
                this.StoredWeapons.Clear();
                this.StoredBioEncodedWeapons.Clear();
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
                    DropThing(newItem);
                return;
            }

            if (!((newItem is ThingWithComps) && ((ThingWithComps)newItem).def.IsWeapon) &&
                !CombatExtendedUtil.IsAmmo(newItem))
            {
                if (!newItem.Spawned)
                    DropThing(newItem);
                return;
            }

            base.Notify_ReceivedThing(newItem);

            if (!CombatExtendedUtil.AddAmmo(newItem))
            {
                if (newItem is ThingWithComps &&
                !this.Contains((ThingWithComps)newItem))
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
        }

		private bool Contains(ThingWithComps t)
		{
            return this.Contains(t, (EquipmentUtility.IsBiocoded(t)) ? this.StoredBioEncodedWeapons : this.StoredWeapons);
		}

        private bool Contains(ThingWithComps t, Dictionary<ThingDef, LinkedList<ThingWithComps>> storage)
        {
            if (t != null && storage.TryGetValue(t.def, out LinkedList<ThingWithComps> l))
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
            if (this.CanAdd(weapon))
            {
                if (weapon.Spawned)
                {
                    weapon.DeSpawn();
                } 

                if (CombatExtendedUtil.AddAmmo(weapon))
                {
                    return true;
                }

                this.AddToSortedList(weapon, (EquipmentUtility.IsBiocoded(weapon)) ? this.StoredBioEncodedWeapons : this.StoredWeapons);
                return true;
            }
            return false;
        }

        private void AddToSortedList(ThingWithComps weapon, Dictionary<ThingDef, LinkedList<ThingWithComps>> storage)
        {
            string weaponDefLabel = weapon.def.label;
			if (!storage.TryGetValue(weapon.def, out LinkedList<ThingWithComps> l))
			{
				l = new LinkedList<ThingWithComps>();
                l.AddFirst(weapon);
                storage[weapon.def] = l;
                return;
			}

            if (weapon.TryGetQuality(out QualityCategory weaponQuality))
            {
                for (LinkedListNode<ThingWithComps> n = l.First; n != null; n = n.Next)
                {
                    if (n.Value.TryGetQuality(out QualityCategory currentQuality))
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
            foreach (IEnumerable<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
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
            var g = new List<ThingWithComps>();
			foreach (KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv in this.StoredWeapons)
			{
                GetFilteredWeaponsFromStorage(bill, filter, g, kv);
            }
            foreach (KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv in this.StoredBioEncodedWeapons)
            {
                GetFilteredWeaponsFromStorage(bill, filter, g, kv);
            }
            if (g.Count > 0)
                gotten = g;
            else
                gotten = null;
            return gotten != null;
        }

        private void GetFilteredWeaponsFromStorage(Bill bill, ThingFilter filter, List<ThingWithComps> gotten, KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv)
        {
            if (filter.Allows(kv.Key))
            {
                foreach (ThingWithComps weapon in kv.Value)
                {
                    if (bill.IsFixedOrAllowedIngredient(weapon) && filter.Allows(weapon))
                    {
                        gotten.Add(weapon);
                    }
                }
            }
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
						(t.def.IsWeapon || CombatExtendedUtil.IsAmmo(t)) && 
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
                foreach (IEnumerable<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
                    this.temp.AddRange(l);
                if (this.forceAddedWeapons == null)
					this.forceAddedWeapons = new List<Thing>(0);
            }

            Scribe_Collections.Look(ref this.temp, false, "storedWeapons", LookMode.Deep, new object[0]);
            Scribe_Values.Look<bool>(ref this.includeInTradeDeals, "includeInTradeDeals", true, false);
			Scribe_Values.Look<bool>(ref this.IncludeInSharedWeapons, "includeInSharedWeapons", true, false);
			Scribe_Collections.Look(ref this.forceAddedWeapons, false, "forceAddedWeapons", LookMode.Deep, new object[0]);
            Scribe_Values.Look(ref this.Name, "name", "", false);

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.StoredWeapons.Clear();
                this.StoredBioEncodedWeapons.Clear();
                if (this.temp != null)
                {
                    foreach (ThingWithComps t in this.temp)
                    {
                        if (EquipmentUtility.IsBiocoded(t))
                            this.AddToSortedList(t, this.StoredBioEncodedWeapons);
                        else
                            this.AddToSortedList(t, this.StoredWeapons);
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
            sb.Append(this.Count);
            sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.IncludeInTradeDeals".Translate());
            sb.Append(": ");
            sb.Append(this.includeInTradeDeals.ToString());
            return sb.ToString();
        }

        public IEnumerable<ThingWithComps> GetWeapons(bool includeBioencoded)
        {
			foreach (LinkedList<ThingWithComps> l in this.StoredWeapons.Values)
				foreach (ThingWithComps t in l)
					yield return t;
            if (includeBioencoded)
                foreach (LinkedList<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
                    foreach (ThingWithComps t in l)
                        yield return t;
        }

        public IEnumerable<ThingWithComps> GetBioEncodedWeapons()
        {
            foreach (LinkedList<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
                foreach (ThingWithComps t in l)
                    yield return t;
        }

        /// <summary>
        /// METHOD SIGNATURE CANNOT BE CHANGED AS MENDING PATCH USES THIS METHOD
        /// </summary>
        public bool Remove(ThingWithComps weapon)
        {
            if (weapon == null)
                return true;

            if (!EquipmentUtility.IsBiocoded(weapon))
            {
                //Log.Warning("Remove non-biocoded");
                return this.RemoveFrom(weapon, this.StoredWeapons);
            }
            //Log.Warning("Remove biocoded");
            //foreach (var l in this.StoredBioEncodedWeapons.Values)
            //    foreach (var w in l)
            //         Log.Warning("  - " + w.Label);
            return this.RemoveFrom(weapon, this.StoredBioEncodedWeapons);
        }

        private bool RemoveFrom(ThingWithComps weapon, Dictionary<ThingDef, LinkedList<ThingWithComps>> storage)
        {
            if (weapon == null)
                return true;

            if (storage.TryGetValue(weapon.def, out var l))
            {
                if (l.Remove(weapon))
                {
                    if (!weapon.Spawned &&
                        !this.DropThing(weapon))
                    {
                        Log.Warning($"failed to drop {weapon.Label} from storage {this.Label}");
                        return false;
                    }
                    //else
                    //    Log.Warning($"could not drop {weapon.Label} in storage {this.Label}");
                }
                //else
                //    Log.Warning($"could not remove {weapon.Label} from storage {this.Label}");
            }
            //else
            //    Log.Warning($"could not find def {weapon.def} in storage {this.Label}");
            return weapon.Spawned;
        }

        public bool RemoveNoDrop(ThingWithComps thing)
        {
#if DEBUG
            Log.Warning(this.GetType().Name + ".RemoveNoDrop " + thing.Label);
#endif
            if (!this.StoredWeapons.TryGetValue(thing.def, out LinkedList<ThingWithComps> l) &&
                !this.StoredBioEncodedWeapons.TryGetValue(thing.def, out l))
            {
                return false;
            }
            return l.Remove(thing);
        }

        public override void TickLong()
        {
            if (this.Spawned && base.Map != null)
            {
                // Fix for an issue where apparel will appear on top of the dresser even though it's already stored inside
                this.HandleThingsOnTop();
            }

            /*if (!this.AreStorageSettingsEqual())
            {
                this.UpdatePreviousStorageFilter();

                WorldComp.SortWeaponStoragesToUse();

                List<ThingWithComps> removed = new List<ThingWithComps>();
                foreach (LinkedList<ThingWithComps> l in this.StoredWeapons.Values)
                {
                    this.CullStorage(removed, l);
                }
                foreach (LinkedList<ThingWithComps> l in this.StoredBioEncodedWeapons.Values)
                {
                    this.CullStorage(removed, l);
                }
            }*/

			if (this.forceAddedWeapons != null && this.forceAddedWeapons.Count > 0)
			{
                foreach (Thing t in this.forceAddedWeapons)
                {
                    try
                    {
                        this.DropThing(t);
                    }
                    catch { }
                }
				this.forceAddedWeapons.Clear();
				this.forceAddedWeapons = null;
			}
        }

        private void CullStorage(List<ThingWithComps> removed, LinkedList<ThingWithComps> l)
        {
            LinkedListNode<ThingWithComps> n = l.First;
            while (n != null)
            {
                var next = n.Next;
                if (!this.CanAdd(n.Value))
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
                    this.DropThing(t);
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

            l.Add(new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone", true),
                defaultLabel = "CommandRenameZoneLabel".Translate(),
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_Rename(this));
                },
            });

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

            if (CombatExtendedUtil.HasCombatExtended)
            {
                l.Add(new Command_Action()
                {
                    icon = UI.AssignUI.ammoTexture,
                    defaultDesc = "WeaponStorage.ManageAmmoDesc".Translate(),
                    defaultLabel = "WeaponStorage.ManageAmmo".Translate(),
                    activateSound = SoundDef.Named("Click"),
                    action = delegate { Find.WindowStack.Add(new UI.AmmoUI(this)); },
                    groupKey = groupKey,
                });
                ++groupKey;
            }

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

            return SaveStorageSettingsGizmoUtil.AddSaveLoadGizmos(l, "Weapon_Management", this.settings.filter);
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