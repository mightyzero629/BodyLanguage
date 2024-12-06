using System;
using System.Collections.Generic;
using MacGruber;
using UnityEngine;
using UnityEngine.Events;

namespace CheesyFX
{
    public class ForceSync
    {
        public bool enabled;
        public Force driver;
        public Force target;
        
        private static List<object> UIElements = new List<object>();
        public JSONStorableBool syncAmplitude = new JSONStorableBool("Sync Amplitude", false);
        public JSONStorableBool syncQuickness = new JSONStorableBool("Sync Quickness", false);
        public JSONStorableFloat amplitudeDelta = new JSONStorableFloat("Amplitude Delta", 100f, -1000f, 1000.0f, false);
        public JSONStorableFloat periodFactor = new JSONStorableFloat("Period Factor", 1f, .1f, 5f, false);
        public JSONStorableFloat periodRatioDelta = new JSONStorableFloat("PeriodRatio Delta", 0.1f, 0.0f, 0.5f, false);
        public JSONStorableFloat quicknessDelta = new JSONStorableFloat("Quickness Delta", 3f, 0.0f, 10.0f, false);
        public JSONStorableFloat phaseOffsetMean = new JSONStorableFloat("Phase Offset Mean", 0f, 0f, 1f, false);
        public JSONStorableFloat phaseOffsetDelta = new JSONStorableFloat("Phase Offset Delta", 1f, 0f, 1f, false);
        
        private float phaseOffsetTarget;
        private float phaseOffset;
        private float timer;

        private Action UpdateAmp;
        private Action UpdateQuickness;
        
        public JSONStorableString info = new JSONStorableString("Info", "");
        
        public ForceSync(Force target, Force driver)
        {
            this.target = target;
            this.driver = driver;
            syncAmplitude.AddCallback(val =>
            {
                if (val) UpdateAmp = () => target.amplitude.current = driver.amplitude.current;
                else UpdateAmp = target.amplitude.Update;
            });
            syncQuickness.AddCallback(val =>
            {
                if (val) UpdateQuickness = () => target.quickness.current = driver.quickness.current;
                else UpdateQuickness = target.quickness.Update;
            });
        }
        
        

        public void UpdateParams()
        {
            target.period.current = driver.period.current;
            UpdateAmp();
            UpdateQuickness();
        }

        public void GetPhase()
        {
            timer -= Time.fixedDeltaTime;
            if (timer < 0f)
            {
                phaseOffsetTarget = Mathf.Abs(NormalDistribution.GetValue(phaseOffsetMean.val, phaseOffsetDelta.val, 3f, true)) % 1f;
                timer = 5f;
            }

            phaseOffset = Mathf.Lerp(phaseOffset, phaseOffsetTarget, Time.fixedDeltaTime);
            var flipThreshold = driver.flip == 1f? 
                driver.periodRatio.current*driver.period.current * (1f-phaseOffset) : 
                (1f-driver.periodRatio.current)*driver.period.current * (1f-phaseOffset);
            // flipThreshold *= periodFactor.val;
            if (phaseOffset == 1f || flipThreshold < Time.fixedDeltaTime)
            {
                target.flip = -driver.flip;
                if (!target.applyReturn.val && target.flip < 0f)
                {
                    target.flip = 0f;
                } 
                return;
            }
            // flipThreshold.Print();
            // $"{driver.timer} {flipThreshold}".Print();
            // $"{driver.flip} {target.flip}".Print();
            if (target.flip != driver.flip && driver.timer < flipThreshold)
            {
                target.flip = driver.flip;
                if (!target.applyReturn.val && target.flip < 0f)
                {
                    target.flip = 0f;
                } 
            }

        }
        
        public void CreateUI(Action back)
        {
            FillMeUp.singleton.ClearUI();
            UIDynamicButton button;
            button = FillMeUp.singleton.CreateButton("Return");
            button.buttonColor = new Color(0.55f, 0.90f, 1f);
            button.button.onClick.AddListener(
                () =>
                {
                    Utils.RemoveUIElements(FillMeUp.singleton, UIElements);
                    back();
                    // FillMeUp.singleton.settingsTabbar.SelectTab(1);
                });
            UIElements.Add(button);
            amplitudeDelta.CreateUI(FillMeUp.singleton, UIElements: UIElements);
            periodFactor.CreateUI(FillMeUp.singleton, UIElements: UIElements);
            periodRatioDelta.CreateUI(FillMeUp.singleton, UIElements: UIElements);
            quicknessDelta.CreateUI(FillMeUp.singleton, UIElements: UIElements);
            phaseOffsetMean.CreateUI(FillMeUp.singleton, UIElements: UIElements);
            phaseOffsetDelta.CreateUI(FillMeUp.singleton, UIElements: UIElements);
        }
    }
}