using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class BreathingDriver : MonoBehaviour
    {
        // public float baseLine;
        // public float delta;
        // public float quicknessIn = .4f;
        // public float quicknessOut = 1.2f;
        //
        // public float maxDepth;
        // public float minDepth;
        public float outTime;
        public float period;
        public float outInRatio;

        public float timer;
        public static float intensity;

        private ChestBreather chestBreather;
        private StomachBreather stomachBreather;

        public BreathingDriver Init()
        {
            timer = period;
            chestBreather = new ChestBreather();
            stomachBreather = new StomachBreather();
            SetParameters(0f);
            return this;
        }

        public void SetParameters(float stimulation)
        {
            intensity = Mathf.Lerp(intensity, stimulation, .05f*Time.fixedDeltaTime);
            outInRatio = Mathf.Lerp(.7f, .43f, intensity);
            // baseLine = intensity;
            var lastPeriod = period;
            period = 2.5f * (1f - intensity) * (1f - intensity) + .75f;
            outTime = period * outInRatio;
            timer = timer * period / lastPeriod;
            chestBreather.SetParameters(intensity);
            stomachBreather.SetParameters(intensity);
        }

        private void FixedUpdate()
        {
            if (SuperController.singleton.freezeAnimation)
            {
                Reset();
                return;
            }
            timer -= Time.fixedDeltaTime;
            if (timer > outTime)
            {
                chestBreather.BreathIn();
                stomachBreather.BreathIn();
            }
            else if (timer > 0)
            {
                chestBreather.BreathOut();
                stomachBreather.BreathOut();
            }
            else timer = period;
        }

        private void OnDisable()
        {
            Reset();
        }

        private void OnDestroy()
        {
            Reset();
        }

        public void Reset()
        {
            chestBreather.Reset();
            stomachBreather.Reset();
        }
    }
}