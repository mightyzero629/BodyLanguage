using System;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class TouchTrigger : TimeBasedTrigger
    {
        // public JSONStorableBool instantReset;

        public override BodyRegionTrigger Init(MVRScript script, TouchZone region)
        {
            base.Init(script, region);
            baseInfo = $"<b>{region.name}:</b>\nTime Touched\n";
            this.region = region;
            region.touchTrigger = this;

            enabledJ.name = $"Enabled ({region.name}T)";
            inputFrom.name = $"Input From ({region.name}T)";
            inputTo.name = $"Input To ({region.name}T)";
            threshold.name = $"Threshold ({region.name}T)";
            decayRate.name = $"Decay Rate ({region.name}T)";
            cap.name = $"Input Cap ({region.name}T)";
            instantReset.name = $"Instant Reset ({region.name}T)";
            instantReset.setCallbackFunction += val =>
            {
                if (!region.touchCollisionListener.isOnStay)
                {
                    if(val)
                    {
                        region.touchCollisionListener.touchTimerReset.Stop();
                        region.timeTouched = 0f;
                    }
                }
            };
            reset.actionCallback += () => region.timeTouched = 0f;
            // instantReset = new JSONStorableBool($"Instant Reset ({region.name}T)", false, val =>
            // {
            //     decayRateSlider.SetVisible(!val);
            //     if (!region.touchCollisionListener.isOnStay) region.timeTouched = 0f;
            // });
            Register();
            return this;
        }

        public override void Register()
        {
            base.Register();
            script.RegisterBool(instantReset);
        }

        public override void Update()
        {
            base.Update();
            onExceeded.Update();
            onUndershot.Update();
            
            // if (condition != null && !condition.IsMet()) return;
            if (region.timeTouched > cap.val) region.timeTouched = cap.val;
            Trigger(region.timeTouched);
            if (Mathf.Abs(lastValue - region.timeTouched) > .001f)
            {
                onValueChanged.Update();
                // region.name.Print();
            }
            if (panelOpen) info.val = $"{baseInfo}{region.timeTouched:0.00}";
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
    }
}