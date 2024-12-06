using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CheesyFX
{
    public class WatchListener
    {
	    private TouchZone touchZone;
	    public IEnumerator watchTimerReset;
	    public bool isOnStay;
	    public WatchListener(TouchZone touchZone)
	    {
		    this.touchZone = touchZone;
	    }
	    
        public void RegisterLookAt(){
        	BodyRegion region = WatchMe.singleton.detailedViewScan.val ? touchZone : touchZone.topParent;
            if (region.numLookAtColliders == 0)
        	{
	            WatchMe.singleton.regionsWatched.Add(region);
	            watchTimerReset.Stop();
	            isOnStay = true;
	            // whileWatched = WhileWatched().Start();
	            // region.parents.ForEach(x => BodyManager.regionsLookedAt.Add(x));
            }
        	region.numLookAtColliders += 1;
        }
        
        public void DeregisterLookAt()
        {
        	BodyRegion region = WatchMe.singleton.detailedViewScan.val ? touchZone : touchZone.topParent;
        	if (region.numLookAtColliders > 0)
        	{
        		region.numLookAtColliders -= 1;
        		if (region.numLookAtColliders == 0)
        		{
	                WatchMe.singleton.regionsWatched.Remove(region);
	                if(touchZone.watchTrigger == null || touchZone.watchTrigger.instantReset.val) touchZone.timeWatched = 0f;
	                else
	                {
		                if(touchZone.watchTrigger.decayRate.val > 0f) watchTimerReset = WatchTimerReset().Start();
	                }
	                isOnStay = false;
	                // region.parents.ForEach(x => BodyManager.regionsLookedAt.Remove(x));
                }
        	}
        }
        
        public IEnumerator WatchTimerReset()
        {
	        while (touchZone.timeWatched > 0f)
	        {
		        touchZone.timeWatched -= touchZone.watchTrigger.decayRate.val * Time.fixedDeltaTime;
		        yield return new WaitForFixedUpdate();
	        }
	        touchZone.timeWatched = 0f;
	        watchTimerReset = null;
        }

        // private IEnumerator WhileWatched()
        // {
	       //  yield return new WaitForEndOfFrame();
	       //  while (touchZone.numLookAtColliders > 0)
	       //  {
		      //   yield return new WaitForEndOfFrame();
		      //   touchZone.timeWatched += Time.deltaTime;
		      //   $"{touchZone.name} {touchZone.timeWatched}".Print();
	       //  }
	       //  
        // }
    }
}