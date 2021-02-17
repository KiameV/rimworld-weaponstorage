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

        private static LinkedList<ThingWithComps> AllWeaponsBeingRepaired = new LinkedList<ThingWithComps>();
        private LinkedList<Building_WeaponStorage> AttachedWeaponStorages = new LinkedList<Building_WeaponStorage>();
        public CompPowerTrader compPowerTrader;

        private AssignedWeaponContainer container = null;
        private ThingWithComps beingRepaird = null;
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
            if (this.beingRepaird != null)
            {
                sb.Append(beingRepaird.Label);
                if (this.container != null)
                {
                    sb.Append(" (");
                    sb.Append(this.container.Pawn.Name.ToStringShort);
                    sb.Append(")");
                }
                sb.Append(Environment.NewLine);
                sb.Append("    ");
                sb.Append(beingRepaird.HitPoints.ToString());
                sb.Append("/");
                sb.Append(beingRepaird.MaxHitPoints);
            }
            else
            {
                sb.Append(Boolean.FalseString);
            }
            return sb.ToString();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.compPowerTrader = base.GetComp<CompPowerTrader>();
            this.compPowerTrader.PowerOutput = -LOW_POWER_COST;

            this.CurrentMap = map;

            foreach (Building_WeaponStorage s in BuildingUtil.FindThingsOfTypeNextTo<Building_WeaponStorage>(base.Map, base.Position, Settings.RepairAttachmentDistance))
            {
                this.Add(s);
            }

#if DEBUG_REPAIR
            Log.Warning(this.Label + " adding attached dressers:");
            foreach (Building_Dresser d in this.AttachedDressers)
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
                this.StopRepairing();
                this.compPowerTrader.PowerOutput = 0;
            });
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            this.StopRepairing();
            this.AttachedWeaponStorages.Clear();
        }

        public override void Discard(bool silentlyRemoveReferences = false)
        {
            base.Discard(silentlyRemoveReferences);
            this.StopRepairing();
            this.AttachedWeaponStorages.Clear();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            this.StopRepairing();
            this.AttachedWeaponStorages.Clear();
        }

        // Normal Tick: 1 / 60 seconds
        // Rare Tick: 4 seconds (.6 on fast)
        // Long Tick: 30 seconds (2.2 on fast)

        private const long THIRTY_SECONDS = 30 * TimeSpan.TicksPerSecond;
        long lastTick = DateTime.Now.Ticks;
        long lastSearch = DateTime.Now.Ticks;
        public override void Tick()
        {
            base.TickRare();
            long now = DateTime.Now.Ticks;
            if (now - lastTick > Settings.RepairAttachmentUpdateIntervalTicks)
            {
                //Log.Warning("Tick: [" + (int)((now - lastTick) / TimeSpan.TicksPerMillisecond) + "] milliseconds");
                lastTick = DateTime.Now.Ticks;
                if (!this.compPowerTrader.PowerOn)
                {
                    // Power is off
                    if (beingRepaird != null)
                    {
                        this.StopRepairing();
                    }
                }
                else if (this.beingRepaird == null)
                {
                    // Power is on and not repairing anything
                    if (now - lastSearch > THIRTY_SECONDS)
                    {
                        lastSearch = now;
                        this.StartRepairing();
                    }
                }
                else if (
                    this.beingRepaird != null &&
                    this.beingRepaird.HitPoints >= this.beingRepaird.MaxHitPoints)
                {
                    // Power is on
                    // Repairing something
                    // Apparel is fully repaired
                    this.beingRepaird.HitPoints = this.beingRepaird.MaxHitPoints;
                    this.StopRepairing();
                    lastSearch = now;
                    this.StartRepairing();
                }

                if (this.beingRepaird != null)
                {
                    this.beingRepaird.HitPoints += Settings.RepairAttachmentMendingSpeed;
                    if (this.beingRepaird.HitPoints > this.beingRepaird.MaxHitPoints)
                    {
                        this.beingRepaird.HitPoints = this.beingRepaird.MaxHitPoints;
                    }

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
        }

        private void OrderAttachedWeaponStorages()
        {
            bool isSorted = true;
            LinkedListNode<Building_WeaponStorage> n = this.AttachedWeaponStorages.First;
            while (n != null)
            {
                var next = n.Next;
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
                n = next;
            }

            if (!isSorted)
            {
                LinkedList<Building_WeaponStorage> ordered = new LinkedList<Building_WeaponStorage>();
                for (n = this.AttachedWeaponStorages.First; n != null; n = n.Next)
                {
                    Building_WeaponStorage s = n.Value;
                    bool inserted = false;
                    for (LinkedListNode<Building_WeaponStorage> o = ordered.First; o != null; o = o.Next)
                    {
                        if (s.settings.Priority > o.Value.settings.Priority)
                        {
                            ordered.AddBefore(o, s);
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted)
                    {
                        ordered.AddLast(s);
                    }
                }
                this.AttachedWeaponStorages.Clear();
                this.AttachedWeaponStorages = ordered;
            }
        }

        private void StartRepairing()
        {
#if AUTO_MENDER
            Log.Warning("Begin RepairChangeDresser.StartRepairing");
            Log.Message("    Currently Being Repaired:");
            foreach(Apparel a in AllApparelBeingRepaired)
            {
                Log.Message("        " + a.Label);
            }
#endif
            this.OrderAttachedWeaponStorages();
            foreach (AssignedWeaponContainer c in WorldComp.AssignedWeaponContainers)
            {
                foreach (ThingWithComps w in c.Weapons)
                {
                    if (w.HitPoints < w.MaxHitPoints &&
                        !AllWeaponsBeingRepaired.Contains(w))
                    {
                        this.beingRepaird = w;
                        this.container = c;
                        AllWeaponsBeingRepaired.AddLast(w);
#if AUTO_MENDER
                        Log.Warning("End RepairChangeDresser.StartRepairing -- " + a.Label);
#endif
                        return;
                    }
                }
            }
            for (LinkedListNode<Building_WeaponStorage> n = this.AttachedWeaponStorages.First; n != null; n = n.Next)
            {
                Building_WeaponStorage ws = n.Value;
                foreach (ThingWithComps w in ws.GetWeapons(true))
                {
                    if (w.HitPoints < w.MaxHitPoints &&
                        !AllWeaponsBeingRepaired.Contains(w))
                    {
                        this.beingRepaird = w;
                        this.container = null;
                        AllWeaponsBeingRepaired.AddLast(w);
#if AUTO_MENDER
                        Log.Warning("End RepairChangeDresser.StartRepairing -- " + a.Label);
#endif
                        return;
                    }
                }
            }
#if AUTO_MENDER
            Log.Warning("End RepairChangeDresser.StartRepairing -- No new repairs to start");
#endif
        }

        private void StopRepairing()
        {
            if (this.beingRepaird != null)
            {
                AllWeaponsBeingRepaired.Remove(this.beingRepaird);
                this.beingRepaird = null;
                this.container = null;
            }
        }

        public void Add(Building_WeaponStorage s)
        {
            if (!this.AttachedWeaponStorages.Contains(s))
            {
                this.AttachedWeaponStorages.AddLast(s);
            }
        }

        public void Remove(Building_WeaponStorage s)
        {
            this.AttachedWeaponStorages.Remove(s);
        }
    }
}
