using RimWorld;
using System.Collections.Generic;
using Verse;
using System;

namespace WeaponStorage
{
	public class SharedWeaponFilter : IExposable
	{
		public string Label;
		public HashSet<ThingDef> AllowedDefs = new HashSet<ThingDef>();
		public FloatRange HpRange = new FloatRange(0f, 1f);
		public QualityRange QualityRange = QualityRange.All;
		public HashSet<Pawn> AssignedPawns = new HashSet<Pawn>();

		// Not saved
		private long lastCacheUpdate = 0;
		private HashSet<ThingDef> foundDefCache = new HashSet<ThingDef>();
		
		public void ExposeData()
		{
			Scribe_Values.Look(ref Label, "label", "", false);
			Scribe_Collections.Look(ref AllowedDefs, "allowedDefs", LookMode.Def);
			Scribe_Values.Look(ref this.HpRange, "hpRange", default(FloatRange), false);
			Scribe_Values.Look(ref this.QualityRange, "qalityRange", default(QualityRange), false);
			Scribe_Collections.Look(ref AssignedPawns, false, "assignedPawns", LookMode.Reference);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
				if (AssignedPawns == null)
					AssignedPawns = new HashSet<Pawn>();
		}

		public bool Allows(ThingWithComps t)
		{
			if (t.TryGetQuality(out QualityCategory qc))
			{
				return
					qc >= this.QualityRange.min && qc <= this.QualityRange.max &&
					t.HitPoints >= this.HpRange.min * t.def.BaseMaxHitPoints;
			}
			return
				t.HitPoints >= this.HpRange.min * t.def.BaseMaxHitPoints;
		}

		public bool FoundDefCacheContains(ThingDef d)
		{
			return this.foundDefCache.Contains(d);
		}

		public void UpdateFoundDefCache()
		{
			long now = DateTime.Now.Ticks;
			if (now - this.lastCacheUpdate > TimeSpan.TicksPerSecond)
			{
				this.lastCacheUpdate = now;

				this.foundDefCache.Clear();
				foreach (ThingDef def in this.AllowedDefs)
                {
                    if (CombatExtendedUtil.GetAmmoCount(def) > 0)
                    {
                        this.foundDefCache.Add(def);
                        continue;
                    }
                    foreach (Building_WeaponStorage s in WorldComp.WeaponStoragesToUse)
					{
						if (s.HasWeapon(this, def))
						{
							this.foundDefCache.Add(def);
							break;
						}
					}
				}
			}
		}

        public void UpdateDefCache(ThingDef def)
        {
            if (this.AllowedDefs.Contains(def))
            {
                if (CombatExtendedUtil.GetAmmoCount(def) > 0)
                {
                    this.foundDefCache.Add(def);
                    return;
                }
                foreach (Building_WeaponStorage s in WorldComp.WeaponStoragesToUse)
                {
                    if (s.HasWeapon(this, def))
                    {
                        this.foundDefCache.Add(def);
                        return;
                    }
                }
            }
            this.foundDefCache.Remove(def);
        }
    }
}
