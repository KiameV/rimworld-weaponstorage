﻿using CombatExtended;
using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using WeaponStorage;

namespace CombatExtendedWeaponStoragePatch
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches_CombatExtended
    {
        public static Assembly CombatExtendedAssembly { get; private set; }
        //public static Type AmmoDef { get; private set; }

        static HarmonyPatches_CombatExtended()
        {
            CombatExtendedAssembly = null;
            bool wsFound = false, ceFound = false;
            CombatExtendedUtil.SetHasCombatExtended(false);
            var mods = new List<ModMetaData>(ModsConfig.ActiveModsInLoadOrder);
            for (int i = 0; i < mods.Count; ++i)
            {
                if (mods[i].Name.StartsWith("[KV] Weapon Storage"))
                {
                    wsFound = true;
                    if (ceFound)
                        break;
                }
                else if (mods[i].Name.Equals("Combat Extended"))
                {
                    ceFound = true;
                    if (wsFound)
                    {
                        Log.Error("Weapon Storage must be loaded after Combat Extended.");
                        return;
                    }
                }
            }

            if (ceFound)
            {
                try
                {
                    bool found = false;
                    foreach (ModContentPack pack in LoadedModManager.RunningMods)
                    {
                        foreach (Assembly assembly in pack.assemblies.loadedAssemblies)
                        {
                            if (assembly.GetName().Name.Equals("CombatExtended"))
                            {
                                /*AmmoDef = assembly.GetType("CombatExtended.AmmoDef");
                                if (AmmoDef == null)
                                    throw new Exception("Unable to find CombatExtended.AmmoDef");*/

                                /*compAmmoUser = assembly.GetType("CombatExtended.CompAmmoUser");
                                if (compAmmoUser == null)
                                    throw new Exception("Unable to find CombatExtended.CompAmmoUser");*/

                                CombatExtendedAssembly = assembly;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                    }

                    var harmony = HarmonyInstance.Create("com.combatextendedweaponstoragepatch.rimworld.mod");
                    harmony.PatchAll(Assembly.GetExecutingAssembly());

                    CombatExtendedUtil.SetHasCombatExtended(true);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to patch \"Combat Extended\"." + Environment.NewLine + e.Message);
                }
            }
            else
            {
                Log.Message("Weapon Storage \"Combat Extended\" Patch did not find \"Combat Extended\". Will not load patch.");
            }
        }
    }

    [HarmonyPatch(typeof(Building_TurretGunCE), "TryOrderReload")]
    static class Patch_Building_TurretGunCE_TryOrderReload
    {
        static void Prefix(Building_TurretGunCE __instance)
        {
            if (__instance.GetType().GetField("mannableComp", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance) is CompMannable mannable &&
                __instance.CompAmmo.UseAmmo)
            {
                CompInventory inv = ThingCompUtility.TryGetComp<CompInventory>(mannable.ManningPawn);
                if (mannable.ManningPawn.IsColonist && inv != null)
                {
                    Thing thing = inv.container.FirstOrDefault((Thing x) => x.def == __instance.CompAmmo.SelectedAmmo);
                    if (thing == null)
                    {
                        AmmoDef ammoDef = __instance.CompAmmo.SelectedAmmo;
                        if (ammoDef != null &&
                            CombatExtendedUtil.HasAmmo(ammoDef))
                        {
                            int magazineSize = __instance.CompAmmo.Props.magazineSize;
                            if (CombatExtendedUtil.TryRemoveAmmo(ammoDef, magazineSize, out Thing ammo))
                            {
                                inv.UpdateInventory();
                                if (!inv.container.TryAdd(ammo as ThingWithComps))
                                {
                                    Log.Error("Failed to add ammo to pawn inventory");
                                    CombatExtendedUtil.AddAmmo(ammo);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

   /* [HarmonyPatch(typeof(JobGiver_TakeAndEquip), "GetPriorityWork")]
    static class Patch_JobGiver_TakeAndEquip_GetPriority
    {
        static void Prefix(JobGiver_TakeAndEquip __instance, Pawn pawn)
        {
            Log.Warning("Start");
            bool trader = pawn.kindDef.trader;
            if (trader)
            {
                return;
            }
            else
            {
                bool flag = pawn.CurJob != null;
                JobDef jobDef = flag ? pawn.CurJob.def : null;
                bool flag2 = flag && !jobDef.isIdle;
                if (flag2)
                {
                    return;
                }
                else
                {
                    bool flag3 = pawn.equipment != null && pawn.equipment.Primary != null;
                    CompAmmoUser compAmmoUser = flag3 ? ThingCompUtility.TryGetComp<CompAmmoUser>(pawn.equipment.Primary) : null;
                    bool flag4 = pawn.Faction.IsPlayer && compAmmoUser != null;
                    if (flag4)
                    {
                        Loadout loadout = pawn.GetLoadout();
                        bool flag5 = loadout != null && loadout.SlotCount > 0;
                        if (flag5)
                        {
                            Log.Warning("flag5");
                        }
                    }
                    bool flag6 = !flag3;
                    if (flag6)
                    {
                        bool flag7 = (bool)typeof(JobGiver_TakeAndEquip).GetMethod("Unload", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { pawn });
                        if (flag7)
                        {
                            Log.Warning("flag7");
                        }
                        else
                        {
                            Log.Warning("flag7else");
                        }
                    }
                    else
                    {
                        CompInventory compInventory = pawn.TryGetComp<CompInventory>();
                        int viableAmmoCarried = 0;
                        float viableAmmoBulk = 0;
                        var primaryAmmoUser = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
                        foreach (AmmoLink link in primaryAmmoUser.Props.ammoSet.ammoTypes)
                        {
                            var count = compInventory.AmmoCountOfDef(link.ammo);
                            viableAmmoCarried += count;
                            viableAmmoBulk += count * link.ammo.GetStatValueAbstract(CE_StatDefOf.Bulk);
                        }

                        // ~2/3rds of the inventory bulk minus non-usable and non-ammo bulk could be filled with ammo
                        float potentialAmmoBulk = .66f * (compInventory.capacityBulk - compInventory.currentBulk + viableAmmoBulk);

                        // There's less ammo [bulk] than fits the potential ammo bulk [bulk]
                        if (viableAmmoBulk < potentialAmmoBulk)
                        {
                            Log.Warning("1");
                            // There's less ammo [nr] than fits a clip [nr]
                            FloatRange magazineSize = new FloatRange(1f, 2f);
                            if (primaryAmmoUser.Props.magazineSize == 0 || viableAmmoCarried < magazineSize.min)
                            {
                                Log.Warning("Unload Start 1");
                                __instance.GetType().GetMethod("Unload", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { pawn });
                                Log.Warning("Unload Done 1");
                                return;
                            }

                            Log.Warning("2");
                            // There's less ammo [nr] than fits two clips [nr] && no enemies are close
                            if (viableAmmoCarried < magazineSize.max
                             && !PawnUtility.EnemiesAreNearby(pawn, 30, true))
                            {
                                Log.Warning("Unload Start 2");
                                __instance.GetType().GetMethod("Unload", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { pawn });
                                Log.Warning("Unload Done 2");
                                return;
                            }
                            Log.Warning("3");
                        }
                    }
                }
            }
        }
    }*/

    [HarmonyPatch(typeof(CompAmmoUser), "TryStartReload")]
    static class Patch_CompAmmoUser_TryStartReload
    {
        static void Prefix(CompAmmoUser __instance)
        {
            if (__instance.turret == null &&
                __instance.Wielder != null &&
                __instance.HasMagazine)
            {
                AmmoDef ammoDef = __instance.SelectedAmmo;//__instance.GetType().GetProperty("CurrentAmmo", BindingFlags.Instance | BindingFlags.Public).GetValue(__instance, null) as Def;
                if (ammoDef != null &&
                    CombatExtendedUtil.HasAmmo(ammoDef))
                {
                    if (!__instance.TryFindAmmoInInventory(out Thing ammo))
                    {
                        int magazineSize = __instance.Props.magazineSize;
                        if (CombatExtendedUtil.TryRemoveAmmo(ammoDef, magazineSize, out ammo))
                        {
                            __instance.CompInventory.ammoList.Add(ammo as ThingWithComps);
                        }
                    }
                }
            }
        }
    }

    class StoredAmmo : Thing
    {
        public readonly Building root;
        public readonly ThingDef ammoDef;
        public readonly int ammoCount;
        public readonly bool forced;
        public StoredAmmo(Building root, ThingDef ammoDef, int ammoCount, bool forced)
        {
            this.root = root;
            this.ammoDef = ammoDef;
            this.ammoCount = ammoCount;
            this.forced = forced;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_ReloadTurret), "HasJobOnThing")]
    static class Patch_WorkGiver_ReloadTurret_HasJobOnThing
    {
        public static Building WS = null;

        // used to manually re-arm turrets
        static void Postfix(WorkGiver_ReloadTurret __instance, ref bool __result, Pawn pawn, Thing t, bool forced)
        {
            Building_TurretGunCE turret = t as Building_TurretGunCE;
            if (__result == false && turret != null && !turret.def.hasInteractionCell)
            {
                if (WorldComp.HasStorages(turret.Map) &&
                    CombatExtendedUtil.HasAmmo(turret.CompAmmo.SelectedAmmo))
                {
                    WS = GenClosest.ClosestThingReachable(turret.Position, turret.Map, ThingRequest.ForDef(WorldComp.WeaponStorageDef), Verse.AI.PathEndMode.ClosestTouch, TraverseParms.For(pawn, pawn.NormalMaxDanger(), TraverseMode.ByPawn, false), 100) as Building;
                    __result = WS != null;
                }
            }
        }

        /*[HarmonyPatch(typeof(JobGiver_Work), "GiverTryGiveJobPrioritized")]
        static class Patch_JobGiver_Work_GiverTryGiveJobPrioritized
        {
            static void Postfix(WorkGiver_ReloadTurret __instance, ref Job __result, Pawn pawn, WorkGiver giver, IntVec3 cell)
            {
                //if (__result)
            }
        }*/
    }

    [HarmonyPatch(typeof(WorkGiver_ReloadTurret), "JobOnThing")]
    static class Patch_WorkGiver_ReloadTurret_JobOnThing
    {
        static bool Prefix(WorkGiver_ReloadTurret __instance, ref Job __result, Pawn pawn, Thing t, bool forced)
        {
            if (Patch_WorkGiver_ReloadTurret_HasJobOnThing.WS != null)
            {
                Building_TurretGunCE turret = t as Building_TurretGunCE;
                __result = new Job(
                    DefDatabase<JobDef>.GetNamed("ReloadTurret", true),
                    t,
                    new StoredAmmo(
                        Patch_WorkGiver_ReloadTurret_HasJobOnThing.WS,
                        turret.CompAmmo.SelectedAmmo,
                        turret.CompAmmo.Props.magazineSize,
                        forced));
                Patch_WorkGiver_ReloadTurret_HasJobOnThing.WS = null;
                return false;
            }
            return true;
        }

        // Used to automatically re-arm turrets
        static void Postfix(WorkGiver_ReloadTurret __instance, ref Job __result, Pawn pawn, Thing t, bool forced)
        {
            if (__result == null)
            {
                if (t is Building_TurretGunCE turret)
                {
                    if (WorldComp.HasStorages(turret.Map) &&
                        CombatExtendedUtil.HasAmmo(turret.CompAmmo.SelectedAmmo))
                    {
                        var storage = GenClosest.ClosestThingReachable(
                            turret.Position, turret.Map, ThingRequest.ForDef(WorldComp.WeaponStorageDef), Verse.AI.PathEndMode.ClosestTouch, TraverseParms.For(pawn, pawn.NormalMaxDanger(), TraverseMode.ByPawn, false), 100);
                        if (storage != null)
                        {
                            // TODO - Add setting where ammo should be dropped
                            //CombatExtendedUtil.TryDropAmmo(turret.CompAmmo.SelectedAmmo, turret.CompAmmo.Props.magazineSize, storage.Position, storage.Map);
                            
                            __result = new Job(
                                DefDatabase<JobDef>.GetNamed("ReloadTurret", true),
                                t,
                                new StoredAmmo(
                                    Patch_WorkGiver_ReloadTurret_HasJobOnThing.WS,
                                    turret.CompAmmo.SelectedAmmo,
                                    turret.CompAmmo.Props.magazineSize,
                                    forced));
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    static class Patch_Pawn_JobTracker_StartJob
    {
        static void Prefix(Pawn_JobTracker __instance, Job newJob, JobCondition lastJobEndCondition, ThinkNode jobGiver, bool resumeCurJobAfterwards, bool cancelBusyStances, ThinkTreeDef thinkTree, JobTag? tag, bool fromQueue)
        {
            if (newJob != null && newJob.targetB != null && newJob.targetB.Thing is StoredAmmo sa)
            {
                Pawn pawn = __instance.GetType().GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as Pawn;
                
                Building_TurretGunCE turret = newJob.targetA.Thing as Building_TurretGunCE;
                
                FieldInfo fi = sa.root.GetType().GetField("AllowAdds", BindingFlags.Instance | BindingFlags.Public);

                try
                {
                    fi.SetValue(sa.root, false);
                    IntVec3 pos;
                    Building building = sa.root as Building;
                    if (sa.forced || sa.root == null)
                        pos = pawn.Position;
                    else
                        pos = building.InteractionCell;
                    
                    if (!CombatExtendedUtil.TryDropAmmo(sa.ammoDef, sa.ammoCount, pos, sa.root.Map, out Thing t))
                    {
                        Log.Error("Could not get ammo");
                    }
                    else
                    {
                        newJob.targetB = t;
                        newJob.count = t.stackCount;
                    }
                }
                finally
                {
                    fi.SetValue(sa.root, true);
                }
            }
        }
    }
    /*[HarmonyPatch(typeof(JobDriver_ReloadTurret), "MakeNewToils")]
    static class Patch_JobDriver_ReloadTurret_MakeNewToils
    {
        static void Prefix(JobDriver_ReloadTurret __instance)
        {
            if (__instance.job.targetB.Thing is StoredAmmo sa)
            {
                Log.Warning("MakeNewToils");
                Building_TurretGunCE turret = __instance.GetType().GetProperty("turret", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null) as Building_TurretGunCE;
                CompAmmoUser compReloader = __instance.GetType().GetProperty("compReloader", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null) as CompAmmoUser;
                Pawn pawn = __instance.pawn;

                FieldInfo fi = sa.root.GetType().GetField("AllowAdds", BindingFlags.Instance | BindingFlags.Public);

                try
                {
                    fi.SetValue(sa.root, false);
                    IntVec3 pos;
                    Building building = sa.root as Building;
                    if (sa.forced || sa.root == null)
                        pos = pawn.Position;
                    else
                        pos = building.InteractionCell;

                    if (!CombatExtendedUtil.TryDropAmmo(sa.ammoDef, sa.ammoCount, pos, sa.root.Map, out Thing t))
                    {
                        Log.Error("Could not get ammo");
                    }
                    else
                    {
                        __instance.GetType().GetProperty("TargetThingB", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, t, null);
                        __instance.job.count = t.stackCount;
                    }
                }
                finally
                {
                    fi.SetValue(sa.root, true);
                }
            }
        }
    }*/

    /*[HarmonyPatch(typeof(JobDriver_ReloadTurret), "TryMakePreToilReservations")]
    static class Patch_JobDriver_ReloadTurret_TryMakePreToilReservations
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(JobDriver_ReloadTurret __instance, ref bool __result, bool errorOnFailed)
        {
            if (__instance.job.targetB.Thing is StoredAmmo sa)
            {
                FieldInfo fi = sa.root.GetType().GetField("AllowAdds", BindingFlags.Instance | BindingFlags.Public);
                IntVec3 position = (IntVec3)sa.root.GetType().GetProperty("InteractionCell", BindingFlags.Instance | BindingFlags.Public).GetValue(sa.root, null);
                try
                {
                    fi.SetValue(sa.root, false);
                    if (CombatExtendedUtil.TryDropAmmo(sa.ammoDef, sa.ammoCount, position, sa.root.Map, out Thing ammo))
                    {
                        Log.Warning("ammo is not null " + (ammo != null).ToString());
                        __instance.job.targetB = ammo;
                        Log.Warning("targetB is not null " + (__instance.job.targetB != null).ToString());
                        Log.Warning("targetB is type " + __instance.job.targetB.Thing.GetType().Name);
                    }
                }
                finally
                {
                    fi.SetValue(sa.root, true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    static class Patch_Pawn_JobTracker_StartJob
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Job newJob, JobCondition lastJobEndCondition, ThinkNode jobGiver, bool resumeCurJobAfterwards, bool cancelBusyStances, ThinkTreeDef thinkTree, JobTag? tag, bool fromQueue)
        {
            if (newJob.targetB.Thing is StoredAmmo sa)
            {
                //Log.Message("def: " + sa.ammoDef + " x" + sa.ammoCount + " (" + sa.Position.x + ", " + sa.Position.y + ", " + sa.Position.z + ") Map not null: " + (sa.MapToUse != null).ToString());
                FieldInfo fi = sa.root.GetType().GetField("AllowAdds", BindingFlags.Instance | BindingFlags.Public);
                IntVec3 position = (IntVec3)sa.root.GetType().GetProperty("InteractionCell", BindingFlags.Instance | BindingFlags.Public).GetValue(sa.root, null);
                try
                {
                    fi.SetValue(sa.root, false);
                    if (CombatExtendedUtil.TryDropAmmo(sa.ammoDef, sa.ammoCount, position, sa.root.Map, out Thing ammo))
                    {
                        newJob.targetB = ammo;
                    }
                }
                finally
                {
                    fi.SetValue(sa.root, true);
                }
            }
        }
    }*/

    /* Prevent pawns from putting ammo in inventory
    [HarmonyPatch(typeof(ThingOwner<Thing>))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch("TryAdd", new Type[] { typeof(Thing), typeof(int), typeof(bool) })]
    static class Patch_ThingOwner_TryAdd
    {
        static bool Prefix(ThingOwner<Thing> __instance, ref int __result, Thing item, int count, bool canMergeWithExistingStacks)
        {
            var inv = __instance.Owner as Pawn_InventoryTracker;
            if (inv?.pawn.IsColonist == true &&
                item?.def is AmmoDef &&
                WorldComp.HasStorages())
            {
                if (CombatExtendedUtil.AddAmmo(item.def, count))
                {
                    __result = count;
                    return false;
                }
            }
            return true;
        }
    }*/

    [HarmonyPatch(typeof(CompAmmoUser), "TryReduceAmmoCount")]
    static class Patch_CompAmmoUser_TryReduceAmmoCount
    {
        static void Postfix(CompAmmoUser __instance, ref bool __result)
        {
            if (__result == false &&
                (__instance.Wielder != null || __instance.turret != null))
            {
                if (!__instance.HasMagazine && __instance.UseAmmo)
                {
                    var def = __instance.GetType().GetField("selectedAmmo", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as ThingDef;
                    if (CombatExtendedUtil.TryRemoveAmmo(def, 1))
                    {
                        //__instance.GetType().GetField("currentAmmoInt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, ammo.def as AmmoDef);
                        //__instance.Props.ammoSet
                        __result = true;
                    }
                }
                var ammoTypes = __instance.Props?.ammoSet?.ammoTypes;
                if (ammoTypes != null)
                {
                    foreach (var t in ammoTypes)
                    {
                        if (CombatExtendedUtil.HasAmmo(t.projectile))
                        {
                            __result = true;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Command_Reload), "MakeAmmoMenu")]
    static class Patch_Command_Reload_MakeAmmoMenu
    {
        static void Postfix(Command_Reload __instance, ref FloatMenu __result)
        {
            if (__instance.compAmmo.turret == null)
            {
                List<FloatMenuOption> options = __result.GetType().GetField("options", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__result) as List<FloatMenuOption>;

                List<AmmoDef> list = new List<AmmoDef>();
                foreach (AmmoLink curLink in __instance.compAmmo.Props.ammoSet.ammoTypes)
                {
                    if (CombatExtendedUtil.HasAmmo(curLink.ammo))
                    {
                        bool containsLabelCap = false;
                        foreach (var o in options)
                        {
                            if (o.Label.Equals(curLink.ammo.ammoClass.LabelCap))
                            {
                                containsLabelCap = true;
                                break;
                            }
                        }
                        
                        if (!containsLabelCap)
                        {
                            options.Insert(0,
                                new FloatMenuOption(
                                    curLink.ammo.ammoClass.LabelCap, delegate
                                    {
                                        if (__instance.compAmmo.SelectedAmmo != curLink.ammo ||
                                            __instance.compAmmo.CurMagCount < __instance.compAmmo.Props.magazineSize)
                                        {
                                            __instance.compAmmo.SelectedAmmo = curLink.ammo;

                                            Building_TurretGunCE turret = __instance.compAmmo.turret;
                                            if (turret == null || turret.MannableComp == null)
                                            {
                                                if (CombatExtendedUtil.TryRemoveAmmo(curLink.ammo, __instance.compAmmo.Props.magazineSize, out Thing ammo))
                                                {
                                                    __instance.compAmmo.TryUnload();
                                                    
                                                    if (!__instance.compAmmo.CompInventory.container.TryAdd(ammo as ThingWithComps))
                                                    {
                                                        Log.Error("Failed to reload ammo");
                                                        CombatExtendedUtil.AddAmmo(ammo);
                                                    }
                                                    __instance.compAmmo.CompInventory.UpdateInventory();

                                                    if (turret != null)
                                                    {
                                                        __instance.compAmmo.turret.TryOrderReload();
                                                    }
                                                    else
                                                    {
                                                        __instance.compAmmo.TryStartReload();
                                                    }
                                                }
                                            }
                                        }
                                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                    }
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(CompInventory), "SwitchToNextViableWeapon")]
    static class Patch_CompInventory_SwitchToNextViableWeapon
    {
        static bool Prefix(CompInventory __instance, bool useFists)
        {
            if (__instance.parent is Pawn pawn &&
                pawn.IsColonist &&
                WorldComp.AssignedWeapons.TryGetValue(pawn, out AssignedWeaponContainer c))
            {
                /*ThingWithComps currentWeapon = pawn.equipment.Primary;
                ThingWithComps melee = null;//, ranged = null;
                foreach (var w in c.Weapons)
                {
                    if (w != currentWeapon)
                    {
                        if (w.def.IsMeleeWeapon)
                            melee = w;
                        //else
                        //    ranged = w;
                    }
                }

                if (currentWeapon.def.IsMeleeWeapon)
                {
                    HarmonyPatchUtil.EquipWeapon(melee, pawn, c);
                }
                /else
                {
                    // Ranged TODO
                }*/
                return false;
            }
            return true;
        }
    }
}