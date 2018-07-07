using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
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

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

    public class Settings : ModSettings
    {
        private const int DEFAULT_REPAIR_SPEED = 1;
        private const float DEFAULT_REPAIR_UPDATE_INTERVAL = 5f;

        //private static ToolDefsLookup ToolDefs = new ToolDefsLookup();

        public static bool ShowWeaponsWhenNotDrafted = false;
        public static int RepairAttachmentDistance = 6;
        public static int RepairAttachmentMendingSpeed = DEFAULT_REPAIR_SPEED;
        private static string RepairAttachmentMendingSpeedBuffer = DEFAULT_REPAIR_SPEED.ToString();
        public static float RepairAttachmentUpdateInterval = DEFAULT_REPAIR_UPDATE_INTERVAL;
        private static string repairAttachmentUpdateIntervalBuffer = DEFAULT_REPAIR_UPDATE_INTERVAL.ToString();


        public static long RepairAttachmentUpdateIntervalTicks { get { return (long)(RepairAttachmentUpdateInterval * TimeSpan.TicksPerSecond); } }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref ShowWeaponsWhenNotDrafted, "WeaponStorage.ShowWeaponsWhenNotDrafted", false, false);
            Scribe_Values.Look<int>(ref RepairAttachmentMendingSpeed, "WeaponStorage.RepairAttachmentHpPerTick", DEFAULT_REPAIR_SPEED, false);
            RepairAttachmentMendingSpeedBuffer = RepairAttachmentMendingSpeed.ToString();
            Scribe_Values.Look<float>(ref RepairAttachmentUpdateInterval, "WeaponStorage.RepairAttachmentUpdateInterval", DEFAULT_REPAIR_UPDATE_INTERVAL, false);
            repairAttachmentUpdateIntervalBuffer = string.Format("{0:0.0###}", RepairAttachmentUpdateInterval);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small);
            l.ColumnWidth = Math.Min(400, rect.width / 2);
            l.Begin(rect);
            l.CheckboxLabeled("WeaponStorage.ShowWeaponsWhenNotDrafted".Translate(), ref ShowWeaponsWhenNotDrafted);
            l.Gap(10);

            l.Label("WeaponStorage.RepairAttachmentSettings".Translate());
            l.Gap(4);
            NumberInput(l, "WeaponStorage.SecondsBetweenTicks",
                ref RepairAttachmentUpdateInterval, ref repairAttachmentUpdateIntervalBuffer,
                DEFAULT_REPAIR_UPDATE_INTERVAL, 0.25f, 120f);
            l.Gap(4);

            NumberInput(l, "WeaponStorage.HPPerTick",
                ref RepairAttachmentMendingSpeed, ref RepairAttachmentMendingSpeedBuffer,
                DEFAULT_REPAIR_SPEED, 1, 60);
            l.End();
        }
        
        public static bool IsTool(Thing t)
        {
            if (t != null)
            {
                return IsTool(t.def);
            }
            return false;
        }

        public static bool IsTool(ThingDef def)
        {
            if (def != null)
            {
                return def.defName.StartsWith("RTFTJ");
                //return ToolDefs.IsTool(def);
            }
            return false;
        }

        private static void NumberInput(Listing_Standard l, string label, ref float val, ref string buffer, float defaultVal, float min, float max)
        {
            try
            {
                l.TextFieldNumericLabeled<float>(label.Translate(), ref val, ref buffer, min, max);
                if (l.ButtonText("ResetButton".Translate()))
                {
                    val = defaultVal;
                    buffer = string.Format("{0:0.0###}", defaultVal);
                }
            }
            catch
            {
                val = min;
                buffer = string.Format("{0:0.0###}", min);
            }
        }

        private static void NumberInput(Listing_Standard l, string label, ref int val, ref string buffer, int defaultVal, int min, int max)
        {
            try
            {
                l.TextFieldNumericLabeled<int>(label.Translate(), ref val, ref buffer, min, max);
                if (l.ButtonText("ResetButton".Translate()))
                {
                    val = defaultVal;
                    buffer = defaultVal.ToString();
                }
            }
            catch
            {
                val = min;
                buffer = min.ToString();
            }
        }
    }
}
