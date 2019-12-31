using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace WeaponStorageUtil
{
    class WeaponStorageUtil
    {
        private static Assembly wsAssembly = null;
        private static bool initialized = false;
        public static bool Exists
        {
            get
            {
                if (!initialized)
                {
                    foreach (ModContentPack pack in LoadedModManager.RunningMods)
                    {
                        foreach (Assembly assembly in pack.assemblies.loadedAssemblies)
                        {
                            if (assembly.GetName().Name.Equals("WeaponStorage") &&
                                assembly.GetType("WeaponStorage.WorldComp") != null)
                            {
                                initialized = true;
                                wsAssembly = assembly;
                                break;
                            }
                        }
                        if (initialized)
                        {
                            break;
                        }
                    }
                    initialized = true;
                }
                return wsAssembly != null;
            }
        }

        public static bool TryEquipType(Pawn p, ThingDef def)
        {
            if (Exists)
            {
                try
                {
                    IDictionary assignedWeapons = wsAssembly.GetType("WeaponStorage.WorldComp").GetField("AssignedWeapons", BindingFlags.Static | BindingFlags.Public).GetValue(null) as IDictionary;
                    if (assignedWeapons != null)
                    {
                        object aw = assignedWeapons[p];
                        if (aw != null)
                        {
                            List<ThingWithComps> weapons = aw.GetType().GetField("Weapons", BindingFlags.Instance | BindingFlags.Public).GetValue(aw) as List<ThingWithComps>;
                            if (weapons != null)
                            {
                                foreach (ThingWithComps w in weapons)
                                {
                                    if (w.def == def)
                                    {
                                        wsAssembly.GetType("WeaponStorage.HarmonyPatchUtil").GetMethod("EquipWeapon", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { w, p, aw });
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Do nothing
                    Log.Warning(e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
                }
            }
            return false;
        }
    }
}
