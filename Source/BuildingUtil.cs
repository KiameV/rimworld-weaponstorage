using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    class BuildingUtil
    {
        public static List<T> FindThingsOfTypeNextTo<T>(Map map, IntVec3 position, int distance) where T : Thing
        {
            int minX = Math.Max(0, position.x - distance);
            int maxX = Math.Min(map.info.Size.x, position.x + distance);
            int minZ = Math.Max(0, position.z - distance);
            int maxZ = Math.Min(map.info.Size.z, position.z + distance);

            List<T> list = new List<T>();
            for (int x = minX - 1; x <= maxX; ++x)
            {
                for (int z = minZ - 1; z <= maxZ; ++z)
                {
                    foreach (Thing t in map.thingGrid.ThingsAt(new IntVec3(x, position.y, z)))
                    {
                        if (t.GetType() == typeof(T))
                        {
                            list.Add((T)t);
                        }
                    }
                }
            }
            return list;
        }

        //private static Random random = null;
        public static bool DropThing(Thing toDrop, Building_WeaponStorage from, Map map, bool makeForbidden = true)
        {
            try
            {
                from.AllowAdds = false;
                return DropThing(toDrop, (Building)from, map, makeForbidden);
            }
            finally
            {
                from.AllowAdds = true;
            }
        }

        public static bool DropThing(Thing toDrop, Building from, Map map, bool makeForbidden = true)
        {
            return DropThing(toDrop, from.InteractionCell, map, makeForbidden);
        }

        public static bool DropThing(Thing toDrop, IntVec3 from, Map map, bool makeForbidden = true)
        {
            bool dropped = false;
            try
            {
                Thing t;
                if (!toDrop.Spawned)
                {
                    dropped = GenThing.TryDropAndSetForbidden(toDrop, from, map, ThingPlaceMode.Near, out t, makeForbidden);
                    if (!dropped || !toDrop.Spawned)
                    {
                        dropped = GenPlace.TryPlaceThing(toDrop, from, map, ThingPlaceMode.Near);
                    }
                }

                toDrop.Position = from;

                dropped = toDrop.Spawned;
            }
            catch (Exception e)
            {
                Log.Error(
                    "ChangeDresser:BuildingUtil.DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
            return dropped;
        }
    }
}
