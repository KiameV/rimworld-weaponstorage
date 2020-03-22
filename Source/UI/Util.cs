using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage.UI
{
	public struct SelectablePawns
	{
		public Pawn Pawn;
		private string labelAndStats;
		public SelectablePawns(Pawn pawn)
		{
			this.Pawn = pawn;
			this.labelAndStats = null;
		}
		public string LabelAndStats
		{
			get
			{
				if (this.labelAndStats == null)
					this.labelAndStats = Pawn.Name.ToStringShort + "-- " + SkillDefOf.Melee.label + ": " + Melee + " -- " + SkillDefOf.Shooting.label + ": " + Ranged;
				return this.labelAndStats;
			}
		}
		public string Melee => ((Pawn.WorkTagIsDisabled(WorkTags.Violent)) ? "-" : Pawn.skills.GetSkill(SkillDefOf.Melee).levelInt.ToString());
		public string Ranged => ((Pawn.WorkTagIsDisabled(WorkTags.Violent)) ? "-" : Pawn.skills.GetSkill(SkillDefOf.Shooting).levelInt.ToString());
	}

	public static class Util
	{
		public static IEnumerable<SelectablePawns> GetPawns(bool excludeNonViolent)
		{
			foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
				if (p != null && p.Faction == Faction.OfPlayer && p.def.race.Humanlike && !p.Dead && p.apparel.LockedApparel?.Count == 0)
                {
                    if (!excludeNonViolent || !p.WorkTagIsDisabled(WorkTags.Violent))
                        yield return new SelectablePawns(p);
                }
		}
	}
}
