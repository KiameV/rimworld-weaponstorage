using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;

namespace WeaponStorage.UI
{
    public class AssignUI : Window
    {
        public enum ApparelFromEnum { Pawn, Storage };
        private readonly Building_WeaponStorage weaponStorage;

        private List<WeaponSelected> selectedWeapons;

        private Pawn selectedPawn = null;
        private string SelectedPawnName
        {
            get
            {
                if (this.selectedPawn != null)
                {
                    return this.selectedPawn.Name.ToStringShort;
                }
                return null;
            }
        }

        private List<Pawn> selectablePawns = new List<Pawn>();
        private List<Pawn> PlayerPawns
        {
            get
            {
                if (selectablePawns.Count == 0)
                {
                    selectablePawns = new List<Pawn>();
                    foreach (Pawn p in PawnsFinder.AllMapsAndWorld_Alive)
                    {
                        if (p.Faction == Faction.OfPlayer && p.def.defName.Equals("Human"))
                        {
                            selectablePawns.Add(p);
                        }
                    }


                    for (int i = AssignedWeaponContainer.AssignedWeapons.Count - 1; i >= 0; --i)
                    {
                        bool delete = false;
                        if (AssignedWeaponContainer.AssignedWeapons[i].Pawn.Dead)
                        {
                            delete = true;
                        }
                        else
                        {
                            bool found = false;
                            foreach (Pawn p in selectablePawns)
                            {
                                if (p.ThingID.Equals(AssignedWeaponContainer.AssignedWeapons[i].Pawn.ThingID))
                                {
                                    found = true;
                                    break;
                                }
                                if (!found)
                                {
                                    delete = true;
                                }
                            }
                        }
                        if (delete)
                        {
                            this.weaponStorage.StoredWeapons.AddRange(AssignedWeaponContainer.AssignedWeapons[i].Weapons);
                            AssignedWeaponContainer.AssignedWeapons.RemoveAt(i);
                        }
                    }
                }
                return selectablePawns;
            }
        }

        public AssignUI(Building_WeaponStorage weaponStorage)
        {
            this.weaponStorage = weaponStorage;

            this.selectedWeapons = null;

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

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                Widgets.Label(new Rect(0, 0, 150, 30), "WeaponStorage.AssignTo".Translate());
                if (Widgets.ButtonText(new Rect(175, 0, 150, 30), this.SelectedPawnName))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (Pawn p in PlayerPawns)
                    {
                        options.Add(new FloatMenuOption(p.Name.ToStringShort, delegate
                        {
                            if (this.selectedPawn != null)
                            {
                                this.SetAssignedWeapons(this.selectedPawn, this.selectedWeapons);
                                this.selectedWeapons.Clear();
                            }

                            this.selectedPawn = p;

                            AssignedWeaponContainer assignedWeapons;
                            if (!AssignedWeaponContainer.TryGetAssignedWeapons(p, out assignedWeapons))
                            {
                                assignedWeapons = new AssignedWeaponContainer();
                                assignedWeapons.Pawn = p;
                            }

                            this.selectedWeapons = new List<WeaponSelected>(assignedWeapons.Weapons.Count + this.weaponStorage.StoredWeapons.Count);
                            foreach (ThingWithComps t in assignedWeapons.Weapons)
                            {
                                if (t != null)
                                {
                                    this.selectedWeapons.Add(new WeaponSelected(t, true));
                                }
                            }
                            foreach (ThingWithComps t in this.weaponStorage.StoredWeapons)
                            {
                                if (t != null)
                                {
                                    this.selectedWeapons.Add(new WeaponSelected(t, false));
                                }
                            }
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                if (this.selectedWeapons != null)
                {
                    Listing_Standard lst = new Listing_Standard();
                    lst.Begin(new Rect(20, 50, 500, 400));
                    for (int i = 0; i < this.selectedWeapons.Count; ++i)
                    {
                        WeaponSelected selected = this.selectedWeapons[i];
                        bool isChecked = selected.isChecked;
                        lst.CheckboxLabeled(selected.thing.Label, ref isChecked);
                        selected.isChecked = isChecked;
                        this.selectedWeapons[i] = selected;
                    }
                    lst.End();
                }
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
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            this.SetAssignedWeapons(this.selectedPawn, this.selectedWeapons);
        }

        private void SetAssignedWeapons(Pawn p, List<WeaponSelected> weapons)
        {
            if (p == null || weapons == null)
            {
                return;
            }
            
            AssignedWeaponContainer assignedWeapons;
            if (!AssignedWeaponContainer.TryGetAssignedWeapons(p, out assignedWeapons))
            {
                assignedWeapons = new AssignedWeaponContainer();
                assignedWeapons.Pawn = p;
            }
            assignedWeapons.Weapons.Clear();
            this.weaponStorage.StoredWeapons.Clear();

            foreach (WeaponSelected selected in weapons)
            {
                if (selected.isChecked)
                {
                    assignedWeapons.Weapons.Add(selected.thing);
                }
                else
                {
                    this.weaponStorage.StoredWeapons.Add(selected.thing);
                }
            }

            if (assignedWeapons.Weapons.Count == 0)
            {
                AssignedWeaponContainer.Remove(p);
            }
            else
            {
                AssignedWeaponContainer.Set(assignedWeapons);
            }
        }
    }
}
