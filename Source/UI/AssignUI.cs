using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;

namespace WeaponStorage.UI
{
    [StaticConstructorOnStartup]
    public class AssignUI : Window
    {
        static AssignUI()
        {
            DropTexture = ContentFinder<Texture2D>.Get("UI/drop", true);
            UnknownWeaponIcon = ContentFinder<Texture2D>.Get("UI/UnknownWeapon", true);
            assignweaponsTexture = ContentFinder<Texture2D>.Get("UI/assignweapons", true);
            emptyTexture = ContentFinder<Texture2D>.Get("UI/empty", true);
            collectTexture = ContentFinder<Texture2D>.Get("UI/collect", true);
            yesSellTexture = ContentFinder<Texture2D>.Get("UI/yessell", true);
            noSellTexture = ContentFinder<Texture2D>.Get("UI/nosell", true);
        }

        private readonly Building_WeaponStorage weaponStorage;

        public static Texture2D DropTexture;
        public static Texture2D UnknownWeaponIcon;
        public static Texture2D assignweaponsTexture;
        public static Texture2D emptyTexture;
        public static Texture2D collectTexture;
        public static Texture2D yesSellTexture;
        public static Texture2D noSellTexture;

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
                    foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_Alive)
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
                            this.weaponStorage.AddWeapons(c.Weapons);
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
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
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
                                size = this.weaponStorage.Count + 1;
                                c = null;
                            }
                            else
                            {
                                size = c.Weapons.Count + this.weaponStorage.Count + 1;
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

                const int HEIGHT = 30;
                const int BUFFER = 2;
                int count = (this.PossibleWeapons != null) ? this.PossibleWeapons.Count : ((this.weaponStorage.StoredWeapons != null) ? this.weaponStorage.Count : 0);
                Rect r = new Rect(0, 20, 384, (count + 1) * (HEIGHT + BUFFER));
                scrollPosition = GUI.BeginScrollView(new Rect(40, 50, 400, 400), scrollPosition, r);

                if (this.selectedPawn != null && this.PossibleWeapons != null)
                {
                    for (int i = 0; i < this.PossibleWeapons.Count; ++i)
                    {
                        GUI.BeginGroup(new Rect(0, 55 + i * (HEIGHT + BUFFER), r.width, HEIGHT));
                        WeaponSelected weapon = this.PossibleWeapons[i];

                        bool isChecked = weapon.isChecked;
                        Widgets.Checkbox(0, (HEIGHT - 20) / 2, ref isChecked, 20);
                        weapon.isChecked = isChecked;

                        Widgets.ThingIcon(new Rect(34, 0, HEIGHT, HEIGHT), weapon.thing);

                        Widgets.Label(new Rect(38 + HEIGHT + 5, 0, 250, HEIGHT), weapon.thing.Label);

                        if (Widgets.ButtonImage(new Rect(r.xMax - 20, 0, 20, 20), DropTexture))
                        {
                            this.PossibleWeapons.RemoveAt(i);
                            this.weaponStorage.Remove(weapon.thing);
                            break;
                        }
                        this.PossibleWeapons[i] = weapon;
                        GUI.EndGroup();
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
                    int i = 0;
                    foreach (ThingWithComps t in this.weaponStorage.StoredWeapons)
                    {
#if DEBUG
                        if (i > 600)
                        {
                            Log.Warning("-" + t.Label);
                        }
#endif
                        GUI.BeginGroup(new Rect(0, 55 + i * (HEIGHT + BUFFER), r.width, HEIGHT));

                        Widgets.ThingIcon(new Rect(34, 0, HEIGHT, HEIGHT), t);

                        Widgets.Label(new Rect(38 + HEIGHT + 5, 0, 250, HEIGHT), t.Label);

                        if (Widgets.ButtonImage(new Rect(r.xMax - 20, 0, 20, 20), DropTexture))
                        {
                            this.weaponStorage.Remove(t);
                            break;
                        }
                        GUI.EndGroup();
                        ++i;
                    }
                }

                GUI.EndScrollView();
            }
            catch (Exception e)
            {
                String msg = this.GetType().Name + " closed due to: " + e.GetType().Name + " " + e.Message;
                Log.Error(msg);
                Messages.Message(msg, MessageTypeDefOf.NegativeEvent);
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

            this.weaponStorage.AddWeapons(c.Weapons);
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
                this.weaponStorage.AddWeapon(primary);
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
