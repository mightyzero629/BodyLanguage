using UnityEngine;

namespace CheesyFX
{
    public class WatchTrigger : TimeBasedTrigger
    {
        public override BodyRegionTrigger Init(MVRScript script, TouchZone region)
        {
            base.Init(script, region);
            baseInfo = $"<b>{region.name}:</b>\nTime Watched\n";
            region.watchTrigger = this;
            
            enabledJ.name = $"Enabled ({region.name}W)";
            inputFrom.name = $"Input From ({region.name}W)";
            inputTo.name = $"Input To ({region.name}W)";
            threshold.name = $"Threshold ({region.name}W)";
            decayRate.name = $"Decay Rate ({region.name}W)";
            cap.name = $"Input Cap ({region.name}W)";
            instantReset.name = $"Instant Reset ({region.name}W)";

            threshold.val = threshold.defaultVal = 10f;
            threshold.max = 30f;
            inputTo.val = inputTo.defaultVal = 60f;
            inputTo.max = 120f;
            
            instantReset.setCallbackFunction += val =>
            {
                if (!region.watchListener.isOnStay)
                {
                    if(val)
                    {
                        region.watchListener.watchTimerReset.Stop();
                        region.timeWatched = 0f;
                    }
                }
            };
            reset.actionCallback += () => region.timeWatched = 0f;
            Register();
            return this;
        }
        
        public override void Update()
        {
            onExceeded.Update();
            onUndershot.Update();
            
            // if (condition != null && !condition.IsMet()) return;
            
            if (region.timeWatched > cap.val) region.timeWatched = cap.val;
            Trigger(region.timeWatched);
            if (Mathf.Abs(lastValue - region.timeWatched) > .001f)
            {
                onValueChanged.Update();
            }
            if (panelOpen) info.val = $"{baseInfo}{region.timeWatched:0.00}";
        }
    }
}