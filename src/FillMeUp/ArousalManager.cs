using UnityEngine;
using System.Collections.Generic;

namespace CheesyFX
{
    public class ArousalManager
    {
        public JSONStorableFloat orgasmThreshold = new JSONStorableFloat("Orgasm Threshold", 1000f, 0f, 200f);
        
        public JSONStorableFloat arousal = new JSONStorableFloat("Arousal", 0f, 0f, 1000f, true, false);
        public JSONStorableFloat sensitivity = new JSONStorableFloat("Sensitivity", 1f, 0f, 10f, true);
        public JSONStorableFloat orgasmCount;
        private float arousalToVAMMoanIntensitiyFactor;

        private JSONStorable VAMMoan;
        private bool VAMMoanLoaded;
        private JSONStorableFloat VAMMoanIntensity;
        // private UIItemHolder triggerHolder = new UIItemHolder();
        // private TransitionTrigger arousalTrigger;

        private float orgasmTimeout;

        public ArousalManager()
        {
            orgasmCount = new JSONStorableFloat("Orgasm Count", 0f, OnOrgasm, 0f, 20f, false, false);
            
            if (VAMMoan != null)
            {
                VAMMoanLoaded = true;
                VAMMoanIntensity = FillMeUp.VAMMoanIntensity;
                // VAMMoan.CallAction("Voice intensity 4");
                // VAMMoan.CallAction("Voice orgasm");
                // VAMMoan.SetStringChooserParamValue("arousalMode", "Manual");
                // VAMMoan.GetFloatParamNames().ToArray().Print();
            }

            // triggerHolder.createItemUI = x =>
            // {
            //     triggerHolder.CreateToggle(x);
            //     triggerHolder.CreateConfigure(x);
            // };
            // arousalTrigger = new TransitionTrigger(triggerHolder, name:"Arousal Trigger");
            // arousalTrigger.maxInput.val = arousal.max;
            // triggerHolder.item = arousalTrigger;

            orgasmThreshold.setCallbackFunction += OrgasmThresholdCallback;
            OrgasmThresholdCallback(orgasmThreshold.val);
        }

        private void OrgasmThresholdCallback(float v)
        {
            arousal.max = v;
            arousalToVAMMoanIntensitiyFactor = 4f / v;
        }
        
        public void Orgasm()
        {
            orgasmCount.val += 1f;
            arousal.val = 0f;
            sensitivity.val *= .5f;
            orgasmTimeout = 20f;
            VAMMoan.CallAction("Voice orgasm");
            // VAMMoanIntensity.val.Print();
        }

        public void OnOrgasm(float count)
        {
            $"Orgasm {count}!".Print();
        }

        public void UpdateArousal(float val)
        {
            float increment = val * sensitivity.val;
            arousal.val += increment;
            // if(arousal.val >= orgasmThreshold.val) Orgasm();
            
        }

        private void SetVAMMoanIntensity()
        {
            if (orgasmTimeout > 0f)
            {
                orgasmTimeout -= Time.deltaTime;
                return;
            }
            if (arousal.val >= orgasmThreshold.val)
            {
                Orgasm();
                return;
            }
            int intensity;
            if (arousal.val == 0f) intensity = 0;
            else intensity = (int)(arousal.val * arousalToVAMMoanIntensitiyFactor) + 1;
            if (VAMMoanIntensity.val == intensity || VAMMoanIntensity.val > 4f) return;
            VAMMoan.CallAction($"Voice intensity {intensity}");
            // intensity.Print();
        }

        public void Update()
        {
            if(VAMMoanLoaded) SetVAMMoanIntensity();
            if(arousal.val > .1f) arousal.val = Mathf.Lerp(arousal.val, 0f, Time.fixedDeltaTime*.25f);
            else if (arousal.val > 0f) arousal.val = 0f;
            sensitivity.val += .001f;
            // arousalTrigger.floatTrigger.Update();
            // arousalTrigger.Trigger(arousal.val);
            // if (arousal.val > 30f) timeAroused.val += Time.fixedDeltaTime;
            // else timeAroused.val -= Time.fixedDeltaTime;
            
            // if(VAMMoanLoaded) VAMMoan.CallStringChooserAction("Update Arousal value", "300");
        }

        public List<object> CreateUI()
        {
            List<object> UIElements = new List<object>();
            // triggerHolder.CreateItems();
            arousal.CreateUI(UIElements);
            orgasmCount.CreateUI(UIElements, rightSide:true);
            // timeAroused.CreateUI(UIElements, true);
            UIDynamic slider = sensitivity.CreateUI(UIElements);
            ((UIDynamicSlider)slider).valueFormat = "{0.000}";
            orgasmThreshold.CreateUI(UIElements, true);
            
            return UIElements;
        }
    }
}