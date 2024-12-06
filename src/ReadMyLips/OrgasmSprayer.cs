using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class OrgasmSprayer : EmoteSprayer
    {
        private JSONStorableFloat burstMin = new JSONStorableFloat("Burst Min", 2f, 0f, 20f);
        private JSONStorableFloat burstMax = new JSONStorableFloat("Burst Max", 4f, 0f, 20f);
        private JSONStorableFloat duration = new JSONStorableFloat("Duration", 5f, 0f, 20f);
        private JSONStorableFloat rate = new JSONStorableFloat("Rate", 1f, 0f, 10f);
        
        private JSONStorableAction trigger = new JSONStorableAction("Trigger Orgsm Emotes", null);
        
        public override void Init()
        {
            base.Init();
            textureChoice.val = "Hearts/01";
            emission.SetBursts(new [] {new ParticleSystem.Burst(0f, (short) burstMin.val, (short) burstMax.val)});
            
            burstMin.AddCallback(val => SetBurst());
            burstMax.AddCallback(val => SetBurst());
            duration.AddCallback(SetDuration);
            rate.AddCallback(SetRate);
            
            trigger.actionCallback = Trigger;
            ReadMyLips.singleton.RegisterAction(trigger);
        }

        private void SetBurst()
        {
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short) burstMin.val, (short) burstMax.val));
        }
        
        private void SetDuration(float val)
        {
            main.duration = val;
        }
        
        public override void CreateUI(List<object> UIElements)
        {
            ReadMyLips.singleton.SetupButton("Trigger", true, Trigger, UIElements);
            base.CreateUI(UIElements);
            var textField = ReadMyLips.singleton.CreateTextField(new JSONStorableString("bla",
                "This will spray a burst of particles at once followed by some more during 'Duration'."));
            textField.height = 110f;
            UIElements.Add(textField);
            burstMin.CreateUI(UIElements);
            burstMax.CreateUI(UIElements);
            duration.CreateUI(UIElements);
            rate.CreateUI(UIElements);
            
            burstMin.slider.wholeNumbers = true;
            burstMax.slider.wholeNumbers = true;
        }
        
        public override JSONClass Store()
        {
            var jc = base.Store();
            burstMin.Store(jc);
            burstMax.Store(jc);
            rate.Store(jc);
            duration.Store(jc);
            return jc;
        }
        
        public override void Load(JSONClass jc)
        {
            base.Load(jc);
            burstMin.Load(jc);
            burstMax.Load(jc);
            rate.Load(jc);
            duration.Load(jc);
        }
    }
}