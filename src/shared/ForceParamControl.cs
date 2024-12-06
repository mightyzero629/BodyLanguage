using System.Collections.Generic;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class ForceParamControl
    {
        public List<object> UIElements = new List<object>();
        private Force force;


        public JSONStorableFloat offset = new JSONStorableFloat("Constant Offset", 0f, 0f, 500f);
        public JSONStorableFloat offsetQuickness = new JSONStorableFloat("Offset Quickness", .5f, .1f, 500f);
        
        private JSONStorableFloat mean = new JSONStorableFloat("Mean", 0f, 0f, 1000f, false);
        private JSONStorableFloat delta = new JSONStorableFloat("Delta", 0f, 0f, 1000f, false);
        private JSONStorableFloat sharpness = new JSONStorableFloat("Distribution Sharpness", 1f, 1f, 3f);
        private JSONStorableFloat quicknessMean = new JSONStorableFloat("Transition Quickness Mean", 4f, 0f, 10f);
        private JSONStorableFloat quicknessDelta = new JSONStorableFloat("Transition Quickness Delta", 4f, 0f, 10f);
        private JSONStorableFloat randomizeTimeMean = new JSONStorableFloat("Randomize Time Mean", 5f, 1f, 10f);
        private JSONStorableFloat randomizeTimeDelta = new JSONStorableFloat("Randomize Time Delta", 3f, 1f, 10f);
        private JSONStorableBool useNormalDistribution = new JSONStorableBool("Use Normal Distribution", true);
        private JSONStorableBool onesided = new JSONStorableBool("Onesided Distribution", false);

        private UIDynamicSlider meanSlider;
        private UIDynamicSlider deltaSlider;
        private UIDynamicSlider sharpnessSlider;
        private UIDynamicSlider transitionQuicknessMeanSlider;
        private UIDynamicSlider transitionQuicknessDeltaSlider;
        private UIDynamicSlider refreshTimeMeanSlider;
        private UIDynamicSlider refreshTimeDeltaSlider;
        private UIDynamicToggle useNormalDistributionToggle;
        private UIDynamicToggle onesidedToggle;

        private UIDynamic offsetSlider;
        private UIDynamic offsetQuicknessSlider;

        public ForceParam lastParam;
        public UIDynamicTabBar tabbar;
        public int lastId;

        public JSONStorableString info = new JSONStorableString("Info", "Inactive");
        public JSONStorableString warning = new JSONStorableString("Warning",
            "Warning: This force is synced. Settings won't have an effect until it is considered 'Master'.\n" +
            "If the handjobs aren't synced both are masters. Else, the master is the one that was first active (changes dynamically if connection is lost).");

        private UIDynamicTextField warningTextField;
        public bool UIOpen;
        
        private static JSONClass cachedPreset;
        private static string cachedForceName;
        

        public ForceParamControl(Force force)
        {
            this.force = force;
            force.enabledJ.name = force.name + " Enabled";
            offset.setCallbackFunction += val =>
            {
                if (force.enabled)
                {
                    force.offsetTarget = val;
                    force.offsetAtTarget = false;
                }
            };
            // presetSystem.Init();
        }

        public List<object> CreateUI(MVRScript script, int tabId = -1, bool createCopyPaste = true)
        {
            UIElements.Clear();
            if(createCopyPaste) CreateCopyPasteButtons(script);
            var toggleArray = UIManager.CreateToggleArray(new []{force.enabledJ, force.constant, force.applyReturn, force.syncToThrust}, script: script);
            if(force.sync?.driver == null) toggleArray.toggles[3].SetVisible(false);
            UIElements.Add(toggleArray);
            
            tabbar = UIManager.CreateTabBar(new [] { "Amplitude", "Period", "PeriodRatio", "Quickness" }, SelectParam);
            UIElements.Add(tabbar);
            meanSlider = (UIDynamicSlider)mean.CreateUI(script, false,  UIElements);
            deltaSlider = (UIDynamicSlider)delta.CreateUI(script, true, UIElements);
            refreshTimeMeanSlider = (UIDynamicSlider)randomizeTimeMean.CreateUI(script, false, UIElements);
            refreshTimeDeltaSlider = (UIDynamicSlider)randomizeTimeDelta.CreateUI(script, true, UIElements);
            transitionQuicknessMeanSlider = (UIDynamicSlider)quicknessMean.CreateUI(script, false, UIElements);
            transitionQuicknessDeltaSlider = (UIDynamicSlider)quicknessDelta.CreateUI(script, true, UIElements);
            sharpnessSlider = (UIDynamicSlider)sharpness.CreateUI(script, false, UIElements);
            sharpnessSlider.slider.wholeNumbers = true;
            
            useNormalDistributionToggle = (UIDynamicToggle)useNormalDistribution.CreateUI(script, true, UIElements);
            onesidedToggle = (UIDynamicToggle)onesided.CreateUI(script, true, UIElements);

            var textfield = script.CreateTextField(info,true);
            UIElements.Add(textfield);
            
            offsetSlider = offset.CreateUI(UIElements);
            offsetQuicknessSlider = offsetQuickness.CreateUI(UIElements);
            force.SetConstant(force.constant.val);
            tabbar.SelectTab(tabId == -1? lastId : tabId);
            UIOpen = true;
            return UIElements;
        }

        public void CreateCopyPasteButtons(MVRScript script)
        {
            script.SetupButton("Copy Settings", false, CopySettings, UIElements);
            script.SetupButton("Paste Settings", true, PasteSettings, UIElements);
        }

        public void RemoveUI(MVRScript script)
        {
            // FillMeUp.singleton.RemoveUIElements(UIElements);
            if(tabbar != null) script.RemoveSpacer(tabbar.spacer);
            Utils.RemoveUIElements(script, UIElements);
            
            UIElements.Clear();
        }

        private void SelectParam(int id)
        {
            if (id > 0)
            {
                if(force.constant.val)
                {
                    "This force is constant (non oscillating). Only 'Amplitude' is relevant in this case.".Print();
                    tabbar.SelectTab(0);
                    return;
                }
                if ((id == 1 || id == 2) && force.sync != null && force.sync.enabled)
                {
                    $"This force is synced to {force.sync.driver.name}. 'Period' and 'PeriodRatio' settings don't have an effect in this state".Print();
                }
            }
            offsetSlider.SetVisible(id == 0);
            offsetQuicknessSlider.SetVisible(id == 0);
            lastId = id;
            RegisterParam(force.parameters[id]);
        }
        
        public void RegisterParam(ForceParam param)
        {
            if(lastParam != null) UnregisterParam();
            param.mean.RegisterSlider(meanSlider.slider);
            param.delta.RegisterSlider(deltaSlider.slider);
            param.sharpness.RegisterSlider(sharpnessSlider.slider);
            param.transitionQuicknessMean.RegisterSlider(transitionQuicknessMeanSlider.slider);
            param.transitionQuicknessDelta.RegisterSlider(transitionQuicknessDeltaSlider.slider);
            param.randomizeTimeMean.RegisterSlider(refreshTimeMeanSlider.slider);
            param.randomizeTimeDelta.RegisterSlider(refreshTimeDeltaSlider.slider);
            
            param.onesided.RegisterToggle(onesidedToggle.toggle);
            param.useNormalDistribution.RegisterToggle(useNormalDistributionToggle.toggle);
            lastParam = param;
        }

        private void UnregisterParam()
        {
            lastParam.mean.RegisterSlider(null);
            lastParam.delta.RegisterSlider(null);
            lastParam.sharpness.RegisterSlider(null);
            lastParam.transitionQuicknessMean.RegisterSlider(null);
            lastParam.randomizeTimeMean.RegisterSlider(null);
            lastParam.randomizeTimeDelta.RegisterSlider(null);
            
            lastParam.onesided.RegisterToggle(null);
            lastParam.useNormalDistribution.RegisterToggle(null);
        }

        public JSONClass Store()
        {
            var jc = new JSONClass();
            force.amplitude.Store(jc);
            force.period.Store(jc);
            force.periodRatio.Store(jc);
            force.quickness.Store(jc);
            offset.Store(jc, false);
            offsetQuickness.Store(jc, false);
            return jc;
        }
        
        public void Load(JSONClass jc, bool setMissingToDefault = true)
        {
            force.amplitude.Load(jc, setMissingToDefault);
            force.period.Load(jc, setMissingToDefault);
            force.periodRatio.Load(jc, setMissingToDefault);
            force.quickness.Load(jc, setMissingToDefault);
            offset.Load(jc, setMissingToDefault);
            offsetQuickness.Load(jc, setMissingToDefault);
        }

        public void CopySettings()
        {
            cachedPreset = Store();
            cachedForceName = force.name;
            $"Stored settings for {cachedForceName}".Print();
        }

        public void PasteSettings()
        {
            if (cachedPreset != null)
            {
                Load(cachedPreset);
                $"Pasted settings from {cachedForceName} to {force.name}".Print();
            }
        }
    }
}