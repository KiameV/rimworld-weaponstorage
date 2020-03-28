using HarmonyLib;
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
					Pawn pawn = __instance.pawn;
					List<Gizmo> l = null;
					if (WorldComp.AssignedWeapons.TryGetValue(__instance.pawn, out AssignedWeaponContainer weapons))
					{
						l = new List<Gizmo>();
						if (__result != null)
							l.AddRange(__result);

                        //if (pawn.equipment.Primary != null)
                        //    l.Add(CreateUnequipGizmo(pawn, weapons));

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
                                show = pawn.equipment.Primary != weapon;
                            }

							if (show)
							{
								l.Add(CreateEquipWeaponGizmo(weapon.def, pawn,
									delegate
									{
										HarmonyPatchUtil.EquipWeapon(weapon, pawn, weapons);

										weapons.SetLastThingUsed(pawn, weapon, false);
									}));
							}
						}
					}

					foreach (SharedWeaponFilter f in WorldComp.SharedWeaponFilter)
					{
						f.UpdateFoundDefCache();
						if (f.AssignedPawns.Contains(pawn))
						{
							if (l == null)
							{
								l = new List<Gizmo>();
								if (__result != null)
									l.AddRange(__result);
							}

							foreach (ThingDef d in f.AllowedDefs)
							{
								if (d != pawn.equipment.Primary?.def &&
                                    f.FoundDefCacheContains(d))
								{
									l.Add(CreateEquipWeaponGizmo(d, pawn,
										delegate
										{
											if (WorldComp.TryRemoveWeapon(d, f, false, out ThingWithComps weapon))
                                            {
                                                HarmonyPatchUtil.EquipWeapon(weapon, pawn);
                                                f.UpdateDefCache(d);
											}
										}, "WeaponStorage.EquipShared"));
								}
							}
						}
					}

					if (l != null)
						__result = l;
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

            private static Command_Action CreateEquipWeaponGizmo(ThingDef def, Pawn pawn, Action equipWeaponAction, string label = "WeaponStorage.Equip")
            {
                Command_Action a = new Command_Action();
                if (def.uiIcon != null)
                {
                    a.icon = def.uiIcon;
                }
                else if (def.graphicData.texPath != null)
                {
                    a.icon = ContentFinder<UnityEngine.Texture2D>.Get(def.graphicData.texPath, true);
                }
                else
                {
                    a.icon = null;
                }
                StringBuilder sb = new StringBuilder(label.Translate());
                sb.Append(" ");
                sb.Append(def.label);
                a.defaultLabel = sb.ToString();
                a.defaultDesc = "WeaponStorage.EquipDesc".Translate();
                a.activateSound = SoundDef.Named("Click");
                a.groupKey = (label + def).GetHashCode();
                a.action = equipWeaponAction;
                return a;
            }
            /*private static Command_Action CreateUnequipGizmo(Pawn pawn, AssignedWeaponContainer weapons)
            {
                return new Command_Action
                {
                    icon = TexCommand.AttackMelee,
                    defaultLabel = "WeaponStorage.Unequip".Translate(),
                    defaultDesc = "WeaponStorage.UnequipDesc".Translate(),
                    activateSound = SoundDef.Named("Click"),
                    groupKey = "WeaponStorage.Unequip".GetHashCode(),
                    action = delegate
                    {
                        HarmonyPatchUtil.UnequipPrimaryWeapon(pawn, weapons);
                        weapons.SetLastThingUsed(pawn, null, true);
                    }
                };
            }*/
        }
    }
}