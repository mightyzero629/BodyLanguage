using System;
using Leap.Unity;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Utils = Leap.Unity.Utils;

namespace CheesyFX
{
    public class SlapTrigger : BodyRegionTrigger
    {
        public JSONStorableFloat cooldown;
        // public JSONStorableBool showCumulative;
        
        public EventTrigger onUndershotCumulative;
        public EventTrigger onExceededCumulative;
        public FloatTrigger onValueChangedCumulative;
        
        public JSONStorableFloat inputFromCumulative;
        public JSONStorableFloat inputToCumulative;
        public JSONStorableFloat thresholdCumulative;

        private float lastCumulativeValue;

        private bool hasRangeCumulative = true;

        public override BodyRegionTrigger Init(MVRScript script, TouchZone region)
        {
            base.Init(script, region);
            baseInfo = $"<b>{region.name}</b>\nLast Slap: {0:0.00}\nCumulative: ";
            info.val = $"{baseInfo}{0:0.00}";
            this.region = region;
            region.slapTrigger = this;

            threshold.val = threshold.defaultVal = 10f;
            inputTo.val = inputTo.defaultVal = 10f;
            
            inputFromCumulative = new JSONStorableFloat($"Input From ({region.name}SC)", 0f, val => hasRangeCumulative = val != inputToCumulative.val, 0f, 1f, false);
            inputToCumulative = new JSONStorableFloat($"Input To ({region.name}SC)", 200f, val => hasRangeCumulative = val != inputFromCumulative.val, 0f, 500f, false);
            thresholdCumulative = new JSONStorableFloat($"Threshold ({region.name}SC)", 100f, 0f, 200f, false);
            
            onUndershotCumulative = new EventTrigger(TouchMe.singleton, "OnUndershotCumulative");
            onExceededCumulative = new EventTrigger(TouchMe.singleton, "OnExceededCumulative");
            onValueChangedCumulative = new FloatTrigger(TouchMe.singleton, "OnValueChangedCumulative");
            reset.actionCallback += () => onValueChangedCumulative.Trigger(0f);

            cooldown = new JSONStorableFloat("Cooldown", 0f, 0f, 5f);

            Register();
            return this;
        }
        
        public override void Register()
        {
            base.Register();
            script.RegisterFloat(cooldown);
            script.DeregisterFloat(inputFromCumulative);
            script.DeregisterFloat(inputToCumulative);
            script.DeregisterFloat(thresholdCumulative);
        }

        public override void Trigger(float v)
        {
            if (!enabled) return;
            if (cooldownTimer > 0f) return;
            cooldownTimer = cooldown.val;
            cumulativeValue += v;
            if (cumulativeValue > cap.val) cumulativeValue = cap.val;
                
            if (panelOpen)
            {
                baseInfo = $"<b>{region.name}</b>\nLast Slap: {v:0.00}\nCumulative: ";
            }
            
            if (hasRange)
            {
                onValueChanged.Trigger((v - inputFrom.val) / (inputTo.val - inputFrom.val));
            }
            else onValueChanged.Trigger(v <= inputFrom.val? 0f : 1f);
            
            if (v < threshold.val)
            {
                if (onUndershot.HasActions()) onUndershot.Trigger();
            }
            else
            {
                if (onExceeded.HasActions()) onExceeded.Trigger();
            }
            
            // if (hasRangeCumulative)
            // {
            //     onValueChangedCumulative.Trigger((v - inputFromCumulative.val) / (inputToCumulative.val - inputFromCumulative.val));
            // }
            // else onValueChangedCumulative.Trigger(v <= inputFromCumulative.val? 0f : 1f);

            lastValue = v;
        }

        public override void Update()
        {
            onUndershot.Update();
            onValueChanged.Update();
            onValueChangedCumulative.Update();
            onExceeded.Update();
            onUndershot.Update();
            onExceededCumulative.Update();
            onUndershotCumulative.Update();
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (cumulativeValue > 0f)
            {
                if (hasRangeCumulative)
                {
                    onValueChangedCumulative.Trigger((cumulativeValue - inputFromCumulative.val) / (inputToCumulative.val - inputFromCumulative.val));
                }
                else onValueChangedCumulative.Trigger(cumulativeValue <= inputFromCumulative.val? 0f : 1f);
                cumulativeValue -= decayRate.val * Time.deltaTime;
                if (panelOpen) info.val = $"{baseInfo}{cumulativeValue:0.00}";
            }
            else if (cumulativeValue < 0)
            {
                cumulativeValue = 0f;
                if (panelOpen) info.val = $"{baseInfo}{0:0.00}";
            }
            
            if (cumulativeValue < thresholdCumulative.val)
            {
                if (lastCumulativeValue > thresholdCumulative.val) onUndershotCumulative.Trigger();
            }
            else
            {
                if (lastCumulativeValue < thresholdCumulative.val) onExceededCumulative.Trigger();
            }
            lastCumulativeValue = cumulativeValue;
            // if (condition != null && !condition.IsMet()) return;

            // Trigger(region.timeTouched);
            // if (Math.Abs(lastValue - region.timeTouched) > .001f)
            // {
            //     
            //     // region.name.Print();
            // }
        }

