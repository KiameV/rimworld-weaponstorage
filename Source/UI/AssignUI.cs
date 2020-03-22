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
        #endregion

        private readonly Building_WeaponStorage weaponStorage;
		
        private AssignedWeaponContainer assignedWeapons = null;

        private List<ThingWithComps> PossibleWeapons = null;
		private IEnumerable<SelectablePawns> selectablePawns;

        private Vector2 scrollPosition = new Vector2(0, 0);

        private string textBuffer = "";

		private float PreviousY = 0;

        public AssignUI(Building_WeaponStorage weaponStorage)
        {
            this.weaponStorage = weaponStorage;

            this.PossibleWeapons = null;

            this.closeOnClickedOutside = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;

			this.selectablePawns = Util.GetPawns(true);
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
            
            int size;
            if (this.assignedWeapons != null)
            {
                size = this.weaponStorage.Count;
            }
            else
            {
                size = this.assignedWeapons.Count + this.weaponStorage.Count;
            }

            this.PossibleWeapons = new List<ThingWithComps>(size);
            if (this.assignedWeapons != null)
            {
                foreach (ThingWithComps w in this.assignedWeapons.Weapons)
                {
                    this.PossibleWeapons.Add(w);
                }
            }
            foreach (ThingWithComps w in this.weaponStorage.AllWeapons)
            {
                this.PossibleWeapons.Add(w);
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
                Widgets.Label(new Rect(0, 4, 100, 30), "WeaponStorage.AssignTo".Translate());
                string label = (this.assignedWeapons != null) ? this.assignedWeapons.Pawn.Name.ToStringShort : "Pawn";
                if (Widgets.ButtonText(new Rect(120, 0, 200, 30), label))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (SelectablePawns p in this.selectablePawns)
                    {
                        options.Add(new FloatMenuOption(p.LabelAndStats, delegate
                        {
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
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                Widgets.Label(new Rect(350, 4, 70, 30), "WeaponStorage.Search".Translate());
                this.textBuffer = Widgets.TextField(new Rect(425, 0, 100, 30), this.textBuffer);

                const int HEIGHT = 30;
                const int BUFFER = 2;
				float width = inRect.width - 100;
				float x = 0, y = 0;
				scrollPosition = GUI.BeginScrollView(new Rect(40, 40, width, inRect.height - y - 85), scrollPosition, new Rect(0, 0, width - 16, this.PreviousY));
                if (this.PossibleWeapons != null)
                {
                    ThingWithComps weapon;
                    for (int i = 0; i < this.PossibleWeapons.Count; ++i)
                    {
						x = 0;
                        weapon = this.PossibleWeapons[i];
                        if (!IncludeWeapon(weapon))
                            continue;

                        if (this.assignedWeapons != null)
                        {
                            bool isChecked = this.IsAssignedWeapon(i);
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
                                        if (!this.weaponStorage.AddWeapon(weapon) &&
                                            !WorldComp.Add(weapon))
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
                                    if (this.weaponStorage.RemoveNoDrop(weapon))
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

                        Widgets.ThingIcon(new Rect(x, y, HEIGHT, HEIGHT), weapon);
						x += HEIGHT + BUFFER;

                        if (Widgets.InfoCardButton(x, y, weapon))
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(weapon));
                        }
						x += HEIGHT + BUFFER;

                        Widgets.Label(new Rect(x, y, 250, HEIGHT), weapon.Label);
						x += 250 + BUFFER;

						if (Widgets.ButtonImage(new Rect(width - 16 - HEIGHT, y, 20, 20), DropTexture))
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
                                if (!this.weaponStorage.Remove(weapon))
                                {
                                    Log.Error("Unable to remove weapon " + weapon);
                                }
                            }
                            this.RebuildPossibleWeapons();
                            break;
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
                    foreach (ThingWithComps t in this.weaponStorage.AllWeapons)
                    {
#if TRACE
                        if (i > 600)
                        {
                            Log.Warning("-" + t.Label);
                        }
#endif
                        if (!IncludeWeapon(t))
                            continue;

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
