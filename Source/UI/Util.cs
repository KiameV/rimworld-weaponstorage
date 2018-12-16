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
		public string Melee => Pawn.skills.GetSkill(SkillDefOf.Melee).levelInt.ToString();
		public string Ranged => Pawn.skills.GetSkill(SkillDefOf.Shooting).levelInt.ToString();
	}

	public static class Util
	{
		public static IEnumerable<SelectablePawns> GetPawns()
		{
			foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
				if (p.Faction == Faction.OfPlayer && p.def.race.Humanlike)
					yield return new SelectablePawns(p);
		}
	}
}
