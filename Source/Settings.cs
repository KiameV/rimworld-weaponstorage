using UnityEngine;
using Verse;

namespace WeaponStorage
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "WeaponStorage".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }
    }

    public class Settings : ModSettings
    {
        public static bool ShowWeaponsWhenNotDrafted = false;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref ShowWeaponsWhenNotDrafted, "WeaponStorage.ShowWeaponsWhenNotDrafted", false, false);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small);
            l.ColumnWidth = System.Math.Min(400, rect.width / 2);
            l.Begin(rect);
            l.CheckboxLabeled("WeaponStorage.ShowWeaponsWhenNotDrafted".Translate(), ref ShowWeaponsWhenNotDrafted);
            //l.Gap(4);
            l.End();
        }
    }
}
