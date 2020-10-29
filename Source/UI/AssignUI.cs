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
        #region Textures
        static AssignUI()
        {
            DropTexture = ContentFinder<Texture2D>.Get("UI/drop", true);
            UnknownWeaponIcon = ContentFinder<Texture2D>.Get("UI/UnknownWeapon", true);
            assignweaponsTexture = ContentFinder<Texture2D>.Get("UI/assignweapons", true);
            emptyTexture = ContentFinder<Texture2D>.Get("UI/empty", true);
            collectTexture = ContentFinder<Texture2D>.Get("UI/collect", true);
            yesSellTexture = ContentFinder<Texture2D>.Get("UI/yessell", true);
            noSellTexture = ContentFinder<Texture2D>.Get("UI/nosell", true);
			meleeTexture = ContentFinder<Texture2D>.Get("UI/melee", true);
			rangedTexture = ContentFinder<Texture2D>.Get("UI/ranged", true);
            ammoTexture = ContentFinder<Texture2D>.Get("UI/ammo", true);
            nextTexture = ContentFinder<Texture2D>.Get("UI/next", true);
            previousTexture = ContentFinder<Texture2D>.Get("UI/previous", true);
            weaponStorageTexture = ContentFinder<Texture2D>.Get("UI/weaponstorage", true);
        }

        public static Texture2D DropTexture;
        public static Texture2D UnknownWeaponIcon;
        public static Texture2D assignweaponsTexture;
        public static Texture2D emptyTexture;
        public static Texture2D collectTexture;
        public static Texture2D yesSellTexture;
        public static Texture2D noSellTexture;
		public static Texture2D meleeTexture;
		public static Texture2D rangedTexture;
        public static Texture2D ammoTexture;
        public static Texture2D nextTexture;
        public static Texture2D previousTexture;
        public static Texture2D weaponStorageTexture;
        #endregion

        private Building_WeaponStorage weaponStorage;
		
        private AssignedWeaponContainer assignedWeapons = null;
        private int pawnIndex = -1;

        private List<ThingWithComps> PossibleWeapons = null;
		private List<SelectablePawns> selectablePawns;

        private Vector2 scrollPosition = new Vector2(0, 0);

        private string textBuffer = "";

		private float PreviousY = 0;

        public AssignUI(Building_WeaponStorage weaponStorage, Pawn pawn = null)
        {
            this.weaponStorage = weaponStorage;

            this.PossibleWeapons = null;

            this.closeOnClickedOutside = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;

			this.selectablePawns = Util.GetPawns(true);
            this.UpdatePawnIndex(pawn);
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(650f, 600f);
            }
        }

        private void RebuildPossibleWeapons()
        {
            if (this.PossibleWeapons != null)
            {
                this.PossibleWeapons.Clear();
                this.PossibleWeapons = null;
            }
            
            int size = (this.weaponStorage != null) ? this.weaponStorage.Count : 0;
            if (this.assignedWeapons != null)
            {
                size += this.assignedWeapons.Count;
            }

            this.PossibleWeapons = new List<ThingWithComps>(size);
            if (this.assignedWeapons != null)
            {
                foreach (ThingWithComps w in this.assignedWeapons.Weapons)
                {
                    this.PossibleWeapons.Add(w);
                }
            }
            if (this.weaponStorage != null)
            {
                foreach (ThingWithComps w in this.weaponStorage.GetWeapons(false))
                {
                    this.PossibleWeapons.Add(w);
                }
                foreach (ThingWithComps w in this.weaponStorage.GetBioEncodedWeapons())
                {
                    this.PossibleWeapons.Add(w);
                }
            }
        }

        private bool IsAssignedWeapon(int i)
        {
            return this.assignedWeapons != null && i < this.assignedWeapons.Count;
        }

#if TRACE
        int i = 600;
