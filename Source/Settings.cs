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

    public enum PreferredDamageTypeEnum
    {
        WeaponStorage_None,
        ArmorBlunt,
        ArmorSharp,
    }

    public class Settings : ModSettings
    {
        private const float FIRST_COLUMN_WIDTH = 250f;
        private const float SECOND_COLUMN_X = FIRST_COLUMN_WIDTH + 10f;
        private const int DEFAULT_REPAIR_SPEED = 1;
        private const float DEFAULT_REPAIR_UPDATE_INTERVAL = 5f;

        //private static ToolDefsLookup ToolDefs = new ToolDefsLookup();

        public static bool ShowWeaponsWhenNotDrafted = false;
        public static bool AutoSwitchMelee = true;
        public static int RepairAttachmentDistance = 6;
        public static PreferredDamageTypeEnum PreferredDamageType = PreferredDamageTypeEnum.ArmorSharp;
        public static int RepairAttachmentMendingSpeed = DEFAULT_REPAIR_SPEED;
        private static string RepairAttachmentMendingSpeedBuffer = DEFAULT_REPAIR_SPEED.ToString();
        public static float RepairAttachmentUpdateInterval = DEFAULT_REPAIR_UPDATE_INTERVAL;
        private static string repairAttachmentUpdateIntervalBuffer = DEFAULT_REPAIR_UPDATE_INTERVAL.ToString();
        public static bool AllowPawnsToDropWeapon = true;
        public static bool PlaceDroppedWeaponsInStorage = true;
        public static bool ShowWeaponStorageButtonForPawns = true;


        public static long RepairAttachmentUpdateIntervalTicks { get { return (long)(RepairAttachmentUpdateInterval * TimeSpan.TicksPerSecond); } }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref ShowWeaponsWhenNotDrafted, "WeaponStorage.ShowWeaponsWhenNotDrafted", false, false);
            Scribe_Values.Look<int>(ref RepairAttachmentMendingSpeed, "WeaponStorage.RepairAttachmentHpPerTick", DEFAULT_REPAIR_SPEED, false);
            RepairAttachmentMendingSpeedBuffer = RepairAttachmentMendingSpeed.ToString();
            Scribe_Values.Look<float>(ref RepairAttachmentUpdateInterval, "WeaponStorage.RepairAttachmentUpdateInterval", DEFAULT_REPAIR_UPDATE_INTERVAL, false);
            repairAttachmentUpdateIntervalBuffer = string.Format("{0:0.0###}", RepairAttachmentUpdateInterval);
            Scribe_Values.Look<bool>(ref AutoSwitchMelee, "WeaponStorage.AutoSwitchMelee", true, false);
            Scribe_Values.Look(ref PreferredDamageType, "WeaponStorage.PreferredDamageType", PreferredDamageTypeEnum.ArmorSharp, false);
            Scribe_Values.Look(ref AllowPawnsToDropWeapon, "WeaponStorage.AllowPawnsToDropWeapon", true, false);
            Scribe_Values.Look(ref PlaceDroppedWeaponsInStorage, "WeaponStorage.PlaceDroppedWeaponsInStorage", true, false);
            Scribe_Values.Look(ref ShowWeaponStorageButtonForPawns, "WeaponStorage.ShowButtonForPawns", true, false);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            float y = 40;
            Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.ShowButtonForPawns".Translate());
            Widgets.Checkbox(new Vector2(SECOND_COLUMN_X, y + 4), ref ShowWeaponStorageButtonForPawns);
            y += 32;
            Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.ShowWeaponsWhenNotDrafted".Translate());
            Widgets.Checkbox(new Vector2(SECOND_COLUMN_X, y + 4), ref ShowWeaponsWhenNotDrafted);
            y += 32;

            y += 20;
            Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.RepairAttachmentSettings".Translate());
            y += 32;

            NumberInput(ref y, "WeaponStorage.SecondsBetweenTicks",
                ref RepairAttachmentUpdateInterval, ref repairAttachmentUpdateIntervalBuffer,
                DEFAULT_REPAIR_UPDATE_INTERVAL, 0.25f, 120f);

            NumberInput(ref y, "WeaponStorage.HPPerTick",
                ref RepairAttachmentMendingSpeed, ref RepairAttachmentMendingSpeedBuffer,
                DEFAULT_REPAIR_SPEED, 1, 60);
            
            y += 20;
            Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.AllowPawnsToDropWeapon".Translate());
            Widgets.Checkbox(new Vector2(SECOND_COLUMN_X, y + 4), ref AllowPawnsToDropWeapon);
            y += 32;
            Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.PlaceDroppedWeaponsInStorage".Translate());
            Widgets.Checkbox(new Vector2(SECOND_COLUMN_X, y + 4), ref PlaceDroppedWeaponsInStorage);
            y += 32;
            Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.AutoSwitchMeleeForTarget".Translate());
            Widgets.Checkbox(new Vector2(SECOND_COLUMN_X, y + 4), ref AutoSwitchMelee);
            y += 32;
            if (AutoSwitchMelee)
            {
                Widgets.Label(new Rect(0, y, FIRST_COLUMN_WIDTH, 30), "WeaponStorage.PreferredDamageType".Translate());
                if (Widgets.ButtonText(new Rect(SECOND_COLUMN_X, y, 100, 30), PreferredDamageType.ToString().Translate()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    if (PreferredDamageType != PreferredDamageTypeEnum.WeaponStorage_None)
                        list.Add(new FloatMenuOption(PreferredDamageTypeEnum.WeaponStorage_None.ToString().Translate(), delegate ()
                        {
                            PreferredDamageType = PreferredDamageTypeEnum.WeaponStorage_None;
                        }));
                    if (PreferredDamageType != PreferredDamageTypeEnum.ArmorBlunt)
                        list.Add(new FloatMenuOption(PreferredDamageTypeEnum.ArmorBlunt.ToString().Translate(), delegate ()
                        {
                            PreferredDamageType = PreferredDamageTypeEnum.ArmorBlunt;
                        }));
                    if (PreferredDamageType != PreferredDamageTypeEnum.ArmorSharp)
                        list.Add(new FloatMenuOption(PreferredDamageTypeEnum.ArmorSharp.ToString().Translate(), delegate ()
                        {
                            PreferredDamageType = PreferredDamageTypeEnum.ArmorSharp;
                        }));
                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }
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

        private static void NumberInput(ref float y, string label, ref float val, ref string buffer, float defaultVal, float min, float max)
        {
            try
            {
                Widgets.Label(new Rect(20, y, FIRST_COLUMN_WIDTH, 30), label.Translate());
                Widgets.TextFieldNumeric<float>(new Rect(20 + SECOND_COLUMN_X, y, 50, 30), ref val, ref buffer, min, max);
                if (Widgets.ButtonText(new Rect(80 + SECOND_COLUMN_X, y, 100, 30), "ResetButton".Translate()))
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
            y += 32;
        }

        private static void NumberInput(ref float y, string label, ref int val, ref string buffer, int defaultVal, int min, int max)
        {
            try
            {
                Widgets.Label(new Rect(20, y, FIRST_COLUMN_WIDTH, 30), label.Translate());
                Widgets.TextFieldNumeric<int>(new Rect(20 + SECOND_COLUMN_X, y, 50, 30), ref val, ref buffer, min, max);
                if (Widgets.ButtonText(new Rect(80 + SECOND_COLUMN_X, y, 100, 30), "ResetButton".Translate()))
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
            y += 32;
        }
    }
}
