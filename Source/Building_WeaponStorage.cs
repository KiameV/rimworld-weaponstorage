using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Verse;

namespace WeaponStorage
{
    public class Building_WeaponStorage : Building_Storage, IStoreSettingsParent
    {
        private LinkedList<ThingWithComps> storedWeapons = new LinkedList<ThingWithComps>();

        private Map CurrentMap { get; set; }

        private bool includeInTradeDeals = true;
        public bool IncludeInTradeDeals { get { return this.includeInTradeDeals; } }

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

            WorldComp.Add(this);
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
                    foreach(ThingWithComps t in this.storedWeapons)
                    {
                        this.DropThing(t, true);
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
        }

        public int Count
        {
            get
            {
                if (this.storedWeapons == null)
                {
                    this.storedWeapons = new LinkedList<ThingWithComps>();
                }
                return this.storedWeapons.Count;
            }
        }

        private System.Random random = null;
        private void DropThing(Thing a, bool makeForbidden = true)
        {
            try
            {
                Thing t;
                if (!a.Spawned)
                {
                    GenThing.TryDropAndSetForbidden(a, base.Position, this.CurrentMap, ThingPlaceMode.Near, out t, makeForbidden);
                    if (!a.Spawned)
                    {
                        GenPlace.TryPlaceThing(a, base.Position, this.CurrentMap, ThingPlaceMode.Near);
                    }
                }
                if (a.Position.Equals(base.Position))
                {
                    IntVec3 pos = a.Position;
                    if (this.random == null)
                        this.random = new System.Random();
                    int dir = this.random.Next(2);
                    int amount = this.random.Next(2);
                    if (amount == 0)
                        amount = -1;
                    if (dir == 0)
                        pos.x = pos.x + amount;
                    else
                        pos.z = pos.z + amount;
                    a.Position = pos;
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public override void Notify_ReceivedThing(Thing newItem)
        {
            if (!(newItem is ThingWithComps) || !((ThingWithComps)newItem).def.IsWeapon)
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

        internal bool AddWeapon(ThingWithComps weapon)
        {
            if (weapon == null || !base.settings.AllowedToAccept(weapon))
            {
                return false;
            }

            if (weapon != null && weapon.Spawned)
            {
                weapon.DeSpawn();
            }

            if (this.storedWeapons.Contains(weapon))
            {
                return true;
            }

            string weaponDefName = weapon.def.defName;
            bool found = false;
            for (LinkedListNode<ThingWithComps> n = this.storedWeapons.First; n != null; n = n.Next)
            {
                string nDefName = n.Value.def.defName;
                if (nDefName.Equals(weaponDefName))
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
                            return true;
                        }
                    }
                }
                else if (weaponDefName.CompareTo(nDefName) < 0)
                {
                    this.storedWeapons.AddBefore(n, weapon);
                    return true;
                }
                else if (found)
                {
                    this.storedWeapons.AddBefore(n, weapon);
                    return true;
                }
            }
            this.storedWeapons.AddLast(weapon);
            return true;
        }

        public void Empty()
        {
            foreach (ThingWithComps twc in storedWeapons)
            {
                this.DropThing(twc, false);
            }
            this.storedWeapons.Clear();
        }

        public void Empty<T>(out List<T> contained) where T : Thing
        {
            contained = new List<T>(this.storedWeapons.Count);
            foreach (ThingWithComps twc in storedWeapons)
            {
                this.DropThing(twc, false);
                contained.Add(twc as T);
            }
            this.storedWeapons.Clear();
        }

        internal void ReclaimWeapons()
        {
            IEnumerable<ThingWithComps> l = 
                BuildingUtil.FindThingsOfTypeNextTo<ThingWithComps>(base.Map, base.Position, 1);
            foreach (ThingWithComps t in l)
            {
                this.AddWeapon(t);
            }
        }
        public void HandleThingsOnTop()
        {
#if TRADE_DEBUG
            Log.Warning("Start ChangeDresser.HandleThingsOnTop for " + this.Label + " Spawned: " + this.Spawned);
#endif
            if (this.Spawned)
            {
                foreach (Thing t in base.Map.thingGrid.ThingsAt(this.Position))
                {
#if TRADE_DEBUG
                    Log.Warning("ChangeDresser.HandleThingsOnTop - Thing " + t?.Label);
#endif
                    if (t != null && t != this)
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
#if TRADE_DEBUG
            Log.Warning("End ChangeDresser.HandleThingsOnTop");
#endif
        }

        public override void TickRare()
        {
            base.TickRare();
            this.HandleThingsOnTop();
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

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.storedWeapons.Clear();
                if (this.temp != null)
                {
                    this.AddWeapons(this.temp);
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
                if (this.storedWeapons == null)
                    this.storedWeapons = new LinkedList<ThingWithComps>();
                return this.storedWeapons;
            }
        }

        public void Remove(ThingWithComps weapon, bool forbidden = true)
        {
            try
            {
                this.DropThing(weapon, forbidden);
                this.storedWeapons.Remove(weapon);
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Remove(ThingWithComp)\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public bool RemoveNoDrop(ThingWithComps thing)
        {
            return this.storedWeapons.Remove(thing);
        }

        private readonly Stopwatch stopWatch = new Stopwatch();
        public override void TickLong()
        {
            try
            {
                if (!this.stopWatch.IsRunning)
                    this.stopWatch.Start();
                else
                {
                    // Do this every minute
                    if (this.stopWatch.ElapsedMilliseconds > 60000)
                    {
                        for (LinkedListNode<ThingWithComps> n = this.storedWeapons.First; n != null; n = n.Next)
                        {
                            if (!this.settings.filter.Allows(n.Value))
                            {
                                this.DropThing(n.Value, false);
                                this.storedWeapons.Remove(n);
                            }
                        }
                        this.stopWatch.Reset();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".TickLong\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

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
    }
}