using System.Collections.Generic;
using UnityEngine;

namespace CheesyFX
{
    public class OrgasmFadeSprayer : EmoteSprayer
    {
        public JSONStorableBool useMultiOrgasms = new JSONStorableBool("Use Multi Orgasm Count", true);
        private JSONStorableFloat burstMin = new JSONStorableFloat("Burst Min", 2f, 0f, 20f);
        private JSONStorableFloat burstMax = new JSONStorableFloat("Burst Max", 5f, 0f, 20f);
        private ParticleSystem.Burst burst;

        private JSONStorableAction trigger = new JSONStorableAction("Trigger Orgsm Fade Emotes", null);
        
        public override void Init()
        {
            base.Init();
            main.loop = false;
            textureChoice.val = "Lips/02";
            main.duration = .2f;
            SetRate(0f);
            lifetime.val = 10f;
            ps.Stop();
            emission.SetBursts(new [] {new ParticleSystem.Burst(0f, (short) burstMin.val, (short) burstMax.val)});
            burstMin.AddCallback(val => SetBurst());
            burstMax.AddCallback(val => SetBurst());
            useMultiOrgasms.AddCallback(SetMultiOrgasms);

            trigger.actionCallback = Trigger;
            ReadMyLips.singleton.RegisterAction(trigger);
        }
        
        private void SetBurst()
        {
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short) burstMin.val, (short) burstMax.val));
            
        }

        private void SetMultiOrgasms(bool val)
        {
            if (!val)
            {
                burstMin.slider.transform.parent.gameObject.SetActive(true);
                burstMax.slider.transform.parent.gameObject.SetActive(true);
                SetBurst();
            }
            else if(burstMin.slider != null)
            {
                burstMin.slider.transform.parent.gameObject.SetActive(false);
                burstMax.slider.transform.parent.gameObject.SetActive(false);
            }
        }

        public void Trigger(float val)
        {
            if (!enabled.val) return;
            if(useMultiOrgasms.val) emission.SetBurst(0, new ParticleSystem.Burst(0f, (short) val));
            ps.Play();
        }
        
        public override void CreateUI(List<object> UIElements)
        {
            ReadMyLips.singleton.SetupButton("Trigger", true, Trigger, UIElements);
            base.CreateUI(UIElements);
            var textField = ReadMyLips.singleton.CreateTextField(new JSONStorableString("bla",
                "This will spray a burst of particles once an orgasm ends. The amount is based on the amount of multi orgasms reached."));
            textField.height = 140f;
            UIElements.Add(textField);
            useMultiOrgasms.CreateUI(UIElements);
            burstMin.CreateUI(UIElements);
            burstMax.CreateUI(UIElements);

            burstMin.slider.wholeNumbers = true;
            burstMax.slider.wholeNumbers = true;
            burstMin.slider.transform.parent.gameObject.SetActive(!useMultiOrgasms.val);
            burstMax.slider.transform.parent.gameObject.SetActive(!useMultiOrgasms.val);
        }
    }
}