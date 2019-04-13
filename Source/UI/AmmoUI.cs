using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace WeaponStorage.UI
{
    [StaticConstructorOnStartup]
    public class AmmoUI : Window
    {
        private readonly Building_WeaponStorage weaponStorage;

        private Vector2 scrollPosition = new Vector2(0, 0);

        private bool performSearch = false;
        private string searchText = "";

        private float PreviousY = 0;

        private enum Tabs
        {
            Empty,
            WeaponStorage_General,
            WeaponStorage_Neolithic,
            WeaponStorage_Grenades,
            WeaponStorage_Rockets,
            WeaponStorage_Shotguns,
            WeaponStorage_Advanced,
        };

        private List<ThingDefCount> ammo = new List<ThingDefCount>();
        private List<ThingDefCount> searchResults = new List<ThingDefCount>();

        /*private List<TabRecord> tabs = new List<TabRecord>();
        private Tabs selectedTab = Tabs.WeaponStorage_General;

        private List<ThingDefCount> general = new List<ThingDefCount>();
        private List<ThingDefCount> neolithic = new List<ThingDefCount>();
        private List<ThingDefCount> grenades = new List<ThingDefCount>();
        private List<ThingDefCount> rockets = new List<ThingDefCount>();
        private List<ThingDefCount> shotguns = new List<ThingDefCount>();
        private List<ThingDefCount> advanced = new List<ThingDefCount>();*/

        public AmmoUI(Building_WeaponStorage weaponStorage)
        {
            this.weaponStorage = weaponStorage;

            this.closeOnClickedOutside = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;

            this.RebuildItems();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(650f, 600f);
            }
        }

        private void RebuildItems()
        {
            ammo.Clear();
            foreach (ThingDefCount tc in CombatExtendedUtil.GetThingCounts())
            {
                ammo.Add(tc);
            }
            /*bool hasCount = false;
            general.Clear();
            neolithic.Clear();
            grenades.Clear();
            rockets.Clear();
            shotguns.Clear();
            advanced.Clear();
            foreach (ThingDefCount tc in CombatExtendedUtil.GetThingCounts())
            {
                if (tc.Count > 0)
                {
                    hasCount = true;

                    string name = tc.ThingDef.GetType().GetField("ammoClass", BindingFlags.Instance | BindingFlags.Public).GetValue(tc.ThingDef).ToString();
                    if (name.StartsWith("Rocket"))
                        this.rockets.Add(tc);
                    else if (name.StartsWith("Grenade"))
                        this.grenades.Add(tc);
                    else if (name.EndsWith("Shot") ||
                             name.EndsWith("Slug") ||
                             name.Equals("Beanbag"))
                        this.shotguns.Add(tc);
                    else if (name.Equals("Javelin") ||
                             name.EndsWith("Arrow") ||
                             name.EndsWith("SlingBullet") ||
                             name.EndsWith("CrossbowBolt"))
                        this.neolithic.Add(tc);
                    else if (name.StartsWith("Charged") ||
                             name.Equals("Ionized") ||
                             name.Equals("IncendiaryFuel") ||
                             name.Equals("ThermobaricFuel") ||
                             name.Equals("FoamFuel") ||
                             name.Equals("Antigrain") ||
                             name.Equals("RadiationIonising"))
                        this.advanced.Add(tc);
                    else
                        this.general.Add(tc);
                }
            }

            if (!hasCount)
            {
                this.selectedTab = Tabs.Empty;
            }*/
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
                float y = 0;

                Widgets.Label(new Rect(0, y, 100, 30), "WeaponStorage.ManageAmmo".Translate());
                y += 32;

                Widgets.Label(new Rect(0, y + 4, 70, 30), "WeaponStorage.Search".Translate());
                string original = searchText;
                this.searchText = Widgets.TextField(new Rect(75, y, 150, 30), this.searchText);
                if (!this.searchText.Equals(original))
                    this.performSearch = true;
                y += 44;

                List<ThingDefCount> toShow = this.GetThingsToShow();
                if (toShow == null || toShow.Count == 0)
                {
                    string text = "No ammo in storage";
                    if (this.searchText.Length > 0)
                        text = "No matches found";
                    Widgets.Label(new Rect(40, y, 200, 30), text);
                }
                else
                {
                    /*if (this.searchText.Length == 0)
                    {
                        y += 32;
                        TabDrawer.DrawTabs(new Rect(0, y, inRect.width, inRect.height - y), this.tabs);
                        y += 20;
                    }*/

                    Widgets.BeginScrollView(
                        new Rect(40, y, inRect.width - 80, inRect.height - y - 50),
                        ref this.scrollPosition,
                        new Rect(0, 0, inRect.width - 96, this.PreviousY));

                    this.PreviousY = 0f;
                    foreach (var tc in toShow)
                    {
                        ThingDef def = tc.ThingDef;
                        Widgets.ThingIcon(new Rect(0f, this.PreviousY, 30, 30), def);

                        if (Widgets.InfoCardButton(40, this.PreviousY, def))
                            Find.WindowStack.Add(new Dialog_InfoCard(def));

                        Widgets.Label(new Rect(70, this.PreviousY, 250, 30), def.label);

                        Widgets.Label(new Rect(340, this.PreviousY, 40, 30), tc.Count.ToString());

                        if (Widgets.ButtonImage(new Rect(inRect.width - 100, this.PreviousY, 20, 20), AssignUI.DropTexture))
                        {
                            CombatExtendedUtil.DropAmmo(def, this.weaponStorage);
                            RebuildItems();
                            this.performSearch = true;
                            break;
                        }
                        this.PreviousY += 32;
                    }
                    Widgets.EndScrollView();
                }
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

        private List<ThingDefCount> GetThingsToShow()
        {
            if (this.performSearch)
            {
                this.performSearch = false;
                this.searchResults.Clear();

                if (!this.searchText.Trim().NullOrEmpty())
                {
                    string text = this.searchText.ToLower().Trim();
                    foreach (ThingDefCount a in this.ammo)
                    {
                        if (a.ThingDef.label.ToLower().IndexOf(text) != -1 ||
                            a.ThingDef.defName.ToLower().IndexOf(text) != -1)
                        {
                            this.searchResults.Add(a);
                        }
                    }
                }
            }

            if (this.searchResults.Count > 0)
                return this.searchResults;
            if (this.searchText.Trim().NullOrEmpty())
                return this.ammo;
            return null;
            /*List<ThingDefCount> toShow = new List<ThingDefCount>();
            if (this.searchText.Length > 0)
            {
                if (this.performSearch)
                {
                    toShow?.Clear();
                    toShow = new List<ThingDefCount>();
                    foreach (var kv in CombatExtendedUtil.Ammo)
                    {
                        if (kv.Key.label.IndexOf(this.searchText) != -1 ||
                            kv.Key.defName.IndexOf(this.searchText) != -1)
                        {
                            toShow.Add(new ThingDefCount(kv.Key, kv.Value));
                        }
                    }
                    this.performSearch = false;
                }
                return toShow;
            }

            if (this.searchText.Length == 0 && this.performSearch)
                toShow.Clear();

            this.tabs.Clear();
            if (this.general.Count > 0)
            {
                this.tabs.Add(this.CreateTabRecord(Tabs.WeaponStorage_General));
            }
            if (this.neolithic.Count > 0)
            {
                this.tabs.Add(this.CreateTabRecord(Tabs.WeaponStorage_Neolithic));
            }
            if (this.grenades.Count > 0)
            {
                this.tabs.Add(this.CreateTabRecord(Tabs.WeaponStorage_Grenades));
            }
            if (this.rockets.Count > 0)
            {
                this.tabs.Add(this.CreateTabRecord(Tabs.WeaponStorage_Rockets));
            }
            if (this.shotguns.Count > 0)
            {
                this.tabs.Add(this.CreateTabRecord(Tabs.WeaponStorage_Shotguns));
            }
            if (this.advanced.Count > 0)
            {
                this.tabs.Add(this.CreateTabRecord(Tabs.WeaponStorage_Advanced));
            }

            switch (this.selectedTab)
            {
                case Tabs.WeaponStorage_Advanced:
                    toShow = this.advanced;
                    break;
                case Tabs.WeaponStorage_General:
                    toShow = this.general;
                    break;
                case Tabs.WeaponStorage_Grenades:
                    toShow = this.grenades;
                    break;
                case Tabs.WeaponStorage_Neolithic:
                    toShow = this.neolithic;
                    break;
                case Tabs.WeaponStorage_Rockets:
                    toShow = this.rockets;
                    break;
                case Tabs.WeaponStorage_Shotguns:
                    toShow = this.shotguns;
                    break;
                default:
                    this.selectedTab = Tabs.Empty;
                    toShow = null;
                    break;
            }
            return toShow;*/
        }

        /*private TabRecord CreateTabRecord(Tabs tab)
        {
            return new TabRecord(
                tab.ToString().Translate(),
                delegate {
                    if (this.selectedTab != tab)
                    {
                        this.selectedTab = tab;
                        this.scrollPosition = new Vector2(0, 0);
                    }
                },
                this.selectedTab == tab);
        }*/
    }
}
