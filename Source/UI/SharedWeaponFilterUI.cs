using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;

namespace WeaponStorage.UI
{
	class SharedWeaponFilterUI : Window
	{
		private Vector2 scrollPosition = new Vector2(0, 0);
		private SharedWeaponFilter filter;
		public override Vector2 InitialSize => new Vector2(300f, 600f);
		private float y = 0;

		private SortedDictionary<string, ThingDef> meleeWeapons = new SortedDictionary<string, ThingDef>();
		private SortedDictionary<string, ThingDef> rangedWeapons = new SortedDictionary<string, ThingDef>();

		public SharedWeaponFilterUI(SharedWeaponFilter filter)
		{
			this.filter = filter;
			this.closeOnClickedOutside = true;
			this.doCloseButton = true;
			this.doCloseX = true;
			this.absorbInputAroundWindow = true;
			this.forcePause = true;

			foreach (ThingDef d in DefDatabase<ThingDef>.AllDefs)
			{
				if (d.IsRangedWeapon)
					this.rangedWeapons.Add(d.label, d);
				else if(d.IsMeleeWeapon)
					this.meleeWeapons.Add(d.label, d);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			float outerY = 3;
			Widgets.Label(new Rect(0, outerY, 70, 32), "WeaponStorage.Name".Translate());
			this.filter.Label = Widgets.TextArea(new Rect(80, outerY - 3, inRect.width - 100, 32), this.filter.Label, false);

			outerY += 36;
			this.DrawHitPointsFilterConfig(0, ref outerY, inRect.width, this.filter);
			this.DrawQualityFilterConfig(0, ref outerY, inRect.width, this.filter);

			Widgets.Label(new Rect(0, outerY, 200, 30), "WeaponStorage.AllowedWeapons".Translate());
			outerY += 32;

			Widgets.BeginScrollView(
				new Rect(10, outerY, inRect.width - 10, inRect.height - 40 - outerY), ref this.scrollPosition,
				new Rect(0, 0, inRect.width - 26, this.y));
			this.y = 0;

			foreach (ThingDef d in this.meleeWeapons.Values)
				this.DrawThingDefCheckbox(d);
			foreach (ThingDef d in this.rangedWeapons.Values)
				this.DrawThingDefCheckbox(d);

			Widgets.EndScrollView();
		}

		private void DrawThingDefCheckbox(ThingDef d)
		{
			Widgets.Label(new Rect(0, this.y, 190, 30), d.label);
			bool orig = this.filter.AllowedDefs.Contains(d);
			bool b = orig;
			Widgets.Checkbox(new Vector2(200, this.y), ref b, 28f);
			if (orig != b)
			{
				if (b)
					this.filter.AllowedDefs.Add(d);
				else
					this.filter.AllowedDefs.Remove(d);
			}
			y += 32;
		}

		private void DrawHitPointsFilterConfig(float x, ref float y, float width, SharedWeaponFilter filter)
		{
			Rect rect = new Rect(x + 20f, y, width - 40f, 28f);
			FloatRange allowedHitPointsPercents = filter.HpRange;
			Widgets.FloatRange(rect, 1, ref allowedHitPointsPercents, 0f, 1f, "HitPoints", ToStringStyle.PercentZero);
			filter.HpRange = allowedHitPointsPercents;
			y += 28f;
			y += 5f;
			Text.Font = GameFont.Small;
		}

		private void DrawQualityFilterConfig(float x, ref float y, float width, SharedWeaponFilter filter)
		{
			Rect rect = new Rect(x + 20f, y, width - 40f, 28f);
			QualityRange allowedQualityLevels = filter.QualityRange;
			Widgets.QualityRange(rect, "WeaponStorageQualityRange".GetHashCode(), ref allowedQualityLevels);
			filter.QualityRange = allowedQualityLevels;
			y += 28f;
			y += 5f;
			Text.Font = GameFont.Small;
		}
	}
}