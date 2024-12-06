using System;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class TimeBasedTrigger : BodyRegionTrigger
    {
        public JSONStorableBool instantReset;

        public override BodyRegionTrigger Init(MVRScript script, TouchZone region)
        {
            base.Init(script, region);
            this.region = region;
            instantReset = new JSONStorableBool($"Instant Reset", false, val =>
            {
                if((object)decayRateSlider != null) decayRateSlider.SetVisible(!val);
            });
            cap.val = cap.defaultVal = 10f;
            Register();
            return this;
        }

        public override void Register()
        {
            base.Register();
            script.RegisterBool(instantReset);
        }

        public override void Trigger(float v)
        {
            if (hasRange)
            {
                onValueChanged.Trigger((v - inputFrom.val) / (inputTo.val - inputFrom.val));
            }
            else onValueChanged.Trigger(v <= inputFrom.val? 0f : 1f);
            
            if (lastValue < threshold.val)
            {
                if (v >= threshold.val && exceededTimer <= 0f && (exceededChance.val == 1f || Random.Range(0f, 1f) < exceededChance.val))
                {
                    onExceeded.Trigger();
                    exceededTimer = exceededCooldown.val;
                }
            }
            else if (v < threshold.val && undershotTimer <= 0f && (undershotChance.val == 1f || Random.Range(0f, 1f) < undershotChance.val))
            {
                onUndershot.Trigger();
                undershotTimer = undershotCooldown.val;
            }
            
            lastValue = v;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            script.DeregisterBool(instantReset);
        }

        public override JSONClass Store(string subScenePrefix)
        {
            JSONClass jc = base.Store(subScenePrefix);
            instantReset.Store(jc);
            return jc;
        }

        public override void Load(JSONClass jc, string subScenePrefix)
        {
            base.Load(jc, subScenePrefix);
            instantReset.Load(jc);
        }

        public override void OpenPanel(MVRScript script, Action back)
        {
            base.OpenPanel(script, back);
            
            UIDynamic spacer;
            UIDynamicButton button;
            
            decayRateSlider.SetVisible(!instantReset.val);
            
            instantReset.CreateUI(UIElements,false);

            // spacer = script.CreateSpacer(true);
            // spacer.height = 35f;
            // UIElements.Add(spacer);
            
            UIManager.SetupInfoOneLine(UIElements,"Threshold Triggers", false, script: script);
            // var spacer = script.CreateSpacer(true);
            // spacer.height = 50f;
            // triggerUIElements.Add(spacer);
            // UIManager.EvenLeftToRight(script, triggerUIElements);
            
            threshold.CreateUI(script, false, UIElements:UIElements);
            spacer = script.CreateSpacer(true);
            spacer.height = 170f;
            UIElements.Add(spacer);

            button = script.CreateButton("On Threshold Undershot", false);
            button.button.onClick.AddListener(onUndershot.OpenPanel);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            UIElements.Add(button);
            undershotChance.CreateUI(script, false, UIElements:UIElements);
            undershotCooldown.CreateUI(script, false, UIElements:UIElements);
            
            button = script.CreateButton("On Threshold Exceeded", true);
            button.button.onClick.AddListener(onExceeded.OpenPanel);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            UIElements.Add(button);
            exceededChance.CreateUI(script, true, UIElements:UIElements);
            exceededCooldown.CreateUI(script, true, UIElements:UIElements);
            
            UIManager.SetupInfoOneLine(UIElements,"Value Triggers", false, script: script);
            spacer = script.CreateSpacer(true);
            spacer.height = 35f;
            UIElements.Add(spacer);
            // UIManager.EvenLeftToRight(script, triggerUIElements);
            
            inputFrom.CreateUI(script, UIElements:UIElements);
            inputTo.CreateUI(script, true, UIElements:UIElements);


            button = script.CreateButton("On Value Changed", false);
            button.button.onClick.AddListener(onValueChanged.OpenPanel);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            UIElements.Add(button);
            
            

            // if (condition == null) return;
            // UIManager.SetupInfoOneLine(triggerUIElements,"Conditions", true, script: script);
            // // UIManager.EvenLeftToRight();
            // button = script.CreateButton("Exclude...");
            // button.button.onClick.AddListener(() => condition.Exclude(condition.excludedChooser.val, true));
            // triggerUIElements.Add(button);
            // button = script.CreateButton("(Re)Include...");
            // button.button.onClick.AddListener(() => condition.Exclude(condition.excludedChooser.val, false));
            // triggerUIElements.Add(button);
            // condition.excludedChooser.CreateUI(script: script, rightSide: true,chooserType:2, UIElements:triggerUIElements);
            //
            // textfield = script.CreateTextField(condition.excludedInfo, true);
            // textfield.height = 200f;
            // triggerUIElements.Add(textfield);
        }
    }
}