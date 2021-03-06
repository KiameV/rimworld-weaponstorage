﻿using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using System;
using System.Linq;

namespace WeaponStorage
{
    public class CombatExtendedUtil : WorldComponent
    {
        public CombatExtendedUtil(World world) : base(world)
        {
            Ammo?.Clear();
            if (Ammo == null)
                Ammo = new Dictionary<ThingDef, int>();
        }

        public static bool HasCombatExtended { get; private set; }
        public static void SetHasCombatExtended(bool enabled)
        {
            HasCombatExtended = enabled;
        }
        static CombatExtendedUtil()
        {
            HasCombatExtended = false;
            bool hasWSCEPatch = false;
            foreach (var m in ModsConfig.ActiveModsInLoadOrder)
            {
                if (m.Name.Equals("Combat Extended"))
                    HasCombatExtended = true;
                if (m.Name.StartsWith("[KV] Weapon Storage Combat Extended Patch"))
                    hasWSCEPatch = true;
            }
            if (HasCombatExtended && !hasWSCEPatch)
            {
                Log.Error("WeaponStorage.UseWSCEPatch".Translate());
            }
        }

        // Key: Ammo Def
        // Value: Ammo Count
        public static Dictionary<ThingDef, int> Ammo = new Dictionary<ThingDef, int>();

        public static bool AddAmmo(ThingDef def, int count)
        {
            if (Ammo.TryGetValue(def, out int i))
            {
                i += count;
            }
            else
            {
                i = count;
            }
            Ammo[def] = i;
            return true;
        }

        public static bool AddAmmo(Thing ammo)
        {
            if (IsAmmo(ammo))
            {
                if (ammo.Spawned)
                    ammo.DeSpawn(DestroyMode.Vanish);

                if (Ammo.TryGetValue(ammo.def, out int i))
                {
                    i += ammo.stackCount;
                }
                else
                {
                    i = ammo.stackCount;
                }
                Ammo[ammo.def] = i;
                return true;
            }
            return false;
        }

        internal static void DropAmmo(ThingDef def, Building_WeaponStorage ws)
        {
            DropAllNoUpate(def, GetAmmoCount(def), ws);
            Ammo[def] = 0;
        }

        public static bool TryDropAmmo(ThingDef def, int count, IntVec3 position, Map map)
        {
            return TryDropAmmo(def, count, position, map, out Thing t);
        }

        public static bool TryDropAmmo(ThingDef def, int count, IntVec3 position, Map map, out Thing droppedAmmo)
        {
            //Log.Warning("Util Drop: " + def.defName + " x" + count);
            if (count > 0 &&
                def != null &&
                Ammo.TryGetValue(def, out int i) &&
                i > 0)
            {
                if (i < count)
                    count = i;

                i -= count;

                //Log.Message("    Dropping Count: " + count);
                droppedAmmo = MakeAmmo(def, count);
                if (BuildingUtil.DropThing(droppedAmmo, position, map))
                {
                    //Log.Message("    Remaining Count: " + i);
                    Ammo[def] = i;
                    return true;
                }
                else
                {
                    Log.Error("Failed to drop " + def.defName + " x" + count);
                }
            }
            droppedAmmo = null;
            return false;
        }

        public static bool TryRemoveAmmo(ThingDef def, int count)
        {
            return TryRemoveAmmo(def, count, out Thing t, false);
        }

        public static bool TryRemoveAmmo(ThingDef def, int count, out Thing ammo, bool spawn = true)
        {
            if (count > 0 && 
                def != null &&
                Ammo.TryGetValue(def, out int i) && 
                i > 0)
            {
                if (i < count)
                    count = i;

                i -= count;
                Ammo[def] = i;

                //Log.Warning("Remove " + def.defName + " x" + count);
                if (spawn)
                    ammo = MakeAmmo(def, count);
                else
                    ammo = null;
                return true;
            }
            ammo = null;
            return false;
        }

        public static bool HasAmmo(ThingDef def)
        {
            return GetAmmoCount(def) > 0;
        }

        public static bool IsAmmo(Thing ammo)
        {
            return
                HasCombatExtended &&
                ammo?.GetType().GetProperty("AmmoDef", BindingFlags.NonPublic | BindingFlags.Instance) != null;
        }

        public static int GetAmmoCount(Thing ammo)
        {
            return GetAmmoCount(ammo.def);
        }

        public static int GetAmmoCount(ThingDef def)
        {
            if (HasCombatExtended &&
                def != null &&
                Ammo.TryGetValue(def, out int i))
            {
                return i;
            }
            return 0;
        }

        public static void EmptyAmmo(Building_WeaponStorage ws)
        {
            foreach (var kv in Ammo)
            {
                DropAllNoUpate(kv.Key, kv.Value, ws);
            }
            Ammo.Clear();
        }

        private static void DropAllNoUpate(ThingDef def, int count, Building_WeaponStorage ws)
        {
            while (count > 0)
            {
                int toDrop = Math.Max(def.stackLimit, 1);
                if (toDrop > count)
                    toDrop = count;
                BuildingUtil.DropThing(MakeAmmo(def, toDrop), ws, ws.Map);
                count -= toDrop;
            }
        }

        private static Thing MakeAmmo(ThingDef def, int count)
        {
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = count;
            if (t.stackCount == 0)
                Log.Error(t.Label + " has stack count of 0");
            return t;
        }

        internal static List<ThingDefCount> GetThingCounts()
        {
            var tc = new List<ThingDefCount>(Ammo.Count);
            foreach (var kv in Ammo)
                tc.Add(new ThingDefCount(kv.Key, kv.Value));
            return tc;
        }

        private List<ThingDefCount> ac = null;
        public override void ExposeData()
        {
            base.ExposeData();
            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                ac = GetThingCounts();
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Ammo.Clear();
                ac?.Clear();
                ac = new List<ThingDefCount>();
            }

            Scribe_Collections.Look(ref ac, "ammo", LookMode.Deep, new object[] { });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Ammo.Clear();
                foreach (var a in ac)
                    if (a.Count > 0)
                        Ammo.Add(a.ThingDef, a.Count);
            }

            if (Scribe.mode == LoadSaveMode.Saving || 
                Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ac?.Clear();
                ac = null;
            }
        }
    }
}
