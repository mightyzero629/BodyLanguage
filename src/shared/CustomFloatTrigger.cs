using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class CustomFloatTrigger : MonoBehaviour
    {
        private MVRScript script;
        public new string name;
        private JSONStorableFloat driver;
        public EventTrigger exceededTrigger;
        public EventTrigger undershotTrigger;
        public FloatTrigger valueTrigger;
        public JSONStorableFloat exceededChance = new JSONStorableFloat("OnExceeded Chance", 1f, 0f, 1f);
        public JSONStorableFloat undershotChance = new JSONStorableFloat("OnUndershot Chance", 1f, 0f, 1f);
        public JSONStorableFloat undershotCooldown = new JSONStorableFloat("OnUndershot Cooldown", 0f, 0f, 30f, false);
        public JSONStorableFloat exceededCooldown = new JSONStorableFloat("OnExceeded Cooldown", 0f, 0f, 30f, false);
        public JSONStorableFloat inputTo;
        public JSONStorableFloat inputFrom;
        public JSONStorableFloat threshold;
        public JSONStorableBool absoluteValue;
        private bool panelOpen;
        private string baseInfo;
        private float _lastValue;
        private float undershotTimer;
        private float exceededTimer;

        public List<object> UIElements = new List<object>();
        // public Condition condition;

        public void Init(MVRScript script, JSONStorableFloat driver, string name, bool absoluteDefault = false, float thresholdDefault = 0f, float fromDefault = 0f, float toDefault = 1f)
        {
            this.script = script;
            this.driver = driver;
            this.name = name;
            inputFrom = new JSONStorableFloat("Input From", fromDefault, 0f, 1f, false);
            inputTo = new JSONStorableFloat("Input To", toDefault, 0f, 1f, false);
            threshold = new JSONStorableFloat("Threshold", thresholdDefault, 0f, 1f, false);
            absoluteValue = new JSONStorableBool("Absolute Value", absoluteDefault);
            exceededTrigger = new EventTrigger(script, "OnExceeded");
            undershotTrigger = new EventTrigger(script, "OnUndershoot");
            valueTrigger = new FloatTrigger(script, "OnValueChanged");

            // condition = new Condition();

            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;
            SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
        }

        public void Trigger(float v)
        {
            if(valueTrigger.HasActions())
            {
                float delta = inputTo.val - inputFrom.val;
                if (delta > 0f)
                {
                    float remappedValue = (v - inputFrom.val) / (inputTo.val - inputFrom.val);
                    // float remappedValue = Mathf.Lerp(outputFrom.val, outputTo.val, (v - inputFrom.val) / (inputTo.val - inputFrom.val));
                    valueTrigger.Trigger(remappedValue);
                }
            }
            if(exceededTrigger.HasActions() || undershotTrigger.HasActions())
            {
                if (_lastValue < threshold.val)
                {
                    if (v >= threshold.val && exceededTimer <= 0f && (exceededChance.val == 1f || Random.Range(0f, 1f) < exceededChance.val))
                    {
                        exceededTrigger.Trigger();
                        exceededTimer = exceededCooldown.val;
                    }
                }
                else if (v < threshold.val && undershotTimer <= 0f && (undershotChance.val == 1f || Random.Range(0f, 1f) < undershotChance.val))
                {
                    undershotTrigger.Trigger();
                    undershotTimer = undershotCooldown.val;
                }
                _lastValue = v;
            }
        }

        private void Update()
        {
            if (undershotTimer > 0f) undershotTimer -= Time.deltaTime;
            if (exceededTimer > 0f) exceededTimer -= Time.deltaTime;
            exceededTrigger.Update();
            undershotTrigger.Update();
            valueTrigger.Update();
            // if (condition != null && !condition.IsMet()) return;
            // if (panelOpen) info.val = $"{baseInfo}\n{driverVal:0.00}";
            Trigger(absoluteValue.val? Mathf.Abs(driver.val) : driver.val);
        }

        private void OnDestroy()
        {
            exceededTrigger.Remove();
            undershotTrigger.Remove();
            valueTrigger.Remove();
            SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRename;
            SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
        }

        public void OnAtomRename(string oldUid, string newUid)
        {
            exceededTrigger.SyncAtomNames();
            undershotTrigger.SyncAtomNames();
            valueTrigger.SyncAtomNames();
            // condition.OnAtomRename();
        }

        public void OnAtomAdded(Atom atom)
        {
            // condition.AddExclusionChoice(atom);
        }

        public JSONClass Store()
        {
            JSONClass jc = new JSONClass();
            absoluteValue.Store(jc);
            inputFrom.Store(jc);
            inputTo.Store(jc);
            undershotCooldown.Store(jc);
            undershotChance.Store(jc);
            exceededCooldown.Store(jc);
            exceededChance.Store(jc);
            threshold.Store(jc);
            jc[exceededTrigger.Name] = exceededTrigger.GetJSON(script.subScenePrefix);
            jc[undershotTrigger.Name] = undershotTrigger.GetJSON(script.subScenePrefix);
            jc[valueTrigger.Name] = valueTrigger.GetJSON(script.subScenePrefix);
            // condition.excludedInfo.Store(jc);
            return jc;
        }

        public void Load(JSONClass jc)
        {
            absoluteValue.Load(jc);
            inputFrom.Load(jc);
            inputTo.Load(jc);
            undershotCooldown.Load(jc);
            undershotChance.Load(jc);
            exceededCooldown.Load(jc);
            exceededChance.Load(jc);
            threshold.Load(jc);
            // condition.excludedInfo.Load(jc);
            exceededTrigger.RestoreFromJSON(jc, script.subScenePrefix, false, true);
            undershotTrigger.RestoreFromJSON(jc, script.subScenePrefix, false, true);
            valueTrigger.RestoreFromJSON(jc, script.subScenePrefix, false, true);
        }

        public void OpenPanel(MVRScript script)
        {
            panelOpen = true;
            UIDynamicButton button;
            absoluteValue.CreateUI(UIElements);
            UIManager.SetupInfoOneLine(UIElements,"Threshold Triggers", false, script: script);

            threshold.CreateUI(script, false, UIElements:UIElements);
            var spacer = script.CreateSpacer(true);
            spacer.height = 120f;
            UIElements.Add(spacer);

            button = script.CreateButton("On Threshold Undershot", false);
            button.button.onClick.AddListener(undershotTrigger.OpenPanel);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            UIElements.Add(button);
            undershotChance.CreateUI(script, false, UIElements:UIElements);
            undershotCooldown.CreateUI(script, false, UIElements:UIElements);
            
            button = script.CreateButton("On Threshold Exceeded", true);
            button.button.onClick.AddListener(exceededTrigger.OpenPanel);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            UIElements.Add(button);
            exceededChance.CreateUI(script, true, UIElements:UIElements);
            exceededCooldown.CreateUI(script, true, UIElements:UIElements);

            UIManager.SetupInfoOneLine(UIElements,"Value Triggers", true, script: script);
            
            inputFrom.CreateUI(script, UIElements:UIElements);
            inputTo.CreateUI(script, true, UIElements:UIElements);
            
            button = script.CreateButton("On Value Changed", false);
            button.button.onClick.AddListener(valueTrigger.OpenPanel);
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