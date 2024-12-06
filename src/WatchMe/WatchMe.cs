using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MVR.FileManagementSecure;
using MVR.Hub;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class WatchMe : MVRScript
    {
        public static WatchMe singleton;
        private UnityEventsListener uiListener;
        private PresetSystem presetSystem;
        public JSONStorableBool detailedViewScan = new JSONStorableBool("Detailed ViewScan", true);
        private JSONStorableBool viewScanDebug = new JSONStorableBool("Debug", false);
        private JSONStorableString info = new JSONStorableString("info", "");
        public HashSet<BodyRegion> regionsWatched = new HashSet<BodyRegion>();
        private TriggerManager<WatchTrigger> watchTriggerManager;

        private List<object> UIElements = new List<object>();
        private UIDynamicTabBar tabbar;
        private int lastTabId;
        
        public static Collider viewTrigger;
        public static JSONStorable viewScan;
        private JSONStorableFloat viewScanUses;

        private void OnEnable()
        {
            if (viewScanUses != null) viewScanUses.val++;
        }

        public override void Init()
        {
            if (FillMeUp.abort) return;
            UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements);
            Utils.OnInitUI(CreateUIElement);
            singleton = this;
            watchTriggerManager = new TriggerManager<WatchTrigger>(this, ClearUI, CreateUI);
            presetSystem = new PresetSystem(this)
            {
                saveDir = "Saves/PluginData/CheesyFX/BodyLanguage/WatchMe/",
                Store = Store,
                Load = Load
            };
            presetSystem.Init();
            CreateUI();
            if (!FindViewScan("CheesyFX.ViewScan", out viewScan))
            {
                ClearUI();
                CreateMissingViewScanUI();
                return;
            }
            FindViewScan("CheesyFX.ViewScan", out viewScan);
            viewScanUses = viewScan.GetFloatJSONParam("uses");
            viewScanUses.val ++;
            var go = GameObject.Find("CheesyFX.ViewTrigger");
            if (go == null)
            {
                SuperController.LogError("WatchMe: ViewTrigger not found. Update FocusOnMe!");
            }
            viewTrigger = go.GetComponent<Collider>();
            viewScanDebug.setCallbackFunction += val => viewScan.SetBoolParamValue("Debug", val);
            foreach (var orifice in FillMeUp.orifices)
            {
                Physics.IgnoreCollision(orifice.enterTriggerCollider, viewTrigger, true);
                Physics.IgnoreCollision(orifice.proximityTrigger, viewTrigger, true);
            }
            foreach (var hand in FillMeUp.hands)
            {
                Physics.IgnoreCollision(hand.proximityTrigger, viewTrigger, true);
            }
            // DeferredInit().Start();
        }
        
        public override void InitUI()
        {
            base.InitUI();
            if(UITransform == null || uiListener != null || FillMeUp.abort) return;
            uiListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
            uiListener.onEnabled.AddListener(() => UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements));
            uiListener.onEnabled.AddListener(() => Utils.OnInitUI(CreateUIElement));
        }

        private IEnumerator DeferredInit()
        {
            yield return new WaitForEndOfFrame();
            FindViewScan("CheesyFX.ViewScan", out viewScan);
            viewScanUses = viewScan.GetFloatJSONParam("uses");
            viewScanUses.val ++;
            viewTrigger = GameObject.Find("CheesyFX.ViewTrigger").GetComponent<Collider>();
            viewScanDebug.setCallbackFunction += val => viewScan.SetBoolParamValue("Debug", val);
            foreach (var orifice in FillMeUp.orifices)
            {
                Physics.IgnoreCollision(orifice.enterTriggerCollider, viewTrigger, true);
                Physics.IgnoreCollision(orifice.proximityTrigger, viewTrigger, true);
                orifice.enterTriggerCollider.NullCheck();
            }
            foreach (var hand in FillMeUp.hands)
            {
                Physics.IgnoreCollision(hand.proximityTrigger, viewTrigger, true);
            }
            ClearUI();
            CreateUI();
        }

        private void OnDisable()
        {
            if (viewScanUses != null) viewScanUses.val--;
        }
        
        private void OnDestroy()
        {
            Destroy(uiListener);
        }
        
        private void Update()
        {
            foreach (var region in regionsWatched)
            {
                region.timeWatched += Time.deltaTime;
                // $"{region.name} {region.timeWatched}".Print();
            }
            if (UIManager.UIOpened && tabbar.id == 0)
            {
                info.val = string.Join("\n", regionsWatched.Select(x => $"{x.name}: {x.timeWatched:0.00}").ToArray());
            }
        }
        
        public void ClearUI()
        {
            UIManager.RemoveUIElements(leftUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
            UIManager.RemoveUIElements(rightUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
        }

        public void CreateUI()
        {
            presetSystem.CreateUI();
            tabbar = UIManager.CreateTabBar(new [] {"Info", "WatchTriggers"}, SelectTab, script:this);
            if(viewScan != null) tabbar.SelectTab(lastTabId);
        }
        
        private void SelectTab(int id)
        {
            lastTabId = id;
            UIManager.RemoveUIElements(UIElements);
            if (id == 0) CreateInfoUI();
            else if (id == 1) UIElements = watchTriggerManager.CreateUI();
        }

        private void CreateInfoUI()
        {
            viewScanDebug.valNoCallback = viewScan.GetBoolParamValue("Debug");
            viewScanDebug.CreateUI(UIElements);
            var infoField = CreateTextField(info, true);
            infoField.height = 600f;
            UIElements.Add(infoField);
        }

        private void CreateMissingViewScanUI()
        {
            this.SetupButton("Install FocusOnMe", false, InstallFocusOnMe);
            this.SetupButton("Download FocusOnMe", true, DownloadFocusOnMe);
            var textfield = CreateTextField(new JSONStorableString("bla",
                "This module requires <b>CheesyFX.FocusOnMe!</b> being installed as a session plugin. " +
                "You don't have to use it's core features, but it has to be present to scan what you're looking at. Use the above buttons to download and install it.\n\n" +
                "But why don't you give it a try after installing it? You'll be amazed!\n"+
                "<b>Dynamic lighting</b>: Numpad+ or the button next to the VaM version.\n"+
                "<b>Dynamic DoF</b>: F8"
                ));
            textfield.height = 600f;
            UIElements.Add(textfield);
        }
        

        private JSONClass Store()
        {
            var jc = new JSONClass();
            return jc;
        }

        private void Load(JSONClass jc)
        {
            watchTriggerManager.Load(jc);
        }
        
        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass jc = base.GetJSON(forceStore:true);
            jc["WatchTriggers"] = watchTriggerManager.Store();
            return jc;
        }
        
        public override void LateRestoreFromJSON(
            JSONClass jc,
            bool restorePhysical,
            bool restoreAppearance,
            bool setMissingToDefault
        )
        {
            Load(jc);
        }
        
        public static bool FindViewScan(string name, out JSONStorable plugin)
        {
            plugin = null;
            Transform sessionPlugins = GameObject.Find("SceneAtoms/CoreControl/SessionPluginManagerContainer/CoreControl/SessionPluginManager/Plugins")?.transform;
            if (sessionPlugins == null) return false;
            foreach (Transform child in sessionPlugins)
            {
                if (child.name.EndsWith(name))
                {               
                    plugin = child.GetComponent<MVRScript>();
                    return true;
                }
            }
            return false;
        }

        private void InstallFocusOnMe()
        {
            var pluginManager = GameObject.Find("SceneAtoms/CoreControl/SessionPluginManagerContainer/CoreControl/SessionPluginManager")
                .GetComponent<MVRPluginManager>();
            var plugin = pluginManager.CreatePlugin();
            var packageUid = GetLatestPackageUid("CheesyFX.FocusOnMe!");
            string url;
            if (packageUid != null)
            {
                url = packageUid + ":/" + "Custom/Scripts/CheesyFX/FocusOnMe/FocusOnMe!.cslist";
                plugin.pluginURLJSON.val = url;
                DeferredInit().Start();
            }
            else
            {
                "CheesyFX.FocusOnMe!.var not found. Download it first.".Print();
            }
        }

        private void DownloadFocusOnMe()
        {
            HubDownloader.singleton.FindPackage("CheesyFX.FocusOnMe!.latest", true);
        }
        
        public static string GetLatestPackageUid(string packageUid){
            int version = FileManagerSecure.GetPackageVersion(packageUid+".latest");
            if (version == -1) return null;
            return $"{packageUid}.{version}";
        }
    }
}