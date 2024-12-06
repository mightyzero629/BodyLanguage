using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class StimSprayer : EmoteSprayer
    {
        public JSONStorableBool toggle = new JSONStorableBool("Trigger Stim Emotes (max rate)", false);
        private JSONStorableFloat threshold = new JSONStorableFloat("Stim Threshold", .5f, 0f, 1f);
        private JSONStorableFloat minRate = new JSONStorableFloat("Min Rate", 0f, 0f, 10f);
        private JSONStorableFloat maxRate = new JSONStorableFloat("Max Rate", 1f, 0f, 10f);
        private float factor;
        private bool testing;

        public override void Init()
        {
            base.Init();
            enabled.setCallbackFunction += SetEnabled;
            textureChoice.val = "Lips/01";
            toggle.AddCallback(Toggle);
            threshold.AddCallback(val => SetFactor());
            minRate.AddCallback(val => SetFactor());
            maxRate.AddCallback(val => SetFactor());
            main.loop = true;
            enabled.name = "Enabled (Stim Emotes)";
            ReadMyLips.singleton.RegisterBool(toggle);
        }

        private void SetFactor()
        {
            factor = (maxRate.val - minRate.val) / (1f - threshold.val) / (1f - threshold.val);
            if(testing) SetRate(maxRate.val);
        }

        public void Update(float val)
        {
            if (ps == null) return;
            if(testing || !go.activeSelf || !enabled.val) return;
            if (val < threshold.val && ps.isEmitting)
            {
                ps.Stop();
            }
            else
            {
                if(!ps.isEmitting) ps.Play();
                SetRate(minRate.val + factor * (val - threshold.val) * (val - threshold.val));
            }
        }

        private void SetEnabled(bool val)
        {
            if (!val) ps.Stop();
        }
        
        public void Toggle(bool val)
        {
            testing = val;
            if(val) SetRate(maxRate.val);
        }

        public override void CreateUI(List<object> UIElements)
        {
            toggle.CreateUI(UIElements, true);
            base.CreateUI(UIElements);
            var textField = ReadMyLips.singleton.CreateTextField(new JSONStorableString("bla",
                "This will continuously spray emotes based on the current stimulation value, starting from 'Stim Threshold'."));
            textField.height = 110f;
            UIElements.Add(textField);
            threshold.CreateUI(UIElements, false);
            minRate.CreateUI(UIElements, false);
            maxRate.CreateUI(UIElements, false);
        }
        
        public override JSONClass Store()
        {
            var jc = base.Store();
            threshold.Store(jc);
            minRate.Store(jc);
            maxRate.Store(jc);
            return jc;
        }
        
        public override void Load(JSONClass jc)
        {
            base.Load(jc);
            threshold.Load(jc);
            minRate.Load(jc);
            maxRate.Load(jc);
        }
    }
}