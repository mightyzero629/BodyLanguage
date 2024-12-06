using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Object = System.Object;

namespace CheesyFX
{
    public class TriggerManager<T> where T : BodyRegionTrigger
    {
        private MVRScript script;
        private Action ClearUI;
        private Action Return;
        public List<object> UIElements = new List<object>();
        public UIDynamicButton addButton;
        public UIDynamicButton removeButton;
        public UIDynamicTwinButton copyPasteButtons;
        public BodyRegionTrigger selectedTrigger;
        public JSONClass cache;
        public string cachedRegion;
        public JSONClass undoCache;
        private string storeName;
        private List<TouchZone> regionsWithTriggers = new List<TouchZone>();
        private JSONStorableString regionInfo = new JSONStorableString("RegionInfo", "");

        public JSONStorableStringChooser regionChooser =
            new JSONStorableStringChooser("regionChooser", BodyRegionMapping.touchZoneNames, BodyRegionMapping.touchZoneNames[0], "Region");

        public JSONStorableBool showRegionsWithTrigger = new JSONStorableBool("Show Only Regions With Triggers", false);

        private JSONStorableAction clear = new JSONStorableAction("Clear", delegate {});
        private int triggerType;
        private bool activeState = true;

        public TriggerManager(MVRScript script, Action ClearUI, Action Return)
        {
            this.script = script;
            this.ClearUI = ClearUI;
            this.Return = Return;
            regionChooser.setCallbackFunction += OnRegionSelected;
            showRegionsWithTrigger.setCallbackFunction += OnShowRegionsChanged;
            clear.actionCallback = Clear;
            switch (typeof(T).ToString())
            {
                case "CheesyFX.TouchTrigger":
                {
                    triggerType = 0;
                    storeName = "TouchTriggers";
                    break;
                }
                case "CheesyFX.SlapTrigger":
                {
                    triggerType = 1;
                    storeName = "SlapTriggers";
                    break;
                }
                case "CheesyFX.WatchTrigger":
                {
                    triggerType = 2;
                    storeName = "WatchTriggers";
                    break;
                }
            }
        }

        private void Start()
        {
            SyncButtons();
        }

        public void SetActive(bool val)
        {
            activeState = val;
            foreach (var region in regionsWithTriggers)
            {
                var trigger = GetTrigger(region);
                trigger.enabled = trigger.enabledJ.val && val;
                if (trigger.enabledJ.toggle)
                {
                    trigger.enabledJ.toggle.interactable = val;
                    var info = trigger.info.val;
                    if(!val && !info.StartsWith("<b>Globally")) trigger.info.val = $"<b>Globally disabled!</b>\n{info}";
                }
            }
        }

        private BodyRegionTrigger GetTrigger(TouchZone touchZone)
        {
            return touchZone.GetTrigger(triggerType);
        }

        public List<object> CreateUI()
        {
            showRegionsWithTrigger.CreateUI(UIElements, rightSide: false);
            regionChooser.CreateUI(UIElements, chooserType:2);
            copyPasteButtons = Utils.SetupTwinButton(script,"Copy", CopySelected, "Paste", Paste, true);
            UIElements.Add(copyPasteButtons);
            addButton = script.SetupButton("Add Trigger", true, AddOrOpenPanel, UIElements);
            addButton.buttonColor = new Color(0.45f, 1f, 0.45f);
            removeButton = script.SetupButton("Remove Trigger", true, DestroySelected, UIElements);
            removeButton.buttonColor = new Color(1f, 0.21f, 0.15f);
            var info = script.CreateTextField(regionInfo);
            info.height = 400f;
            UIElements.Add(info);
            script.SetupButton("Clear", false, clear.actionCallback.Invoke, UIElements);
            SyncButtons();
            return UIElements;
        }

        public void SyncButtons()
        {
            if (selectedTrigger == null)
            {
                addButton.label = "Add Trigger";
                removeButton.gameObject.SetActive(false);
            }
            else
            {
                addButton.label = "Open Panel";
                removeButton.gameObject.SetActive(true);
                removeButton.buttonColor = new Color(1f, 0.21f, 0.15f);
                removeButton.SetCallback(DestroySelected);
                removeButton.label = "Remove Trigger";
            }
            if(cache != null) copyPasteButtons.labelRight.text = $"Paste <b>{cachedRegion}</b>";
            else copyPasteButtons.labelRight.text = $"Paste";
        }

