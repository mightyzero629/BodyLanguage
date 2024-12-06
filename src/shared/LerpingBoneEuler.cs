using System;
using UnityEngine;

namespace CheesyFX
{
    public class LerpingBoneEuler : MonoBehaviour
    {
        public DAZBone dazBone;
        private Vector3 val;
        public bool isAtTarget = true;
        public bool active;
        public float quicknessIn = 2f;
        public float quicknessOut = 1f;
        public static float updateThreshold = 0f;
        private Vector3 _target;
        public Vector3 target
        {
            get { return _target; }
            set
            {
            	// if ((_target - value).sqrMagnitude < .01f) return;
            	_target = value;
                if(!enabled) enabled = true;
            }
        }

        public Vector3 baseRotation
        {
            get { return dazBone.baseJointRotation; }
            set
            {
            	dazBone.baseJointRotation = value;
            }
        }

        public LerpingBoneEuler Init(DAZBone bone)
        {
            dazBone = bone;
            if (dazBone == null)
            {
                return null;
            }
            return this;
        }

        public void Update()
        {
            if((val - _target).sqrMagnitude < 1f)
            {
            	baseRotation = _target;
                enabled = false;
            	return;
            }
            if(_target != Vector3.zero) val = Vector3.Lerp(val, _target, quicknessIn*Time.deltaTime);
            else val = Vector3.Lerp(val, _target, quicknessOut*Time.deltaTime);
            if ((val - baseRotation).sqrMagnitude > updateThreshold)
            {
                baseRotation = val;
            }
            // baseRotation.Print();
        }

        public void Reset()
        {
            val = baseRotation = target = Vector3.zero;
            enabled = false;
        }
    }
}