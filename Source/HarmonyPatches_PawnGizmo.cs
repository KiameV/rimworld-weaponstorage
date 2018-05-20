using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace WeaponStorage
{
    partial class HarmonyPatches
    {
        [HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
        static class Patch_Pawn_DraftController_GetGizmos
        {
            static void Postfix(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
            {
                try
                {
                    AssignedWeaponContainer weapons;
                    if (WorldComp.AssignedWeapons.TryGetValue(__instance.pawn, out weapons))
                    {
                        List<Gizmo> l = new List<Gizmo>();
                        if (__result != null)
                            l.AddRange(__result);
                        Pawn pawn = __instance.pawn;

                        foreach (ThingWithComps weapon in weapons.Weapons)
                        {
                            bool isTool = Settings.IsTool(weapon);
                            bool show = false;
                            if (pawn.Drafted)
                            {
                                show = true;
                            }
                            else // Not drafted
                            {
                                if (isTool || Settings.ShowWeaponsWhenNotDrafted)
                                {
                                    show = true;
                                }
                            }

                            if (show)
                            {
                                l.Add(CreateEquipWeaponGizmo(weapon, pawn,
                                    delegate
                                    {
                                        HarmonyPatchUtil.EquipWeapon(weapon, pawn, weapons);

                                        weapons.SetLastThingUsed(pawn, weapon);
                                    }));
                            }
                        }

                        __result = l;
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorOnce(
                        "Exception while getting gizmos for pawn "
                        + __instance.pawn.Name.ToStringShort +
                        Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace,
                        (__instance.pawn.Name.ToStringFull + "WSGIZMO").GetHashCode());
                }
            }

            static Command_Action CreateEquipWeaponGizmo(Thing weapon, Pawn pawn, Action equipWeaponAction)
            {
                Command_Action a = new Command_Action();
                if (weapon.def.uiIcon != null)
                {
                    a.icon = weapon.def.uiIcon;
                }
                else if (weapon.def.graphicData.texPath != null)
                {
                    a.icon = ContentFinder<UnityEngine.Texture2D>.Get(weapon.def.graphicData.texPath, true);
                }
                else
                {
                    a.icon = null;
                }
                StringBuilder sb = new StringBuilder("WeaponStorage.Equip".Translate());
                sb.Append(" ");
                sb.Append(weapon.def.label);
                a.defaultLabel = sb.ToString();
                a.defaultDesc = "Equip this item.";
                a.activateSound = SoundDef.Named("Click");
                a.groupKey = weapon.def.GetHashCode();
                a.action = equipWeaponAction;
                return a;
            }
        }
    }
}