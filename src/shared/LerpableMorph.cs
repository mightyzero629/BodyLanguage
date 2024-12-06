using UnityEngine;

namespace CheesyFX
{
    public class LerpableMorph
    	{
    		public DAZMorph dazMorph;
            private float val;
    		public float lipLift;
    		public float lLowerLidLift;
    		public float rLowerLidLift;
    		public float weight;
    		public float max;
            public bool isAtTarget = true;
    		public bool active;
    		public static float quicknessIn = 2f;
    		public static float quicknessOut = .5f;
            public static float updateThreshold = .001f;
	        public string name => dazMorph.displayName;
    		public string uid => dazMorph.uid;
    		private float _target;
    		public float target
    		{
    			get { return _target; }
    			set
    			{
    				if (_target == value) return;
    				_target = value;
    				isAtTarget = false;
    				if (value > 0f) active = true;
    			}
    		}
    
    		public float morphVal
    		{
    			get { return dazMorph.morphValue; }
    			set
    			{
    				dazMorph.morphValue = value;
    			}
    		}
    
    		public LerpableMorph(DAZMorph dazMorph1)
    		{
    			dazMorph = dazMorph1;
                FillMeUp.onMorphsDeactivated.AddListener(() => dazMorph = FillMeUp.morphControl.GetMorphByUid(dazMorph1.uid));
    		}

            // private static int skipped;
            // private static int calls;
            
    		public void LerpToTarget()
    		{
    			if (isAtTarget) return;
                // calls++;
    			if(Mathf.Abs(val - _target) < .0025f)
    			{
    				morphVal = _target;
    				isAtTarget = true;
    				if (_target == 0f) active = false;
    				return;
    			}
    			if(_target > 0) val = Mathf.Lerp(val, _target, quicknessIn*Time.deltaTime);
    			else val = Mathf.Lerp(val, _target, quicknessOut*Time.deltaTime);
                if (Mathf.Abs(val - morphVal) > updateThreshold)
                {
	                morphVal = val;
	                
                }
                // else
                // {
	               //  skipped++;
	               //  
                // }
                // $"calls: {calls} skipped: {skipped}".Print();
            }
    	}
}