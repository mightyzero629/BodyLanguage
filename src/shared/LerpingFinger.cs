using System;
using MeshVR.Hands;
using UnityEngine;

namespace CheesyFX
{
    public class LerpingFinger : MonoBehaviour
    {
        // public HandOutput hand;
        private JSONStorableFloat[] jsons = new JSONStorableFloat[5];
        private Vector5 val;
        public bool isAtTarget = true;
        public bool active;
        public float quicknessIn = 2f;
        public float quicknessOut = 1f;
        public static float updateThreshold = .01f;
        private Vector5 _target;
        public Vector5 target
        {
            get { return _target; }
            set
            {
                if ((_target - value).sqrMagnitude < .001f) return;
                _target = value;
                if(!enabled) enabled = true;
            }
        }

        public Vector5 rotation
        {
            get { return new Vector5(jsons[0].val, jsons[1].val, jsons[2].val, jsons[3].val, jsons[4].val); }
            set
            {
                jsons[0].val = value.x;
                jsons[1].val = value.y;
                jsons[2].val = value.z;
                jsons[3].val = value.p;
                jsons[4].val = value.q;
            }
        }

        public LerpingFinger Init(HandOutput hand, int finger)
        {
            if (finger == 0)
            {
                jsons[0] = hand.indexProximalBendJSON;
                jsons[1] = hand.indexMiddleBendJSON;
                jsons[2] = hand.indexDistalBendJSON;
                jsons[3] = hand.indexProximalSpreadJSON;
                jsons[4] = hand.indexProximalTwistJSON;
            }
            if (finger == 1)
            {
                jsons[0] = hand.middleProximalBendJSON;
                jsons[1] = hand.middleMiddleBendJSON;
                jsons[2] = hand.middleDistalBendJSON;
                jsons[3] = hand.middleProximalSpreadJSON;
                jsons[4] = hand.middleProximalTwistJSON;
            }
            if (finger == 2)
            {
                jsons[0] = hand.ringProximalBendJSON;
                jsons[1] = hand.ringMiddleBendJSON;
                jsons[2] = hand.ringDistalBendJSON;
                jsons[3] = hand.ringProximalSpreadJSON;
                jsons[4] = hand.ringProximalTwistJSON;
            }
            else if (finger == 3)
            {
                jsons[0] = hand.pinkyProximalBendJSON;
                jsons[1] = hand.pinkyMiddleBendJSON;
                jsons[2] = hand.pinkyDistalBendJSON;
                jsons[3] = hand.pinkyProximalSpreadJSON;
                jsons[4] = hand.pinkyProximalTwistJSON;
            }
            else if (finger == 4)
            {
                jsons[0] = hand.thumbProximalBendJSON;
                jsons[1] = hand.thumbMiddleBendJSON;
                jsons[2] = hand.thumbDistalBendJSON;
                jsons[3] = hand.thumbProximalSpreadJSON;
                jsons[4] = hand.thumbProximalTwistJSON;
            }

            enabled = false;
            return this;
        }

        public void Update()
        {
            if(Vector5.SqrMagnitude(val - _target) < .001f)
            {
                rotation = _target;
                enabled = false;
                return;
            }
            if(_target != Vector5.zero) val = Vector5.Lerp(val, _target, quicknessIn*Time.deltaTime);
            else val = Vector5.Lerp(val, _target, quicknessOut*Time.deltaTime);
            if (Vector5.SqrMagnitude(val - rotation) > updateThreshold)
            {
                rotation = val;
            }
            // baseRotation.Print();
        }

        public void Reset()
        {
            val = _target = Vector5.zero;
            rotation = Vector5.zero;
            enabled = false;
        }
    }
}