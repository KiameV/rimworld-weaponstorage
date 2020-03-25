using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using WeaponStorage;

namespace MendingWeaponStoragePatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches_Mending
    {
        static HarmonyPatches_Mending()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(m => "MendAndRecycle".Equals(m.Name)))
            {
                try
                {
                    var harmony = new Harmony("com.mendingweaponstoragepatch.rimworld.mod");
                    harmony.PatchAll(Assembly.GetExecutingAssembly());

                    Log.Message(
                        "MendingWeaponStoragePatch Harmony Patches:" + Environment.NewLine +
                        "  Postfix:" + Environment.NewLine +
                        "    WorkGiver_DoBill.TryFindBestBillIngredients - Priority Last");
                }
                catch(Exception e)
                {
                    Log.Error("Failed to patch Mending & Recycling." + Environment.NewLine + e.Message);
                }
            }
            else
            {
                Log.Message("MendingWeaponStoragePatch did not find MendAndRecycle. Will not load patch.");
            }
        }
    }

    [HarmonyPriority(Priority.Last)]
	[HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients")]
	static class Patch_WorkGiver_DoBill_TryFindBestBillIngredients
	{
		static void Postfix(ref bool __result, WorkGiver_DoBill __instance, Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen)
		{
			if (__result == false &&
				pawn != null && bill != null && bill.recipe != null &&
                bill.Map == pawn.Map &&
                bill.recipe.defName.IndexOf("Weapon") != -1)
            {
                IEnumerable<Building_WeaponStorage> storages = WorldComp.GetWeaponStorages(bill.Map);
                if (storages == null)
                {
                    Log.Message("MendingWeaponStoragePatch failed to retrieve WeaponStorages");
                    return;
                }

                foreach (Building_WeaponStorage ws in storages)
                {
                    if ((float)(ws.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius)
                    {
						foreach (KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv in ws.StoredWeapons)
						{
                            FindMatches(ref __result, bill, chosen, ws, kv);
                        }
                        foreach (KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv in ws.StoredBioEncodedWeapons)
                        {
                            FindMatches(ref __result, bill, chosen, ws, kv);
                        }
                    }
                }
            }
        }

        private static void FindMatches(ref bool __result, Bill bill, List<ThingCount> chosen, Building_WeaponStorage ws, KeyValuePair<ThingDef, LinkedList<ThingWithComps>> kv)
        {
            if (bill.ingredientFilter.Allows(kv.Key))
            {
                foreach (ThingWithComps t in kv.Value)
                {
                    if (bill.ingredientFilter.Allows(t) &&
                        t.HitPoints != t.MaxHitPoints)
                    {
                        ws.Remove(t);
                        if (t.Spawned == false)
                        {
                            Log.Error("Failed to spawn weapon-to-mend [" + t.Label + "] from weapon storage [" + ws.Label + "].");
                            __result = false;
                        }
                        else
                        {
                            __result = true;
                            chosen.Add(new ThingCount(t, 1));
                        }
                        return;
                    }
                }
            }
        }
    }
}