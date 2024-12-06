using System;
using UnityEngine;

namespace CheesyFX
{
    public class LerpingBone : MonoBehaviour
    {
        public DAZBone dazBone;
        private Quaternion val;
        public bool isAtTarget = true;
        public bool active;
        public float quicknessIn = 2f;
        public float quicknessOut = 1f;
        public static float updateThreshold = .999f;
        private Quaternion _target;
        public Quaternion target
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

        public LerpingBone Init(DAZBone bone)
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
            if(Quaternion.Dot(val, _target) > .999f)
            {
                baseRotation = _target.eulerAngles;
                enabled = false;
                return;
            }
            if(_target != Quaternion.identity) val = Quaternion.Lerp(val, _target, quicknessIn*Time.deltaTime);
            else val = Quaternion.Lerp(val, _target, quicknessOut*Time.deltaTime);
            if (Quaternion.Dot(val, Quaternion.Euler(baseRotation)) < updateThreshold)
            {
                baseRotation = val.eulerAngles;
            }
            // baseRotation.Print();
        }

        public void Reset()
        {
            val = target = Quaternion.identity;
            baseRotation = Vector3.zero;
            enabled = false;
        }
    }
}