using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace WeaponStorage
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "WeaponStorage".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

    public class Settings : ModSettings
    {
        //private static ToolDefsLookup ToolDefs = new ToolDefsLookup();

        public static bool ShowWeaponsWhenNotDrafted = false;
        public static int RepairAttachmentDistance = 6;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref ShowWeaponsWhenNotDrafted, "WeaponStorage.ShowWeaponsWhenNotDrafted", false, false);

            /*if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Log.Warning("Root Dir: " + Mod.Content.RootDir);
                ToolDefs.Load(Mod.Content.RootDir + "\\About\\tooldefs.xml");
            }*/
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small);
            l.ColumnWidth = System.Math.Min(400, rect.width / 2);
            l.Begin(rect);
            l.CheckboxLabeled("WeaponStorage.ShowWeaponsWhenNotDrafted".Translate(), ref ShowWeaponsWhenNotDrafted);
            //l.Gap(8);
            l.End();
        }
        
        public static bool IsTool(Thing t)
        {
            if (t != null)
            {
                return IsTool(t.def);
            }
            return false;
        }

        public static bool IsTool(ThingDef def)
        {
            if (def != null)
            {
                return def.defName.StartsWith("RTFTJ");
                //return ToolDefs.IsTool(def);
            }
            return false;
        }

        /*internal class ToolDefsLookup
        {
            private List<string> startsWith = null;
            private List<string> contains = null;
            private List<string> equals = null;

            public ToolDefsLookup() { }

            public void Load(string xmlFile)
            {
                if (this.startsWith != null)
                {
                    this.startsWith.Clear();
                    this.startsWith = null;
                }
                if (this.contains != null)
                {
                    this.contains.Clear();
                    this.contains = null;
                }
                if (this.equals != null)
                {
                    this.equals.Clear();
                    this.equals = null;
                }

                if (!File.Exists(xmlFile))
                {
                    Log.Error("Unable to find file [" + xmlFile + "]");
                    return;
                }

                try
                {
                    using (FileStream fs = File.OpenRead(xmlFile))
                    {
                        XDocument xml = null;
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            xml = XDocument.Parse(sr.ReadToEnd());
                            XElement root = xml.Element("tool_defs");
                            if (root == null)
                            {
                                throw new Exception("Invalid markup, missing root element \"tool_defs\"");
                            }

                            XElement el = root.Element("starts_with");
                            if (el!= null)
                                this.startsWith = this.ParseValue(el.Value);

                            el = root.Element("contains");
                            if (el != null)
                                this.contains = this.ParseValue(el.Value);

                            el = root.Element("equals");
                            if (el != null)
                                this.equals = this.ParseValue(el.Value);
                        }
                    }
                }
                catch(Exception e)
                {
                    Log.Error("Failed to read \"tooldefs.xml\"\n" + e.StackTrace);
                }
            }

            public bool IsTool(ThingDef def)
            {
                if (def != null)
                {
                    string defName = def.defName;
                    if (this.equals != null)
                    {
                        foreach(string s in this.equals)
                        {
                            if (defName.Equals(s))
                            {
                                return true;
                            }
                        }
                    }

                    if (this.startsWith != null)
                    {
                        foreach(string s in this.startsWith)
                        {
                            if (defName.StartsWith(s))
                            {
                                return true;
                            }
                        }
                    }

                    if (this.contains != null)
                    {
                        foreach(string s in this.contains)
                        {
                            if (defName.Contains(s))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            private List<string> ParseValue(string v)
            {
                if (v != null)
                {
                    string[] values = v.Split(',');
                    if (values != null && values.Length > 0)
                    {
                        List<string> rv = null;
                        foreach (string val in values)
                        {
                            string s = val.Trim();
                            if (s.Length > 0)
                            {
                                if (rv == null)
                                {
                                    rv = new List<string>();
                                }
                                rv.Add(s);
                            }
                        }
                        return rv;
                    }
                }
                return null;
            }
        }*/
    }
}
