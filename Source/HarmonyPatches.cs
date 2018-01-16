using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace WeaponStorage
{
    [StaticConstructorOnStartup]
    partial class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("com.weaponstorage.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Log.Message(
                "WeaponStorage Harmony Patches:" + Environment.NewLine +
                "  Prefix:" + Environment.NewLine +
                "    Pawn_HealthTracker.MakeDowned - not blocking" + Environment.NewLine +
                "    Dialog_FormCaravan.PostOpen" + Environment.NewLine +
                "    CaravanExitMapUtility.ExitMapAndCreateCaravan(IEnumerable<Pawn>, Faction, int)" + Environment.NewLine +
                "    CaravanExitMapUtility.ExitMapAndCreateCaravan(IEnumerable<Pawn>, Faction, int, int)" + Environment.NewLine +
                "  Postfix:" + Environment.NewLine +
                "    Pawn_DraftController.GetGizmos" + Environment.NewLine +
                "    Pawn_TraderTracker.ColonyThingsWillingToBuy" + Environment.NewLine +
                "    TradeShip.ColonyThingsWillingToBuy" + Environment.NewLine +
                "    Window.PreClose" + Environment.NewLine +
                "    ReservationManager.CanReserve" + Environment.NewLine +
                "    CaravanFormingUtility.StopFormingCaravan" + Environment.NewLine +
                "    Pawn_DraftController.Drafted { set; }");
        }
    }

    static class HarmonyPatchUtil
    {
        public static void EquipWeapon(ThingWithComps weapon, Pawn pawn, AssignedWeaponContainer weapons)
        {
#if DEBUG
            Log.Warning("HarmonyPatchUtil.EquipWeapon " + weapon.Label + " " + pawn.Name.ToStringShort + " " + weapons.Count);
#endif
            ThingWithComps p = pawn.equipment.Primary;
            if (p != null)
            {
                pawn.equipment.Remove(p);
                weapons.Add(p);
            }

            if (weapons.Remove(weapon))
            {
                pawn.equipment.AddEquipment(weapon);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
    static class Patch_Pawn_DraftController
    {
        static void Postfix(Pawn_DraftController __instance)
        {
            Pawn pawn = __instance.pawn;
            AssignedWeaponContainer weapons;
            if (WorldComp.TryGetAssignedWeapons(pawn.ThingID, out weapons))
            {
                if (pawn.Drafted)
                {
                    // Going combat
                    if (weapons.LastWeaponUsed != null)
                    {
                        HarmonyPatchUtil.EquipWeapon(weapons.LastWeaponUsed, pawn, weapons);
                    }
                }
                else
                {
                    // Going civilian
                    if (weapons.LastToolUsed != null)
                    {
                        HarmonyPatchUtil.EquipWeapon(weapons.LastToolUsed, pawn, weapons);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    static class Patch_Pawn_HealthTracker_MakeDowned
    {
        static void Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = (Pawn)__instance.GetType().GetField(
                "pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (pawn != null &&
                !__instance.Downed &&
                pawn.Faction == Faction.OfPlayer && 
                pawn.def.race.Humanlike)
            {
                ThingWithComps primary = pawn.equipment.Primary;
                if (primary != null &&
                    (primary.def.IsMeleeWeapon || primary.def.IsRangedWeapon))
                {
                    AssignedWeaponContainer c;
                    if (WorldComp.TryGetAssignedWeapons(pawn.ThingID, out c))
                    {
                        c.Add(primary);
                        pawn.equipment.Remove(primary);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeUndowned")]
    static class Patch_Pawn_HealthTracker_MakeUndowned
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = (Pawn)__instance.GetType().GetField(
                "pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (pawn != null &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.def.race.Humanlike)
            {
                AssignedWeaponContainer c;
                if (WorldComp.TryGetAssignedWeapons(pawn.ThingID, out c) &&
                    pawn.equipment.Primary == null)
                {
                    if (c.LastWeaponUsed != null && c.Remove(c.LastWeaponUsed))
                    {
                        pawn.equipment.AddEquipment(c.LastWeaponUsed);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ReservationManager), "CanReserve")]
    static class Patch_ReservationManager_CanReserve
    {
        private static FieldInfo mapFI = null;
        static void Postfix(ref bool __result, ReservationManager __instance, Pawn claimant, LocalTargetInfo target, int maxPawns, int stackCount, ReservationLayerDef layer, bool ignoreOtherReservations)
        {
            if (mapFI == null)
            {
                mapFI = typeof(ReservationManager).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
            }

#if DEBUG_RESERVE
            Log.Warning("\nCanReserve original result: " + __result);
#endif
            if (!__result && (target.Thing == null || target.GetType() == typeof(Building_WeaponStorage)))
            {
                IEnumerable<Thing> things = ((Map)mapFI.GetValue(__instance))?.thingGrid.ThingsAt(target.Cell);
                if (things != null)
                {
#if DEBUG_RESERVE
                    Log.Warning("CanReserve - Found things");
#endif
                    foreach (Thing t in things)
                    {
#if DEBUG_RESERVE
                        Log.Warning("CanReserve - def " + t.def.defName);
#endif
                        if (t.GetType() == typeof(Building_WeaponStorage))
                        {
#if DEBUG_RESERVE
                            Log.Warning("CanReserve is now true\n");
#endif
                            __result = true;
                        }
                    }
                }
            }
        }
    }

    static class TradeUtil
    {
        public static IEnumerable<Thing> EmptyWeaponStorages(Map map)
        {
            List<Thing> l = new List<Thing>();
            foreach (Building_WeaponStorage ws in WorldComp.WeaponStoragesToUse)
            {
                if (ws.Map == map && ws.Spawned && ws.IncludeInTradeDeals)
                {
                    foreach (ThingWithComps t in ws.StoredWeapons)
                    {
                        l.Add(t);
                    }
                    ws.Empty();
                }
            }
            return l;
        }

        public static void ReclaimWeapons()
        {
            foreach (Building_WeaponStorage ws in WorldComp.WeaponStoragesToUse)
            {
                if (ws.Map != null && ws.Spawned)
                {
                    ws.ReclaimWeapons();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_TraderTracker), "ColonyThingsWillingToBuy")]
    static class Patch_TradeShip_ColonyThingsWillingToBuy
    {
        // Before a caravan trade
        static void Postfix(ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {
            if (playerNegotiator != null && playerNegotiator.Map != null)
            {
                List<Thing> result = new List<Thing>(__result);
                result.AddRange(TradeUtil.EmptyWeaponStorages(playerNegotiator.Map));
                __result = result;
            }
        }
    }

    [HarmonyPatch(typeof(TradeShip), "ColonyThingsWillingToBuy")]
    static class Patch_PassingShip_TryOpenComms
    {
        // Before an orbital trade
        static void Postfix(ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {
            if (playerNegotiator != null && playerNegotiator.Map != null)
            {
                List<Thing> result = new List<Thing>(__result);
                result.AddRange(TradeUtil.EmptyWeaponStorages(playerNegotiator.Map));
                __result = result;
            }
        }
    }

    [HarmonyPatch(typeof(TradeDeal), "Reset")]
    static class Patch_TradeDeal_Reset
    {
        // On Reset from Trade Dialog
        static void Prefix()
        {
            TradeUtil.ReclaimWeapons();
        }
    }

    [HarmonyPatch(typeof(Dialog_Trade), "Close")]
    static class Patch_Window_PreClose
    {
        static void Postfix(bool doCloseSound)
        {
            TradeUtil.ReclaimWeapons();
        }
    }

    #region Caravan Forming
    [HarmonyPatch(typeof(Dialog_FormCaravan), "PostOpen")]
    static class Patch_Dialog_FormCaravan_PostOpen
    {
        static void Prefix(Window __instance)
        {
            Type type = __instance.GetType();
            if (type == typeof(Dialog_FormCaravan))
            {
                Map map = __instance.GetType().GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance) as Map;

                foreach (Building_WeaponStorage storage in WorldComp.GetWeaponStorages(map))
                {
                    storage.Empty();
                }
            }
        }
    }

    [HarmonyPatch(typeof(CaravanFormingUtility), "StopFormingCaravan")]
    static class Patch_CaravanFormingUtility_StopFormingCaravan
    {
        [HarmonyPriority(Priority.First)]
        static void Postfix(Lord lord)
        {
            foreach (Building_WeaponStorage storage in WorldComp.GetWeaponStorages(lord.Map))
            {
                storage.ReclaimWeapons();
            }
        }
    }

    [HarmonyPatch(
        typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan",
        new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int), typeof(int) })]
    static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan_1
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(IEnumerable<Pawn> pawns, Faction faction, int exitFromTile, int directionTile)
        {
            if (faction == Faction.OfPlayer)
            {
                List<Pawn> p = new List<Pawn>(pawns);
                if (p.Count > 0)
                {
                    foreach (Building_WeaponStorage storage in WorldComp.GetWeaponStorages(p[0].Map))
                    {
                        storage.ReclaimWeapons();
                    }
                }
            }
        }
    }

    [HarmonyPatch(
        typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan",
        new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int) })]
    static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan_2
    {
        static void Prefix(IEnumerable<Pawn> pawns, Faction faction, int startingTile)
        {
            if (faction == Faction.OfPlayer)
            {
                List<Pawn> p = new List<Pawn>(pawns);
                if (p.Count > 0)
                {
                    foreach (Building_WeaponStorage storage in WorldComp.GetWeaponStorages(p[0].Map))
                    {
                        storage.ReclaimWeapons();
                    }
                }
            }
        }
    }
    #endregion

    #region Handle "Do until X" for stored weapons
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    static class Patch_RecipeWorkerCounter_CountProducts
    {
        static void Postfix(ref int __result, RecipeWorkerCounter __instance, Bill_Production bill)
        {
            List<ThingCountClass> products = __instance.recipe.products;
            if (WorldComp.WeaponStoragesToUse.Count > 0 && products != null)
            {
                foreach(ThingCountClass product in products)
                {
                    ThingDef def = product.thingDef;
                    if (def.IsWeapon)
                    {
                        foreach (Building_WeaponStorage ws in WorldComp.WeaponStoragesToUse)
                        {
                            if (bill.Map == ws.Map)
                            {
                                __result += ws.GetWeaponCount(def);
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
}
