using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;

namespace WeaponStorage.UI
{
	class SharedWeaponsUI : Window
	{
		private Vector2 scrollPosition = new Vector2(0, 0);
		private IEnumerable<SelectablePawns> pawns;
		private SharedWeaponFilter selectedFilter = null;
		private float lastX = 0, lastY = 0;

		public SharedWeaponsUI()
		{
			this.closeOnClickedOutside = true;
			this.doCloseButton = true;
			this.doCloseX = true;
			this.absorbInputAroundWindow = true;
			this.forcePause = true;

			this.pawns = Util.GetPawns(true);
		}

		public override Vector2 InitialSize => new Vector2(800f, 600f);

		public override void DoWindowContents(Rect inRect)
		{
			if (!Find.WindowStack.CurrentWindowGetsInput)
				return;

			Text.Font = GameFont.Small;

			float outerY = 0;
			if (Widgets.ButtonText(new Rect(0, outerY, 250, 32), ((this.selectedFilter == null) ? "WeaponStorage.SharedWeaponsFilter".Translate().ToString() : this.selectedFilter.Label)))
			{
				if (WorldComp.SharedWeaponFilter.Count > 0)
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					foreach (SharedWeaponFilter f in WorldComp.SharedWeaponFilter)
						options.Add(new FloatMenuOption(f.Label, () => this.selectedFilter = f, MenuOptionPriority.Default, null, null, 0f, null, null));
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}
			if (Widgets.ButtonText(new Rect(275, outerY, 100, 32), "WeaponStorage.New".Translate()))
			{
				SharedWeaponFilter f = new SharedWeaponFilter() { Label = "" };
				WorldComp.SharedWeaponFilter.Add(f);
				Find.WindowStack.Add(new SharedWeaponFilterUI(f));
			}
			if (this.selectedFilter != null &&
				Widgets.ButtonText(new Rect(400, outerY, 100, 32), "WeaponStorage.Edit".Translate()))
			{
				Find.WindowStack.Add(new SharedWeaponFilterUI(this.selectedFilter));
			}
			if (this.selectedFilter != null &&
				Widgets.ButtonText(new Rect(525, outerY, 100, 32), "Delete".Translate()))
			{
				WorldComp.SharedWeaponFilter.Remove(this.selectedFilter);
				this.selectedFilter = null;
			}
			outerY += 60;

			if (WorldComp.SharedWeaponFilter.Count == 0)
				return;

            // Column Headers
            float x = 0f;
			Widgets.Label(new Rect(x, outerY, 100, 30), "MedGroupColonist".Translate());
			x += 120;
			Widgets.DrawTextureFitted(new Rect(x, outerY, 30, 30), AssignUI.meleeTexture, 1f);
			x += 40;
			Widgets.DrawTextureFitted(new Rect(x, outerY, 30, 30), AssignUI.rangedTexture, 1f);
			x += 50;

            float outerX = x;

            Vector2 temp = new Vector2(this.scrollPosition.x, 0);
            Widgets.BeginScrollView(
                new Rect(x, outerY, inRect.width - outerX - 36, 32), ref temp,
                new Rect(0, 0, this.lastX, 32), false);
            x = 0;
            foreach (SharedWeaponFilter f in WorldComp.SharedWeaponFilter)
			{
				Widgets.Label(new Rect(x, 0, 200, 30), f.Label);
				x += 150;
			}
            Widgets.EndScrollView();

			outerY += 32;

            // Row Headers
            temp = new Vector2(0, this.scrollPosition.y);
            Widgets.BeginScrollView(
                new Rect(20, outerY, 200, inRect.height - outerY - 76), ref temp,
                new Rect(0, 0, 100, this.lastY), false);
            float y = 0f;
            foreach (SelectablePawns p in this.pawns)
            {
                x = 0;
                Widgets.Label(new Rect(0, y + 2, 100, 30), p.Pawn.Name.ToStringShort);
                x += 110;
                Widgets.Label(new Rect(x, y + 2, 30, 30), p.Melee);
                x += 40;
                Widgets.Label(new Rect(x, y + 2, 30, 30), p.Ranged);
                x += 50;
                y += 32;
                Widgets.DrawLineHorizontal(0, y, 200);
            }
            Widgets.EndScrollView();

            Widgets.BeginScrollView(
				new Rect(x, outerY, inRect.width - outerX - 20, inRect.height - outerY - 60), ref scrollPosition,
				new Rect(0, 0, this.lastX, this.lastY));
			y = 0;
			foreach (SelectablePawns p in this.pawns)
			{
				x = 0;
				foreach (SharedWeaponFilter f in WorldComp.SharedWeaponFilter)
				{
					bool orig = f.AssignedPawns.Contains(p.Pawn);
					bool b = orig;
					Widgets.Checkbox(new Vector2(x, y + 2), ref b, 26);
					if (b != orig)
					{
						if (b)
							f.AssignedPawns.Add(p.Pawn);
						else
							f.AssignedPawns.Remove(p.Pawn);
					}
					x += 150;
				}
				y += 32;
                Widgets.DrawLineHorizontal(0, y, this.lastX);
            }

			Widgets.EndScrollView();

            this.lastX = x;
            this.lastY = y;
        }
	}
}
