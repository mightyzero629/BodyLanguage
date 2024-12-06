using System;
using UnityEngine;

namespace CheesyFX
{
    public class LerpingMorph : MonoBehaviour
    	{
    		public DAZMorph dazMorph;
            private float val;
            public bool isAtTarget = true;
    		public bool active;
    		public float quicknessIn = 2f;
    		public float quicknessOut = 1f;
            public static float updateThreshold = .0001f;
	        public string name => dazMorph.displayName;
    		public string uid => dazMorph.uid;
    		private float _target;
    		public float target
    		{
    			get { return _target; }
    			set
    			{
    				if (Math.Abs(_target - value) < .1f) return;
    				_target = value;
                    if(!enabled) enabled = true;
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
    
    		public LerpingMorph Init(string uid)
    		{
	            dazMorph = FillMeUp.morphControl.GetMorphByUid(uid);
	            FillMeUp.onMorphsDeactivated.AddListener(() => dazMorph = FillMeUp.morphControl.GetMorphByUid(uid));
	            if (dazMorph == null)
	            {
		            uid.Print();
		            return null;
	            }
                return this;
            }

            public void Update()
    		{
    			if(Mathf.Abs(val - _target) < .0025f)
    			{
    				morphVal = _target;
                    enabled = false;
    				return;
    			}
    			if(_target > 0) val = Mathf.Lerp(val, _target, quicknessIn*Time.deltaTime);
    			else val = Mathf.Lerp(val, _target, quicknessOut*Time.deltaTime);
                if (Mathf.Abs(val - morphVal) > updateThreshold)
                {
	                morphVal = val;
                }
            }

            public void Reset()
            {
	            val = morphVal = target = 0f;
	            enabled = false;
            }
    	}
}