        public void OnDestroy()
        {
            base.OnDestroy();
            onUndershotCumulative.Remove();
            onExceededCumulative.Remove();
            onValueChangedCumulative.Remove();
            script.DeregisterFloat(inputFromCumulative);
            script.DeregisterFloat(inputToCumulative);
            script.DeregisterFloat(thresholdCumulative);
            script.DeregisterFloat(cooldown);
        }
        
        public override void OnAtomRename(string oldUid, string newUid)
        {
            base.OnAtomRename(oldUid, newUid);
            onUndershotCumulative.SyncAtomNames();
            onExceededCumulative.SyncAtomNames();
            onValueChangedCumulative.SyncAtomNames();
        }

        public override JSONClass Store(string subScenePrefix)
        {
            JSONClass jc = base.Store(subScenePrefix);
            thresholdCumulative.Store(jc);
            inputToCumulative.Store(jc);
            inputFromCumulative.Store(jc);
            cooldown.Store(jc);
            jc[onExceededCumulative.Name] = onExceededCumulative.GetJSON(subScenePrefix);
            jc[onUndershotCumulative.Name] = onUndershotCumulative.GetJSON(subScenePrefix);
            jc[onValueChangedCumulative.Name] = onValueChangedCumulative.GetJSON(subScenePrefix);
            cooldown.Store(jc);
            // cumulative.Store(jc);
            return jc;
        }

        public override void Load(JSONClass jc, string subScenePrefix)
        {
            base.Load(jc, subScenePrefix);
            thresholdCumulative.Load(jc);
            inputToCumulative.Load(jc);
            inputFromCumulative.Load(jc);
            cooldown.Load(jc);
            onExceededCumulative.RestoreFromJSON(jc, subScenePrefix, false, true);
            onUndershotCumulative.RestoreFromJSON(jc, subScenePrefix, false, true);
            onValueChangedCumulative.RestoreFromJSON(jc, subScenePrefix, false, true);
        }

        public override void OpenPanel(MVRScript script, Action back)
        {
            base.OpenPanel(script, back);
            
            UIDynamic spacer;
            UIDynamicButton button;

            
            // decayRateSlider.SetVisible(cumulative.val);
            spacer = script.CreateSpacer(true);
            spacer.height = 100f;
            UIElements.Add(spacer);
            
            cooldown.CreateUI(script, rightSide: false, UIElements: UIElements);
            
            UIManager.SetupInfoOneLine(UIElements,"Threshold Triggers", false, script: script);
            threshold.CreateUI(script, true, UIElements:UIElements);
            button = script.SetupButton("On Threshold Exceeded", false, onExceeded.OpenPanel, UIElements);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            button = script.SetupButton("On Threshold Undershot", false, onUndershot.OpenPanel, UIElements);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);

            UIManager.SetupInfoOneLine(UIElements,"Value Triggers", false, script: script);
            spacer = script.CreateSpacer(true);
            spacer.height = 30f;
            UIElements.Add(spacer);
            inputFrom.CreateUI(script, UIElements:UIElements);
            inputTo.CreateUI(script, true, UIElements:UIElements);
            button = script.SetupButton("On Value Changed", false, onValueChanged.OpenPanel, UIElements);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            spacer = script.CreateSpacer(true);
            spacer.height = 100f;
            UIElements.Add(spacer);
            
            //cumulative
            UIManager.SetupInfoOneLine(UIElements,"Threshold Triggers Cumulative", false, script: script);
            thresholdCumulative.CreateUI(script, true, UIElements:UIElements);
            button = script.SetupButton("On Threshold Exceeded Cumulative", false, onExceededCumulative.OpenPanel, UIElements);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            button = script.SetupButton("On Threshold Undershot Cumulative", false, onUndershotCumulative.OpenPanel, UIElements);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);

            UIManager.SetupInfoOneLine(UIElements,"Value Triggers Cumulative", false, script: script);
            spacer = script.CreateSpacer(true);
            spacer.height = 30f;
            UIElements.Add(spacer);
            inputFromCumulative.CreateUI(script, UIElements:UIElements);
            inputToCumulative.CreateUI(script, true, UIElements:UIElements);
            button = script.SetupButton("On Value Changed Cumulative", false, onValueChangedCumulative.OpenPanel, UIElements);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            cap.CreateUI(script, true, UIElements:UIElements);


            // if (condition == null) return;
            // UIManager.SetupInfoOneLine(UIElements,"Conditions", true, script: script);
            // // UIManager.EvenLeftToRight();
            // button = script.CreateButton("Exclude...");
            // button.button.onClick.AddListener(() => condition.Exclude(condition.excludedChooser.val, true));
            // UIElements.Add(button);
            // button = script.CreateButton("(Re)Include...");
            // button.button.onClick.AddListener(() => condition.Exclude(condition.excludedChooser.val, false));
            // UIElements.Add(button);
            // condition.excludedChooser.CreateUI(script: script, rightSide: true,chooserType:2, UIElements:UIElements);
            //
            // textfield = script.CreateTextField(condition.excludedInfo, true);
            // textfield.height = 200f;
            // UIElements.Add(textfield);
        }
    }
}