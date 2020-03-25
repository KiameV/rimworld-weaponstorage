using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace WeaponStorage
{
    partial class HarmonyPatches
    {
        [HarmonyPatch(typeof(WealthWatcher), "ForceRecount")]
        static class Patch_WealthWatcher_ForceRecount
        {
            static void Postfix(WealthWatcher __instance)
            {
                Map map = (Map)__instance.GetType().GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                FieldInfo wealthItemsFI = __instance.GetType().GetField("wealthItems", BindingFlags.NonPublic | BindingFlags.Instance);
                float wealthItems = (float)wealthItemsFI.GetValue(__instance);

                wealthItems = TallyWealth(WorldComp.GetWeaponStorages(map), wealthItems);

                wealthItemsFI.SetValue(__instance, wealthItems);
            }

            private static float TallyWealth(IEnumerable<Building_WeaponStorage> storages, float wealthItems)
            {
                foreach (Building_WeaponStorage storage in storages)
                {
                    foreach (ThingWithComps t in storage.GetWeapons(true))
                    {
                        wealthItems += (float)t.stackCount + t.MarketValue;
                    }
                }
                return wealthItems;
            }
        }
    }
}