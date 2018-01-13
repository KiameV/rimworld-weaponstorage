using System;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    public class AssignedWeaponContainer : IExposable
    {
        public string PawnId = "";
        public ThingWithComps LastWeaponUsed = null;
        public ThingWithComps LastToolUsed = null;

        private LinkedList<ThingWithComps> weapons = new LinkedList<ThingWithComps>();
        public IEnumerable<ThingWithComps> Weapons
        {
            get { return this.weapons; }
            set
            {
                this.weapons.Clear();
                foreach (ThingWithComps t in value)
                    this.Add(t);
            }
        }
        public int Count { get { return this.weapons.Count; } }

        private List<ThingWithComps> tmp = null;
        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.tmp = new List<ThingWithComps>(this.weapons);
            }

            Scribe_Values.Look(ref this.PawnId, "pawn", "", true);
            Scribe_Collections.Look(ref this.tmp, "weapons", LookMode.Deep, new object[0]);
            Scribe_References.Look(ref this.LastWeaponUsed, "weaponUsedBeforeDowned", false);
            Scribe_References.Look(ref this.LastToolUsed, "lastToolUsed", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.tmp.Clear();
                this.tmp = null;
            }

            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.weapons.Clear();
                this.Weapons = this.tmp;
            }
        }

        public void Add(ThingWithComps weapon)
        {
            bool isTool = Settings.IsTool(weapon);
            if (isTool)
            {
                this.weapons.AddLast(weapon);
            }
            else
            {
                this.weapons.AddFirst(weapon);
            }
        }

        public void Clear()
        {
            this.weapons.Clear();
        }

        public bool Remove(ThingWithComps weapon)
        {
            return this.weapons.Remove(weapon);
        }
    }
}
