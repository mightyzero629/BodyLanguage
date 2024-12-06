using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheesyFX
{
    public class Breather
    {
        public List<DAZMorph> morphs = new List<DAZMorph>();
        public float baseLine;
        public float delta;
        public float quicknessIn = .4f;
        public float quicknessOut = 1.2f;

        public float maxDepth;
        public float minDepth;

        public virtual void SetParameters(float intensity)
        {
        }

        public void BreathIn()
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                morphs[i].morphValue = Mathf.Lerp(morphs[i].morphValue, maxDepth, Time.fixedDeltaTime*quicknessIn);
            }
        }

        public void BreathOut()
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                morphs[i].morphValue = Mathf.Lerp(morphs[i].morphValue, minDepth, Time.fixedDeltaTime*quicknessOut);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                morphs[i].morphValue = 0f;
            }
        }
    }
}