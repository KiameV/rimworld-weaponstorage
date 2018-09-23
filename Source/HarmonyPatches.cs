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
                "    MakeUndowned - Priority First" + Environment.NewLine +
                "    Pawn.Kill - Priority First" + Environment.NewLine +
                "    Verb_ShootOneUse.Notify_EquipmentLost - Priority First" + Environment.NewLine +
                "    Pawn_EquipmentTracker.MakeRoomFor - Priority First" + Environment.NewLine +
                "  Postfix:" + Environment.NewLine +
                "    Pawn_TraderTracker.ColonyThingsWillingToBuy" + Environment.NewLine +
                "    TradeShip.ColonyThingsWillingToBuy" + Environment.NewLine +
                "    Window.PreClose" + Environment.NewLine +
                "    ReservationManager.CanReserve" + Environment.NewLine +
                "    CaravanFormingUtility.StopFormingCaravan" + Environment.NewLine +
                "    Pawn_DraftController.Drafted { set; }" + Environment.NewLine +
                "    WealthWatcher.ForceRecount" + Environment.NewLine +
                "    MakeDowned - Priority First" + Environment.NewLine +
                "    Pawn.Kill - Priority First");
        }
    }

    static class HarmonyPatchUtil
    {
        public static void EquipWeapon(ThingWithComps weapon, Pawn pawn, AssignedWeaponContainer c)
        {
            ThingWithComps primary = pawn.equipment.Primary;
            if (primary != null)
            {
                pawn.equipment.Remove(primary);
                if (pawn.equipment.Primary != null)
                {
                    // In case the primary weapon is not removed
                    if (weapon.Spawned == false)
                    {
                        if (!c.Weapons.Contains(weapon))
                        {
                            c.Weapons.Add(weapon);
                        }
                        c.Weapons.Remove(pawn.equipment.Primary);
                    }
                    Log.Warning("Failed to replace " + pawn.Name.ToStringShort + "'s primary weapon [" + pawn.equipment.Primary.Label + "] with [" + weapon.Label + "].");
                    return;
                }
                if (!c.Weapons.Contains(primary))
                {
                    c.Weapons.Add(primary);
                }
            }
            pawn.equipment.AddEquipment(weapon);
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
    static class Patch_Pawn_DraftController
    {
        static void Postfix(Pawn_DraftController __instance)
        {
            Pawn pawn = __instance.pawn;
            AssignedWeaponContainer weapons;
            if (WorldComp.AssignedWeapons.TryGetValue(pawn, out weapons))
            {
                ThingWithComps w;
                if (weapons.TryGetLastThingUsed(pawn, out w))
                {
                    HarmonyPatchUtil.EquipWeapon(w, pawn, weapons);
                }
            }
        }
    }

    /*[HarmonyPatch(typeof(Pawn_EquipmentTracker), "AddEquipment")]
    static class Patch_Pawn_AddEquipment
    {
        [HarmonyPriority(Priority.First)]
        static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            Pawn pawn = (Pawn)__instance.GetType().GetField(
                "pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
#if TRY_ADD_EQUIPMENT
            Log.Warning("Begin Pawn_EquipmentTracker.AddEquipment " + pawn.Name.ToStringShort);
#endif
            AssignedWeaponContainer c;
            if (WorldComp.AssignedWeapons.TryGetValue(pawn, out c))
            {
                c.Add(newEq);
                c.SetLastThingUsed(pawn, newEq);
#if TRY_ADD_EQUIPMENT
                Log.Message("    Assigned Weapons After Add:");
                foreach (ThingWithComps w in c.Weapons)
                {
                    Log.Warning("        " + w.Label);
                }
#endif
            }
#if TRY_ADD_EQUIPMENT
            Log.Warning("End Pawn_EquipmentTracker.AddEquipment");
#endif
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
    static class Patch_Pawn_TryDropEquipment
    {
        [HarmonyPriority(Priority.First)]
        static void Postfix(ref bool __result, Pawn_EquipmentTracker __instance, ref ThingWithComps resultingEq)
        {
            Pawn pawn = (Pawn)__instance.GetType().GetField(
                "pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
#if TRY_DROP_EQUIPMENT
            Log.Warning("Begin Pawn_EquipmentTracker.TryDropEquipment " + pawn.Name.ToStringShort);
#endif
            if (__result && resultingEq != null)
            {
                AssignedWeaponContainer c;
                if (WorldComp.AssignedWeapons.TryGetValue(pawn, out c))
                {
                    c.Remove(resultingEq);

#if TRY_DROP_EQUIPMENT
                    Log.Message("    Assigned Weapons After Drop:");
                    foreach (ThingWithComps w in c.Weapons)
                    {
                        Log.Warning("        " + w.Label);
                    }
#endif
                    }
                }
#if TRY_DROP_EQUIPMENT
            Log.Warning("End Pawn_EquipmentTracker.TryDropEquipment");
#endif
        }
    }*/

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    static class Patch_Pawn_HealthTracker_MakeDowned
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = (Pawn)__instance.GetType().GetField(
                "pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
#if DOWNED
            Log.Warning("Begin MakeDowned.Postfix: " + pawn.Name.ToStringShort);
#endif
            if (pawn != null &&
                !__instance.Downed &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.def.race != null &&
                pawn.def.race.Humanlike &&
                pawn.equipment != null &&
                pawn.equipment.Primary != null)
            {
#if DOWNED
                Log.Message("    Primary: " + ((primary != null) ? primary.Label : "<null>"));
#endif
                AssignedWeaponContainer c;
                if (WorldComp.AssignedWeapons.TryGetValue(pawn, out c))
                {
#if DOWNED
                    Log.Message("    Assigned Weapons Count: " + c.Weapons.Count);
                    foreach (ThingWithComps w in c.Weapons)
                    {
                        Log.Message("        " + w.Label);
                    }
#endif
                    pawn.equipment.Remove(pawn.equipment.Primary);
                }
            }
#if DOWNED
            Log.Warning("End MakeDowned.Prefix: " + pawn.Name.ToStringShort);
#endif
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeUndowned")]
    static class Patch_Pawn_HealthTracker_MakeUndowned
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = (Pawn)__instance.GetType().GetField(
                "pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (pawn != null &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.def.race.Humanlike)
            {
                AssignedWeaponContainer c;
                if (WorldComp.AssignedWeapons.TryGetValue(pawn, out c) &&
                    pawn.equipment.Primary == null)
                {
                    ThingWithComps w;
                    if (c.TryGetLastThingUsed(pawn, out w))
                    {
                        HarmonyPatchUtil.EquipWeapon(w, pawn, c);
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

                foreach (Building_WeaponStorage s in WorldComp.GetWeaponStorages(map))
                {
                    s.Empty();
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
            foreach (Building_WeaponStorage s in WorldComp.WeaponStoragesToUse)
            {
                s.ReclaimWeapons();
            }
        }
    }

    [HarmonyPatch(
        typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan",
        new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(IEnumerable<Pawn> pawns, Faction faction, int exitFromTile, int directionTile, int destinationTile, bool sendMessage)
        {
            if (faction == Faction.OfPlayer)
            {
                List<Pawn> p = new List<Pawn>(pawns);
                if (p.Count > 0)
                {
                    foreach (Building_WeaponStorage s in WorldComp.WeaponStoragesToUse)
                    {
                        s.ReclaimWeapons();
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
            List<ThingDefCountClass> products = __instance.recipe.products;
            if (WorldComp.WeaponStoragesToUse.Count > 0 && products != null)
            {
                foreach (ThingDefCountClass product in products)
                {
                    ThingDef def = product.thingDef;
                    if (def.IsWeapon)
                    {
                        foreach (Building_WeaponStorage ws in WorldComp.GetWeaponStorages(bill.Map))
                        {
                            __result += ws.GetWeaponCount(def, bill.ingredientFilter);
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Pawn Death
    [HarmonyPatch(typeof(Pawn), "Kill")]
    static class Patch_Pawn_Kill
    {
        private static Map map;

        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn __instance)
        {
            map = __instance.Map;
        }

        [HarmonyPriority(Priority.First)]
        static void Postfix(Pawn __instance)
        {
            if (__instance.Dead)
            {
                AssignedWeaponContainer c;
                if (WorldComp.AssignedWeapons.TryGetValue(__instance, out c))
                {
                    WorldComp.AssignedWeapons.Remove(__instance);

                    foreach (ThingWithComps w in c.Weapons)
                    {
                        if (!WorldComp.Add(w))
                        {
                            BuildingUtil.DropThing(w, __instance.Position, map, true);
                        }
                    }
                }
            }
        }
    }
#endregion

    [HarmonyPatch(typeof(Verb_ShootOneUse), "Notify_EquipmentLost")]
    static class Patch_Verb_ShootOneUse_Notify_EquipmentLost
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Verb_ShootOneUse __instance)
        {
            foreach (AssignedWeaponContainer c in WorldComp.AssignedWeapons.Values)
            {
                if (c.Weapons.Remove(__instance.EquipmentSource))
                {
                    foreach (ThingWithComps w in c.Weapons)
                    {
                        if (w.def.IsRangedWeapon)
                        {
                            HarmonyPatchUtil.EquipWeapon(w, c.Pawn, c);
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "MakeRoomFor")]
    static class Patch_Pawn_EquipmentTracker_MakeRoomFor
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (eq.def.equipmentType == EquipmentType.Primary && __instance.Primary != null)
            {
                AssignedWeaponContainer c;
                if (WorldComp.AssignedWeapons.TryGetValue(__instance.pawn, out c) && 
                    c.Weapons.Contains(eq))
                {
                    __instance.Remove(eq);
                }
            }
        }
    }
}
