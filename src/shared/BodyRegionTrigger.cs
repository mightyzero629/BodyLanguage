using System;
using System.Collections.Generic;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class BodyRegionTrigger : MonoBehaviour
    {
        protected MVRScript script;
        public TouchZone region;
        public JSONStorableBool enabledJ;
        public JSONStorableAction reset;
        public EventTrigger onUndershot;
        public EventTrigger onExceeded;
        public FloatTrigger onValueChanged;
        public JSONStorableFloat inputTo;
        public JSONStorableFloat inputFrom;
        public JSONStorableFloat threshold;
        public JSONStorableFloat decayRate;
        public JSONStorableFloat cap;
        public UIDynamic decayRateSlider;
        public bool panelOpen;
        public JSONStorableString info = new JSONStorableString("info", "");
        public string baseInfo;
        public float lastValue;

        public float cooldownTimer;
        public float cumulativeValue;

        public bool hasRange = true;

        public float exceededTimer;
        public float undershotTimer;
        
        public List<object> UIElements = new List<object>();
        
        public JSONStorableFloat exceededChance = new JSONStorableFloat("OnExceeded Chance", 1f, 0f, 1f);
        public JSONStorableFloat undershotChance = new JSONStorableFloat("OnUndershot Chance", 1f, 0f, 1f);
        public JSONStorableFloat undershotCooldown = new JSONStorableFloat("OnUndershot Cooldown", 0f, 0f, 30f, false);
        public JSONStorableFloat exceededCooldown = new JSONStorableFloat("OnExceeded Cooldown", 0f, 0f, 30f, false);
        // public Condition condition;

        public virtual BodyRegionTrigger Init(MVRScript script, TouchZone region)
        {
            SimpleTriggerHandler.LoadAssets();
            this.script = script;
            enabledJ = new JSONStorableBool($"Enabled ({region.name}S)", true);
            reset = new JSONStorableAction($"Reset ({region.name}S)", () =>
            {
                cumulativeValue = 0f;
                info.val = $"{baseInfo}\n{0f:0.00}";
                onValueChanged.Trigger(0f);
            });
            inputFrom = new JSONStorableFloat($"Input From ({region.name}S)", 0f, val => hasRange = val != inputTo.val, 0f, 1f, false);
            inputTo = new JSONStorableFloat($"Input To ({region.name}S)", 1f, val => hasRange = val != inputFrom.val, 0f, 200f, false);
            threshold = new JSONStorableFloat($"Threshold ({region.name}S)", .5f, 0f, 1f, false);
            decayRate = new JSONStorableFloat($"Decay Rate ({region.name}S)", 1f, 0f, 20f, false);
            cap = new JSONStorableFloat($"Input Cap ({region.name}S)", 200f, 0f, 500f, false);
            onUndershot = new EventTrigger(script, "OnUndershot");
            onExceeded = new EventTrigger(script, "OnExceeded");
            onValueChanged = new FloatTrigger(script, "OnValueChanged");

            // condition = new Condition();
            enabledJ.setCallbackFunction += val => enabled = val;

            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;
            SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;

            return this;
        }

        public virtual void Update()
        {
            if (undershotTimer > 0f) undershotTimer -= Time.deltaTime;
            if (exceededTimer > 0f) exceededTimer -= Time.deltaTime;
        }

        public virtual void Register()
        {
            script.RegisterBool(enabledJ);
            script.RegisterAction(reset);
            script.RegisterFloat(inputFrom);
            script.RegisterFloat(inputTo);
            script.RegisterFloat(threshold);
            script.RegisterFloat(decayRate);
            script.RegisterFloat(cap);
        }

        public virtual void Trigger(float v)
        {
        }

        public virtual void OnDestroy()
        {
            onUndershot.Remove();
            onExceeded.Remove();
            onValueChanged.Remove();
            script.DeregisterBool(enabledJ);
            script.DeregisterAction(reset);
            script.DeregisterFloat(inputFrom);
            script.DeregisterFloat(inputTo);
            script.DeregisterFloat(threshold);
            script.DeregisterFloat(decayRate);
            script.DeregisterFloat(cap);
            SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRename;
            SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
        }

        public virtual void OnAtomRename(string oldUid, string newUid)
        {
            onUndershot.SyncAtomNames();
            onExceeded.SyncAtomNames();
            onValueChanged.SyncAtomNames();
            // condition.OnAtomRename();
        }

        public void OnAtomAdded(Atom atom)
        {
            // condition.AddExclusionChoice(atom);
        }

        private void OnResetRateChanged(float val)
        {
            if(val > 0f)
            {
                if (!region.touchCollisionListener.isOnStay && region.touchCollisionListener.touchTimerReset == null)
                {
                    region.touchCollisionListener.touchTimerReset =
                        region.touchCollisionListener.TouchTimerReset().Start();
                }
            }
            else if(!region.touchCollisionListener.isOnStay)
            {
                region.touchCollisionListener.touchTimerReset.Stop();
            }
        }

        public virtual JSONClass Store(string subScenePrefix)
        {
            JSONClass jc = new JSONClass();
            inputFrom.Store(jc);
            inputTo.Store(jc);
            threshold.Store(jc);
            enabledJ.Store(jc);
            decayRate.Store(jc);
            cap.Store(jc);
            exceededChance.Store(jc);
            undershotChance.Store(jc);
            exceededCooldown.Store(jc);
            undershotCooldown.Store(jc);
            jc[onExceeded.Name] = onExceeded.GetJSON(subScenePrefix);
            jc[onUndershot.Name] = onUndershot.GetJSON(subScenePrefix);
            jc[onValueChanged.Name] = onValueChanged.GetJSON(subScenePrefix);
            // condition.excludedInfo.Store(jc);
            return jc;
        }

        public virtual void Load(JSONClass jc, string subScenePrefix)
        {
            inputFrom.Load(jc);
            inputTo.Load(jc);
            threshold.Load(jc);
            enabledJ.Load(jc);
            decayRate.Load(jc);
            cap.Load(jc);
            exceededChance.Load(jc);
            undershotChance.Load(jc);
            exceededCooldown.Load(jc);
            undershotCooldown.Load(jc);
            // condition.excludedInfo.Load(jc);
            onExceeded.RestoreFromJSON(jc, subScenePrefix, false, true);
            onUndershot.RestoreFromJSON(jc, subScenePrefix, false, true);
            onValueChanged.RestoreFromJSON(jc, subScenePrefix, false, true);
        }

        public virtual void OpenPanel(MVRScript script, Action back)
        {
            panelOpen = true;
            UIDynamicButton button;
            button = script.CreateButton("Return");
            button.buttonColor = new Color(0.55f, 0.90f, 1f);
            button.button.onClick.AddListener(
                () =>
                {
                    Utils.RemoveUIElements(script, UIElements);
                    panelOpen = false;
                    back();
                });
            UIElements.Add(button);

            enabledJ.CreateUI(UIElements, rightSide: false);
            cap.CreateUI(script, UIElements:UIElements);
            

            var textfield = script.CreateTextField(info, true);
            textfield.ForceHeight(115f);
            textfield.UItext.fontSize = 34;
            textfield.UItext.alignment = TextAnchor.MiddleCenter;
            UIElements.Add(textfield);
            
            script.SetupButton("Reset Value", true, reset.actionCallback.Invoke, UIElements);
            decayRateSlider = decayRate.CreateUI(UIElements, true);

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