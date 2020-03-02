using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    partial class HarmonyPatches
    {
        struct StoredWeapons
        {
            public readonly Building_WeaponStorage Storage;
            public readonly ThingWithComps Weapon;
            public StoredWeapons(Building_WeaponStorage storage, ThingWithComps weapon)
            {
                this.Storage = storage;
                this.Weapon = weapon;
            }
        }

        struct WeaponsToUse
        {
            public readonly List<StoredWeapons> Weapons;
            public readonly int Count;
            public WeaponsToUse(List<StoredWeapons> weapons, int count)
            {
                this.Weapons = weapons;
                this.Count = count;
            }
        }

        class NeededIngrediants
        {
            public readonly ThingFilter Filter;
            public int Count;
            public readonly Dictionary<Def, List<StoredWeapons>> FoundThings;

            public NeededIngrediants(ThingFilter filter, int count)
            {
                this.Filter = filter;
                this.Count = count;
                this.FoundThings = new Dictionary<Def, List<StoredWeapons>>();
            }
            public void Add(StoredWeapons things)
            {
                List<StoredWeapons> l;
                if (!this.FoundThings.TryGetValue(things.Weapon.def, out l))
                {
                    l = new List<StoredWeapons>();
                    this.FoundThings.Add(things.Weapon.def, l);
                }
                l.Add(things);
            }
            public void Clear()
            {
                this.FoundThings.Clear();
            }
            public bool CountReached()
            {
                foreach (List<StoredWeapons> l in this.FoundThings.Values)
                {
                    if (this.CountReached(l))
                        return true;
                }
                return false;
            }
            private bool CountReached(List<StoredWeapons> l)
            {
                int count = this.Count;
                foreach (StoredWeapons st in l)
                {
                    count -= st.Weapon.stackCount;
                }
                return count <= 0;
            }
            public List<StoredWeapons> GetFoundThings()
            {
                foreach (List<StoredWeapons> l in this.FoundThings.Values)
                {
                    if (this.CountReached(l))
                    {
#if DEBUG
                        Log.Warning("Count [" + Count + "] reached with: " + l[0].Weapon.def.label);
#endif
                        return l;
                    }
                }
                return null;
            }
        }

        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients")]
        static class Patch_WorkGiver_DoBill_TryFindBestBillIngredients
        {
            static void Postfix(ref bool __result, Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen)
            {
                if (bill.Map == null)
                {
                    Log.Error("Bill's map is null");
                    return;
                }

                if (__result == true || !WorldComp.HasStorages(bill.Map) || bill.Map != pawn.Map)
                    return;

#if DEBUG || DROP_DEBUG || BILL_DEBUG
            Log.Warning("TryFindBestBillIngredients.Postfix __result: " + __result);
#endif
                Dictionary<ThingDef, int> chosenAmounts = new Dictionary<ThingDef, int>();
                foreach (ThingCount c in chosen)
                {
                    int count;
                    if (chosenAmounts.TryGetValue(c.Thing.def, out count))
                    {
                        count += c.Count;
                    }
                    else
                    {
                        count = c.Count;
                    }
                    chosenAmounts[c.Thing.def] = count;
                }

#if DEBUG || DROP_DEBUG || BILL_DEBUG
                Log.Warning("    ChosenAmounts:");
                //foreach (KeyValuePair<ThingLookup, int> kv in chosenAmounts)
                {
                //    Log.Warning("        " + kv.Key.Def.label + " - " + kv.Value);
                }
#endif

                LinkedList<NeededIngrediants> neededIngs = new LinkedList<NeededIngrediants>();
                foreach (IngredientCount ing in bill.recipe.ingredients)
                {
                    bool found = false;
                    foreach (KeyValuePair<ThingDef, int> kv in chosenAmounts)
                    {
                        if ((int)ing.GetBaseCount() == kv.Value)
                        {
#if DEBUG || DROP_DEBUG || BILL_DEBUG
                        Log.Warning("    Needed Ing population count is the same");
#endif
                            if (ing.filter.Allows(kv.Key))
                            {
#if DEBUG || DROP_DEBUG || BILL_DEBUG
                            Log.Warning("    Needed Ing population found: " + kv.Key.label + " count: " + kv.Value);
#endif
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
#if DEBUG || DROP_DEBUG || BILL_DEBUG
                    Log.Warning("    Needed Ing population not found");
#endif
                        neededIngs.AddLast(new NeededIngrediants(ing.filter, (int)ing.GetBaseCount()));
                    }
                }

#if DEBUG || DROP_DEBUG || BILL_DEBUG
            Log.Warning("    Needed Ings:");
            foreach (NeededIngrediants ings in neededIngs)
            {
                Log.Warning("        " + ings.Count);
            }
#endif

                List<WeaponsToUse> weaponsToUse = new List<WeaponsToUse>();
                foreach (Building_WeaponStorage storage in WorldComp.GetWeaponStorages(bill.Map))
                {
                    if ((float)(storage.Position - billGiver.Position).LengthHorizontalSquared < Math.Pow(bill.ingredientSearchRadius, 2))
                    {
                        LinkedListNode<NeededIngrediants> n = neededIngs.First;
                        while (n != null)
                        {
                            var next = n.Next;
                            NeededIngrediants neededIng = n.Value;

                            List<ThingWithComps> gotten;
                            if (storage.TryGetFilteredWeapons(bill, neededIng.Filter, out gotten))
                            {
                                foreach (ThingWithComps got in gotten)
                                {
                                    neededIng.Add(new StoredWeapons(storage, got));
                                }
                                if (neededIng.CountReached())
                                {
                                    weaponsToUse.Add(new WeaponsToUse(neededIng.GetFoundThings(), neededIng.Count));
                                    neededIng.Clear();
                                    neededIngs.Remove(n);
                                }
                            }
                            n = next;
                        }
                    }
                }

#if DEBUG || DROP_DEBUG || BILL_DEBUG
            Log.Warning("    neededIngs.count: " + neededIngs.Count);
#endif

                if (neededIngs.Count == 0)
                {
                    __result = true;
                    foreach (WeaponsToUse ttu in weaponsToUse)
                    {
                        int count = ttu.Count;
                        foreach (StoredWeapons sa in ttu.Weapons)
                        {
                            if (count <= 0)
                                break;

                            if (sa.Storage.Remove(sa.Weapon))
                            {
                                count -= sa.Weapon.stackCount;
                                chosen.Add(new ThingCount(sa.Weapon, sa.Weapon.stackCount));
                            }
                        }
                    }
                }

                weaponsToUse.Clear();
                foreach (NeededIngrediants n in neededIngs)
                    n.Clear();
                neededIngs.Clear();
                chosenAmounts.Clear();
            }
        }
    }
}