#endif
        public override void DoWindowContents(Rect inRect)
        {
#if TRACE
            ++i;
#endif
			GUI.color = Color.white;
            Text.Font = GameFont.Small;
            try
            {
                float x = 0, y = 0;

                #region Assign To
                Widgets.Label(new Rect(x, y + 4, 100, 30), "WeaponStorage.AssignTo".Translate());
                x += 80;
                
                if (this.selectablePawns.Count > 0 &&
                    GUI.Button(new Rect(x, y, 30, 30), previousTexture))
                {
                    --this.pawnIndex;
                    if (this.pawnIndex < 0 || this.assignedWeapons == null)
                        this.pawnIndex = this.selectablePawns.Count - 1;
                    this.LoadAssignedWeapons();
                }
                x += 30;

                string label = (this.assignedWeapons != null) ? this.assignedWeapons.Pawn.Name.ToStringShort : "";
                if (Widgets.ButtonText(new Rect(x, y, 200, 30), label))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (SelectablePawns p in this.selectablePawns)
                    {
                        options.Add(new FloatMenuOption(p.LabelAndStats, delegate
                        {
                            this.UpdatePawnIndex(p.Pawn);
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                x += 200;

                if (this.selectablePawns.Count > 0 &&
                    GUI.Button(new Rect(x, y, 30, 30), nextTexture))
                {
                    ++this.pawnIndex;
                    if (this.pawnIndex >= this.selectablePawns.Count || this.assignedWeapons == null)
                        this.pawnIndex = 0;
                    this.LoadAssignedWeapons();
                }
                x += 40;
                #endregion
                y += 40;

                #region Weapon Storage
                x = 0;
                Widgets.Label(new Rect(x, y - 4, 100, 60), "WeaponStorage".Translate());
                x += 80;

                if (WorldComp.HasStorages() &&
                    GUI.Button(new Rect(x, y, 30, 30), previousTexture))
                {
                    this.NextWeaponStorage(-1);
                }
                x += 30;

                label = (this.weaponStorage != null) ? this.weaponStorage.Label : "";
                if (Widgets.ButtonText(new Rect(x, y, 250, 30), label))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (var ws in WorldComp.GetWeaponStorages())
                    {
                        options.Add(new FloatMenuOption(ws.Label, delegate
                        {
                            this.weaponStorage = ws;
                            this.RebuildPossibleWeapons();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                x += 250;

                if (WorldComp.HasStorages() &&
                    GUI.Button(new Rect(x, y, 30, 30), nextTexture))
                {
                    this.NextWeaponStorage(1);
                }
                x += 40;
                #endregion
                y += 40;

                Widgets.Label(new Rect(0, y + 4, 70, 30), "WeaponStorage.Search".Translate());
                this.textBuffer = Widgets.TextField(new Rect(80, y, 200, 30), this.textBuffer);
                y += 40;

                const int HEIGHT = 30;
                const int BUFFER = 2;
				float width = inRect.width - 100;
				scrollPosition = GUI.BeginScrollView(new Rect(40, y, width, inRect.height - y - 121), scrollPosition, new Rect(0, 0, width - 16, this.PreviousY));
                x = y = 0;
                if (this.PossibleWeapons != null)
                {
                    ThingWithComps weapon;
                    for (int i = 0; i < this.PossibleWeapons.Count; ++i)
                    {
						x = 0;
                        weapon = this.PossibleWeapons[i];
                        bool isChecked = false;

                        if (this.assignedWeapons != null && this.weaponStorage != null)
                        {
                            isChecked = this.IsAssignedWeapon(i);
                            if (!isChecked && !this.IncludeWeapon(weapon))
                                continue;

                            bool backup = isChecked;
                            Widgets.Checkbox(x, y, ref isChecked, 20);
							x += 20 + BUFFER;
                            if (isChecked != backup)
                            {
                                if (this.IsAssignedWeapon(i))
                                {
                                    if (this.assignedWeapons.Pawn.equipment.Primary == weapon)
                                    {
                                        this.assignedWeapons.Pawn.equipment.Remove(weapon);
                                        if (this.assignedWeapons.Pawn.jobs.curJob.def == JobDefOf.Hunt)
                                        {
                                            this.assignedWeapons.Pawn.jobs.StopAll();
                                        }
                                    }
                                    if (this.assignedWeapons.Remove(weapon))
                                    {
                                        if (this.weaponStorage == null ||
                                            (!this.weaponStorage.AddWeapon(weapon) &&
                                             !WorldComp.Add(weapon)))
                                        {
                                            BuildingUtil.DropSingleThing(weapon, this.assignedWeapons.Pawn.Position, this.assignedWeapons.Pawn.Map);
                                        }
                                    }
                                    else
                                    {
                                        Log.Error("Unable to remove weapon " + weapon);
                                    }
                                }
                                else
                                {
                                    if (this.weaponStorage != null && this.weaponStorage.RemoveNoDrop(weapon))
                                    {
                                        this.assignedWeapons.Add(weapon);
                                    }
                                    else
                                    {
                                        Log.Error("Unable to remove weapon " + weapon);
                                    }
                                }
                                this.RebuildPossibleWeapons();
                                break;
                            }
                        }

                        if (!isChecked && !this.IncludeWeapon(weapon))
                            continue;

                        Widgets.ThingIcon(new Rect(x, y, HEIGHT, HEIGHT), weapon);
						x += HEIGHT + BUFFER;

                        if (Widgets.InfoCardButton(x, y, weapon))
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(weapon));
                        }
						x += HEIGHT + BUFFER;

                        Widgets.Label(new Rect(x, y, 250, HEIGHT), weapon.Label);
						x += 250 + BUFFER;

                        if (this.weaponStorage != null && 
                            Widgets.ButtonImage(new Rect(width - 16 - HEIGHT, y, 20, 20), DropTexture))
                        {
                            if (this.IsAssignedWeapon(i))
                            {
                                if (!this.assignedWeapons.Remove(weapon))
                                {
                                    Log.Error("Unable to drop assigned weapon");
                                }
                            }
                            else
                            {
                                if (this.weaponStorage == null || !this.weaponStorage.Remove(weapon))
                                {
                                    Log.Error("Unable to remove weapon " + weapon);
                                }
                            }
                            this.RebuildPossibleWeapons();
                            break;
                        }

                        var biocodableComp = weapon.GetComp<CompBiocodableWeapon>();
                        if (biocodableComp?.CodedPawn != null)
                        {
                            y += HEIGHT - 4;
                            Widgets.Label(new Rect(x - 250 - BUFFER, y, 250, 20), biocodableComp.CompInspectStringExtra());
                            y += 4;
                        }

                        this.PossibleWeapons[i] = weapon;
						y += HEIGHT + BUFFER;
					}
                }
                else
                {
#if TRACE
                    if (i > 600)
                    {
                        Log.Warning("WeaponStorage DoWindowContents: Display non-checkbox weapons. Count: " + this.weaponStorage.Count);
                    }
#endif
                    if (this.weaponStorage != null)
                    {
                        foreach (ThingWithComps t in this.weaponStorage.GetWeapons(false))
                        {
#if TRACE
                        if (i > 600)
                        {
                            Log.Warning("-" + t.Label);
                        }
#endif
                            //if (!IncludeWeapon(t))
                            //    continue;

                            x = 34;
                            Widgets.ThingIcon(new Rect(x, y, HEIGHT, HEIGHT), t);
                            x += HEIGHT + BUFFER;

                            if (Widgets.InfoCardButton(x, y, t))
                            {
                                Find.WindowStack.Add(new Dialog_InfoCard(t));
                            }
                            x += HEIGHT + BUFFER;

                            Widgets.Label(new Rect(x, y, 250, HEIGHT), t.Label);
                            x += 250 + BUFFER;

                            if (Widgets.ButtonImage(new Rect(x + 100, y, 20, 20), DropTexture))
                            {
                                this.weaponStorage.Remove(t);
                                break;
                            }
                            y += HEIGHT + BUFFER;
                        }

                        foreach (ThingWithComps t in this.weaponStorage.GetBioEncodedWeapons())
                        {
#if TRACE
                        if (i > 600)
                        {
                            Log.Warning("-" + t.Label);
                        }
#endif
                            //if (!IncludeWeapon(t))
                            //    continue;

                            x = 34;
                            Widgets.ThingIcon(new Rect(x, y, HEIGHT, HEIGHT), t);
                            x += HEIGHT + BUFFER;

                            if (Widgets.InfoCardButton(x, y, t))
                            {
                                Find.WindowStack.Add(new Dialog_InfoCard(t));
                            }
                            x += HEIGHT + BUFFER;

                            Widgets.Label(new Rect(x, y, 250, HEIGHT), t.Label);
                            x += 250 + BUFFER;

                            if (Widgets.ButtonImage(new Rect(x + 100, y, 20, 20), DropTexture))
                            {
                                this.weaponStorage.Remove(t);
                                break;
                            }

                            var biocodableComp = t.GetComp<CompBiocodableWeapon>();
                            if (biocodableComp?.CodedPawn != null)
                            {
                                y += HEIGHT - 4;
                                Widgets.Label(new Rect(x - 250 - BUFFER, y, 250, 20), biocodableComp.CompInspectStringExtra());
                                y += 4;
                            }

                            y += HEIGHT + BUFFER;
                        }
                    }
                }

                GUI.EndScrollView();
				this.PreviousY = y;
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
            }
        }

        private void NextWeaponStorage(int increment)
        {
            List<Building_WeaponStorage> ws = WorldComp.GetWeaponStorages();
            if (this.weaponStorage == null)
            {
                this.weaponStorage = ws[(increment < 0) ? ws.Count - 1 : 0];
            }
            else
            {
                for (int i = 0; i < ws.Count; ++i)
                {
                    if (this.weaponStorage == ws[i])
                    {
                        i += increment;
                        if (i < 0)
                            this.weaponStorage = ws[ws.Count - 1];
                        else if (i >= ws.Count)
                            this.weaponStorage = ws[0];
                        else
                            this.weaponStorage = ws[i];
                        break;
                    }
                }
            }
            this.RebuildPossibleWeapons();
        }

        private void UpdatePawnIndex(Pawn p)
        {
            this.pawnIndex = -1;
            if (p != null)
            {
                for (pawnIndex = 0; pawnIndex < this.selectablePawns.Count; ++pawnIndex)
                {
                    if (this.selectablePawns[this.pawnIndex].Equals(p))
                    {
                        this.LoadAssignedWeapons();
                        break;
                    }
                }
            }
        }

        private void LoadAssignedWeapons()
        {
            SelectablePawns p = this.selectablePawns[this.pawnIndex];
            if (!WorldComp.AssignedWeapons.TryGetValue(p.Pawn, out this.assignedWeapons))
            {
                this.assignedWeapons = new AssignedWeaponContainer
                {
                    Pawn = p.Pawn
                };
                if (p.Pawn.equipment.Primary != null)
                {
                    this.assignedWeapons.Add(p.Pawn.equipment.Primary);
                }
                WorldComp.AssignedWeapons.Add(p.Pawn, this.assignedWeapons);
            }
            this.RebuildPossibleWeapons();
        }

        private bool IncludeWeapon(ThingWithComps weapon)
        {
            if (this.textBuffer.Length > 0)
            {
                string search = this.textBuffer.ToLower();
                if ((search.StartsWith("mel") || search.StartsWith("mee")) && weapon.def.IsMeleeWeapon)
                    return true;
                else if (search.StartsWith("ran") && weapon.def.IsRangedWeapon)
                    return true;
                else if (weapon.Label.ToLower().Contains(search))
                    return true;
                return false;
            }
            return true;
        }
    }
}