        public void AddOrOpenPanel()
        {
            if(selectedTrigger == null)
            {
                selectedTrigger = script.gameObject.AddComponent<T>().Init(script, BodyRegionMapping.touchZones[regionChooser.val]);
                if (!activeState) selectedTrigger.enabled = false;
                SetRegionInfo();
                SyncButtons();
            }
            else
            {
                ClearUI();
                selectedTrigger.OpenPanel(script, Return);
                if (!activeState)
                {
                    selectedTrigger.enabledJ.toggle.interactable = false;
                    var info = selectedTrigger.info.val;
                    if(!info.StartsWith("<b>Globally")) selectedTrigger.info.val = $"<b>Globally disabled!</b>\n{info}";
                }
                else
                {
                    selectedTrigger.info.val = selectedTrigger.info.val.Replace("<b>Globally disabled!</b>\n", "");
                }
            }
        }

        public void DestroySelected()
        {
            undoCache = selectedTrigger.Store(script.subScenePrefix);
            selectedTrigger.region.DestroyTrigger(triggerType);
            selectedTrigger = null;
            addButton.label = "Add Trigger";
            removeButton.SetCallback(Undo);
            removeButton.buttonColor = new Color(0.45f, 1f, 0.45f);
            removeButton.label = "Undo";
            SetRegionInfo();
        }

        private void Clear()
        {
            foreach (var region in regionsWithTriggers)
            {
                region.DestroyTrigger(triggerType);
            }
            selectedTrigger = null;
            SyncButtons();
            SetRegionInfo();
        }

        private void SetRegionInfo()
        {
            regionsWithTriggers = BodyRegionMapping.touchZones.Values.Where(x => GetTrigger(x) != null).ToList();
            regionInfo.val = string.Join("\n", regionsWithTriggers.Select(x => x.name).ToArray());
        }

        public void OnRegionSelected(string name)
        {
            selectedTrigger = GetTrigger(BodyRegionMapping.touchZones[name]);
            if(addButton != null) SyncButtons();
        }

        public void OnShowRegionsChanged(bool val)
        {
            if (val)
            {
                regionChooser.choices = BodyRegionMapping.touchZones.Values.Where(x => GetTrigger(x) != null).Select(x => x.name).ToList();
                if (!regionChooser.choices.Contains(regionChooser.val))
                {
                    if (regionChooser.choices.Count == 0)
                    {
                        regionChooser.valNoCallback = string.Empty;
                        script.RemoveButton(addButton);
                        script.RemoveButton(removeButton);
                    }
                    else regionChooser.val = regionChooser.choices[0];
                }
            }
            else
            {
                regionChooser.choices = BodyRegionMapping.touchZoneNames;
                regionChooser.val = selectedTrigger.region.name;
            }
        }

        public JSONClass Store()
        {
            JSONClass jc = new JSONClass();
            foreach (var tz in BodyRegionMapping.touchZones.Values.ToList())
            {
                if (GetTrigger(tz) != null) jc[tz.name] = GetTrigger(tz).Store(script.subScenePrefix);
            }
            return jc;
        }
        
        public void Load(JSONClass jc)
        {
            if (jc.HasKey(storeName))
            {
                foreach (var name in jc[storeName].AsObject.Keys)
                {
                    selectedTrigger = script.gameObject.AddComponent<T>()
                        .Init(script, BodyRegionMapping.touchZones[name]);
                    selectedTrigger.Load(jc[storeName][name].AsObject, script.subScenePrefix);
                }
            }
            SetRegionInfo();
            if (regionsWithTriggers.Count > 0) regionChooser.val = regionsWithTriggers[0].name;
        }

        public void CopySelected()
        {
            if(string.IsNullOrEmpty(regionChooser.val) || selectedTrigger == null)
            {
                $"TouchMe: Region '{regionChooser.val}' has no {typeof(T).ToString().Replace("CheesyFX.", "")} to copy.".Print();
                return;
            }
            cache = selectedTrigger.Store(script.subScenePrefix);
            cachedRegion = regionChooser.val;
            // script.SaveJSON(cache, "Saves/cache.json");
            SyncButtons();
        }

        public void Paste()
        {
            if (cache == null)
            {
                $"{script.name}: Copy a {typeof(T).ToString().Replace("CheesyFX.", "")} first.".Print();
                return;
            }
            if (string.IsNullOrEmpty(regionChooser.val))
            {
                $"{script.name}: First select a region to copy to.".Print();
                return;
            }
            selectedTrigger = script.gameObject.AddComponent<T>().Init(script, BodyRegionMapping.touchZones[regionChooser.val]);
            JSONClass jc = new JSONClass();
            foreach (var key in cache.Keys)
            {
                jc[key.Replace(cachedRegion, regionChooser.val)] = cache[key];
            }
            selectedTrigger.Load(jc, script.subScenePrefix);
            SyncButtons();
            SetRegionInfo();
        }

        public void Undo()
        {
            AddOrOpenPanel();
            selectedTrigger.Load(undoCache, script.subScenePrefix);
            SyncButtons();
            removeButton.buttonColor = new Color(1f, 0.21f, 0.15f);
            SetRegionInfo();
        }
    }
}