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
					this.labelAndStats = GetLabelAndStatsFor(Pawn);
				return this.labelAndStats;
			}
		}
		public static string GetLabelAndStatsFor(Pawn pawn)
		{
			var melee = (pawn.WorkTagIsDisabled(WorkTags.Violent) ? "-" : pawn.skills.GetSkill(SkillDefOf.Melee).levelInt.ToString());
			var ranged = (pawn.WorkTagIsDisabled(WorkTags.Violent) ? "-" : pawn.skills.GetSkill(SkillDefOf.Shooting).levelInt.ToString());
			return $"{pawn.Name.ToStringShort} -- {SkillDefOf.Melee.label}: {melee} -- {SkillDefOf.Shooting.label}: {ranged}";
		}
		public string Melee => ((Pawn.WorkTagIsDisabled(WorkTags.Violent)) ? "-" : Pawn.skills.GetSkill(SkillDefOf.Melee).levelInt.ToString());
		public string Ranged => ((Pawn.WorkTagIsDisabled(WorkTags.Violent)) ? "-" : Pawn.skills.GetSkill(SkillDefOf.Shooting).levelInt.ToString());

        public override bool Equals(object o)
        {
			if (o is SelectablePawns sp)
				return this.Pawn?.thingIDNumber == sp.Pawn?.thingIDNumber;
			if (o is Pawn p)
				return this.Pawn?.thingIDNumber == p.thingIDNumber;
			return false;
        }

        public override int GetHashCode()
        {
			return this.Pawn.GetHashCode() * this.labelAndStats.GetHashCode();
        }
    }

	public static class Util
	{
		public static List<SelectablePawns> GetPawns(bool excludeNonViolent)
		{
			if (!Settings.EnableAssignWeapons)
				return new List<SelectablePawns>(0);

			SortedDictionary<string, List<Pawn>> pawns = new SortedDictionary<string, List<Pawn>>();
			foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
				if (p != null && p.Faction == Faction.OfPlayer && p.def.race.Humanlike && !p.Dead && p.apparel?.LockedApparel?.Count == 0)
                {
					if (!excludeNonViolent || !p.WorkTagIsDisabled(WorkTags.Violent))
					{
						string name = p.Name.ToStringShort;
						if (!pawns.TryGetValue(name, out List<Pawn> ps))
						{
							ps = new List<Pawn>();
							pawns[name] = ps;
						}
						ps.Add(p);
					}
                }

			List<SelectablePawns> result = new List<SelectablePawns>(pawns.Count);
			foreach (List<Pawn> ps in pawns.Values)
				foreach (Pawn p in ps)
					result.Add(new SelectablePawns(p));

			return result;
		}
	}
}
