using System;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    public class AssignedWeaponContainer : IExposable
    {
        public HashSet<int> weaponIds = new HashSet<int>();
        public List<ThingWithComps> weapons = new List<ThingWithComps>();
        public Pawn Pawn;
        private ThingWithComps LastWeaponUsed = null;
        private ThingWithComps LastToolUsed = null;

        private List<AssignedWeapon> tmp;

        public IEnumerable<ThingWithComps> Weapons => this.weapons;
        public int Count => this.weaponIds.Count;

        public void ExposeData()
        {
#if ASSIGNED_WEAPONS
            Log.Warning("AssignedWeaponContainer.ExposeData: " + Scribe.mode);
#endif
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.tmp = new List<AssignedWeapon>(this.weapons.Count);
                ThingWithComps primary = this.Pawn.equipment.Primary;
#if ASSIGNED_WEAPONS
                Log.Message("    Primary: " + ((primary == null) ? "<null>" : primary.Label));
                Log.Message("    Creating tmp:");
#endif
                foreach (ThingWithComps w in this.Weapons)
                {
                    AssignedWeapon aw = new AssignedWeapon
                    {
                        IsEquipped = primary == w,
                        Weapon = w
                    };
#if ASSIGNED_WEAPONS
                    Log.Message("        " + ((aw.Weapon == null) ? "<null>" : aw.Weapon.Label) + "    IsEquipped: " + aw.IsEquipped);
#endif
                    tmp.Add(aw);
                }
            }

            Scribe_References.Look(ref this.Pawn, "pawn");
            Scribe_Collections.Look(ref this.tmp, "weapons", LookMode.Deep, new object[0]);
            Scribe_References.Look(ref this.LastWeaponUsed, "lastWeaponUsed", false);
            Scribe_References.Look(ref this.LastToolUsed, "lastToolUsed", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.weaponIds == null)
                    this.weaponIds = new HashSet<int>();

                this.weapons.Clear();
                this.weaponIds.Clear();
                foreach (AssignedWeapon aw in this.tmp)
                {
                    if (aw.Weapon == null)
                    {
                        Log.Error($"failed to load weapon assigned to {Pawn.Name.ToStringShort}");
                    }
                    else
                    {
                        this.weapons.Add(aw.Weapon);
                        this.weaponIds.Add(aw.Weapon.thingIDNumber);
                    }
                }

                if (Scribe.mode == LoadSaveMode.PostLoadInit && Pawn != null)
                {
                    foreach (var w in weapons)
                    {
                        foreach (Verb v in w.GetComp<CompEquippable>()?.AllVerbs)
                        {
                            v.caster = this.Pawn;
                        }
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.tmp != null)
                {
                    this.tmp.Clear();
                    this.tmp = null;
                }
            }
#if ASSIGNED_WEAPONS
            Log.Warning("End AssignedWeaponContainer.ExposeData: " + Scribe.mode);
#endif
        }

        public void Add(ThingWithComps weapon)
        {
            if (!this.Contains(weapon))
            {
                this.weapons.Add(weapon);
                this.weaponIds.Add(weapon.thingIDNumber);
            }
        }

        public bool Contains(ThingWithComps weapon)
        {
            return weapon != null && this.weaponIds.Contains(weapon.thingIDNumber);
        }

        public void Clear()
        {
            this.weaponIds.Clear();
            this.weapons.Clear();
        }

        public bool TryGetLastThingUsed(Pawn pawn, out ThingWithComps t)
        {
#if LAST_THING_USED
            Log.Warning("Begin AssignedWeaponContainer.TryGetLastThingUsed " + pawn.Name.ToStringShort);
#endif
            bool result = false;
            if (pawn.Drafted)
            {
                t = this.LastWeaponUsed;
            }
            else
            {
                t = this.LastToolUsed;
            }

            if (t != null)
            {
                if (this.weaponIds.Contains(t.thingIDNumber))
                {
                    result = true;
                }
                else
                {
                    this.SetLastThingUsed(pawn, null, false);
                }
            }
            if (!result)
                t = null;
            
#if LAST_THING_USED
            Log.Message("    Last Tool Used: " + ((this.LastToolUsed == null) ? "<null>" : this.LastToolUsed.Label));
            Log.Message("    Last Weapon Used: " + ((this.LastWeaponUsed == null) ? "<null>" : this.LastWeaponUsed.Label));
            Log.Warning("End AssignedWeaponContainer.TryGetLastThingUsed -- " + result + " " + ((t == null) ? "<null>" : t.Label));
#endif
            return result;
        }

        public void SetLastThingUsed(Pawn pawn, ThingWithComps t, bool isForceMelee)
        {
#if LAST_THING_USED
            Log.Warning("Begin AssignedWeaponContainer.SetLastThingUsed " + pawn.Name.ToStringShort + " " + t.Label);
#endif
            if (pawn.Drafted || !Settings.IsTool(t))
            {
                this.LastWeaponUsed = t;
            }
            
            if (!pawn.Drafted)
            {
                this.LastToolUsed = t;
            }
#if LAST_THING_USED
            Log.Message("    Last Tool Used: " + ((this.LastToolUsed == null) ? "<null>" : this.LastToolUsed.Label));
            Log.Message("    Last Weapon Used: " + ((this.LastWeaponUsed == null) ? "<null>" : this.LastWeaponUsed.Label));
            Log.Warning("End AssignedWeaponContainer.SetLastThingUsed");
#endif
        }

        public bool Remove(ThingWithComps weapon)
        {
#if DEBUG
            Log.Warning(this.GetType().Name + ".Remove(" + weapon.Label + ")");
#endif
            if (this.LastToolUsed == weapon)
            {
                this.LastToolUsed = null;
            }
            if (this.LastWeaponUsed == weapon)
            {
                this.LastWeaponUsed = null;
            }
            this.weaponIds.Remove(weapon.thingIDNumber);
            return this.weapons.Remove(weapon);
        }
        
        private class AssignedWeapon : IExposable
        {
            public bool IsEquipped = false;
            public ThingWithComps Weapon = null;
            public void ExposeData()
            {
                try
                {
                    Scribe_Values.Look(ref IsEquipped, "isEquipped", false);
                    if (IsEquipped)
                    {
#if ASSIGNED_WEAPONS
                    Log.Warning("AssignedWeapon.Expose: " + ((this.Weapon == null) ? "<null>" : Weapon.Label) + " as Reference");
#endif
                        Scribe_References.Look(ref Weapon, "weapon");
                    }
                    else
                    {
#if ASSIGNED_WEAPONS
                    Log.Warning("AssignedWeapon.Expose: " + ((this.Weapon == null) ? "<null>" : Weapon.Label) + " as Deep");
#endif
                        Scribe_Deep.Look(ref Weapon, "weapon", null);
                    }
                }
                catch
                {
                    Weapon = null;
                    IsEquipped = false;
                }
            }
        }
    }
}