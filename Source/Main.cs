using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Verse;

namespace WeaponStorage
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("com.weaponstorage.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            UI.AssignUI.DropTexture = ContentFinder<UnityEngine.Texture2D>.Get("UI/drop", true);

            Log.Message("WeaponStorage: Adding Harmony Prefix to Pawn_HealthTracker.MakeDowned - not blocking");
            Log.Message("WeaponStorage: Adding Harmony Postfix to Pawn_DraftController.GetGizmos");
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
    static class Patch_Pawn_DraftController_GetGizmos
    {
        static void Postfix(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.pawn.Drafted || Settings.ShowWeaponsWhenNotDrafted)
            {
                AssignedWeaponContainer weapons;
                if (WorldComp.TryGetAssignedWeapons(__instance.pawn.ThingID, out weapons))
                {
                    List<Gizmo> l;
                    if (__result != null)
                        l = new List<Gizmo>(__result);
                    else
                        l = new List<Gizmo>(weapons.Weapons.Count);

                    try
                    {
                        for (int i = 0; i < weapons.Weapons.Count; ++i)
                        {
                            ThingWithComps t = weapons.Weapons[i];

                            Command_Action a = new Command_Action();
                            try
                            {
                                a.icon = ContentFinder<UnityEngine.Texture2D>.Get(t.def.graphicData.texPath, true);
                            }
                            catch
                            {
                                a.icon = null;
                            }
                            StringBuilder sb = new StringBuilder("WeaponStorage.Equip".Translate());
                            sb.Append(" ");
                            sb.Append(t.def.label);
                            a.defaultLabel = sb.ToString();
                            a.defaultDesc = "Equip this item.";
                            a.activateSound = SoundDef.Named("Click");
                            a.groupKey = t.def.GetHashCode();
                            a.action = delegate
                            {
                                ThingWithComps p = __instance.pawn.equipment.Primary;
                                if (p != null)
                                {
                                    if (!p.def.IsRangedWeapon && !p.def.IsMeleeWeapon)
                                    {
                                        ThingWithComps temp;
                                        __instance.pawn.equipment.TryDropEquipment(p, out temp, __instance.pawn.Position, true);
                                    }
                                    else
                                    {
                                        __instance.pawn.equipment.Remove(p);
                                    }
                                }

                                for (int j = 0; j < weapons.Weapons.Count; ++j)
                                {
                                    if (weapons.Weapons[j].thingIDNumber == t.thingIDNumber)
                                    {
                                        if (p == null)
                                        {
                                            weapons.Weapons.RemoveAt(j);
                                        }
                                        else
                                        {
                                            weapons.Weapons[j] = p;
                                        }
                                        break;

                                    }
                                }

                                __instance.pawn.equipment.AddEquipment(t);
                            };
                            l.Add(a);
                        }
                    }
                    catch
                    {

                    }

                    __result = l;
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
                        c.Weapons.Add(primary);
                        pawn.equipment.Remove(primary);
                    }
                }
            }
        }
    }
}
