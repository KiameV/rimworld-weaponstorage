using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;

namespace WeaponStorage.UI
{
    public class AssignUI : Window
    {
        private readonly Building_WeaponStorage weaponStorage;

        private Pawn selectedPawn = null;
        private List<WeaponSelected> PossibleWeapons = null;

        private Vector2 scrollPosition = new Vector2(0, 0);

        private List<Pawn> selectablePawns = null;
        private List<Pawn> PlayerPawns
        {
            get
            {
                if (selectablePawns == null)
                {
                    selectablePawns = new List<Pawn>();
                    Dictionary<string, Pawn> pawnLookup = new Dictionary<string, Pawn>();
                    foreach (Pawn p in PawnsFinder.AllMapsAndWorld_Alive)
                    {
                        if (p.Faction == Faction.OfPlayer && p.def.race.Humanlike)
                        {
                            selectablePawns.Add(p);
                            pawnLookup.Add(p.ThingID, p);
                        }
                    }

                    for (int i = WorldComp.AssignedWeapons.Count - 1; i >= 0; --i)
                    {
                        AssignedWeaponContainer c = WorldComp.AssignedWeapons[i];
                        Pawn cPawn;
                        if (!pawnLookup.TryGetValue(c.PawnId, out cPawn) || cPawn.Dead)
                        {
                            this.weaponStorage.StoredWeapons.AddRange(c.Weapons);
                            WorldComp.AssignedWeapons.RemoveAt(i);
                        }
                    }
                }
                return selectablePawns;
            }
        }

        public AssignUI(Building_WeaponStorage weaponStorage)
        {
            this.weaponStorage = weaponStorage;

            this.PossibleWeapons = null;

            this.closeOnEscapeKey = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(650f, 600f);
            }
        }

#if DEBUG
        int i = 600;
#endif
        public override void DoWindowContents(Rect inRect)
        {
#if DEBUG
            ++i;
#endif
            try
            {
                Widgets.Label(new Rect(0, 0, 150, 30), "WeaponStorage.AssignTo".Translate());
                string label = (this.selectedPawn != null) ? this.selectedPawn.NameStringShort : "Pawn";
                if (Widgets.ButtonText(new Rect(175, 0, 150, 30), label))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (Pawn p in PlayerPawns)
                    {
                        options.Add(new FloatMenuOption(p.Name.ToStringShort, delegate
                        {
                            if (this.selectedPawn != null)
                            {
                                this.SetAssignedWeapons(this.selectedPawn, this.PossibleWeapons);
                                this.PossibleWeapons.Clear();
                            }
                            this.PossibleWeapons = null;

                            this.selectedPawn = p;

                            int size;
                            AssignedWeaponContainer c;
                            if (!WorldComp.TryGetAssignedWeapons(p.ThingID, out c))
                            {
                                size = this.weaponStorage.StoredWeapons.Count + 1;
                                c = null;
                            }
                            else
                            {
                                size = c.Weapons.Count + this.weaponStorage.StoredWeapons.Count + 1;
                            }

                            this.PossibleWeapons = new List<WeaponSelected>(size);
                            ThingWithComps primary = p.equipment.Primary;
                            if (primary != null)
                            {
                                this.PossibleWeapons.Add(new WeaponSelected(primary, true));
                            }
                            if (c != null)
                            {
                                foreach (ThingWithComps t in c.Weapons)
                                {
                                    this.PossibleWeapons.Add(new WeaponSelected(t, true));
                                }
                            }
                            foreach (ThingWithComps t in this.weaponStorage.StoredWeapons)
                            {
                                this.PossibleWeapons.Add(new WeaponSelected(t, false));
                            }
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                int count = (this.PossibleWeapons != null) ? this.PossibleWeapons.Count : ((this.weaponStorage.StoredWeapons != null) ? this.weaponStorage.StoredWeapons.Count : 0);
                Rect r = new Rect(0, 20, 334, count * 26);
                scrollPosition = GUI.BeginScrollView(new Rect(40, 50, 350, 400), scrollPosition, r);
                Listing_Standard lst = new Listing_Standard();
                lst.Begin(r);

                if (this.selectedPawn != null && this.PossibleWeapons != null)
                {
                    for (int i = 0; i < this.PossibleWeapons.Count; ++i)
                    {
                        WeaponSelected selected = this.PossibleWeapons[i];
                        bool isChecked = selected.isChecked;
                        lst.CheckboxLabeled(selected.thing.Label, ref isChecked);
                        selected.isChecked = isChecked;
                        this.PossibleWeapons[i] = selected;
                    }
                }
                else
                {
#if DEBUG
                    if (i > 600)
                    {
                        Log.Warning("WeaponStorage DoWindowContents: Display non-checkbox weapons. Count: " + this.weaponStorage.StoredWeapons.Count);
                    }
#endif
                    foreach (ThingWithComps t in this.weaponStorage.StoredWeapons)
                    {
#if DEBUG
                        if (i > 600)
                        {
                            Log.Warning("-" + t.Label);
                        }
#endif
                        lst.Label(t.Label);
                    }
                }

                lst.End();
                GUI.EndScrollView();
            }
            catch (Exception e)
            {
                String msg = this.GetType().Name + " closed due to: " + e.GetType().Name + " " + e.Message;
                Log.Error(msg);
                Messages.Message(msg, MessageSound.Negative);
                base.Close();
            }
            finally
            {
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
#if DEBUG
                if (i > 600)
                {
                    i = 0;
                }
#endif
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            this.SetAssignedWeapons(this.selectedPawn, this.PossibleWeapons);

            if (this.selectablePawns != null)
            {
                this.selectablePawns.Clear();
                this.selectablePawns = null;
            }
        }

        private void SetAssignedWeapons(Pawn p, List<WeaponSelected> weapons)
        {
            if (p == null || weapons == null)
            {
                return;
            }

            AssignedWeaponContainer c;
            if (!WorldComp.TryGetAssignedWeapons(p.ThingID, out c))
            {
                c = new AssignedWeaponContainer();
                c.PawnId = p.ThingID;
            }

            this.weaponStorage.StoredWeapons.AddRange(c.Weapons);
            c.Weapons.Clear();

            bool primaryFound = false;
            ThingWithComps primary = p.equipment.Primary;
            foreach (WeaponSelected s in this.PossibleWeapons)
            {
                if (s.isChecked)
                {
                    if (primary != null && !primaryFound &&
                        primary.thingIDNumber == s.thing.thingIDNumber)
                    {
                        primaryFound = true;
                    }
                    else
                    {
                        if (this.weaponStorage.RemoveNoDrop(s.thing))
                        {
                            c.Weapons.Add(s.thing);
                        }
                    }
                }
            }

            if (!primaryFound && primary != null &&
                (primary.def.IsMeleeWeapon || primary.def.IsRangedWeapon))
            {
                p.equipment.Remove(primary);
                this.weaponStorage.StoredWeapons.Add(primary);
            }

            if (c.Weapons.Count == 0)
            {
                WorldComp.AssignedWeapons.Remove(c);
            }
            else
            {
#if DEBUG
                Log.Warning("AssignUI.SetAssignedWeapons: Try Add");
#endif
                WorldComp.Add(c);
            }
        }
    }
}
