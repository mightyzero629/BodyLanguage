using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class TouchMe : MVRScript
    {
        public static TouchMe singleton;
        public static string packageUid => FillMeUp.packageUid;
        private UnityEventsListener uiListener;
        private PresetSystem presetSystem;

        private TriggerManager<SlapTrigger> slapTriggerManager;
        private TriggerManager<TouchTrigger> touchTriggerManager;

        // private SlapHandler slapHandler;
        

        private List<object> UIElements = new List<object>();
        private UIDynamicTabBar tabbar;
        private int lastTabId;
        
        public static List<Atom> excludedAtoms = new List<Atom>();
        private static JSONStorableString excludedInfo = new JSONStorableString("excludeInfo", "<b>Excluded Atoms</b>\n");

        private static JSONStorableStringChooser excludedChooser =
            new JSONStorableStringChooser("atomChooser", null, "", "... this Atom");
        public static JSONStorableBool enableTouchTriggers = new JSONStorableBool("Enable Touch Triggers", true);
        public static JSONStorableBool enableSlapTriggers = new JSONStorableBool("Enable Slap Triggers", true);

        private JSONClass Store()
        {
            var jc = new JSONClass();
            jc["NippleManager"] = NippleManager.Store();
            jc["SlapHandler"] = SlapHandler.Store();
            return jc;
        }
        
        private void Load(JSONClass jc)
        {
            SlapHandler.Load(jc);
            NippleManager.Load(jc);
            touchTriggerManager.Load(jc);
            slapTriggerManager.Load(jc);
        }

        public override void Init()
        {
            // FillMeUp.singleton.NullCheck();
            // ReferenceEquals(FillMeUp.singleton, null).Print();
            if (FillMeUp.abort) return;
            singleton = this;
            // packageUid = Utils.GetPackagePath(this);
            UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements);
            Utils.OnInitUI(CreateUIElement);

            SlapHandler.Init();
            NippleManager.Init();
            slapTriggerManager = new TriggerManager<SlapTrigger>(this, ClearUI, CreateUI);
            touchTriggerManager = new TriggerManager<TouchTrigger>(this, ClearUI, CreateUI);
            
            excludedChooser.choices = SuperController.singleton.GetAtomUIDs();
            SuperController.singleton.onAtomAddedHandlers += AddExclusionChoice;
            SuperController.singleton.onAtomUIDsChangedHandlers += SyncAtomNames;
            
            RegisterString(excludedInfo);
            excludedInfo.isRestorable = false;
            RegisterStringChooser(excludedChooser);
            RegisterBool(enableTouchTriggers);
            RegisterBool(enableSlapTriggers);
            
            BodyRegionMapping.touchZones["Labia"].touchCollisionListener.onStayEvent.AddListener(() =>ReadMyLips.Stimulate(20f*Time.fixedDeltaTime, doStim:true));
            BodyRegionMapping.touchZones["Labia"].touchCollisionListener.onEnterEvent.AddListener(() =>ReadMyLips.Stimulate(.5f, doStim:true));
            BodyRegionMapping.touchZones["lNipple"].touchCollisionListener.onStayEvent.AddListener(() =>ReadMyLips.Stimulate(0.5f*Time.fixedDeltaTime, doStim:true));
            BodyRegionMapping.touchZones["rNipple"].touchCollisionListener.onStayEvent.AddListener(() =>ReadMyLips.Stimulate(0.5f*Time.fixedDeltaTime, doStim:true));
            BodyRegionMapping.touchZones["lAreola"].touchCollisionListener.onStayEvent.AddListener(() =>ReadMyLips.Stimulate(0.5f*Time.fixedDeltaTime, doStim:true));
            BodyRegionMapping.touchZones["rAreola"].touchCollisionListener.onStayEvent.AddListener(() =>ReadMyLips.Stimulate(0.5f*Time.fixedDeltaTime, doStim:true));
            // BodyRegionMapping.touchZones["rThumb"].touchCollisionListener.onEnterEvent.AddListener(() =>FillMeUp.rHand.SpreadThumb());
            // BodyRegionMapping.touchZones["lThumb"].touchCollisionListener.onEnterEvent.AddListener(() =>FillMeUp.lHand.SpreadThumb());

            enableTouchTriggers.setCallbackFunction += touchTriggerManager.SetActive;
            enableSlapTriggers.setCallbackFunction += slapTriggerManager.SetActive;
            
            presetSystem = new PresetSystem(this)
            {
                saveDir = "Saves/PluginData/CheesyFX/BodyLanguage/TouchMe/",
                Store = Store,
                Load = Load
            };
            presetSystem.Init();

            ExcludeDefaults();
            CreateUI();
            // containingAtom.rigidbodies.FirstOrDefault(x => x.name == "Gen1").NullCheck();
        }
        
        public override void InitUI()
        {
            base.InitUI();
            if(UITransform == null || uiListener != null || FillMeUp.abort) return;
            uiListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
            uiListener.onEnabled.AddListener(() => UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements));
            uiListener.onEnabled.AddListener(() => Utils.OnInitUI(CreateUIElement));
        }

        public static void OnEnter(TouchZone touchZone, Collision collision)
        {
            ContactPoint contactPoint = collision.contacts[0];
            Vector3 velocity = collision.relativeVelocity;
            float intensity = Math.Abs(Vector3.Dot(velocity, contactPoint.normal));
            if (intensity >= SlapHandler.sexSlapThreshold) SlapHandler.Slap(touchZone, collision, intensity, contactPoint);
            // var collidingAtom = collision.collider.GetAtom();
            // if (!FillMeUp.penetratingAtoms.Values.Contains(collidingAtom) && collidingAtom.IsToyOrDildo() || BodyRegionMapping.GetParentNames(collision.rigidbody).Contains("Hands"))
            // {
            //     PoseMe.gaze.Focus(collision.collider);
            // }
        }

        private void OnDestroy()
        {
            foreach(TouchZone touchZone in BodyRegionMapping.touchZones.Values.ToList()){
                touchZone.Destroy();
            }
            AudioImporter.UnloadBundles();
            SlapHandler.Destroy();
            SuperController.singleton.onAtomAddedHandlers -= AddExclusionChoice;
            SuperController.singleton.onAtomUIDsChangedHandlers -= SyncAtomNames;
            Destroy(uiListener);
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass jc = base.GetJSON();
            jc["NippleManager"] = NippleManager.Store();
            jc["SlapHandler"] = SlapHandler.Store();
            jc["TouchTriggers"] = touchTriggerManager.Store();
            jc["SlapTriggers"] = slapTriggerManager.Store();
            return jc;
        }

        public override void LateRestoreFromJSON(
            JSONClass jc,
            bool restorePhysical,
            bool restoreAppearance,
            bool setMissingToDefault
        )
        {
            if (jc.HasKey("excludeInfo"))
            {
                ResetExcluded();
                var uidsToIgnore = ((string)jc["excludeInfo"]).Split(new [] {"\n"}, StringSplitOptions.None).Skip(1);
                foreach (var uid in uidsToIgnore)
                {
                    Exclude(uid, true);
                }
            }
            Load(jc);
        }

        public void CreateUI()
        {
            presetSystem.CreateUI();
            tabbar = UIManager.CreateTabBar(new [] {"General", "Slaps", "Nipples", "TouchTriggers", "SlapTriggers"}, SelectTab, script:this);
            tabbar.SelectTab(lastTabId);
        }
        
        public void ClearUI()
        {
            UIManager.RemoveUIElements(leftUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
            UIManager.RemoveUIElements(rightUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
        }

        private static List<object> CreateGeneralUI()
        {
            List<object> UIElements = new List<object>();
            var button = TouchMe.singleton.CreateButton("Disable Touches with...");
            button.button.onClick.AddListener(() => Exclude(excludedChooser.val, true));
            UIElements.Add(button);
	        
            button = TouchMe.singleton.CreateButton("Enable Touches with...");
            button.button.onClick.AddListener(() => Exclude(excludedChooser.val, false));
            UIElements.Add(button);
	        
            excludedChooser.CreateUI(UIElements:UIElements, rightSide: true, chooserType:2);
            button = TouchMe.singleton.CreateButton("Reset");
            button.button.onClick.AddListener(ResetExcluded);
            UIElements.Add(button);
	        
            var textfield = TouchMe.singleton.CreateTextField(excludedInfo, true);
            textfield.height = 400f;
            UIElements.Add(textfield);

            enableTouchTriggers.CreateUI(UIElements, false);
            enableSlapTriggers.CreateUI(UIElements, false);
            return UIElements;
        }

        private void SelectTab(int id)
        {
            lastTabId = id;
            UIManager.RemoveUIElements(UIElements);
            if(id == 0) UIElements = CreateGeneralUI();
            else if(id == 1) UIElements = SlapHandler.CreateUI();
            else if (id == 2) UIElements = NippleManager.CreateUI();
            else if (id == 3) UIElements = touchTriggerManager.CreateUI();
            else UIElements = slapTriggerManager.CreateUI();
        }
        
        public static void Exclude(string uid, bool val)
        {
            Atom atom = SuperController.singleton.GetAtomByUid(uid);
            if (atom == null) return;
            if (val)
            {
                if(!excludedAtoms.Contains(atom))
                {
                    excludedAtoms.Add(atom);
                    excludedInfo.val += atom.uid + "\n";
                }
            }
            else if(excludedAtoms.Remove(atom))
            { 
                excludedInfo.val = "<b>Excluded Atoms</b>\n"+string.Join("\n", excludedAtoms.Select(x => x.uid).ToArray());
            }
        }
        
        private static void AddExclusionChoice(Atom atom)
        {
            if (excludedChooser.choices.Contains(atom.uid)) return;
            var choices = excludedChooser.choices;
            choices.Insert(0, atom.uid);
            excludedChooser.choices = new List<string>(choices);
        }
        
        private static void ExcludeDefaults()
        {
            string[] typesToExclude = {"Panel", "Slate", "Wall", "Glass", "CustomUnityAsset", "Apt", "Cyberpunk", "Deco"};
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                if(typesToExclude.Any(x => atom.type.Contains(x))) Exclude(atom.uid, true);
            }
        }
        
        private static void SyncAtomNames(List<string> uids)
        {
            excludedChooser.choices = new List<string>(uids);
            var ignoredUids = excludedAtoms.Select(x => x.uid).ToArray();
            excludedInfo.val = "<b>Excluded Atoms</b>\n"+string.Join("\n", ignoredUids);
        }
        
        public static void ResetExcluded()
        {
            excludedAtoms.Clear();
            excludedInfo.val = "<b>Excluded Atoms</b>\n";
        }
        
        public void RemoveUIElements(List<object> UIElements)
		{
			for (int i=0; i<UIElements.Count; ++i)
			{
				if(UIElements[i] == null) continue;
				if (UIElements[i] is JSONStorableParam)
				{
					JSONStorableParam jsp = UIElements[i] as JSONStorableParam;
					if (jsp is JSONStorableFloat)
						RemoveSlider(jsp as JSONStorableFloat);
					else if (jsp is JSONStorableBool)
						RemoveToggle(jsp as JSONStorableBool);
					else if (jsp is JSONStorableColor)
						RemoveColorPicker(jsp as JSONStorableColor);
					else if (jsp is JSONStorableString)
						RemoveTextField(jsp as JSONStorableString);
					else if (jsp is JSONStorableStringChooser)
					{
						// Workaround for VaM not cleaning its panels properly.
						JSONStorableStringChooser jssc = jsp as JSONStorableStringChooser;
						RectTransform popupPanel = jssc.popup?.popupPanel;
						RemovePopup(jssc);
						if (popupPanel != null)
							UnityEngine.Object.Destroy(popupPanel.gameObject);
					}
				}
				else if (UIElements[i] is UIDynamic)
				{
					UIDynamic uid = UIElements[i] as UIDynamic;
					if (uid is UIDynamicButton)
						RemoveButton(uid as UIDynamicButton);
                    else if (uid is UIDynamicSlider)
						RemoveSlider(uid as UIDynamicSlider);
					else if (uid is UIDynamicToggle)
						RemoveToggle(uid as UIDynamicToggle);
					else if (uid is UIDynamicColorPicker)
						RemoveColorPicker(uid as UIDynamicColorPicker);
					else if (uid is UIDynamicTextField)
						RemoveTextField(uid as UIDynamicTextField);
					else if (uid is UIDynamicPopup)
					{
						// Workaround for VaM not cleaning its panels properly.
						UIDynamicPopup uidp = uid as UIDynamicPopup;
						RectTransform popupPanel = uidp.popup?.popupPanel;
						RemovePopup(uidp);
						if (popupPanel != null)
							UnityEngine.Object.Destroy(popupPanel.gameObject);
					}
                    else if (uid is UIDynamicV3Slider)
                    {
                        var v3Slider = uid as UIDynamicV3Slider;
                        leftUIElements.Remove(v3Slider.transform);
                        rightUIElements.Remove(v3Slider.spacer.transform);
                        Destroy(v3Slider.spacer.gameObject);
                        Destroy(v3Slider.gameObject);
                    }
                    else if (uid is UIDynamicTabBar)
                    {
	                    var tabbar = uid as UIDynamicTabBar;
                        leftUIElements.Remove(tabbar.transform);
                        rightUIElements.Remove(tabbar.spacer.transform);
                        Destroy(tabbar.spacer.gameObject);
                        Destroy(tabbar.gameObject);
                    }
                    else
						RemoveSpacer(uid);
				}
			}

			UIElements.Clear();
		}
        
    }
}