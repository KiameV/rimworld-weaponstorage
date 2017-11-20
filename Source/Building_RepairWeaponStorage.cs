using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace WeaponStorage
{
    class Building_RepairWeaponStorage : Building
    {
        private const int LOW_POWER_COST = 10;
        //private const int RARE_TICKS_PER_HP = 4;

        private LinkedList<Building_WeaponStorage> AttachedWeaponStorages = new LinkedList<Building_WeaponStorage>();
        public CompPowerTrader compPowerTrader;

        private Pawn AssignedTo = null;
        private ThingWithComps BeingRepaird = null;
        private Map CurrentMap;
        //private int rareTickCount = 0;

        public override string GetInspectString()
        {
            //this.Tick();
            StringBuilder sb = new StringBuilder(base.GetInspectString());
            if (sb.Length > 0)
                sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.AttachedWeaponStorages".Translate());
            sb.Append(": ");
            sb.Append(this.AttachedWeaponStorages.Count);
            sb.Append(Environment.NewLine);
            sb.Append("WeaponStorage.IsRepairing".Translate());
            sb.Append(": ");
            if (BeingRepaird == null)
            {
                sb.Append(Boolean.FalseString);
                if (this.AssignedTo != null)
                {
                    sb.Append(" (");
                    sb.Append(this.AssignedTo.Name.ToStringShort);
                    sb.Append(")");
                }
            }
            else
            {
                sb.Append(BeingRepaird.Label);
            }
            return sb.ToString();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.compPowerTrader = base.GetComp<CompPowerTrader>();
            this.compPowerTrader.PowerOutput = -LOW_POWER_COST;

            this.CurrentMap = map;

            foreach (Building_WeaponStorage ws in 
                BuildingUtil.FindThingsOfTypeNextTo<Building_WeaponStorage>(base.Map, base.Position, Settings.RepairAttachmentDistance))
            {
                this.AddWeaponStorage(ws);
            }

#if DEBUG_REPAIR
            Log.Warning(this.Label + " adding attached WeaponStorages:");
            foreach (Building_WeaponStorage d in this.AttachedWeaponStorages)
            {
                Log.Warning(" " + d.Label);
            }
#endif

            this.compPowerTrader.powerStartedAction = new Action(delegate ()
            {
                this.compPowerTrader.PowerOutput = LOW_POWER_COST;
            });

            this.compPowerTrader.powerStoppedAction = new Action(delegate ()
            {
                this.PlaceWeaponInStorage();
                this.compPowerTrader.PowerOutput = 0;
            });
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            this.PlaceWeaponInStorage();
            this.AttachedWeaponStorages.Clear();
        }

        public override void Discard()
        {
            base.Discard();
            this.PlaceWeaponInStorage();
            this.AttachedWeaponStorages.Clear();
        }

        public override void DeSpawn()
        {
            base.DeSpawn();
            this.PlaceWeaponInStorage();
            this.AttachedWeaponStorages.Clear();
        }

        public override void TickLong()
        {
            if (!this.compPowerTrader.PowerOn)
            {
                // Power is off
                if (BeingRepaird != null)
                {
                    this.PlaceWeaponInStorage();
                }
            }
            else if (
                this.AssignedTo != null && 
                this.AssignedTo.Drafted && 
                this.AssignedTo.equipment.Primary == this.BeingRepaird)
            {
                // The weapon is in use
                this.AssignedTo = null;
                this.BeingRepaird = this.FindWeaponToRepair();
            }
            else if (this.BeingRepaird == null)
            {
                // Power is on and not repairing anything
                this.BeingRepaird = this.FindWeaponToRepair();
            }
            else if (
                this.BeingRepaird != null &&
                this.BeingRepaird.HitPoints == this.BeingRepaird.MaxHitPoints)
            {
                // Power is on
                // Repairing something
                // Weapon is fully repaired
                this.PlaceWeaponInStorage();
                this.BeingRepaird = this.FindWeaponToRepair();
            }

            if (this.BeingRepaird != null)
            {
                this.BeingRepaird.HitPoints += 1;

                float generatedHeat = GenTemperature.ControlTemperatureTempChange(
                    base.Position, base.Map, 10, float.MaxValue);
                this.GetRoomGroup().Temperature += generatedHeat;

                this.compPowerTrader.PowerOutput = -this.compPowerTrader.Props.basePowerConsumption;
            }
            else
            {
                this.compPowerTrader.PowerOutput = LOW_POWER_COST;
            }
        }

        private void OrderAttachedWeaponStorages()
        {
            bool isSorted = true;
            for (LinkedListNode<Building_WeaponStorage> n = this.AttachedWeaponStorages.First; n != null; n = n.Next)
            {
                if (!n.Value.Spawned)
                {
                    this.AttachedWeaponStorages.Remove(n);
                }
                else if (
                    n.Next != null &&
                    n.Value.settings.Priority < n.Next.Value.settings.Priority)
                {
                    isSorted = false;
                }
            }

            if (!isSorted)
            {
                LinkedList<Building_WeaponStorage> ordered = new LinkedList<Building_WeaponStorage>();
                for (LinkedListNode<Building_WeaponStorage> n = this.AttachedWeaponStorages.First; n != null; n = n.Next)
                {
                    Building_WeaponStorage ws = n.Value;
                    bool inserted = false;
                    for (LinkedListNode<Building_WeaponStorage> o = ordered.First; o != null; o = o.Next)
                    {
                        if (ws.settings.Priority > o.Value.settings.Priority)
                        {
                            ordered.AddBefore(o, ws);
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted)
                    {
                        ordered.AddLast(ws);
                    }
                }
                this.AttachedWeaponStorages.Clear();
                this.AttachedWeaponStorages = ordered;

                Log.Warning("WS New Order:");
                foreach(Building_WeaponStorage ws in this.AttachedWeaponStorages)
                {
                    Log.Warning(" " + ws.Label + " " + ws.settings.Priority);
                }
            }
        }

        private ThingWithComps FindWeaponToRepair()
        {
            try
            {
                PawnLookupUtil.Initialize();
                // Try to repair equiped weapons
                foreach (Pawn p in PawnLookupUtil.PlayerPawns)
                {
                    if (!p.Drafted)
                    {
                        ThingWithComps t = p.equipment.Primary;
                        if (t != null && (t.def.IsMeleeWeapon || t.def.IsRangedWeapon))
                        {
                            if (t.HitPoints < t.MaxHitPoints)
                            {
                                this.AssignedTo = p;
                                return t;
                            }
                        }
                    }
                }

                // Try to repair assigned weapons
                foreach (AssignedWeaponContainer c in WorldComp.AssignedWeapons)
                {
                    foreach (ThingWithComps w in c.Weapons)
                    {
                        if (w.HitPoints < w.MaxHitPoints)
                        {
                            if (PawnLookupUtil.TryGetPawn(c.PawnId, out this.AssignedTo))
                            {
                                return w;
                            }
                        }
                    }
                }

                // Find weapons in storage
                this.OrderAttachedWeaponStorages();
                for (LinkedListNode<Building_WeaponStorage> n = this.AttachedWeaponStorages.First; n != null; n = n.Next)
                {
                    Building_WeaponStorage d = n.Value;
                    foreach (ThingWithComps twc in d.StoredWeapons)
                    {
                        if (twc.HitPoints < twc.MaxHitPoints)
                        {
                            d.RemoveNoDrop(twc);
                            return twc;
                        }
                    }
                }
            }
            finally
            {
                PawnLookupUtil.Clear();
            }
            return null;
        }

        private void PlaceWeaponInStorage()
        {
            if (this.BeingRepaird == null)
            {
                return;
            }

            if (this.AssignedTo != null)
            {
                this.BeingRepaird = null;
                this.AssignedTo = null;
                return;
            }

            Building_WeaponStorage WeaponStorageToUse = null;
            this.OrderAttachedWeaponStorages();
            for (LinkedListNode<Building_WeaponStorage> n = this.AttachedWeaponStorages.First; n != null; n = n.Next)
            {
                Building_WeaponStorage d = n.Value;
                if (d.settings.AllowedToAccept(this.BeingRepaird))
                {
                    WeaponStorageToUse = d;
                }
            }

            if (WeaponStorageToUse != null)
            {
                WeaponStorageToUse.AddWeapon(this.BeingRepaird);
            }
            else
            {
                BuildingUtil.DropThing(this.BeingRepaird, this, this.CurrentMap, false);
            }
            this.BeingRepaird = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving && this.AssignedTo != null)
            {
                return;
            }

            Scribe_Deep.Look(ref this.BeingRepaird, "beingRepaired", new object[0]);
        }

        public void AddWeaponStorage(Building_WeaponStorage ws)
        {
            if (this.AttachedWeaponStorages.Contains(ws))
            {
                return;
            }
            this.AttachedWeaponStorages.AddLast(ws);
        }

        public void RemoveWeaponStorage(Building_WeaponStorage ws)
        {
            this.AttachedWeaponStorages.Remove(ws);
        }
    }
}
